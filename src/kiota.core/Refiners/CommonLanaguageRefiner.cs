using System;
using System.Linq;

namespace kiota.core {
    public abstract class CommonLanguageRefiner : ILanguageRefiner
    {
        public abstract void Refine(CodeNamespace generatedCode);

        internal void AddInnerClasses(CodeElement current) {
            if(current is CodeClass currentClass) {
                foreach(var parameter in current.GetChildElements().OfType<CodeMethod>().SelectMany(x =>x.Parameters).Where(x => x.Type.ActionOf))
                    currentClass.AddInnerClass(parameter.Type.TypeDefinition);
            }
            foreach(var childClass in current.GetChildElements())
                AddInnerClasses(childClass);
        }
        private readonly CodeUsingComparer usingComparerWithDeclarations = new CodeUsingComparer(true);
        private readonly CodeUsingComparer usingComparerWithoutDeclarations = new CodeUsingComparer(false);
        protected void AddPropertiesAndMethodTypesImports(CodeElement current, bool includeParentNamespaces, bool includeCurrentNamespace, bool compareOnDeclaration) {
            if(current is CodeClass currentClass) {
                var currentClassNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
                var propertiesTypes = currentClass
                                    .InnerChildElements
                                    .OfType<CodeProperty>()
                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)
                                    .Select(x => x.Type)
                                    .Distinct();
                var methods = currentClass
                                    .InnerChildElements
                                    .OfType<CodeMethod>()
                                    .Where(x => x.MethodKind == CodeMethodKind.Custom);
                var methodsReturnTypes = methods
                                    .Select(x => x.ReturnType)
                                    .Distinct();
                var methodsParametersTypes = methods
                                    .SelectMany(x => x.Parameters)
                                    .Where(x => x.ParameterKind == CodeParameterKind.Custom)
                                    .Select(x => x.Type)
                                    .Distinct();
                var usingsToAdd = propertiesTypes
                                    .Union(methodsParametersTypes)
                                    .Union(methodsReturnTypes)
                                    .Select(x => new Tuple<CodeType, CodeNamespace>(x, x?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>()))
                                    .Where(x => x.Item2 != null && (includeCurrentNamespace || x.Item2 != currentClassNamespace))
                                    .Where(x => includeParentNamespaces || !currentClassNamespace.IsChildOf(x.Item2))
                                    .Select(x => new CodeUsing(currentClass) { Name = x.Item2.Name, Declaration = x.Item1 })
                                    .Distinct(compareOnDeclaration ? usingComparerWithDeclarations : usingComparerWithoutDeclarations)
                                    .ToArray();
                if(usingsToAdd.Any())
                    currentClass.AddUsing(usingsToAdd);
            }
            foreach(var childElement in current.GetChildElements())
                AddPropertiesAndMethodTypesImports(childElement, includeParentNamespaces, includeCurrentNamespace, compareOnDeclaration);
        }
    }
}
