using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;

public class RustRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    private const string AbstractionsNamespaceName = "kiota_abstractions";
    private const string SerializationNamespaceName = "kiota_serialization";
    private const string MultipartBodyClassName = "MultipartBody";

    protected static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsNamespaceName, "RequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsNamespaceName, "Method", "RequestInformation", "RequestOption"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            AbstractionsNamespaceName, "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            AbstractionsNamespaceName, "ParseNode"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            AbstractionsNamespaceName, "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(p => p.IsOfKind(CodePropertyKind.AdditionalData)),
            AbstractionsNamespaceName, "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsNamespaceName, "Parsable"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase),
            AbstractionsNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName),
    };

    public RustRefiner(GenerationConfiguration configuration) : base(configuration) { }

    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var defaultConfiguration = new GenerationConfiguration();

            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                static s => s.ToSnakeCase(),
                false);
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by_{x.ToSnakeCase()}",
                static x => x.ToSnakeCase(),
                GenerationLanguage.Rust);
            CorrectCommonNames(generatedCode);
            var reservedNamesProvider = new RustReservedNamesProvider();
            cancellationToken.ThrowIfCancellationRequested();

            CorrectNames(generatedCode, s =>
            {
                if (s.Contains('_', StringComparison.OrdinalIgnoreCase) &&
                    s.ToPascalCase(UnderscoreArray) is string refinedName &&
                    !reservedNamesProvider.ReservedNames.Contains(s) &&
                    !reservedNamesProvider.ReservedNames.Contains(refinedName))
                    return refinedName;
                return s;
            });

            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            ReplacePropertyNames(generatedCode,
                [
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.QueryParameter,
                    CodePropertyKind.RequestBuilder,
                ],
                static s => s.ToSnakeCase());

            // Rust has no inheritance, so we do NOT call MoveRequestBuilderPropertiesToBaseType.
            // Request builder properties (request_adapter, path_parameters, url_template) stay on the struct.

            RemoveRequestConfigurationClasses(generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                }, new CodeType
                {
                    Name = "DefaultQueryParameters",
                    IsExternal = true,
                });

            MoveInnerClassesToNamespace(generatedCode);

            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            AddConstructorsForDefaultValues(generatedCode, true);
            cancellationToken.ThrowIfCancellationRequested();

            AddDiscriminatorMappingsUsingsToParentClasses(generatedCode, "ParseNode", addUsings: true, includeParentNamespace: true);

            ReplaceReservedNames(generatedCode, reservedNamesProvider, x => $"r#{x}");
            ReplaceReservedExceptionPropertyNames(
                generatedCode,
                new RustExceptionsReservedNamesProvider(),
                static x => $"{x}_escaped"
            );

            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}_json.JsonSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}_json.JsonParseNodeFactory",
                }
            );
            AddSerializationModulesImport(generatedCode,
                [$"{AbstractionsNamespaceName}.ApiClientBuilder",
                    $"{AbstractionsNamespaceName}.SerializationWriterFactoryRegistry"],
                [$"{AbstractionsNamespaceName}.ParseNodeFactoryRegistry"]);
            cancellationToken.ThrowIfCancellationRequested();

            AddParentClassToErrorClasses(
                generatedCode,
                "ApiError",
                AbstractionsNamespaceName
            );
            DeduplicateErrorMappings(generatedCode);
            RemoveCancellationParameter(generatedCode);
            DisambiguatePropertiesWithClassNames(generatedCode);
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlBuilder);
        }, cancellationToken);
    }

    private static void CorrectCommonNames(CodeElement currentElement)
    {
        if (currentElement is CodeMethod m &&
            currentElement.Parent is CodeClass parentClass)
        {
            // Rust uses snake_case for method names
            var snakeName = m.Name.ToSnakeCase();
            if (!snakeName.Equals(m.Name, StringComparison.Ordinal))
                parentClass.RenameChildElement(m.Name, snakeName);
            // Rust uses PascalCase for type names
            parentClass.Name = parentClass.Name.ToFirstCharacterUpperCase();
        }
        else if (currentElement is CodeIndexer i)
        {
            i.IndexParameter.Name = i.IndexParameter.Name.ToSnakeCase();
        }
        else if (currentElement is CodeEnum e)
        {
            // Rust enum variants are PascalCase
            foreach (var option in e.Options.ToList())
            {
                option.Name = option.Name.ToFirstCharacterUpperCase();
            }
        }
        CrawlTree(currentElement, element => CorrectCommonNames(element));
    }

    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer)).ToList().ForEach(x =>
            {
                x.Optional = false;
                x.Type.IsNullable = false;
                if (x.Type.Name.StartsWith('I'))
                    x.Type.Name = x.Type.Name[1..];
            });
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = "HashMap<String, Box<dyn Fn(&dyn ParseNode, &mut Self)>>";
            currentMethod.Name = "get_field_deserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.RawUrlConstructor, CodeMethodKind.ClientConstructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]);
        }
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, types: currentMethod.Parameters
            .Select(static x => x.Type)
            .Union(new[] { currentMethod.ReturnType })
            .ToArray());
        currentMethod.Parameters.ToList().ForEach(static x => x.Name = x.Name.ToSnakeCase());
    }

    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        ArgumentNullException.ThrowIfNull(currentProperty);

        if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.DefaultValue = "Vec::new()";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.DefaultValue = "RequestHeaders::new()";
        else if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
        {
            currentProperty.Type.Name = "std::sync::Arc<dyn RequestAdapter + Send + Sync>";
            currentProperty.Type.IsNullable = false;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
        {
            currentProperty.Type.Name = currentProperty.Type.Name.TrimStart('I');
            currentProperty.Name = currentProperty.Name.ToSnakeCase();
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "HashMap<String, serde_json::Value>";
            currentProperty.DefaultValue = "HashMap::new()";
            currentProperty.Name = currentProperty.Name.ToSnakeCase();
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "String";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "HashMap<String, String>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "HashMap::new()";
        }
        else
        {
            if (!currentProperty.IsNameEscaped)
                currentProperty.SerializationName = currentProperty.Name;
            currentProperty.Name = currentProperty.Name.ToSnakeCase();
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, types: currentProperty.Type);
    }

    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x =>
            x.Name.StartsWith('I') && (
                x.Name.Equals("IAdditionalDataHolder", StringComparison.OrdinalIgnoreCase) ||
                x.Name.Equals("IBackedModel", StringComparison.OrdinalIgnoreCase)
            )).ToList().ForEach(x => x.Name = x.Name[1..]);
    }

    protected static void DisambiguatePropertiesWithClassNames(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            var sameNameProperty = currentClass.Properties
                .FirstOrDefault(x => x.Name.Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase));
            if (sameNameProperty != null)
            {
                currentClass.RemoveChildElement(sameNameProperty);
                if (string.IsNullOrEmpty(sameNameProperty.SerializationName))
                    sameNameProperty.SerializationName = sameNameProperty.Name;
                sameNameProperty.Name = $"{sameNameProperty.Name}_prop";
                currentClass.AddProperty(sameNameProperty);
            }
        }
        CrawlTree(currentElement, DisambiguatePropertiesWithClassNames);
    }

    private static void MoveInnerClassesToNamespace(CodeElement currentElement)
    {
        if (currentElement is CodeClass parentClass)
        {
            var innerClasses = parentClass.InnerClasses.ToArray();
            if (innerClasses.Length > 0 && parentClass.Parent is CodeNamespace ns)
            {
                foreach (var innerClass in innerClasses)
                {
                    parentClass.RemoveChildElement(innerClass);
                    ns.AddClass(innerClass);
                    // Add a using so the parent class can still reference the moved type
                    parentClass.AddUsing(new CodeUsing
                    {
                        Name = innerClass.Name,
                        Declaration = new CodeType
                        {
                            Name = innerClass.Name,
                            TypeDefinition = innerClass,
                            IsExternal = false,
                        }
                    });
                }
            }
        }
        CrawlTree(currentElement, MoveInnerClassesToNamespace);
    }

    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
        {"TimeSpan", ("chrono::Duration", new CodeUsing {
                            Name = "chrono",
                            Declaration = new CodeType {
                                Name = "chrono",
                                IsExternal = true,
                            },
                        })},
        {"DateTimeOffset", ("chrono::DateTime<chrono::Utc>", new CodeUsing {
                            Name = "chrono",
                            Declaration = new CodeType {
                                Name = "chrono",
                                IsExternal = true,
                            },
                        })},
        {"DateOnly", ("chrono::NaiveDate", new CodeUsing {
                            Name = "chrono",
                            Declaration = new CodeType {
                                Name = "chrono",
                                IsExternal = true,
                            },
                        })},
        {"TimeOnly", ("chrono::NaiveTime", new CodeUsing {
                            Name = "chrono",
                            Declaration = new CodeType {
                                Name = "chrono",
                                IsExternal = true,
                            },
                        })},
        {"Guid", ("uuid::Uuid", new CodeUsing {
                            Name = "uuid",
                            Declaration = new CodeType {
                                Name = "uuid",
                                IsExternal = true,
                            },
                        })},
    };
}
