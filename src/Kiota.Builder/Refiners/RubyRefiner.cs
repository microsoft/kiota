using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class RubyRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public RubyRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddParsableInheritanceForModelClasses(generatedCode);
            AddInheritedAndMethodTypesImports(generatedCode);
            FixReferencesToEntityType(generatedCode);
            FixInheritedEntityType(generatedCode, null, "Graphrubyv4::Utilities::Users::");
            AddDefaultImports(generatedCode, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders, defaultSymbolsForApiClient);
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                    CodePropertyKind.BackingStore,
                                                }, _configuration.UsesBackingStore, true);
            ReplaceReservedNames(generatedCode, new RubyReservedNamesProvider(), x => $"{x}_escaped");
            ReplaceRelativeImportsByImportPath(generatedCode, '.');
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "microsoft_kiota_abstractions"),
            new ("HttpMethod", "microsoft_kiota_abstractions"),
            new ("RequestInfo", "microsoft_kiota_abstractions"),
            new ("ResponseHandler", "microsoft_kiota_abstractions"),
            new ("QueryParametersBase", "microsoft_kiota_abstractions"),
            new ("SerializationWriterFactory", "microsoft_kiota_abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "microsoft_kiota_abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "microsoft_kiota_abstractions"),
            new ("Parsable", "microsoft_kiota_abstractions"),
        };
        private static readonly Tuple<string, string>[] defaultSymbolsForApiClient = new Tuple<string, string>[] { 
            new ("ApiClientBuilder", "microsoft_kiota_abstractions"),
            new ("SerializationWriterFactoryRegistry", "microsoft_kiota_abstractions"),
            new ("ParseNodeFactoryRegistry", "microsoft_kiota_abstractions"),
        };
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"MicrosoftKiotaAbstractions::Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        protected static void AddInheritedAndMethodTypesImports(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                if(declaration.Inherits != null){
                    currentClass.AddUsing(new CodeUsing(currentElement) { Name = declaration.Inherits.Name, Declaration = declaration.Inherits});
                }
            }
            CrawlTree(currentElement, (x) => AddInheritedAndMethodTypesImports(x));
        }

        protected static void FixInheritedEntityType(CodeElement currentElement, CodeClass entityClass = null, string prefix = ""){
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                if("entity".Equals(declaration?.Inherits?.Name, StringComparison.OrdinalIgnoreCase)){
                    // currentClass.AddUsing(new CodeUsing(currentElement) { Name = declaration.Inherits.Name, Declaration = declaration.Inherits});
                    var currentChild = declaration?.Inherits as CodeType;
                    currentChild.Name = prefix + declaration?.Inherits?.Name.ToFirstCharacterUpperCase();
                }
            }
            CrawlTree(currentElement, (c) => FixInheritedEntityType(c, entityClass, prefix));
        }
    }
}
