using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Dart;


namespace Kiota.Builder.Refiners;

public class DartRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    private const string MultipartBodyClassName = "MultipartBody";
    private const string AbstractionsNamespaceName = "microsoft_kiota_abstractions/microsoft_kiota_abstractions";
    private const string SerializationNamespaceName = "microsoft_kiota_serialization";
    private static readonly CodeUsingDeclarationNameComparer usingComparer = new();

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
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            AbstractionsNamespaceName, "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsNamespaceName, "Parsable"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.QueryParameter) && !string.IsNullOrEmpty(prop.SerializationName),
            AbstractionsNamespaceName, "QueryParameterAttribute"),
        new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal),
            AbstractionsNamespaceName, "ParseNodeHelper"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Headers),
            AbstractionsNamespaceName, "RequestHeaders"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase),
            AbstractionsNamespaceName, KiotaBuilder.UntypedNodeName),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            AbstractionsNamespaceName, MultipartBodyClassName),
    };


    public DartRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var defaultConfiguration = new GenerationConfiguration();

            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                static s => s.ToFirstCharacterLowerCase(),
                false);
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by{x.ToPascalCase('_')}",
                static x => x.ToCamelCase('_'),
                GenerationLanguage.Dart);
            CorrectCommonNames(generatedCode);
            var reservedNamesProvider = new DartReservedNamesProvider();
            cancellationToken.ThrowIfCancellationRequested();
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
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            ReplacePropertyNames(generatedCode,
                [
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.QueryParameter,
                    CodePropertyKind.RequestBuilder,
                ],
                static s => s.ToCamelCase(UnderscoreArray));

            AddQueryParameterExtractorMethod(generatedCode);
            // This adds the BaseRequestBuilder class as a superclass
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = AbstractionsNamespaceName,
                        IsExternal = true,
                    }
                }, addCurrentTypeAsGenericTypeParameter: true);
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
            MoveQueryParameterClass(generatedCode);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true, codeTypeFilter);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            AddConstructorsForDefaultValues(generatedCode, true);
            AddConstructorForErrorClass(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            AddAsyncSuffix(generatedCode);
            AddDiscriminatorMappingsUsingsToParentClasses(generatedCode, "ParseNode", addUsings: true, includeParentNamespace: true);

            ReplaceReservedNames(generatedCode, reservedNamesProvider, x => $"{x}_");
            ReplaceReservedModelTypes(generatedCode, reservedNamesProvider, x => $"{x}Object");
            ReplaceReservedExceptionPropertyNames(
                generatedCode,
                new DartExceptionsReservedNamesProvider(),
                static x => $"{x.ToFirstCharacterLowerCase()}_"
            );

            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}_json/{SerializationNamespaceName}_json.JsonSerializationWriterFactory",
                    $"{SerializationNamespaceName}_text/{SerializationNamespaceName}_text.TextSerializationWriterFactory",
                    $"{SerializationNamespaceName}_form/{SerializationNamespaceName}_form.FormSerializationWriterFactory",
                    $"{SerializationNamespaceName}_multipart/{SerializationNamespaceName}_multipart.MultipartSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    $"{SerializationNamespaceName}_json/{SerializationNamespaceName}_json.JsonParseNodeFactory",
                    $"{SerializationNamespaceName}_form/{SerializationNamespaceName}_form.FormParseNodeFactory",
                    $"{SerializationNamespaceName}_text/{SerializationNamespaceName}_text.TextParseNodeFactory"
                }
            );
            AddSerializationModulesImport(generatedCode,
                [$"{AbstractionsNamespaceName}.ApiClientBuilder",
                    $"{AbstractionsNamespaceName}.SerializationWriterFactoryRegistry"],
                [$"{AbstractionsNamespaceName}.ParseNodeFactoryRegistry"]);
            cancellationToken.ThrowIfCancellationRequested();

            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiException",
                    AbstractionsNamespaceName
            );
            DeduplicateErrorMappings(generatedCode);
            RemoveCancellationParameter(generatedCode);
            DisambiguatePropertiesWithClassNames(generatedCode);
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlBuilder);
            AddCustomMethods(generatedCode);
            EscapeStringValues(generatedCode);
            AliasUsingWithSameSymbol(generatedCode);
        }, cancellationToken);
    }

    ///error classes should always have a constructor for the copyWith method
    private void AddConstructorForErrorClass(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass && codeClass.IsErrorDefinition)
        {
            // Add parameterless constructor if not already present
            if (!codeClass.Methods.Any(x => x.IsOfKind(CodeMethodKind.Constructor) && !x.Parameters.Any()))
            {
                var parameterlessConstructor = CreateConstructor(codeClass, "Instantiates a new {TypeName} and sets the default values.");
                codeClass.AddMethod(parameterlessConstructor);
            }
            var messageParameter = CreateErrorMessageParameter("String");
            // Add message constructor if not already present
            if (!codeClass.Methods.Any(x => x.IsOfKind(CodeMethodKind.Constructor) && x.Parameters.Any(p => p.IsOfKind(CodeParameterKind.ErrorMessage))))
            {
                var messageConstructor = CreateConstructor(codeClass, "Instantiates a new {TypeName} with the specified error message.");
                messageConstructor.AddParameter(messageParameter);
                codeClass.AddMethod(messageConstructor);
            }

            TryAddErrorMessageFactoryMethod(
               codeClass,
               methodName: "createFromDiscriminatorValueWithMessage",
               parseNodeTypeName: "ParseNode",
               messageParameter: messageParameter,
               setParent: false);
        }
        CrawlTree(currentElement, element => AddConstructorForErrorClass(element));
    }

    private static CodeMethod CreateConstructor(CodeClass codeClass, string descriptionTemplate)
    {
        return new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            IsAsync = false,
            IsStatic = false,
            Documentation = new(new() {
                {"TypeName", new CodeType {
                    IsExternal = false,
                    TypeDefinition = codeClass,
                }}
            })
            {
                DescriptionTemplate = descriptionTemplate,
            },
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "void", IsExternal = true },
            Parent = codeClass,
        };
    }

    /// <summary> 
    /// Corrects common names so they can be used with Dart.
    /// This normally comes down to changing the first character to lower case.
    /// <example><code>GetFieldDeserializers</code> is corrected to <code>getFieldDeserializers</code>
    /// </summary>
    private static void CorrectCommonNames(CodeElement currentElement)
    {
        if (currentElement is CodeMethod m &&
            currentElement.Parent is CodeClass parentClass)
        {
            parentClass.RenameChildElement(m.Name, m.Name.ToFirstCharacterLowerCase());
            parentClass.Name = parentClass.Name.ToFirstCharacterUpperCase();
        }
        else if (currentElement is CodeIndexer i)
        {
            i.IndexParameter.Name = i.IndexParameter.Name.ToFirstCharacterLowerCase();
        }
        else if (currentElement is CodeEnum e)
        {
            var options = e.Options.ToList();
            foreach (var option in options)
            {
                option.Name = DartConventionService.getCorrectedEnumName(option.Name);
                option.SerializationName = option.SerializationName.Replace("'", "\\'", StringComparison.OrdinalIgnoreCase);
            }
            ///ensure enum options with the same corrected name get a unique name
            var nameGroups = options.Select((Option, index) => new { Option, index }).GroupBy(s => s.Option.Name).ToList();
            foreach (var group in nameGroups.Where(g => g.Count() > 1))
            {
                foreach (var entry in group.Skip(1).Select((g, i) => new { g, i }))
                {
                    options[entry.g.index].Name = options[entry.g.index].Name + entry.i;
                }
            }
        }
        else if (currentElement is CodeProperty p && p.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum && !string.IsNullOrEmpty(p.DefaultValue))
        {
            p.DefaultValue = DartConventionService.getCorrectedEnumName(p.DefaultValue.Trim('"').CleanupSymbolName());
            if (new DartReservedNamesProvider().ReservedNames.Contains(p.DefaultValue))
            {
                p.DefaultValue += "_";
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
                x.Type.IsNullable = true;
                if (x.Type.Name.StartsWith('I'))
                    x.Type.Name = x.Type.Name[1..];
            });
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = "Map<String, void Function(ParseNode)>";
            currentMethod.Name = "getFieldDeserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.RawUrlConstructor, CodeMethodKind.ClientConstructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        }
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, types: currentMethod.Parameters
                                                .Select(static x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
                                                .ToArray());
        currentMethod.Parameters.ToList().ForEach(static x => x.Name = x.Name.ToFirstCharacterLowerCase());
    }

    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        ArgumentNullException.ThrowIfNull(currentProperty);

        if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.DefaultValue = "List<RequestOption>()";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.DefaultValue = $"{currentProperty.Type.Name.ToFirstCharacterLowerCase()}()";
        else if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
        {
            currentProperty.Type.Name = "RequestAdapter";
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
        {
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            currentProperty.SerializationName = currentProperty.Name;
            currentProperty.Name = currentProperty.Name.ToFirstCharacterLowerCase();
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameter))
        {
            currentProperty.DefaultValue = $"{currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "Map<String, Object?>";
            currentProperty.DefaultValue = "{}";
            currentProperty.Name = currentProperty.Name.ToFirstCharacterLowerCase();
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.UrlTemplate))
        {
            currentProperty.Type.IsNullable = true;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = true;
            currentProperty.Type.Name = "Map<String, dynamic>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        }
        else
        {
            if (!currentProperty.IsNameEscaped)
                currentProperty.SerializationName = currentProperty.Name;

            currentProperty.Name = currentProperty.Name.ToFirstCharacterLowerCase();
        }
        currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, types: currentProperty.Type);
    }

    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase) || "IBackedModel".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    public static IEnumerable<CodeTypeBase> codeTypeFilter(IEnumerable<CodeTypeBase> usingsToAdd)
    {
        var result = usingsToAdd.OfType<CodeType>().Except(usingsToAdd.Where(static codeType => codeType.Parent is ClassDeclaration declaration && declaration.Parent is CodeClass codeClass && codeClass.IsErrorDefinition));
        var genericParameterTypes = usingsToAdd.OfType<CodeType>().Where(
            static codeType => codeType.Parent is CodeParameter parameter
            && parameter.IsOfKind(CodeParameterKind.RequestConfiguration)).Select(x => x.GenericTypeParameterValues.First());

        return result.Union(genericParameterTypes);
    }
    protected static void AddAsyncSuffix(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
            currentMethod.Name += "Async";
        CrawlTree(currentElement, AddAsyncSuffix);
    }
    private void AddQueryParameterExtractorMethod(CodeElement currentElement, string methodName = "toMap")
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.QueryParameters))
        {
            currentClass.StartBlock.AddImplements(new CodeType
            {
                IsExternal = true,
                Name = "AbstractQueryParameters"
            });
            currentClass.AddMethod(new CodeMethod
            {
                Name = methodName,
                Access = AccessModifier.Public,
                ReturnType = new CodeType
                {
                    Name = "Map<String, dynamic>",
                    IsNullable = false,
                },
                IsAsync = false,
                IsStatic = false,
                Kind = CodeMethodKind.QueryParametersMapper,
                Documentation = new()
                {
                    DescriptionTemplate = "Extracts the query parameters into a map for the URI template parsing.",
                },
            });
            currentClass.AddUsing(new CodeUsing
            {
                Name = "AbstractQueryParameters",
                Declaration = new CodeType { Name = AbstractionsNamespaceName, IsExternal = true },
            });
        }
        CrawlTree(currentElement, x => AddQueryParameterExtractorMethod(x, methodName));
    }

    private void MoveQueryParameterClass(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var parentNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
            var nestedClasses = currentClass.InnerClasses.Where(x => x.IsOfKind(CodeClassKind.QueryParameters));
            foreach (CodeClass nestedClass in nestedClasses)
            {
                parentNamespace.AddClass(nestedClass);
                currentClass.RemoveChildElementByName(nestedClass.Name);
            }
        }
        CrawlTree(currentElement, x => MoveQueryParameterClass(x));
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
                sameNameProperty.Name = $"{sameNameProperty.Name}Prop";
                currentClass.AddProperty(sameNameProperty);
            }
        }
        CrawlTree(currentElement, DisambiguatePropertiesWithClassNames);
    }
    private void AddCustomMethods(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            if (currentClass.IsOfKind(CodeClassKind.RequestBuilder))
            {
                currentClass.AddMethod(new CodeMethod
                {
                    Name = "clone",
                    Access = AccessModifier.Public,
                    ReturnType = new CodeType
                    {
                        Name = currentClass.Name,
                        IsNullable = false,
                    },
                    IsAsync = false,
                    IsStatic = false,

                    Kind = CodeMethodKind.Custom,
                    Documentation = new()
                    {
                        DescriptionTemplate = "Clones the requestbuilder.",
                    },
                });
            }
            if (currentClass.IsOfKind(CodeClassKind.Model) && currentClass.IsErrorDefinition)
            {
                currentClass.AddMethod(new CodeMethod
                {
                    Name = "copyWith",
                    Access = AccessModifier.Public,
                    ReturnType = new CodeType
                    {
                        Name = currentClass.Name,
                        IsNullable = false,
                    },
                    IsAsync = false,
                    IsStatic = false,

                    Kind = CodeMethodKind.Custom,
                    Documentation = new()
                    {
                        DescriptionTemplate = "Creates a copy of the object.",
                    },
                });
            }
        }
        CrawlTree(currentElement, x => AddCustomMethods(x));
    }

    private void EscapeStringValues(CodeElement currentElement)
    {
        if (currentElement is CodeProperty property)
        {
            if (!String.IsNullOrEmpty(property.SerializationName) && property.SerializationName.Contains('$', StringComparison.Ordinal))
            {
                property.SerializationName = property.SerializationName.Replace("$", "\\$", StringComparison.Ordinal);
            }
            if (property.DefaultValue.Contains('$', StringComparison.Ordinal))
            {
                property.DefaultValue = property.DefaultValue.Replace("$", "\\$", StringComparison.Ordinal);
            }
        }
        else if (currentElement is CodeMethod method && method.HasUrlTemplateOverride)
        {
            method.UrlTemplateOverride = method.UrlTemplateOverride.Replace("$", "\\$", StringComparison.Ordinal);
        }
        CrawlTree(currentElement, EscapeStringValues);
    }

    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {

    {"TimeSpan", ("Duration", null)},
    {"DateTimeOffset", ("DateTime", null)},
    {"Guid", ("UuidValue", new CodeUsing {
                            Name = "UuidValue",
                            Declaration = new CodeType {
                                Name = "uuid/uuid",
                                IsExternal = true,
                            },
                        })},
    };
    private static void AliasUsingWithSameSymbol(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.StartBlock != null && currentClass.StartBlock.Usings.Any(x => !x.IsExternal))
        {
            var duplicatedSymbolsUsings = currentClass.StartBlock.Usings
                .Distinct(usingComparer)
                .Where(static x => !string.IsNullOrEmpty(x.Declaration?.Name) && x.Declaration.TypeDefinition != null)
                .GroupBy(static x => x.Declaration!.Name, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .Union(currentClass.StartBlock
                    .Usings
                    .Where(x => !x.IsExternal)
                    .Where(x => x.Declaration!
                        .Name
                        .Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase)));
            foreach (var usingElement in duplicatedSymbolsUsings)
            {
                var replacement = string.Join("_", usingElement.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>().Name
                    .Split(".", StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.ToLowerInvariant())
                    .ToArray());
                usingElement.Alias = $"{(string.IsNullOrEmpty(replacement) ? string.Empty : $"{replacement}")}_{usingElement.Declaration!.TypeDefinition!.Name.ToLowerInvariant()}";
            }
        }
        CrawlTree(currentElement, AliasUsingWithSameSymbol);
    }
}
