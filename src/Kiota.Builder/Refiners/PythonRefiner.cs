using System.Linq;
using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class PythonRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public PythonRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "_by_id");
            RemoveCancellationParameter(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.__instance.createBackingStore()");
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AliasUsingsWithSameSymbol(generatedCode);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            ReplaceBinaryByNativeType(generatedCode, "BytesIO", "io", true);
            ReplaceReservedNames(generatedCode, new PythonReservedNamesProvider(), x => $"{x}_escaped");
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                }, _configuration.UsesBackingStore, true,
                                                "get_",
                                                "set_");
            AddConstructorsForDefaultValues(generatedCode, true);
            ReplaceDefaultSerializationModules(generatedCode, "kiota-serialization-json.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "kiota-serialization-json.JsonParseNodeFactory");
            AddSerializationModulesImport(generatedCode,
                new[] { "microsoft_kiota_abstractions.ApiClientBuilder",
                        "kiota-abstractions.serialization.SerializationWriterFactoryRegistry"},
                new[] { "kiota-abstractions.serialization.ParseNodeFactoryRegistry" });
        }
        private static readonly CodeUsingDeclarationNameComparer usingComparer = new();
        private static void AliasUsingsWithSameSymbol(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass &&
                currentClass.StartBlock != null &&
                currentClass.StartBlock.Usings.Any(x => !x.IsExternal)) {
                    var duplicatedSymbolsUsings = currentClass.StartBlock.Usings.Where(x => !x.IsExternal)
                                                                            .Distinct(usingComparer)
                                                                            .GroupBy(x => x.Declaration.Name, StringComparer.OrdinalIgnoreCase)
                                                                            .Where(x => x.Count() > 1)
                                                                            .SelectMany(x => x)
                                                                            .Union(currentClass.StartBlock
                                                                                    .Usings
                                                                                    .Where(x => !x.IsExternal)
                                                                                    .Where(x => x.Declaration
                                                                                                    .Name
                                                                                                    .Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase)));
                    foreach(var usingElement in duplicatedSymbolsUsings)
                            usingElement.Alias = (usingElement.Declaration
                                                            .TypeDefinition
                                                            .GetImmediateParentOfType<CodeNamespace>()
                                                            .Name +
                                                usingElement.Declaration
                                                            .TypeDefinition
                                                            .Name)
                                                .GetNamespaceImportSymbol();
                }
            CrawlTree(currentElement, AliasUsingsWithSameSymbol);
        }
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "kiota.abstractions", "RequestAdapter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "kiota.abstractions", "HttpMethod", "RequestInformation", "RequestOption"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "kiota.abstractions", "ResponseHandler"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "kiota.abstractions.serialization", "SerializationWriter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "kiota.abstractions.serialization", "ParseNode"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.IndexerBackwardCompatibility),
                "kiota.abstractions", "getPathParameters"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "kiota.abstractions.serialization", "Parsable"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
                "kiota.abstractions.serialization", "Parsable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                        method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
                "kiota.abstractions.store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
                "kiota-abstractions.store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
        };
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
                currentProperty.Type.Name = "RequestAdapter";
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase))
                currentProperty.Type.Name = $"timedelta";
            else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "Dict[str, Any]";
                currentProperty.DefaultValue = "dict()";
            } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "Dict<str, Any>";
                if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = "dict()";
            }
        }
        private static void CorrectMethodType(CodeMethod currentMethod) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "RequestOption[]");
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
            else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer))
                currentMethod.ReturnType.Name = $"Map<string, (item: T, node: ParseNode) => void>";
            else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor)) {
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                    .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
                    .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
                var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                if(urlTplParams != null &&
                    urlTplParams.Type is CodeType originalType) {
                    originalType.Name = "Map<string, unknown>";
                    urlTplParams.Description = "The raw url or the Url template parameters for the request.";
                    var unionType = new CodeUnionType {
                        Name = "rawUrlOrTemplateParameters",
                        IsNullable = true,
                    };
                    unionType.AddType(originalType, new() {
                        Name = "string",
                        IsNullable = true,
                        IsExternal = true,
                    });
                    urlTplParams.Type = unionType;
                }
            }
        }
    }
}
