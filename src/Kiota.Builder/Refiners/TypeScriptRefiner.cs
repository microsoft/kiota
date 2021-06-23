using System.Linq;
using System;

namespace Kiota.Builder.Refiners {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddDefaultImports(generatedCode, Array.Empty<Tuple<string, string>>(), defaultNamespacesForModels, defaultNamespacesForRequestBuilders, defaultSymbolsForApiClient);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, "ById");
            CorrectCoreType(generatedCode);
            CorrectCoreTypesForBackingStoreUsings(generatedCode, "@microsoft/kiota-abstractions");
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
            ReplaceRelativeImportsByImportPath(generatedCode, '.');
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = "Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "@microsoft/kiota-abstractions"),
            new ("HttpMethod", "@microsoft/kiota-abstractions"),
            new ("RequestInfo", "@microsoft/kiota-abstractions"),
            new ("ResponseHandler", "@microsoft/kiota-abstractions"),
            new ("SerializationWriterFactory", "@microsoft/kiota-abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("SerializationWriter", "@microsoft/kiota-abstractions"),
            new ("ParseNode", "@microsoft/kiota-abstractions"),
            new ("Parsable", "@microsoft/kiota-abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultSymbolsForApiClient = new Tuple<string, string>[] { 
            new ("registerDefaultSerializers", "@microsoft/kiota-abstractions"),
            new ("enableBackingStore", "@microsoft/kiota-abstractions"),
            new ("SerializationWriterFactoryRegistry", "@microsoft/kiota-abstractions"),
            new ("ParseNodeFactoryRegistry", "@microsoft/kiota-abstractions"),
        };
        private static void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty) {
                if(currentProperty.IsOfKind(CodePropertyKind.HttpCore))
                    currentProperty.Type.Name = "HttpCore";
                else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                    currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
                else if(currentProperty.IsOfKind(CodePropertyKind.SerializerFactory))
                    currentProperty.Type.Name = "SerializationWriterFactory";
                else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = $"Date";
                else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                    currentProperty.Type.Name = "Map<string, unknown>";
                    currentProperty.DefaultValue = "new Map<string, unknown>()";
                }
            }
            if (currentElement is CodeMethod currentMethod) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
                else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer))
                    currentMethod.ReturnType.Name = $"Map<string, (item: T, node: ParseNode) => void>";
                else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor))
                    currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.HttpCore, CodeParameterKind.SerializationFactory)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            }
            
                
            CrawlTree(currentElement, CorrectCoreType);
        }
        private static void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeMethod currentMethod && currentMethod.Name.Equals("defaultResponseHandler", StringComparison.OrdinalIgnoreCase)) 
                currentMethod.Parameters.First().Type.Name = "ReadableStream";
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
