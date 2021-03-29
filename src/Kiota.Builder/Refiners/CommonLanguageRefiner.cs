using System;
using System.Collections.Generic;
using System.Linq;
using static Kiota.Builder.CodeClass;

namespace Kiota.Builder {
    public abstract class CommonLanguageRefiner : ILanguageRefiner
    {
        public abstract void Refine(CodeNamespace generatedCode);

        private const string binaryType = "binary";
        protected void ReplaceBinaryByNativeType(CodeElement currentElement, string symbol, string ns, bool addDeclaration = false) {
            if(currentElement is CodeMethod currentMethod) {
                var parentClass = currentMethod.Parent as CodeClass;
                var shouldInsertUsing = false;
                if(currentMethod.ReturnType.Name == binaryType) {
                    currentMethod.ReturnType.Name = symbol;
                    shouldInsertUsing = true;
                }
                var binaryParameter = currentMethod.Parameters.FirstOrDefault(x => x.Type.Name.Equals(binaryType));
                if(binaryParameter != null) {
                    binaryParameter.Type.Name = symbol;
                    shouldInsertUsing = true;
                }
                if(shouldInsertUsing) {
                    var newUsing = new CodeUsing(parentClass) {
                        Name = ns,
                    };
                    if(addDeclaration)
                        newUsing.Declaration = new CodeType(newUsing) {
                            Name = symbol,
                        };
                    parentClass.AddUsing(newUsing);
                }
            }
            CrawlTree(currentElement, c => ReplaceBinaryByNativeType(c, symbol, ns, addDeclaration));
        }
        private const string pathSegmentPropertyName = "pathSegment";
        protected void ConvertDeserializerPropsToMethods(CodeElement currentElement, string prefix = null) {
            if(currentElement is CodeClass currentClass) {
                var deserializerProp = currentClass.InnerChildElements.OfType<CodeProperty>().FirstOrDefault(x => x.PropertyKind == CodePropertyKind.Deserializer);
                if(deserializerProp != null) {
                    currentClass.AddMethod(new CodeMethod(currentClass) {
                        Name = $"{prefix}{deserializerProp.Name}",
                        MethodKind = CodeMethodKind.DeserializerBackwardCompatibility,
                        IsAsync = false,
                        IsStatic = false,
                        ReturnType = deserializerProp.Type,
                        Access = AccessModifier.Public,
                    });
                    currentClass.InnerChildElements.Remove(deserializerProp);
                }
            }
            CrawlTree(currentElement, c => ConvertDeserializerPropsToMethods(c, prefix));
        }
        // temporary patch of type to it resolves as the builder sets types we didn't generate to entity
        protected void FixReferencesToEntityType(CodeElement currentElement, CodeClass entityClass = null){
            if(entityClass == null)
                entityClass = currentElement.GetImmediateParentOfType<CodeNamespace>()
                            .GetRootNamespace()
                            .GetChildElementOfType<CodeClass>(x => x.Name.Equals("entity", StringComparison.InvariantCultureIgnoreCase));

            if(currentElement is CodeMethod currentMethod 
                && currentMethod.ReturnType is CodeType currentReturnType
                && currentReturnType.Name.Equals("entity", StringComparison.InvariantCultureIgnoreCase)
                && currentReturnType.TypeDefinition == null)
                currentReturnType.TypeDefinition = entityClass;

            CrawlTree(currentElement, (c) => FixReferencesToEntityType(c, entityClass));
        }
        protected void ConvertUnionTypesToWrapper(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod) {
                if(currentMethod.ReturnType is CodeUnionType currentUnionType)
                    currentMethod.ReturnType = ConvertUnionTypeToWrapper(currentMethod.Parent as CodeClass, currentUnionType);
                else if(currentMethod.Parameters.Any(x => x.Type is CodeUnionType))
                    foreach(var currentParameter in currentMethod.Parameters.Where(x => x.Type is CodeUnionType))
                        currentParameter.Type = ConvertUnionTypeToWrapper(currentMethod.Parent as CodeClass, currentParameter.Type as CodeUnionType);
            }
            else if (currentElement is CodeIndexer currentIndexer && currentIndexer.ReturnType is CodeUnionType currentUnionType)
                currentIndexer.ReturnType = ConvertUnionTypeToWrapper(currentIndexer.Parent as CodeClass, currentUnionType);

            CrawlTree(currentElement, ConvertUnionTypesToWrapper);
        }
        private CodeTypeBase ConvertUnionTypeToWrapper(CodeClass codeClass, CodeUnionType codeUnionType)
        {
            if(codeClass == null) throw new ArgumentNullException(nameof(codeClass));
            if(codeUnionType == null) throw new ArgumentNullException(nameof(codeUnionType));
            var newClass = new CodeClass(codeClass) {
                Name = codeUnionType.Name
            };
            newClass.AddProperty(codeUnionType
                                    .Types
                                    .Select(x => new CodeProperty(newClass) {
                                        Name = x.Name,
                                        Type = x
                                    }).ToArray());
            return new CodeType(codeClass) {
                Name = newClass.Name,
                TypeDefinition = newClass,
                CollectionKind = codeUnionType.CollectionKind,
                IsNullable = codeUnionType.IsNullable,
                ActionOf = codeUnionType.ActionOf,
            };
        }
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
                        if(!currentClass.InnerChildElements.OfType<CodeClass>().Any(x => x.Name.Equals(returnType.TypeDefinition.Name))) {
                            currentClass.AddInnerClass(returnType.TypeDefinition);
                        }
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
