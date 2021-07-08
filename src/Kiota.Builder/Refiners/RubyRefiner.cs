using System;
using System.Collections.Generic;
using System.Linq;

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
            AddDefaultImports(generatedCode, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders);
            AddGetterAndSetterMethodsRuby(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                    CodePropertyKind.BackingStore,
                                                }, _configuration.UsesBackingStore, true);
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

        private static void AddGetterAndSetterMethodsRuby(CodeElement current, HashSet<CodePropertyKind> propertyKindsToAddAccessors, bool removeProperty, bool parameterAsOptional) {
            if(!(propertyKindsToAddAccessors?.Any() ?? true)) return;
            if(current is CodeProperty currentProperty &&
                propertyKindsToAddAccessors.Contains(currentProperty.PropertyKind) &&
                current.Parent is CodeClass parentClass &&
                !parentClass.IsOfKind(CodeClassKind.QueryParameters)) {
                if(removeProperty && currentProperty.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.AdditionalData)) // we never want to remove backing stores
                    parentClass.RemoveChildElement(currentProperty);
                else {
                    currentProperty.Access = AccessModifier.Private;
                    currentProperty.NamePrefix = "_";
                }
                parentClass.AddMethod(new CodeMethod(parentClass) {
                    Name = $"{GetterPrefix}{current.Name}",
                    Access = AccessModifier.Public,
                    IsAsync = false,
                    MethodKind = CodeMethodKind.Getter,
                    ReturnType = currentProperty.Type,
                    Description = $"Gets the {current.Name} property value. {currentProperty.Description}",
                    AccessedProperty = currentProperty,
                });
                if(!currentProperty.ReadOnly) {
                    var setter = parentClass.AddMethod(new CodeMethod(parentClass) {
                        Name = $"{SetterPrefix}{current.Name}",
                        Access = AccessModifier.Public,
                        IsAsync = false,
                        MethodKind = CodeMethodKind.Setter,
                        Description = $"Sets the {current.Name} property value. {currentProperty.Description}",
                        AccessedProperty = currentProperty,
                    }).First();
                    setter.ReturnType = new CodeType(setter) {
                        Name = "void"
                    };
                    setter.Parameters.Add(new(setter) {
                        Name = "value",
                        ParameterKind = CodeParameterKind.SetterValue,
                        Description = $"Value to set for the {current.Name} property.",
                        Optional = parameterAsOptional,
                        Type = currentProperty.Type,
                    });
                }
            }
            CrawlTree(current, x => AddGetterAndSetterMethods(x, propertyKindsToAddAccessors, removeProperty, parameterAsOptional));
        }
    }
}
