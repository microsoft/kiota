using System.Linq;
using System;

namespace Kiota.Builder.Refiners {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddDefaultImports(generatedCode, Array.Empty<Tuple<string, string>>(), defaultNamespacesForModels, defaultNamespacesForRequestBuilders);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            CorrectCoreTypesForBackingStore(generatedCode, "@microsoft/kiota-abstractions", "BackingStoreFactorySingleton.instance.createBackingStore()");
            FixReferencesToEntityType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AddParsableInheritanceForModelClasses(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "ReadableStream", "web-streams-polyfill/es2018", true);
            ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                }, _configuration.UsesBackingStore, false);
            AddConstructorsForDefaultValues(generatedCode, true);
            ReplaceRelativeImportsByImportPath(generatedCode, '.', _configuration.ClientNamespaceName);
            ReplaceDefaultSerializationModules(generatedCode, "@microsoft/kiota-serialization-json.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "@microsoft/kiota-serialization-json.JsonParseNodeFactory");
            AddSerializationModulesImport(generatedCode,
                new[] { "@microsoft/kiota-abstractions.registerDefaultSerializer", 
                        "@microsoft/kiota-abstractions.enableBackingStoreForSerializationWriterFactory",
                        "@microsoft/kiota-abstractions.SerializationWriterFactoryRegistry"},
                new[] { "@microsoft/kiota-abstractions.registerDefaultDeserializer",
                        "@microsoft/kiota-abstractions.ParseNodeFactoryRegistry" });
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.AddImplements(new CodeType{
                    IsExternal = true,
                    Name = "Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "@microsoft/kiota-abstractions"),
            new ("HttpMethod", "@microsoft/kiota-abstractions"),
            new ("RequestInformation", "@microsoft/kiota-abstractions"),
            new ("ResponseHandler", "@microsoft/kiota-abstractions"),
            new ("MiddlewareOption", "@microsoft/kiota-abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("SerializationWriter", "@microsoft/kiota-abstractions"),
            new ("ParseNode", "@microsoft/kiota-abstractions"),
            new ("Parsable", "@microsoft/kiota-abstractions"),
        };
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if(currentProperty.IsOfKind(CodePropertyKind.HttpCore))
                currentProperty.Type.Name = "HttpCore";
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase))
                currentProperty.Type.Name = $"Date";
            else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "Map<string, unknown>";
                currentProperty.DefaultValue = "new Map<string, unknown>()";
            }
        }
        private static void CorrectMethodType(CodeMethod currentMethod) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "MiddlewareOption[]");
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
            else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer))
                currentMethod.ReturnType.Name = $"Map<string, (item: T, node: ParseNode) => void>";
            else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.HttpCore, CodeParameterKind.BackingStore))
                    .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
                    .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        }
        private static void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeMethod currentMethod && currentMethod.Name.Equals("defaultResponseHandler", StringComparison.OrdinalIgnoreCase)) 
                currentMethod.Parameters.First().Type.Name = "ReadableStream";
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
