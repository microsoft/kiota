using System;
using System.Linq;
using System.Text.RegularExpressions;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public CSharpRefiner(CodeNamespace root) : base(root)
        {
            
        }
        public override void Refine()
        {
            AddDefaultImports(rootNamespace);
            MoveClassesWithNamespaceNamesUnderNamespace(rootNamespace);
            ConvertUnionTypesToWrapper(rootNamespace);
            AddPropertiesAndMethodTypesImports(rootNamespace, false, false, false);
            AddAsyncSuffix(rootNamespace);
            AddInnerClasses(rootNamespace);
            AddParsableInheritanceForModelClasses(rootNamespace);
            CapitalizeNamespacesFirstLetters(rootNamespace);
            ReplaceBinaryByNativeType(rootNamespace, "Stream", "System.IO");
            MakeEnumPropertiesNullable(rootNamespace);
        }
        private void MakeEnumPropertiesNullable(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.Model)
                currentClass.InnerChildElements
                    	    .Values
                            .OfType<CodeProperty>()
                            .Where(x => x.Type is CodeType propType && propType.TypeDefinition is CodeEnum)
                            .ToList()
                            .ForEach(x => x.Type.IsNullable = true);
            CrawlTree(currentElement, MakeEnumPropertiesNullable);
        }
        private void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.Model) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"IParsable<{currentClass.Name.ToFirstCharacterUpperCase()}>",
                });
                declaration.Usings.Add(new CodeUsing(currentClass) {
                    Name = "Kiota.Abstractions.Serialization"
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly string[] defaultNamespacesForClasses = new string[] {"System", "System.Collections.Generic", "System.Linq"};
        private static readonly string[] defaultNamespacesForRequestBuilders = new string[] { "System.Threading.Tasks", "System.IO", "Kiota.Abstractions", "Kiota.Abstractions.Serialization"};
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                currentClass.AddUsing(defaultNamespacesForClasses.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
                if(currentClass.ClassKind == CodeClassKind.RequestBuilder)
                    currentClass.AddUsing(defaultNamespacesForRequestBuilders.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CapitalizeNamespacesFirstLetters(CodeElement current) {
            if(current is CodeNamespace currentNamespace)
                currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
            CrawlTree(current, CapitalizeNamespacesFirstLetters);
        }
        private void AddAsyncSuffix(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
                currentMethod.Name += "Async";
            CrawlTree(currentElement, AddAsyncSuffix);
        }
    }
}
