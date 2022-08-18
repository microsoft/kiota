using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class PythonRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public PythonRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override void Refine(CodeNamespace generatedCode)
    {
        AddDefaultImports(generatedCode, defaultUsingEvaluators);
        DisableActionOf(generatedCode, 
        CodeParameterKind.RequestConfiguration);
        ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "_by_id");
        RemoveCancellationParameter(generatedCode);
        CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
        CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.__instance.create_backing_store()");
        AddPropertiesAndMethodTypesImports(generatedCode, true, true, true, codeTypeFilter);           
        AddParsableImplementsForModelClasses(generatedCode, "Parsable");
        ReplaceBinaryByNativeType(generatedCode, "bytes",null);
        ReplaceReservedNames(
            generatedCode,
            new PythonReservedNamesProvider(), x => $"{x}_escaped"
        );
        AddGetterAndSetterMethods(generatedCode,
            new() {
                CodePropertyKind.Custom,
                CodePropertyKind.AdditionalData,
            },
            _configuration.UsesBackingStore,
            false,
            string.Empty,
            string.Empty);
        AddConstructorsForDefaultValues(generatedCode, true);
        var defaultConfiguration = new GenerationConfiguration();
        ReplaceDefaultSerializationModules(
            generatedCode,
            defaultConfiguration.Serializers,
            new (StringComparer.OrdinalIgnoreCase) {
                "serialization_json.json_serialization_writer_factory.JsonSerializationWriterFactory"
            }
        );
        ReplaceDefaultDeserializationModules(
            generatedCode,
            defaultConfiguration.Deserializers,
            new (StringComparer.OrdinalIgnoreCase) {
                "serialization_json.json_parse_node_factory.JsonParseNodeFactory"
            }
        );
        AddSerializationModulesImport(generatedCode,
        new[] { $"{AbstractionsPackageName}.api_client_builder.register_default_serializer", 
                $"{AbstractionsPackageName}.api_client_builder.enable_backing_store_for_serialization_writer_factory",
                $"{AbstractionsPackageName}.serialization.SerializationWriterFactoryRegistry"},
        new[] { $"{AbstractionsPackageName}.api_client_builder.register_default_deserializer",
                $"{AbstractionsPackageName}.serialization.ParseNodeFactoryRegistry" });
        AddParentClassToErrorClasses(
                generatedCode,
                "APIError",
                "kiota.abstractions.api_error"
        );
        AddQueryParameterMapperMethod(
            generatedCode
        );
    }

    private const string AbstractionsPackageName = "kiota.abstractions";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
        new (x => x is CodeClass @class, "__future__", "annotations"),
        new (x => x is CodeClass @class, "typing", "Any, Callable, Dict, List, Optional, Union"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            $"{AbstractionsPackageName}.request_adapter", "RequestAdapter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.method", "Method"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.request_information", "RequestInformation"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            $"{AbstractionsPackageName}.request_option", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            $"{AbstractionsPackageName}.response_handler", "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            $"{AbstractionsPackageName}.serialization", "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            $"{AbstractionsPackageName}.serialization", "ParseNode"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.IndexerBackwardCompatibility),
            $"{AbstractionsPackageName}.get_path_parameters", "get_path_parameters"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            $"{AbstractionsPackageName}.serialization", "Parsable", "ParsableFactory"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            $"{AbstractionsPackageName}.serialization", "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            $"{AbstractionsPackageName}.serialization", "AdditionalDataHolder"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            $"{AbstractionsPackageName}.store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            $"{AbstractionsPackageName}.store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
        new (x => x is CodeClass && x.Parent is CodeClass, "dataclasses", "dataclass"),
        
    };
    private static void CorrectImplements(ProprietableBlockDeclaration block) {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            currentProperty.Type.Name = "RequestAdapter";
        else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
        else if(currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.Type.Name = "List[RequestOption]";
        else if(currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.Type.Name = "Dict[str, str]";
        else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameters))
            currentProperty.Type.Name = $"{currentProperty.Parent?.Parent.Name.ToFirstCharacterUpperCase()}.{currentProperty.Type.Name.ToFirstCharacterUpperCase()}";
        else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
            currentProperty.Type.Name = "Dict[str, Any]";
            currentProperty.DefaultValue = "{}";
        } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "Dict[str, Any]";
            if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "{}";
        } else
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        }
        else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer))
            currentMethod.ReturnType.Name = $"Dict[str, Callable[[ParseNode], None]]";
        else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.Factory)) {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore, CodeParameterKind.ParseNode))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if(urlTplParams != null &&
                urlTplParams.Type is CodeType originalType) {
                originalType.Name = "Dict[str, Any]";
                urlTplParams.Description = "The raw url or the Url template parameters for the request.";
                var unionType = new CodeUnionType {
                    Name = "raw_url_or_template_parameters",
                    IsNullable = true,
                };
                unionType.AddType(originalType, new() {
                    Name = "str",
                    IsNullable = true,
                    IsExternal = true,
                });
                urlTplParams.Type = unionType;
            }
        }
        CorrectDateTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                            .Select(x => x.Type)
                                            .Union(new CodeTypeBase[] { currentMethod.ReturnType})
                                            .ToArray());
    }
    
    // Caters for QueryParameters and RequestConfiguration which are implemented as nested classes.
    // No imports required for nested classes in Python.
    public static IEnumerable<CodeTypeBase> codeTypeFilter(IEnumerable<CodeTypeBase> usingsToAdd)
        {
            var nestedTypes = usingsToAdd.OfType<CodeType>().Where(
                static codeType => codeType.TypeDefinition is CodeClass codeClass 
                && codeClass.IsOfKind(CodeClassKind.RequestConfiguration,CodeClassKind.QueryParameters));

            return usingsToAdd.Except(nestedTypes);
        }
    
    private const string DateTimePackageName = "datetime";
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("datetime", new CodeUsing {
                                    Name = "datetime",
                                    Declaration = new CodeType {
                                        Name = DateTimePackageName,
                                        IsExternal = true,
                                    },
                                })},
    {"TimeSpan", ("timedelta", new CodeUsing {
                                    Name = "timedelta",
                                    Declaration = new CodeType {
                                        Name = DateTimePackageName,
                                        IsExternal = true,
                                    },
                                })},
    {"DateOnly", ("date", new CodeUsing {
                            Name = "date",
                            Declaration = new CodeType {
                                Name = DateTimePackageName,
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", ("time", new CodeUsing {
                            Name = "time",
                            Declaration = new CodeType {
                                Name = DateTimePackageName,
                                IsExternal = true,
                            },
                        })},
    };
}
