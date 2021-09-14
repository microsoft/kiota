using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using static Kiota.Builder.CodeClass;

namespace Kiota.Builder.Refiners {
    public abstract class CommonLanguageRefiner : ILanguageRefiner
    {
        protected CommonLanguageRefiner(GenerationConfiguration configuration) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public abstract void Refine(CodeNamespace generatedCode);
        /// <summary>
        ///     This method adds the imports for the default serializers and deserializers to the api client class.
        ///     It also updates the module names to replace the fully qualified class name by the class name without the namespace.
        /// </summary>
        protected void AddSerializationModulesImport(CodeElement generatedCode, string[] serializationWriterFactoryInterfaceAndRegistrationFullName = default, string[] parseNodeFactoryInterfaceAndRegistrationFullName = default) {
            if(serializationWriterFactoryInterfaceAndRegistrationFullName == null)
                serializationWriterFactoryInterfaceAndRegistrationFullName = Array.Empty<string>();
            if(parseNodeFactoryInterfaceAndRegistrationFullName == null)
                parseNodeFactoryInterfaceAndRegistrationFullName = Array.Empty<string>();
            if(generatedCode is CodeMethod currentMethod &&
                currentMethod.IsOfKind(CodeMethodKind.ClientConstructor) &&
                currentMethod.Parent is CodeClass currentClass &&
                currentClass.StartBlock is CodeClass.Declaration declaration) {
                    var cumulatedSymbols = currentMethod.DeserializerModules
                                                        .Union(currentMethod.SerializerModules)
                                                        .Union(serializationWriterFactoryInterfaceAndRegistrationFullName)
                                                        .Union(parseNodeFactoryInterfaceAndRegistrationFullName)
                                                        .Where(x => !string.IsNullOrEmpty(x))
                                                        .ToList();
                    currentMethod.DeserializerModules = currentMethod.DeserializerModules.Select(x => x.Split('.').Last()).ToList();
                    currentMethod.SerializerModules = currentMethod.SerializerModules.Select(x => x.Split('.').Last()).ToList();
                    declaration.Usings.AddRange(cumulatedSymbols.Select(x => new CodeUsing(currentClass){
                        Name = x.Split('.').Last(),
                        Declaration = new CodeType(currentClass) {
                            Name = x.Split('.').SkipLast(1).Aggregate((x, y) => $"{x}.{y}"),
                            IsExternal = true,
                        }
                    }));
                    return;
                }
            CrawlTree(generatedCode, x => AddSerializationModulesImport(x, serializationWriterFactoryInterfaceAndRegistrationFullName, parseNodeFactoryInterfaceAndRegistrationFullName));
        }
        protected static void ReplaceDefaultSerializationModules(CodeElement generatedCode, params string[] moduleNames) {
            if(ReplaceSerializationModules(generatedCode, x => x.SerializerModules, "Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory", moduleNames))
                return;
            CrawlTree(generatedCode, (x) => ReplaceDefaultSerializationModules(x, moduleNames));
        }
        protected static void ReplaceDefaultDeserializationModules(CodeElement generatedCode, params string[] moduleNames) {
            if(ReplaceSerializationModules(generatedCode, x => x.DeserializerModules, "Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory", moduleNames))
                return;
            CrawlTree(generatedCode, (x) => ReplaceDefaultDeserializationModules(x, moduleNames));
        }
        private static bool ReplaceSerializationModules(CodeElement generatedCode, Func<CodeMethod, List<string>> propertyGetter, string initialName, params string[] moduleNames) {
            if(generatedCode is CodeMethod currentMethod &&
                currentMethod.IsOfKind(CodeMethodKind.ClientConstructor)) {
                    var modules = propertyGetter.Invoke(currentMethod);
                    if(modules.Count == 1 &&
                        modules.Any(x => initialName.Equals(x, StringComparison.OrdinalIgnoreCase))) {
                        modules.Clear();
                        modules.AddRange(moduleNames);
                        return true;
                }
            }

            return false;
        }
        internal const string GetterPrefix = "get-";
        internal const string SetterPrefix = "set-";
        protected static void CorrectCoreTypesForBackingStore(CodeElement currentElement, string storeNamespace, string defaultPropertyValue) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder)
                && currentClass.StartBlock is CodeClass.Declaration currentDeclaration) {
                CorrectCoreTypeForBackingStoreUsings(currentDeclaration, storeNamespace, defaultPropertyValue);
                var backedModelImplements = currentDeclaration.Implements.FirstOrDefault(x => "IBackedModel".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                if(backedModelImplements != null)
                    backedModelImplements.Name = backedModelImplements.Name[1..]; //removing the "I"
                var backingStoreProperty = currentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.BackingStore));
                if(backingStoreProperty != null)
                    backingStoreProperty.DefaultValue = defaultPropertyValue;
                
            }
            CrawlTree(currentElement, (x) => CorrectCoreTypesForBackingStore(x, storeNamespace, defaultPropertyValue));
        }
        private static void CorrectCoreTypeForBackingStoreUsings(CodeClass.Declaration currentDeclaration, string storeNamespace, string defaultPropertyValue) {
            foreach(var backingStoreUsing in currentDeclaration.Usings.Where(x => "Microsoft.Kiota.Abstractions.Store".Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase))) {
                if(backingStoreUsing?.Declaration != null) {
                    if(backingStoreUsing.Name.StartsWith("I"))
                        backingStoreUsing.Name = backingStoreUsing.Name[1..]; // removing the "I"
                    backingStoreUsing.Declaration.Name = storeNamespace;
                }
            }
            var defaultValueUsing = currentDeclaration
                                        .Usings
                                        .FirstOrDefault(x => "BackingStoreFactorySingleton".Equals(x.Name, StringComparison.OrdinalIgnoreCase) &&
                                            x.Declaration != null &&
                                            x.Declaration.IsExternal &&
                                            x.Declaration.Name.Equals(storeNamespace, StringComparison.OrdinalIgnoreCase));
            if(defaultValueUsing != null)
                defaultValueUsing.Name = defaultPropertyValue.Split('.').First();
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
                    ReturnType = currentProperty.Type.Clone() as CodeTypeBase,
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
                        Name = "void",
                        IsNullable = false,
                    };
                    setter.Parameters.Add(new(setter) {
                        Name = "value",
                        ParameterKind = CodeParameterKind.SetterValue,
                        Description = $"Value to set for the {current.Name} property.",
                        Optional = parameterAsOptional,
                        Type = currentProperty.Type.Clone() as CodeTypeBase,
                    });
                }
            }
            CrawlTree(current, x => AddGetterAndSetterMethods(x, propertyKindsToAddAccessors, removeProperty, parameterAsOptional));
        }
        protected static void AddConstructorsForDefaultValues(CodeElement current, bool addIfInherited) {
            if(current is CodeClass currentClass &&
                !currentClass.IsOfKind(CodeClassKind.RequestBuilder, CodeClassKind.QueryParameters) &&
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
        protected static void ReplaceReservedNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement, HashSet<Type> codeElementExceptions = null) {
            if(current is CodeClass currentClass && currentClass.StartBlock is CodeClass.Declaration currentDeclaration)
                currentDeclaration.Usings
                                    .Select(x => x.Declaration)
                                    .Where(x => x != null && !x.IsExternal)
                                    .Join(provider.ReservedNames, x => x.Name, y => y, (x, y) => x)
                                    .ToList()
                                    .ForEach(x => {
                                        x.Name = replacement.Invoke(x.Name);
                                    });

            // Check if the current name meets the following conditions to be replaced
            // 1. In the list of reserved names
            // 2. If it is a reserved name, make sure that the CodeElement type is worth replacing(not on the blacklist)
            if (provider.ReservedNames.Contains(current.Name) && (!codeElementExceptions?.Contains(current.GetType()) ?? true))
                current.Name = replacement.Invoke(current.Name);

            CrawlTree(current, x => ReplaceReservedNames(x, provider, replacement, codeElementExceptions));
        }
        protected static void AddDefaultImports(CodeElement current, Tuple<string, string>[] defaultNamespaces, Tuple<string, string>[] defaultNamespacesForModels, Tuple<string, string>[] defaultNamespacesForRequestBuilders) {
            if(current is CodeClass currentClass) {
                CodeUsing usingSelector(Tuple<string, string> x)
                {
                    var nUsing = new CodeUsing(currentClass)
                    {
                        Name = x.Item1,
                    };
                    nUsing.Declaration = new CodeType(nUsing) { Name = x.Item2, IsExternal = true };
                    return nUsing;
                }
                if (currentClass.IsOfKind(CodeClassKind.Model))
                    currentClass.AddUsing(defaultNamespaces.Union(defaultNamespacesForModels)
                                            .Select(usingSelector).ToArray());
                if(currentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
                    var usingsToAdd = defaultNamespaces.Union(defaultNamespacesForRequestBuilders);
                    currentClass.AddUsing(usingsToAdd.Select(usingSelector).ToArray());
                }
            }
            CrawlTree(current, c => AddDefaultImports(c, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders));
        }
        private const string BinaryType = "binary";
        protected static void ReplaceBinaryByNativeType(CodeElement currentElement, string symbol, string ns, bool addDeclaration = false) {
            if(currentElement is CodeMethod currentMethod) {
                var parentClass = currentMethod.Parent as CodeClass;
                var shouldInsertUsing = false;
                if(BinaryType.Equals(currentMethod.ReturnType?.Name)) {
                    currentMethod.ReturnType.Name = symbol;
                    shouldInsertUsing = true;
                }
                var binaryParameter = currentMethod.Parameters.FirstOrDefault(x => x.Type.Name.Equals(BinaryType));
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
        private const string PathSegmentPropertyName = "pathSegment";
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
        protected static void ReplaceIndexersByMethodsWithParameter(CodeElement currentElement, CodeNamespace rootNamespace, bool parameterNullable, string methodNameSuffix = default) {
            if(currentElement is CodeIndexer currentIndexer) {
                var currentParentClass = currentElement.Parent as CodeClass;
                currentParentClass.RemoveChildElement(currentElement);
                var pathSegment = currentParentClass
                                    .FindChildByName<CodeProperty>(PathSegmentPropertyName)
                                    ?.DefaultValue;
                if(!string.IsNullOrEmpty(pathSegment))
                    foreach(var returnType in currentIndexer.ReturnType.AllTypes)
                        AddIndexerMethod(rootNamespace,
                                        currentParentClass,
                                        returnType.TypeDefinition as CodeClass,
                                        pathSegment.Trim('\"').TrimStart('/'),
                                        methodNameSuffix,
                                        currentIndexer.Description,
                                        parameterNullable);
            }
            CrawlTree(currentElement, c => ReplaceIndexersByMethodsWithParameter(c, rootNamespace, parameterNullable, methodNameSuffix));
        }
        private static void AddIndexerMethod(CodeElement currentElement, CodeClass targetClass, CodeClass indexerClass, string pathSegment, string methodNameSuffix, string description, bool parameterNullable) {
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
                    IsNullable = parameterNullable,
                    IsExternal = true,
                };
                method.Parameters.Add(parameter);
                parentClass.AddMethod(method);
            }
            CrawlTree(currentElement, c => AddIndexerMethod(c, targetClass, indexerClass, pathSegment, methodNameSuffix, description, parameterNullable));
        }
        internal void AddInnerClasses(CodeElement current, bool prefixClassNameWithParentName) {
            if(current is CodeClass currentClass) {
                foreach(var innerClass in currentClass
                                        .GetChildElements(true)
                                        .OfType<CodeMethod>()
                                        .SelectMany(x => x.Parameters)
                                        .Where(x => x.Type.ActionOf && x.IsOfKind(CodeParameterKind.QueryParameter))
                                        .SelectMany(x => x.Type.AllTypes)
                                        .Select(x => x.TypeDefinition)
                                        .OfType<CodeClass>()) {
                    if(prefixClassNameWithParentName && !innerClass.Name.StartsWith(currentClass.Name, StringComparison.OrdinalIgnoreCase)) {
                        innerClass.Name = $"{currentClass.Name}{innerClass.Name}";
                        innerClass.StartBlock.Name = innerClass.Name;
                    }
                    
                    if(currentClass.FindChildByName<CodeClass>(innerClass.Name) == null) {
                        currentClass.AddInnerClass(innerClass);
                    }
                    (innerClass.StartBlock as Declaration).Inherits = new CodeType(innerClass) { Name = "QueryParametersBase", IsExternal = true };
                }
            }
            CrawlTree(current, x => AddInnerClasses(x, prefixClassNameWithParentName));
        }
        private static readonly CodeUsingComparer usingComparerWithDeclarations = new(true);
        private static readonly CodeUsingComparer usingComparerWithoutDeclarations = new(false);
        protected readonly GenerationConfiguration _configuration;

        protected static void AddPropertiesAndMethodTypesImports(CodeElement current, bool includeParentNamespaces, bool includeCurrentNamespace, bool compareOnDeclaration) {
            if(current is CodeClass currentClass) {
                var currentClassNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
                var currentClassChildren = currentClass.GetChildElements(true);
                var propertiesTypes = currentClassChildren
                                    .OfType<CodeProperty>()
                                    .Select(x => x.Type)
                                    .Distinct();
                var methods = currentClassChildren
                                    .OfType<CodeMethod>();
                var methodsReturnTypes = methods
                                    .Select(x => x.ReturnType)
                                    .Distinct();
                var methodsParametersTypes = methods
                                    .SelectMany(x => x.Parameters)
                                    .Where(x => x.IsOfKind(CodeParameterKind.Custom, CodeParameterKind.RequestBody))
                                    .Select(x => x.Type)
                                    .Distinct();
                var indexerTypes = currentClassChildren
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
        protected static void ReplaceRelativeImportsByImportPath(CodeElement currentElement, char namespaceNameSeparator, string namespacePrefix) {
            ReplaceRelativeImportsByImportPath(currentElement, namespaceNameSeparator, namespacePrefix?.Length ?? 0);
        }
        protected static void ReplaceRelativeImportsByImportPath(CodeElement currentElement, char namespaceNameSeparator, int prefixLength) {
            if(currentElement is CodeClass currentClass && currentClass.StartBlock is Declaration currentDeclaration
                && currentElement.Parent is CodeNamespace currentNamespace) {
                currentDeclaration.Usings.RemoveAll(x => currentDeclaration.Name.Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase));
                foreach(var codeUsing in currentDeclaration.Usings
                                            .Where(x => (!x.Declaration?.IsExternal) ?? true)) {
                    var relativeImportPath = GetRelativeImportPathForUsing(codeUsing, currentNamespace, namespaceNameSeparator, prefixLength);
                    codeUsing.Name = $"{codeUsing.Declaration?.Name?.ToFirstCharacterUpperCase() ?? codeUsing.Name}";
                    codeUsing.Declaration = new CodeType(codeUsing) {
                        Name = $"{relativeImportPath}{(string.IsNullOrEmpty(relativeImportPath) ? codeUsing.Name : codeUsing.Declaration.Name.ToFirstCharacterLowerCase())}",
                        IsExternal = false,
                    };
                }
            }

            CrawlTree(currentElement, x => ReplaceRelativeImportsByImportPath(x, namespaceNameSeparator, prefixLength));
        }
        private static string GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace, char namespaceNameSeparator, int prefixLength) {
            if(codeUsing.Declaration == null)
                return string.Empty;//it's an external import, add nothing
            var typeDef = codeUsing.Declaration.TypeDefinition;

            if(typeDef == null)
                return "./"; // it's relative to the folder, with no declaration (default failsafe)
            else
                return GetImportRelativePathFromNamespaces(currentNamespace, 
                                                        typeDef.GetImmediateParentOfType<CodeNamespace>(), namespaceNameSeparator, prefixLength);
        }
        private static string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace, char namespaceNameSeparator, int prefixLength) {
            if(currentNamespace == null)
                throw new ArgumentNullException(nameof(currentNamespace));
            else if (importNamespace == null)
                throw new ArgumentNullException(nameof(importNamespace));
            else if(currentNamespace.Name.Equals(importNamespace.Name, StringComparison.OrdinalIgnoreCase)) // we're in the same namespace
                return "./";
            else
                return GetRelativeImportPathFromSegments(currentNamespace, importNamespace, namespaceNameSeparator, prefixLength);                
        }
        private static string GetRelativeImportPathFromSegments(CodeNamespace currentNamespace, CodeNamespace importNamespace, char namespaceNameSeparator, int prefixLength) {
            var currentNamespaceSegments = currentNamespace
                                    .Name[prefixLength..]
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegments = importNamespace
                                .Name[prefixLength..]
                                .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegmentsCount = importNamespaceSegments.Length;
            var currentNamespaceSegmentsCount = currentNamespaceSegments.Length;
            var deeperMostSegmentIndex = 0;
            while(deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegmentsCount)) {
                if(currentNamespaceSegments.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.OrdinalIgnoreCase))
                    deeperMostSegmentIndex++;
                else
                    break;
            }
            if (deeperMostSegmentIndex == currentNamespaceSegmentsCount) { // we're in a parent namespace and need to import with a relative path
                return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            } else { // we're in a sub namespace and need to go "up" with dot dots
                var upMoves = currentNamespaceSegmentsCount - deeperMostSegmentIndex;
                var pathSegmentSeparator = upMoves > 0 ? "/" : string.Empty;
                return string.Join("/", Enumerable.Repeat("..", upMoves)) +
                        pathSegmentSeparator +
                        GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            }
        }
        private static string GetRemainingImportPath(IEnumerable<string> remainingSegments) {
            if(remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }
        protected static void PatchHeaderParametersType(CodeElement currentElement, string newTypeName) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Headers)))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Headers))
                                        .ToList()
                                        .ForEach(x => x.Type.Name = newTypeName);
            CrawlTree(currentElement, (x) => PatchHeaderParametersType(x, newTypeName));
        }
        protected static void CrawlTree(CodeElement currentElement, Action<CodeElement> function) {
            foreach(var childElement in currentElement.GetChildElements())
                function.Invoke(childElement);
        }
        protected static void CorrectCoreType(CodeElement currentElement, Action<CodeMethod> correctMethodType, Action<CodeProperty> correctPropertyType) {
            switch(currentElement) {
                case CodeProperty property:
                    correctPropertyType.Invoke(property);
                    break;
                case CodeMethod method:
                    correctMethodType.Invoke(method);
                    break;
            }
            CrawlTree(currentElement, x => CorrectCoreType(x, correctMethodType, correctPropertyType));
        }
        protected static void MakeModelPropertiesNullable(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass &&
                currentClass.IsOfKind(CodeClassKind.Model))
                currentClass.GetChildElements(true)
                            .OfType<CodeProperty>()
                            .Where(x => x.IsOfKind(CodePropertyKind.Custom))
                            .ToList()
                            .ForEach(x => x.Type.IsNullable = true);
            CrawlTree(currentElement, MakeModelPropertiesNullable);
        }
    }
}
