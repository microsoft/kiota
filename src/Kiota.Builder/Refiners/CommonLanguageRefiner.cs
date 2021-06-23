using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeClass;

namespace Kiota.Builder.Refiners {
    public abstract class CommonLanguageRefiner : ILanguageRefiner
    {
        protected CommonLanguageRefiner(GenerationConfiguration configuration) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public abstract void Refine(CodeNamespace generatedCode);
        internal const string GetterPrefix = "get-";
        internal const string SetterPrefix = "set-";
        protected static void CorrectCoreTypesForBackingStoreUsings(CodeElement currentElement, string storeNamespace) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)
                && currentClass.StartBlock is CodeClass.Declaration currentDeclaration) {
                foreach(var backingStoreUsing in currentDeclaration.Usings.Where(x => "Microsoft.Kiota.Abstractions.Store".Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase))) {
                    if(backingStoreUsing?.Declaration != null) {
                        backingStoreUsing.Name = backingStoreUsing.Name[1..]; // removing the "I"
                        backingStoreUsing.Declaration.Name = storeNamespace;
                    }
                }
                var backedModelImplements = currentDeclaration.Implements.FirstOrDefault(x => "IBackedModel".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                if(backedModelImplements != null)
                    backedModelImplements.Name = backedModelImplements.Name[1..]; //removing the "I"
            }
            CrawlTree(currentElement, (x) => CorrectCoreTypesForBackingStoreUsings(x, storeNamespace));
        }
        private static bool DoesAnyParentHaveAPropertyWithDefaultValue(CodeClass current) {
            if(current.StartBlock is CodeClass.Declaration currentDeclaration &&
                currentDeclaration.Inherits?.TypeDefinition is CodeClass parentClass) {
                    if(parentClass.GetChildElements(true).OfType<CodeProperty>().Any(x => !string.IsNullOrEmpty(x.DefaultValue)))
                        return true;
                    else
                        return DoesAnyParentHaveAPropertyWithDefaultValue(parentClass);
            } else
                return false;
        }
        protected static void AddGetterAndSetterMethods(CodeElement current, HashSet<CodePropertyKind> propertyKindsToAddAccessors, bool removeProperty, bool parameterAsOptional) {
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
        protected static void AddConstructorsForDefaultValues(CodeElement current, bool addIfInherited) {
            if(current is CodeClass currentClass && 
                (currentClass.GetChildElements(true).OfType<CodeProperty>().Any(x => !string.IsNullOrEmpty(x.DefaultValue)) ||
                addIfInherited && DoesAnyParentHaveAPropertyWithDefaultValue(currentClass)) &&
                !currentClass.GetChildElements(true).OfType<CodeMethod>().Any(x => x.IsOfKind(CodeMethodKind.ClientConstructor)))
                currentClass.AddMethod(new CodeMethod(current) {
                    Name = "constructor",
                    MethodKind = CodeMethodKind.Constructor,
                    ReturnType = new CodeType(current) {
                        Name = "void"
                    },
                    IsAsync = false,
                    Description = $"Instantiates a new {current.Name} and sets the default values."
                });
            CrawlTree(current, x => AddConstructorsForDefaultValues(x, addIfInherited));
        }
        protected static void ReplaceReservedNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement) {
            if(current is CodeClass currentClass && currentClass.StartBlock is CodeClass.Declaration currentDeclaration)
                currentDeclaration.Usings
                                    .Select(x => x.Declaration)
                                    .Where(x => x != null && !x.IsExternal)
                                    .Join(provider.ReservedNames, x => x.Name, y => y, (x, y) => x)
                                    .ToList()
                                    .ForEach(x => {
                                        x.Name = replacement.Invoke(x.Name);
                                    });
            if(provider.ReservedNames.Contains(current.Name))
                current.Name = replacement.Invoke(current.Name);

            CrawlTree(current, x => ReplaceReservedNames(x, provider, replacement));
        }

        protected static void AddDefaultImports(CodeElement current, Tuple<string, string>[] defaultNamespaces, Tuple<string, string>[] defaultNamespacesForModels, Tuple<string, string>[] defaultNamespacesForRequestBuilders, Tuple<string, string>[] defaultSymbolsForApiClient) {
            if(current is CodeClass currentClass) {
                Func<Tuple<string, string>, CodeUsing> usingSelector = x => {
                                                            var nUsing = new CodeUsing(currentClass) { 
                                                                Name = x.Item1,
                                                            };
                                                            nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                                                            return nUsing;
                                                        };
                if(currentClass.IsOfKind(CodeClassKind.Model))
                    currentClass.AddUsing(defaultNamespaces.Union(defaultNamespacesForModels)
                                            .Select(usingSelector).ToArray());
                if(currentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
                    var usingsToAdd = defaultNamespaces.Union(defaultNamespacesForRequestBuilders);
                    if(currentClass.GetChildElements(true).OfType<CodeMethod>().Any(x => x.IsOfKind(CodeMethodKind.ClientConstructor)))
                        usingsToAdd = usingsToAdd.Union(defaultSymbolsForApiClient);
                    currentClass.AddUsing(usingsToAdd.Select(usingSelector).ToArray());
                }
            }
            CrawlTree(current, c => AddDefaultImports(c, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders, defaultSymbolsForApiClient));
        }
        private const string binaryType = "binary";
        protected static void ReplaceBinaryByNativeType(CodeElement currentElement, string symbol, string ns, bool addDeclaration = false) {
            if(currentElement is CodeMethod currentMethod) {
                var parentClass = currentMethod.Parent as CodeClass;
                var shouldInsertUsing = false;
                if(binaryType.Equals(currentMethod.ReturnType?.Name)) {
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
                        Name = addDeclaration ? symbol : ns,
                    };
                    if(addDeclaration)
                        newUsing.Declaration = new CodeType(newUsing) {
                            Name = ns,
                            IsExternal = true,
                        };
                    parentClass.AddUsing(newUsing);
                }
            }
            CrawlTree(currentElement, c => ReplaceBinaryByNativeType(c, symbol, ns, addDeclaration));
        }
        private const string pathSegmentPropertyName = "pathSegment";
        // temporary patch of type to it resolves as the builder sets types we didn't generate to entity
        protected static void FixReferencesToEntityType(CodeElement currentElement, CodeClass entityClass = null){
            if(entityClass == null && currentElement is CodeNamespace currentNamespace)
                entityClass = currentNamespace.FindChildByName<CodeClass>("entity");

            if(currentElement is CodeMethod currentMethod 
                && currentMethod.ReturnType is CodeType currentReturnType
                && currentReturnType.Name.Equals("entity", StringComparison.OrdinalIgnoreCase)
                && currentReturnType.TypeDefinition == null)
                currentReturnType.TypeDefinition = entityClass;

            CrawlTree(currentElement, (c) => FixReferencesToEntityType(c, entityClass));
        }
        protected static void ConvertUnionTypesToWrapper(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod) {
                if(currentMethod.ReturnType is CodeUnionType currentUnionType)
                    currentMethod.ReturnType = ConvertUnionTypeToWrapper(currentMethod.Parent as CodeClass, currentUnionType);
                if(currentMethod.Parameters.Any(x => x.Type is CodeUnionType))
                    foreach(var currentParameter in currentMethod.Parameters.Where(x => x.Type is CodeUnionType))
                        currentParameter.Type = ConvertUnionTypeToWrapper(currentMethod.Parent as CodeClass, currentParameter.Type as CodeUnionType);
            }
            else if (currentElement is CodeIndexer currentIndexer && currentIndexer.ReturnType is CodeUnionType currentUnionType)
                currentIndexer.ReturnType = ConvertUnionTypeToWrapper(currentIndexer.Parent as CodeClass, currentUnionType);
            else if(currentElement is CodeProperty currentProperty && currentProperty.Type is CodeUnionType currentPropUnionType)
                currentProperty.Type = ConvertUnionTypeToWrapper(currentProperty.Parent as CodeClass, currentPropUnionType);

            CrawlTree(currentElement, ConvertUnionTypesToWrapper);
        }
        private static CodeTypeBase ConvertUnionTypeToWrapper(CodeClass codeClass, CodeUnionType codeUnionType)
        {
            if(codeClass == null) throw new ArgumentNullException(nameof(codeClass));
            if(codeUnionType == null) throw new ArgumentNullException(nameof(codeUnionType));
            var newClass = new CodeClass(codeClass) {
                Name = codeUnionType.Name,
                Description = $"Union type wrapper for classes {codeUnionType.Types.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}"
            };
            newClass.AddProperty(codeUnionType
                                    .Types
                                    .Select(x => new CodeProperty(newClass) {
                                        Name = x.Name,
                                        Type = x,
                                        Description = $"Union type representation for type {x.Name}"
                                    }).ToArray());
            return new CodeType(codeClass) {
                Name = newClass.Name,
                TypeDefinition = newClass,
                CollectionKind = codeUnionType.CollectionKind,
                IsNullable = codeUnionType.IsNullable,
                ActionOf = codeUnionType.ActionOf,
            };
        }
        protected static void MoveClassesWithNamespaceNamesUnderNamespace(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && 
                !string.IsNullOrEmpty(currentClass.Name) &&
                currentClass.Parent is CodeNamespace parentNamespace) {
                var childNamespaceWithClassName = parentNamespace.GetChildElements(true)
                                                                .OfType<CodeNamespace>()
                                                                .FirstOrDefault(x => x.Name
                                                                                    .EndsWith(currentClass.Name, StringComparison.OrdinalIgnoreCase));
                if(childNamespaceWithClassName != null) {
                    parentNamespace.RemoveChildElement(currentClass);
                    childNamespaceWithClassName.AddClass(currentClass);
                }
            }
            CrawlTree(currentElement, MoveClassesWithNamespaceNamesUnderNamespace);
        }
        protected static void ReplaceIndexersByMethodsWithParameter(CodeElement currentElement, CodeNamespace rootNamespace, string methodNameSuffix = default) {
            if(currentElement is CodeIndexer currentIndexer) {
                var currentParentClass = currentElement.Parent as CodeClass;
                currentParentClass.RemoveChildElement(currentElement);
                var pathSegment = currentParentClass
                                    .FindChildByName<CodeProperty>(pathSegmentPropertyName)
                                    ?.DefaultValue;
                if(!string.IsNullOrEmpty(pathSegment))
                    foreach(var returnType in currentIndexer.ReturnType.AllTypes)
                        AddIndexerMethod(rootNamespace,
                                        currentParentClass,
                                        returnType.TypeDefinition as CodeClass,
                                        pathSegment.Trim('\"').TrimStart('/'),
                                        methodNameSuffix,
                                        currentIndexer.Description);
            }
            CrawlTree(currentElement, c => ReplaceIndexersByMethodsWithParameter(c, rootNamespace, methodNameSuffix));
        }
        private static void AddIndexerMethod(CodeElement currentElement, CodeClass targetClass, CodeClass indexerClass, string pathSegment, string methodNameSuffix, string description) {
            if(currentElement is CodeProperty currentProperty && currentProperty.Type.AllTypes.Any(x => x.TypeDefinition == targetClass)) {
                var parentClass = currentElement.Parent as CodeClass;
                var method = new CodeMethod(parentClass) {
                    IsAsync = false,
                    IsStatic = false,
                    Access = AccessModifier.Public,
                    MethodKind = CodeMethodKind.IndexerBackwardCompatibility,
                    Name = pathSegment + methodNameSuffix,
                    Description = description,
                };
                method.ReturnType = new CodeType(method) {
                    IsNullable = false,
                    TypeDefinition = indexerClass,
                    Name = indexerClass.Name,
                };
                method.PathSegment = pathSegment;
                var parameter = new CodeParameter(method) {
                    Name = "id",
                    Optional = false,
                    ParameterKind = CodeParameterKind.Custom,
                    Description = "Unique identifier of the item"
                };
                parameter.Type = new CodeType(parameter) {
                    Name = "String",
                    IsNullable = false,
                    IsExternal = true,
                };
                method.Parameters.Add(parameter);
                parentClass.AddMethod(method);
            }
            CrawlTree(currentElement, c => AddIndexerMethod(c, targetClass, indexerClass, pathSegment, methodNameSuffix, description));
        }
        internal void AddInnerClasses(CodeElement current) {
            if(current is CodeClass currentClass) {
                foreach(var parameter in currentClass.GetChildElements(true).OfType<CodeMethod>().SelectMany(x =>x.Parameters).Where(x => x.Type.ActionOf && x.IsOfKind(CodeParameterKind.QueryParameter))) 
                    foreach(var returnType in parameter.Type.AllTypes) {
                        var innerClass = returnType.TypeDefinition as CodeClass;
                        if(innerClass == null)
                            continue;
                            
                        if(currentClass.FindChildByName<CodeClass>(returnType.TypeDefinition.Name) == null) {
                            currentClass.AddInnerClass(innerClass);
                        }
                        (innerClass.StartBlock as Declaration).Inherits = new CodeType(returnType.TypeDefinition) { Name = "QueryParametersBase", IsExternal = true };
                    }
            }
            CrawlTree(current, AddInnerClasses);
        }
        private static readonly CodeUsingComparer usingComparerWithDeclarations = new CodeUsingComparer(true);
        private static readonly CodeUsingComparer usingComparerWithoutDeclarations = new CodeUsingComparer(false);
        protected readonly GenerationConfiguration _configuration;

        protected static void AddPropertiesAndMethodTypesImports(CodeElement current, bool includeParentNamespaces, bool includeCurrentNamespace, bool compareOnDeclaration) {
            if(current is CodeClass currentClass) {
                var currentClassNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
                var propertiesTypes = currentClass
                                    .GetChildElements(true)
                                    .OfType<CodeProperty>()
                                    .Select(x => x.Type)
                                    .Distinct();
                var methods = currentClass
                                    .GetChildElements(true)
                                    .OfType<CodeMethod>();
                var methodsReturnTypes = methods
                                    .Select(x => x.ReturnType)
                                    .Distinct();
                var methodsParametersTypes = methods
                                    .SelectMany(x => x.Parameters)
                                    .Where(x => x.IsOfKind(CodeParameterKind.Custom))
                                    .Select(x => x.Type)
                                    .Distinct();
                var indexerTypes = currentClass
                                    .GetChildElements(true)
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
        protected static void ReplaceRelativeImportsByImportPath(CodeElement currentElement, char namespaceNameSeparator) {
            if(currentElement is CodeClass currentClass && currentClass.StartBlock is CodeClass.Declaration currentDeclaration
                && currentElement.Parent is CodeNamespace currentNamespace) {
                currentDeclaration.Usings.RemoveAll(x => currentDeclaration.Name.Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase));
                foreach(var codeUsing in currentDeclaration.Usings
                                            .Where(x => (!x.Declaration?.IsExternal) ?? true)) {
                    var relativeImportPath = GetRelativeImportPathForUsing(codeUsing, currentNamespace, namespaceNameSeparator);
                    codeUsing.Name = $"{codeUsing.Declaration?.Name?.ToFirstCharacterUpperCase() ?? codeUsing.Name}";
                    codeUsing.Declaration = new CodeType(codeUsing) {
                        Name = $"{relativeImportPath}{(string.IsNullOrEmpty(relativeImportPath) ? codeUsing.Name : codeUsing.Declaration.Name.ToFirstCharacterLowerCase())}",
                        IsExternal = false,
                    };
                }
            }

            CrawlTree(currentElement, x => ReplaceRelativeImportsByImportPath(x, namespaceNameSeparator));
        }
        private static string GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace, char namespaceNameSeparator) {
            if(codeUsing.Declaration == null)
                return string.Empty;//it's an external import, add nothing
            var typeDef = codeUsing.Declaration.TypeDefinition;

            if(typeDef == null)
                return "./"; // it's relative to the folder, with no declaration (default failsafe)
            else
                return GetImportRelativePathFromNamespaces(currentNamespace, 
                                                        typeDef.GetImmediateParentOfType<CodeNamespace>(), namespaceNameSeparator);
        }
        private static string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace, char namespaceNameSeparator) {
            if(currentNamespace == null)
                throw new ArgumentNullException(nameof(currentNamespace));
            else if (importNamespace == null)
                throw new ArgumentNullException(nameof(importNamespace));
            else if(currentNamespace.Name.Equals(importNamespace.Name, StringComparison.OrdinalIgnoreCase)) // we're in the same namespace
                return "./";
            else
                return GetRelativeImportPathFromSegments(currentNamespace, importNamespace, namespaceNameSeparator);                
        }
        private static string GetRelativeImportPathFromSegments(CodeNamespace currentNamespace, CodeNamespace importNamespace, char namespaceNameSeparator) {
            var currentNamespaceSegements = currentNamespace
                                    .Name
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegments = importNamespace
                                .Name
                                .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegmentsCount = importNamespaceSegments.Length;
            var currentNamespaceSegementsCount = currentNamespaceSegements.Length;
            var deeperMostSegmentIndex = 0;
            while(deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegementsCount)) {
                if(currentNamespaceSegements.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.OrdinalIgnoreCase))
                    deeperMostSegmentIndex++;
                else
                    break;
            }
            if (deeperMostSegmentIndex == currentNamespaceSegementsCount) { // we're in a parent namespace and need to import with a relative path
                return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            } else { // we're in a sub namespace and need to go "up" with dot dots
                var upMoves = currentNamespaceSegementsCount - deeperMostSegmentIndex;
                var upMovesBuilder = new StringBuilder();
                for(var i = 0; i < upMoves; i++)
                    upMovesBuilder.Append("../");
                return upMovesBuilder.ToString() + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            }
        }
        private static string GetRemainingImportPath(IEnumerable<string> remainingSegments) {
            if(remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }
        protected static void CrawlTree(CodeElement currentElement, Action<CodeElement> function) {
            foreach(var childElement in currentElement.GetChildElements())
                function.Invoke(childElement);
        }
    }
}
