using System.Linq;
using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AliasUsingsWithSameSymbol(generatedCode);
            AddParsableInheritanceForModelClasses(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "ReadableStream", "web-streams-polyfill/es2018", true);
            ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                }, _configuration.UsesBackingStore, false);
            AddConstructorsForDefaultValues(generatedCode, true);
            ReplaceDefaultSerializationModules(generatedCode, "@microsoft/kiota-serialization-json.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "@microsoft/kiota-serialization-json.JsonParseNodeFactory");
            AddSerializationModulesImport(generatedCode,
                new[] { "@microsoft/kiota-abstractions.registerDefaultSerializer", 
                        "@microsoft/kiota-abstractions.enableBackingStoreForSerializationWriterFactory",
                        "@microsoft/kiota-abstractions.SerializationWriterFactoryRegistry"},
                new[] { "@microsoft/kiota-abstractions.registerDefaultDeserializer",
                        "@microsoft/kiota-abstractions.ParseNodeFactoryRegistry" });
        }
        private static readonly CodeUsingDeclarationNameComparer usingComparer = new();
        private static void AliasUsingsWithSameSymbol(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass &&
                currentClass.StartBlock is CodeClass.Declaration currentDeclaration &&
                currentDeclaration.Usings.Any(x => !x.IsExternal)) {
                    var duplicatedSymbolsUsings = currentDeclaration.Usings.Where(x => !x.IsExternal)
                                                                            .Distinct(usingComparer)
                                                                            .GroupBy(x => x.Declaration.Name, StringComparer.OrdinalIgnoreCase)
                                                                            .Where(x => x.Count() > 1)
                                                                            .SelectMany(x => x)
                                                                            .Union(currentDeclaration
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
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.HttpCore),
                "@microsoft/kiota-abstractions", "HttpCore"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "@microsoft/kiota-abstractions", "HttpMethod", "RequestInformation", "RequestOption"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "@microsoft/kiota-abstractions", "ResponseHandler"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "@microsoft/kiota-abstractions", "SerializationWriter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "@microsoft/kiota-abstractions", "ParseNode"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "@microsoft/kiota-abstractions", "Parsable"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
                "@microsoft/kiota-abstractions", "Parsable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                        method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
                "@microsoft/kiota-abstractions", "BackingStoreFactory", "BackingStoreFactorySingleton"),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
                "@microsoft/kiota-abstractions", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
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
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "RequestOption[]");
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
    }
}
