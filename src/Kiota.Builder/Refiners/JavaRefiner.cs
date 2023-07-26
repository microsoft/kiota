using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Java;

namespace Kiota.Builder.Refiners;
public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public JavaRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                });
            RemoveRequestConfigurationClassesCommonProperties(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestConfiguration",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true
                    }
                });
            var reservedNamesProvider = new JavaReservedNamesProvider();
            CorrectNames(generatedCode, s =>
            {
                if (s.Contains('_', StringComparison.OrdinalIgnoreCase) &&
                     s.ToPascalCase(UnderscoreArray) is string refinedName &&
                    !reservedNamesProvider.ReservedNames.Contains(s) &&
                    !reservedNamesProvider.ReservedNames.Contains(refinedName))
                    return refinedName;
                else
                    return s;
            });
            RemoveClassNamePrefixFromNestedClasses(generatedCode);
            InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                true,
                static x => $"By{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterLowerCase());
            cancellationToken.ThrowIfCancellationRequested();
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                true,
                SerializationNamespaceName,
                "ComposedTypeWrapper"
            );
            AddRawUrlConstructorOverload(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceBinaryByNativeType(generatedCode, "InputStream", "java.io", true, true);
            ReplacePropertyNames(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                },
                static s => s.ToCamelCase(UnderscoreArray));
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.BackingStore,
                },
                static (_, s) => s.ToCamelCase(UnderscoreArray),
                _configuration.UsesBackingStore,
                true,
                "get",
                "set",
                string.Empty
            );
            ReplaceReservedNames(generatedCode, reservedNamesProvider, x => $"{x}Escaped", new HashSet<Type> { typeof(CodeEnumOption) });
            ReplaceReservedExceptionPropertyNames(generatedCode, new JavaExceptionsReservedNamesProvider(), x => $"{x}Escaped");
            LowerCaseNamespaceNames(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            cancellationToken.ThrowIfCancellationRequested();
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            AddEnumSetImport(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            SetSetterParametersToNullable(generatedCode, new Tuple<CodeMethodKind, CodePropertyKind>(CodeMethodKind.Setter, CodePropertyKind.AdditionalData));
            AddConstructorsForDefaultValues(generatedCode, true);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
            var defaultConfiguration = new GenerationConfiguration();
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}.JsonSerializationWriterFactory",
                    $"{SerializationNamespaceName}.TextSerializationWriterFactory",
                    $"{SerializationNamespaceName}.FormSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}.JsonParseNodeFactory",
                    $"{SerializationNamespaceName}.FormParseNodeFactory",
                    $"{SerializationNamespaceName}.TextParseNodeFactory"
                }
            );
            AddSerializationModulesImport(generatedCode,
                                        new[] { $"{AbstractionsNamespaceName}.ApiClientBuilder",
                                                $"{SerializationNamespaceName}.SerializationWriterFactoryRegistry" },
                                        new[] { $"{SerializationNamespaceName}.ParseNodeFactoryRegistry" });
            cancellationToken.ThrowIfCancellationRequested();
            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiException",
                    AbstractionsNamespaceName
            );
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: true
            );
            RemoveHandlerFromRequestBuilder(generatedCode);
            SplitLongDiscriminatorMethods(generatedCode);
        }, cancellationToken);
    }
    private const int MaxDiscriminatorLength = 500;
    private static void SplitLongDiscriminatorMethods(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod &&
            !currentMethod.IsOverload &&
            currentMethod.IsOfKind(CodeMethodKind.Factory) &&
            currentMethod.Parent is CodeClass parentClass &&
            parentClass.IsOfKind(CodeClassKind.Model) &&
            parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation &&
            parentClass.DiscriminatorInformation.DiscriminatorMappings.Count() > MaxDiscriminatorLength)
        {
            var discriminatorsCount = parentClass.DiscriminatorInformation.DiscriminatorMappings.Count();
            for (var currentDiscriminatorPageIndex = 0; currentDiscriminatorPageIndex * MaxDiscriminatorLength < discriminatorsCount; currentDiscriminatorPageIndex++)
            {
                var newMethod = (CodeMethod)currentMethod.Clone();
                newMethod.Name = $"{currentMethod.Name}_{currentDiscriminatorPageIndex}";
                newMethod.OriginalMethod = currentMethod;
                newMethod.Access = AccessModifier.Private;
                newMethod.RemoveParametersByKind(CodeParameterKind.ParseNode);
                newMethod.AddParameter(new CodeParameter
                {
                    Type = new CodeType
                    {
                        Name = "String",
                        IsNullable = true,
                        IsExternal = true
                    },
                    Optional = false,
                    Documentation = new()
                    {
                        Description = "Discriminator value from the payload",
                    },
                    Name = "discriminatorValue"
                });
                parentClass.AddMethod(newMethod);
            }
        }
        CrawlTree(currentElement, SplitLongDiscriminatorMethods);
    }
    private static void SetSetterParametersToNullable(CodeElement currentElement, params Tuple<CodeMethodKind, CodePropertyKind>[] accessorPairs)
    {
        if (currentElement is CodeMethod method &&
            accessorPairs.Any(x => method.IsOfKind(x.Item1) && (method.AccessedProperty?.IsOfKind(x.Item2) ?? false)))
            foreach (var param in method.Parameters)
                param.Type.IsNullable = true;
        CrawlTree(currentElement, element => SetSetterParametersToNullable(element, accessorPairs));
    }
    private static void AddEnumSetImport(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) &&
            currentClass.Properties.Any(x => x.Type is CodeType xType && xType.TypeDefinition is CodeEnum xEnumType && xEnumType.Flags))
        {
            var nUsing = new CodeUsing
            {
                Name = "EnumSet",
                Declaration = new CodeType { Name = "java.util", IsExternal = true },
            };
            currentClass.AddUsing(nUsing);
        }

        CrawlTree(currentElement, AddEnumSetImport);
    }
    private static readonly JavaConventionService conventionService = new();
    private const string AbstractionsNamespaceName = "com.microsoft.kiota";
    private const string SerializationNamespaceName = "com.microsoft.kiota.serialization";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsNamespaceName, "RequestAdapter"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.PathParameters),
            "java.util", "HashMap"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsNamespaceName, "RequestInformation", "RequestOption", "HttpMethod"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "java.net", "URISyntaxException"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "java.util", "Collection", "Map"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            SerializationNamespaceName, "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            SerializationNamespaceName, "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.Parameters.Any(x => !x.Optional),
                "java.util", "Objects"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor) &&
                    method.Parameters.Any(x => x.IsOfKind(CodeParameterKind.RequestBody) &&
                                        x.Type.Name.Equals(conventionService.StreamTypeName, StringComparison.OrdinalIgnoreCase)),
            "java.io", "InputStream"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            SerializationNamespaceName, "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            SerializationNamespaceName, "ParseNode"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            SerializationNamespaceName, "Parsable", "ParsableFactory"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            "java.util", "HashMap", "Map"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "com.microsoft.kiota.store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            "com.microsoft.kiota.store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton"),
        new (static x => x is CodeProperty prop && "decimal".Equals(prop.Type.Name, StringComparison.OrdinalIgnoreCase) ||
                x is CodeMethod method && "decimal".Equals(method.ReturnType.Name, StringComparison.OrdinalIgnoreCase) ||
                x is CodeParameter para && "decimal".Equals(para.Type.Name, StringComparison.OrdinalIgnoreCase),
            "java.math", "BigDecimal"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.QueryParameter) && !string.IsNullOrEmpty(prop.SerializationName),
                AbstractionsNamespaceName, "QueryParameter"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal),
            SerializationNamespaceName, "ParseNodeHelper"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName)
    };
    private const string MultipartBodyClassName = "MultipartBody";
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
        {
            currentProperty.Type.Name = "RequestAdapter";
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
        else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameter))
            currentProperty.DefaultValue = $"new {currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "Map<String, Object>";
            currentProperty.DefaultValue = "new HashMap<>()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = true;
            currentProperty.Type.Name = "HashMap<String, Object>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "new HashMap<>()";
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer)).ToList().ForEach(x =>
            {
                x.Optional = false;
                x.Type.IsNullable = true;
                if (x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase))
                    x.Type.Name = x.Type.Name[1..];
            });
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = "Map<String, java.util.function.Consumer<ParseNode>>";
            currentMethod.Name = "getFieldDeserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters))
                .ToList()
                .ForEach(x => x.Type.IsNullable = true);
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if (urlTplParams != null)
                urlTplParams.Type.Name = "HashMap<String, Object>";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Factory) && currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(static x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("OffsetDateTime", new CodeUsing {
                                    Name = "OffsetDateTime",
                                    Declaration = new CodeType {
                                        Name = "java.time",
                                        IsExternal = true,
                                    },
                                })},
    {"TimeSpan", ("PeriodAndDuration", new CodeUsing {
                                    Name = "PeriodAndDuration",
                                    Declaration = new CodeType {
                                        Name = AbstractionsNamespaceName,
                                        IsExternal = true,
                                    },
                                })},
    {"DateOnly", ("LocalDate", new CodeUsing {
                            Name = "LocalDate",
                            Declaration = new CodeType {
                                Name = "java.time",
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", ("LocalTime", new CodeUsing {
                            Name = "LocalTime",
                            Declaration = new CodeType {
                                Name = "java.time",
                                IsExternal = true,
                            },
                        })},
    {"Guid", ("UUID", new CodeUsing {
                            Name = "UUID",
                            Declaration = new CodeType {
                                Name = "java.util",
                                IsExternal = true,
                            },
                        })},
    };
    private void InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            var codeMethods = currentClass.Methods;
            if (codeMethods.Any())
            {
                var originalExecutorMethods = codeMethods.Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor)).ToArray();
                var executorMethodsToAdd = originalExecutorMethods
                                    .Union(originalExecutorMethods
                                            .Select(static x => GetMethodClone(x, CodeParameterKind.RequestConfiguration)))
                                    .OfType<CodeMethod>()
                                    .ToArray();
                var originalGeneratorMethods = codeMethods.Where(static x => x.IsOfKind(CodeMethodKind.RequestGenerator)).ToArray();
                var generatorMethodsToAdd = originalGeneratorMethods
                                    .Select(static x => GetMethodClone(x, CodeParameterKind.RequestConfiguration))
                                    .OfType<CodeMethod>()
                                    .ToArray();
                if (executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                    currentClass.AddMethod(executorMethodsToAdd
                                            .Union(generatorMethodsToAdd)
                                            .ToArray());
            }
        }

        CrawlTree(currentElement, InsertOverrideMethodForRequestExecutorsAndBuildersAndConstructors);
    }
    private static void RemoveClassNamePrefixFromNestedClasses(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var prefix = currentClass.Name;
            var requestConfigClasses = currentClass
                                    .Methods
                                    .SelectMany(static x => x.Parameters)
                                    .Where(static x => x.Type.ActionOf && x.IsOfKind(CodeParameterKind.RequestConfiguration))
                                    .SelectMany(static x => x.Type.AllTypes)
                                    .Select(static x => x.TypeDefinition)
                                    .OfType<CodeClass>()
                                    .ToArray();
            // ensure we do not miss out the types present in request configuration objects i.e. the query parameters
            var innerClasses = requestConfigClasses
                                    .SelectMany(static x => x.Properties)
                                    .Where(static x => x.IsOfKind(CodePropertyKind.QueryParameters))
                                    .SelectMany(static x => x.Type.AllTypes)
                                    .Select(static x => x.TypeDefinition)
                                    .OfType<CodeClass>().Union(requestConfigClasses)
                                    .Where(x => x.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var innerClass in innerClasses)
            {
                innerClass.Name = innerClass.Name[prefix.Length..];

                if (innerClass.IsOfKind(CodeClassKind.RequestConfiguration))
                    RemovePrefixFromQueryProperties(innerClass, prefix);
            }
            RemovePrefixFromRequestConfigParameters(currentClass, prefix);
        }
        CrawlTree(currentElement, x => RemoveClassNamePrefixFromNestedClasses(x));
    }
    private static void RemovePrefixFromQueryProperties(CodeElement currentElement, String prefix)
    {
        if (currentElement is CodeClass currentClass)
        {
            var queryProperty = currentClass
                                .Properties
                                .Where(static x => x.IsOfKind(CodePropertyKind.QueryParameters))
                                .Select(static x => x.Type)
                                .OfType<CodeTypeBase>()
                                .Where(x => x.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var property in queryProperty)
            {
                property.Name = property.Name[prefix.Length..];
            }
        }
    }
    private static void RemovePrefixFromRequestConfigParameters(CodeElement currentElement, String prefix)
    {
        if (currentElement is CodeClass currentClass)
        {
            var parameters = currentClass
                                .Methods
                                .SelectMany(static x => x.Parameters)
                                .Where(static x => x.Type.ActionOf && x.IsOfKind(CodeParameterKind.RequestConfiguration))
                                .Select(static x => x.Type)
                                .OfType<CodeTypeBase>()
                                .Where(x => x.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var parameter in parameters)
            {
                parameter.Name = parameter.Name[prefix.Length..];
            }
        }
    }
    private static void LowerCaseNamespaceNames(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace codeNamespace)
        {
            if (!string.IsNullOrEmpty(codeNamespace.Name))
                codeNamespace.Name = codeNamespace.Name.ToLowerInvariant();

            CrawlTree(currentElement, LowerCaseNamespaceNames);
        }
    }
}
