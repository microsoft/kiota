using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;

public class PythonRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public PythonRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeduplicateErrorMappings(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore,
                static s => s,
                false,
                $"{SerializationModuleName}",
                "ComposedTypeWrapper"
            );
            CorrectCommonNames(generatedCode);
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlConstructor);
            RemoveUntypedNodeTypeValues(generatedCode);
            DisableActionOf(generatedCode,
            CodeParameterKind.RequestConfiguration);
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
                new CodeUsing
                {
                    Name = "BaseRequestBuilder",
                    Declaration = new CodeType
                    {
                        Name = $"{AbstractionsPackageName}.base_request_builder",
                        IsExternal = true
                    }
                }, AccessModifier.Public);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by_{x.ToSnakeCase()}",
                static x => x.ToSnakeCase(),
                GenerationLanguage.Python);
            RemoveCancellationParameter(generatedCode);
            RemoveRequestConfigurationClasses(
                generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    Declaration = new CodeType
                    {
                        Name = $"{AbstractionsPackageName}.base_request_configuration",
                        IsExternal = true
                    }
                },
                new CodeType
                {
                    Name = "QueryParameters",
                    IsExternal = true,
                },
                keepRequestConfigurationClass: true,
                addDeprecation: true
            );
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            CorrectCoreTypesForBackingStore(generatedCode, "field(default_factory=BackingStoreFactorySingleton(backing_store_factory=None).backing_store_factory.create_backing_store, repr=False)");
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true, codeTypeFilter);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceBinaryByNativeType(generatedCode, "bytes", string.Empty, true);
            ReplaceReservedNames(
                generatedCode,
                new PythonReservedNamesProvider(),
                static x => $"{x}_"
            );
            ReplaceReservedExceptionPropertyNames(
                generatedCode,
                new PythonExceptionsReservedNamesProvider(),
                static x => $"{x}_"
            );
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ReplacePropertyNames(generatedCode,
                new() {
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                    CodePropertyKind.RequestBuilder,
                    CodePropertyKind.BackingStore
                },
                static s => s.ToFirstCharacterLowerCase().ToSnakeCase());
            AddParentClassToErrorClasses(
                generatedCode,
                "APIError",
                $"{AbstractionsPackageName}.api_error"
            );
            AddConstructorsForErrorClasses(generatedCode);
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                },
                static (_, s) => s.ToSnakeCase(),
                false,
                false,
                string.Empty,
                string.Empty);
            AddConstructorsForDefaultValues(generatedCode, true);
            var defaultConfiguration = new GenerationConfiguration();
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "kiota_serialization_json.json_serialization_writer_factory.JsonSerializationWriterFactory",
                    "kiota_serialization_text.text_serialization_writer_factory.TextSerializationWriterFactory",
                    "kiota_serialization_form.form_serialization_writer_factory.FormSerializationWriterFactory",
                    "kiota_serialization_multipart.multipart_serialization_writer_factory.MultipartSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "kiota_serialization_json.json_parse_node_factory.JsonParseNodeFactory",
                    "kiota_serialization_text.text_parse_node_factory.TextParseNodeFactory",
                    "kiota_serialization_form.form_parse_node_factory.FormParseNodeFactory",
                }
            );
            AddSerializationModulesImport(generatedCode,
            new[] { $"{AbstractionsPackageName}.api_client_builder.register_default_serializer",
                    $"{AbstractionsPackageName}.api_client_builder.enable_backing_store_for_serialization_writer_factory",
                    $"{AbstractionsPackageName}.serialization.SerializationWriterFactoryRegistry"},
            new[] { $"{AbstractionsPackageName}.api_client_builder.register_default_deserializer",
                    $"{AbstractionsPackageName}.serialization.ParseNodeFactoryRegistry" });
            cancellationToken.ThrowIfCancellationRequested();
            AddQueryParameterMapperMethod(
                generatedCode,
                "get_query_parameter",
                "original_name"
            );
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: true,
                includeParentNamespace: true
            );
            AddPrimaryErrorMessage(generatedCode,
                "primary_message",
                () => new CodeType { Name = "str", IsNullable = false, IsExternal = true },
                true
            );
        }, cancellationToken);
    }

    private const string MultipartBodyClassName = "MultipartBody";
    private const string AbstractionsPackageName = "kiota_abstractions";
    private const string SerializationModuleName = $"{AbstractionsPackageName}.serialization";
    private const string StoreModuleName = $"{AbstractionsPackageName}.store";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeClass, "__future__", "annotations"),
        new (static x => x is CodeClass, "typing", "Any, Optional, TYPE_CHECKING, Union"),
        new (static x => x is CodeClass, "collections.abc", "Callable"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            $"{AbstractionsPackageName}.request_adapter", "RequestAdapter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.method", "Method"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) && method.Parameters.Any(static y => y.IsOfKind(CodeParameterKind.RequestBody) && y.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase)),
            $"{AbstractionsPackageName}.multipart_body", MultipartBodyClassName),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.request_information", "RequestInformation"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.request_option", "RequestOption"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.default_query_parameters", "QueryParameters"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            SerializationModuleName, "SerializationWriter"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            SerializationModuleName, "ParseNode"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.IndexerBackwardCompatibility),
            $"{AbstractionsPackageName}.get_path_parameters", "get_path_parameters"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            SerializationModuleName, "Parsable", "ParsableFactory"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            SerializationModuleName, "Parsable"),
        new (static x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(static x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            SerializationModuleName, "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            StoreModuleName, "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (static x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            StoreModuleName, "BackedModel", "BackingStore", "BackingStoreFactorySingleton" ),
        new (static x => x is CodeClass @class && (@class.IsOfKind(CodeClassKind.Model) || x.Parent is CodeClass), "dataclasses", "dataclass, field"),
         new (static x => x is CodeClass @class && @class.OriginalComposedType is CodeIntersectionType intersectionType && intersectionType.Types.Any(static y => !y.IsExternal),
            SerializationModuleName, "ParseNodeHelper"),
        new (static x => x is IDeprecableElement element && element.Deprecation is not null && element.Deprecation.IsDeprecated,
            "warnings", "warn"),
    };

    private static void CorrectCommonNames(CodeElement currentElement)
    {
        if (currentElement is CodeMethod m &&
            currentElement.Parent is CodeClass parentClassM)
        {
            parentClassM.RenameChildElement(m.Name, m.Name.ToFirstCharacterLowerCase().ToSnakeCase());

            foreach (var param in m.Parameters)
            {
                if (param.SerializationName != null)
                    param.SerializationName = param.Name;
                param.Name = param.Name.ToFirstCharacterLowerCase().ToSnakeCase();
            }
            if (parentClassM.IsOfKind(CodeClassKind.Model))
            {
                foreach (var prop in parentClassM.Properties)
                {
                    if (string.IsNullOrEmpty(prop.SerializationName))
                    {
                        prop.SerializationName = prop.Name;
                    }

                    parentClassM.RenameChildElement(prop.Name, prop.Name.ToFirstCharacterLowerCase().ToSnakeCase());
                }
            }
        }
        else if (currentElement is CodeClass c)
        {
            c.Name = c.Name.ToFirstCharacterUpperCase();
        }
        else if (currentElement is CodeProperty p &&
            (p.IsOfKind(CodePropertyKind.RequestAdapter) ||
            p.IsOfKind(CodePropertyKind.PathParameters) ||
            p.IsOfKind(CodePropertyKind.QueryParameters) ||
            p.IsOfKind(CodePropertyKind.UrlTemplate)) &&
            currentElement.Parent is CodeClass parentClassP)
        {
            if (p.SerializationName != null)
                p.SerializationName = p.Name;

            parentClassP.RenameChildElement(p.Name, p.Name.ToFirstCharacterLowerCase().ToSnakeCase());
        }
        else if (currentElement is CodeIndexer i)
        {
            i.IndexParameter.Name = i.IndexParameter.Name.ToFirstCharacterLowerCase().ToSnakeCase();
        }
        else if (currentElement is CodeEnum e)
        {
            foreach (var option in e.Options)
            {
                if (!string.IsNullOrEmpty(option.Name) && Char.IsLower(option.Name[0]))
                {
                    if (string.IsNullOrEmpty(option.SerializationName))
                    {
                        option.SerializationName = option.Name;
                    }
                    option.Name = option.Name.ToPascalCase();
                }
            }
        }

        CrawlTree(currentElement, CorrectCommonNames);
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            currentProperty.Type.Name = "RequestAdapter";
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..].ToFirstCharacterUpperCase(); // removing the "I"
        else if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.Type.Name = "list[RequestOption]";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.Type.Name = "dict[str, Union[str, list[str]]]";
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "dict[str, Any]";
            currentProperty.DefaultValue = "field(default_factory=dict)";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "Union[str, dict[str, Any]]";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        }
        else if (currentProperty.Kind is CodePropertyKind.Custom && currentProperty.Type.IsNullable && string.IsNullOrEmpty(currentProperty.DefaultValue))
        {
            currentProperty.DefaultValue = "None";
            currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameters, CodePropertyKind.QueryParameter)
                 && currentProperty.Type.IsArray && !currentProperty.Type.IsNullable)
        {
            // Set the default_factory so that one single instance of the default values
            // are not shared across instances of the class.
            // This is required as of Python 3.11 with dataclasses.
            // https://github.com/python/cpython/issues/8884
            //
            // Also handle the case change that would otherwise have been done
            // below in the final else block.
            currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
            currentProperty.DefaultValue = "field(default_factory=list)";
        }
        else
        {
            currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, true, currentProperty.Type);
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith('I')).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
            currentMethod.ReturnType.Name = "dict[str, Callable[[ParseNode], None]]";
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.Factory))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore, CodeParameterKind.ParseNode))
                .Where(static x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(static x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if (urlTplParams != null &&
                urlTplParams.Type is CodeType originalType)
            {
                originalType.Name = "Union[str, dict[str, Any]]";
                urlTplParams.Documentation.DescriptionTemplate = "The raw url or the url-template parameters for the request.";
            }
        }
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, true, currentMethod.Parameters
                                            .Select(x => x.Type)
                                            .Union(new[] { currentMethod.ReturnType })
                                            .ToArray());
    }

    // Caters for QueryParameters and RequestConfiguration which are implemented as nested classes.
    // No imports required for nested classes in Python.
    public static IEnumerable<CodeTypeBase> codeTypeFilter(IEnumerable<CodeTypeBase> usingsToAdd)
    {
        var nestedTypes = usingsToAdd.OfType<CodeType>().Where(
            static codeType => codeType.TypeDefinition is CodeClass codeClass
            && codeClass.IsOfKind(CodeClassKind.RequestConfiguration, CodeClassKind.QueryParameters));

        return usingsToAdd.Except(nestedTypes);
    }

    private const string DateTimePackageName = "datetime";
    private const string UUIDPackageName = "uuid";
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("datetime.datetime", new CodeUsing {
                                    Name = DateTimePackageName,
                                    Declaration = new CodeType {
                                        Name = "-",
                                        IsExternal = true,
                                    },
                                })},
    {"TimeSpan", ("datetime.timedelta", new CodeUsing {
                                    Name = DateTimePackageName,
                                    Declaration = new CodeType {
                                        Name = "-",
                                        IsExternal = true,
                                    },
                                })},
    {"DateOnly", ("datetime.date", new CodeUsing {
                            Name = DateTimePackageName,
                            Declaration = new CodeType {
                                Name = "-",
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", ("datetime.time", new CodeUsing {
                            Name = DateTimePackageName,
                            Declaration = new CodeType {
                                Name = "-",
                                IsExternal = true,
                            },
                        })},
    {"Guid", ("uuid", new CodeUsing {
                        Name = "UUID",
                        Declaration = new CodeType {
                            Name = UUIDPackageName,
                            IsExternal = true,
                        },
                    })},
    };

    private static void AddConstructorsForErrorClasses(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass && codeClass.IsErrorDefinition)
        {
            // Add parameterless constructor if not already present
            if (!codeClass.Methods.Any(static x => x.IsOfKind(CodeMethodKind.Constructor) && !x.Parameters.Any()))
            {
                var parameterlessConstructor = CreateConstructor(codeClass, "Instantiates a new {TypeName} and sets the default values.");
                codeClass.AddMethod(parameterlessConstructor);
            }

            // Add message constructor if not already present
            var messageParameter = CreateErrorMessageParameter("str", optional: true, defaultValue: "None");
            if (!codeClass.Methods.Any(static x => x.IsOfKind(CodeMethodKind.Constructor) && x.Parameters.Any(static p => p.IsOfKind(CodeParameterKind.ErrorMessage))))
            {
                var messageConstructor = CreateConstructor(codeClass, "Instantiates a new {TypeName} with the specified error message.");
                messageConstructor.AddParameter(messageParameter);
                codeClass.AddMethod(messageConstructor);
            }

            TryAddErrorMessageFactoryMethod(codeClass,
                "create_from_discriminator_value_with_message",
                "ParseNode",
                messageParameter,
                parseNodeParameterName: "parse_node"
            );
        }
        CrawlTree(currentElement, AddConstructorsForErrorClasses);
    }

    private static CodeMethod CreateConstructor(CodeClass codeClass, string descriptionTemplate)
    {
        return new CodeMethod
        {
            Name = "__init__",
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
            ReturnType = new CodeType { Name = "None", IsExternal = true },
            Parent = codeClass,
        };
    }
}
