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
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "_by_id");
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddParsableInheritanceForModelClasses(generatedCode);
            AddInheritedAndMethodTypesImports(generatedCode);
            AddDefaultImports(generatedCode, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders);
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                    CodePropertyKind.BackingStore,
                                                }, _configuration.UsesBackingStore, true);
            ReplaceReservedNames(generatedCode, new RubyReservedNamesProvider(), x => $"{x}_escaped");
            ReplaceRelativeImportsByImportPath(generatedCode, '.', _configuration.ClientNamespaceName);
            AddNamespaceModuleImports(generatedCode , _configuration.ClientNamespaceName);
            FixReferencesToEntityType(generatedCode);
            FixInheritedEntityType(generatedCode);
            ReplaceDefaultSerializationModules(generatedCode, "microsoft_kiota_serialization.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "microsoft_kiota_serialization.JsonParseNodeFactory");
            AddSerializationModulesImport(generatedCode,
                                        new [] { "microsoft_kiota_abstractions.ApiClientBuilder",
                                                "microsoft_kiota_abstractions.SerializationWriterFactoryRegistry" },
                                        new [] { "microsoft_kiota_abstractions.ParseNodeFactoryRegistry" });
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "microsoft_kiota_abstractions"),
            new ("HttpMethod", "microsoft_kiota_abstractions"),
            new ("RequestInformation", "microsoft_kiota_abstractions"),
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
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) 
                && currentClass.StartBlock is CodeClass.Declaration declaration) {
                declaration.AddImplements(new CodeType {
                    IsExternal = true,
                    Name = $"MicrosoftKiotaAbstractions::Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        protected static void AddInheritedAndMethodTypesImports(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) 
                && currentClass.StartBlock is CodeClass.Declaration declaration && declaration.Inherits != null) {
                currentClass.AddUsing(new CodeUsing { Name = declaration.Inherits.Name, Declaration = declaration.Inherits});
            }
            CrawlTree(currentElement, (x) => AddInheritedAndMethodTypesImports(x));
        }

        protected static void FixInheritedEntityType(CodeElement currentElement, string prefix = ""){

            var nameSpaceName = string.IsNullOrEmpty(prefix) ? FetchEntityNamespace(currentElement).NormalizeNameSpaceName("::") : prefix; 
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) 
                && currentClass.StartBlock is CodeClass.Declaration declaration && declaration.Inherits != null 
                && "entity".Equals(declaration.Inherits.Name, StringComparison.OrdinalIgnoreCase)) {
                declaration.Inherits.Name = prefix + declaration.Inherits.Name.ToFirstCharacterUpperCase();
            }
            CrawlTree(currentElement, (c) => FixInheritedEntityType(c, nameSpaceName));
        }
        protected static string FetchEntityNamespace(CodeElement currentElement){
            Queue<CodeElement> children = new Queue<CodeElement>();
            children.Enqueue(currentElement);
            while(children.Count > 0){
                foreach(var childElement in children.Dequeue().GetChildElements())
                    if(childElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) 
                    && "entity".Equals(currentClass?.Name, StringComparison.OrdinalIgnoreCase)) {
                        return string.IsNullOrEmpty(currentClass?.Parent?.Name) ? string.Empty : currentClass?.Parent?.Name + "::";
                    } else {
                        children.Enqueue(childElement);
                    }
            }
            return null;
        }
        protected void AddNamespaceModuleImports(CodeElement current, String clientNamespaceName) {
            const string dot = ".";
            if(current is CodeClass currentClass) {
                var Module = currentClass.GetImmediateParentOfType<CodeNamespace>();
                if(!String.IsNullOrEmpty(Module.Name)){
                    var modulesProperties = Module.Name.Replace(clientNamespaceName+dot, string.Empty).Split(dot);
                    for (int i = modulesProperties.Length - 1; i >= 0; i--){
                        var prefix = String.Concat(Enumerable.Repeat("../", modulesProperties.Length -i-1));
                        var usingName = modulesProperties[i].ToSnakeCase();
                        currentClass.AddUsing(new CodeUsing { 
                            Name = usingName,
                            Declaration = new CodeType {
                                IsExternal = false,
                                Name = $"{(string.IsNullOrEmpty(prefix) ? "./" : prefix)}{usingName}",
                            }
                        });
                    }
                } 
            }
            CrawlTree(current, c => AddNamespaceModuleImports(c, clientNamespaceName));
        }
    }
}
