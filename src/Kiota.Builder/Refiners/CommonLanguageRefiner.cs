using System;
using System.Collections.Generic;
using System.Linq;
using static Kiota.Builder.CodeClass;

namespace Kiota.Builder {
    public abstract class CommonLanguageRefiner : ILanguageRefiner
    {
        public abstract void Refine(CodeNamespace generatedCode);

        private const string pathSegmentPropertyName = "pathSegment";
        protected void MoveClassesWithNamespaceNamesUnderNamespace(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && 
                currentClass.Parent is CodeNamespace parentNamespace) {
                var childNamespaceWithClassName = parentNamespace.InnerChildElements
                                                                .OfType<CodeNamespace>()
                                                                .FirstOrDefault(x => x.Name
                                                                                    .EndsWith(currentClass.Name, StringComparison.InvariantCultureIgnoreCase));
                if(childNamespaceWithClassName != null) {
                    parentNamespace.InnerChildElements.Remove(currentClass);
                    childNamespaceWithClassName.AddClass(currentClass);
                }
            }
            CrawlTree(currentElement, MoveClassesWithNamespaceNamesUnderNamespace);
        }
        protected void ReplaceIndexersByMethodsWithParameter(CodeElement currentElement, string methodNameSuffix = default) {
            if(currentElement is CodeIndexer currentIndexer) {
                var currentParentClass = currentElement.Parent as CodeClass;
                currentParentClass.InnerChildElements.Remove(currentElement);
                var pathSegment = currentParentClass
                                    .GetChildElements()
                                    .OfType<CodeProperty>()
                                    .FirstOrDefault(x => x.Name.Equals(pathSegmentPropertyName, StringComparison.InvariantCultureIgnoreCase))
                                    ?.DefaultValue;
                if(!string.IsNullOrEmpty(pathSegment))
                    foreach(var returnType in currentIndexer.ReturnType.AllTypes)
                        AddIndexerMethod(currentElement.GetImmediateParentOfType<CodeNamespace>().GetRootNamespace(), 
                                        currentParentClass,
                                        returnType.TypeDefinition,
                                        pathSegment.Trim('\"').TrimStart('/'),
                                        methodNameSuffix);
            }
            CrawlTree(currentElement, c => ReplaceIndexersByMethodsWithParameter(c, methodNameSuffix));
        }
        protected void AddIndexerMethod(CodeElement currentElement, CodeClass targetClass, CodeClass indexerClass, string pathSegment, string methodNameSuffix) {
            if(currentElement is CodeProperty currentProperty && currentProperty.Type.AllTypes.Any(x => x.TypeDefinition == targetClass)) {
                var parentClass = currentElement.Parent as CodeClass;
                var method = new CodeMethod(parentClass) {
                    IsAsync = false,
                    IsStatic = false,
                    Access = AccessModifier.Public,
                    MethodKind = CodeMethodKind.IndexerBackwardCompatibility,
                    Name = pathSegment + methodNameSuffix,
                };
                method.ReturnType = new CodeType(method) {
                    IsNullable = false,
                    TypeDefinition = indexerClass,
                    Name = indexerClass.Name,
                };
                method.GenerationProperties.Add(pathSegmentPropertyName, pathSegment);
                var parameter = new CodeParameter(method) {
                    Name = "id",
                    Optional = false,
                    ParameterKind = CodeParameterKind.Custom
                };
                parameter.Type = new CodeType(parameter) {
                    Name = "String",
                    IsNullable = false,
                };
                method.Parameters.Add(parameter);
                parentClass.AddMethod(method);
            }
            CrawlTree(currentElement, c => AddIndexerMethod(c, targetClass, indexerClass, pathSegment, methodNameSuffix));
        }
        internal void AddInnerClasses(CodeElement current) {
            if(current is CodeClass currentClass) {
                foreach(var parameter in current.GetChildElements().OfType<CodeMethod>().SelectMany(x =>x.Parameters).Where(x => x.Type.ActionOf && x.ParameterKind == CodeParameterKind.QueryParameter)) 
                    foreach(var returnType in parameter.Type.AllTypes) {
                        currentClass.AddInnerClass(returnType.TypeDefinition);
                        (returnType.TypeDefinition.StartBlock as Declaration).Inherits = new CodeType(returnType.TypeDefinition) { Name = "QueryParametersBase", IsExternal = true };
                    }
            }
            CrawlTree(current, AddInnerClasses);
        }
        private readonly CodeUsingComparer usingComparerWithDeclarations = new CodeUsingComparer(true);
        private readonly CodeUsingComparer usingComparerWithoutDeclarations = new CodeUsingComparer(false);
        protected void AddPropertiesAndMethodTypesImports(CodeElement current, bool includeParentNamespaces, bool includeCurrentNamespace, bool compareOnDeclaration) {
            if(current is CodeClass currentClass) {
                var currentClassNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
                var propertiesTypes = currentClass
                                    .InnerChildElements
                                    .OfType<CodeProperty>()
                                    .Select(x => x.Type)
                                    .Distinct();
                var methods = currentClass
                                    .InnerChildElements
                                    .OfType<CodeMethod>();
                var methodsReturnTypes = methods
                                    .Select(x => x.ReturnType)
                                    .Distinct();
                var methodsParametersTypes = methods
                                    .SelectMany(x => x.Parameters)
                                    .Where(x => x.ParameterKind == CodeParameterKind.Custom)
                                    .Select(x => x.Type)
                                    .Distinct();
                var indexerTypes = currentClass
                                    .InnerChildElements
                                    .OfType<CodeIndexer>()
                                    .Select(x => x.ReturnType)
                                    .Distinct();
                var usingsToAdd = propertiesTypes
                                    .Union(methodsParametersTypes)
                                    .Union(methodsReturnTypes)
                                    .Union(indexerTypes)
                                    .Union(new List<CodeType> { (currentClass.StartBlock as CodeClass.Declaration)?.Inherits })
                                    .Where(x => x != null)
                                    .SelectMany(x => x?.AllTypes?.Select(y => new Tuple<CodeType, CodeNamespace>(y, y?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())))
                                    .Where(x => x.Item2 != null && (includeCurrentNamespace || x.Item2 != currentClassNamespace))
                                    .Where(x => includeParentNamespaces || !currentClassNamespace.IsChildOf(x.Item2))
                                    .Select(x => new CodeUsing(currentClass) { Name = x.Item2.Name, Declaration = x.Item1 })
                                    .Distinct(compareOnDeclaration ? usingComparerWithDeclarations : usingComparerWithoutDeclarations)
                                    .ToArray();
                if(usingsToAdd.Any())
                    currentClass.AddUsing(usingsToAdd);
            }
            CrawlTree(current, (x) => AddPropertiesAndMethodTypesImports(x, includeParentNamespaces, includeCurrentNamespace, compareOnDeclaration));
        }
        protected void CrawlTree(CodeElement currentElement, Action<CodeElement> function) {
            foreach(var childElement in currentElement.GetChildElements())
                function.Invoke(childElement);
        }
    }
}
