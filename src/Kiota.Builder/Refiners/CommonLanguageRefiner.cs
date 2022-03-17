﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
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
    protected void AddSerializationModulesImport(CodeElement generatedCode, string[] serializationWriterFactoryInterfaceAndRegistrationFullName = default, string[] parseNodeFactoryInterfaceAndRegistrationFullName = default, char separator = '.') {
        if(serializationWriterFactoryInterfaceAndRegistrationFullName == null)
            serializationWriterFactoryInterfaceAndRegistrationFullName = Array.Empty<string>();
        if(parseNodeFactoryInterfaceAndRegistrationFullName == null)
            parseNodeFactoryInterfaceAndRegistrationFullName = Array.Empty<string>();
        if(generatedCode is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.ClientConstructor) &&
            currentMethod.Parent is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration declaration) {
                var cumulatedSymbols = currentMethod.DeserializerModules
                                                    .Union(currentMethod.SerializerModules)
                                                    .Union(serializationWriterFactoryInterfaceAndRegistrationFullName)
                                                    .Union(parseNodeFactoryInterfaceAndRegistrationFullName)
                                                    .Where(x => !string.IsNullOrEmpty(x))
                                                    .ToList();
                currentMethod.DeserializerModules = currentMethod.DeserializerModules.Select(x => x.Split(separator).Last()).ToList();
                currentMethod.SerializerModules = currentMethod.SerializerModules.Select(x => x.Split(separator).Last()).ToList();
                declaration.AddUsings(cumulatedSymbols.Select(x => new CodeUsing {
                    Name = x.Split(separator).Last(),
                    Declaration = new CodeType {
                        Name = x.Split(separator).SkipLast(1).Aggregate((x, y) => $"{x}{separator}{y}"),
                        IsExternal = true,
                    }
                }).ToArray());
                return;
            }
        CrawlTree(generatedCode, x => AddSerializationModulesImport(x, serializationWriterFactoryInterfaceAndRegistrationFullName, parseNodeFactoryInterfaceAndRegistrationFullName, separator));
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
    protected static void CorrectCoreTypesForBackingStore(CodeElement currentElement, string defaultPropertyValue) {
        if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder)
            && currentClass.StartBlock is ClassDeclaration currentDeclaration) {
            var backedModelImplements = currentDeclaration.Implements.FirstOrDefault(x => "IBackedModel".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
            if(backedModelImplements != null)
                backedModelImplements.Name = backedModelImplements.Name[1..]; //removing the "I"
            var backingStoreProperty = currentClass.GetPropertyOfKind(CodePropertyKind.BackingStore);
            if(backingStoreProperty != null)
                backingStoreProperty.DefaultValue = defaultPropertyValue;
            
        }
        CrawlTree(currentElement, (x) => CorrectCoreTypesForBackingStore(x, defaultPropertyValue));
    }
    private static bool DoesAnyParentHaveAPropertyWithDefaultValue(CodeClass current) {
        if(current.StartBlock is ClassDeclaration currentDeclaration &&
            currentDeclaration.Inherits?.TypeDefinition is CodeClass parentClass) {
                if(parentClass.Properties.Any(x => !string.IsNullOrEmpty(x.DefaultValue)))
                    return true;
                else
                    return DoesAnyParentHaveAPropertyWithDefaultValue(parentClass);
        } else
            return false;
    }
    protected static void AddGetterAndSetterMethods(CodeElement current, HashSet<CodePropertyKind> propertyKindsToAddAccessors, bool removeProperty, bool parameterAsOptional, string getterPrefix, string setterPrefix) {
        if(!(propertyKindsToAddAccessors?.Any() ?? true)) return;
        if(current is CodeProperty currentProperty &&
            propertyKindsToAddAccessors.Contains(currentProperty.Kind) &&
            current.Parent is CodeClass parentClass &&
            !parentClass.IsOfKind(CodeClassKind.QueryParameters)) {
            if(removeProperty && currentProperty.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.AdditionalData)) // we never want to remove backing stores
                parentClass.RemoveChildElement(currentProperty);
            else {
                currentProperty.Access = AccessModifier.Private;
                currentProperty.NamePrefix = "_";
            }
            var isSerializationNameNullOrEmpty = string.IsNullOrEmpty(currentProperty.SerializationName);
            var propertyOriginalName = (isSerializationNameNullOrEmpty ? current.Name : currentProperty.SerializationName)
                                        .ToFirstCharacterLowerCase();
            var accessorName = (currentProperty.IsNameEscaped && !isSerializationNameNullOrEmpty ? currentProperty.SerializationName : current.Name)
                                .ToFirstCharacterUpperCase();
            currentProperty.Getter = parentClass.AddMethod(new CodeMethod {
                Name = $"get-{accessorName}",
                Access = AccessModifier.Public,
                IsAsync = false,
                Kind = CodeMethodKind.Getter,
                ReturnType = currentProperty.Type.Clone() as CodeTypeBase,
                Description = $"Gets the {propertyOriginalName} property value. {currentProperty.Description}",
                AccessedProperty = currentProperty,
            }).First();
            currentProperty.Getter.Name = $"{getterPrefix}{accessorName}"; // so we don't get an exception for duplicate names when no prefix
            if(!currentProperty.ReadOnly) {
                var setter = parentClass.AddMethod(new CodeMethod {
                    Name = $"set-{accessorName}",
                    Access = AccessModifier.Public,
                    IsAsync = false,
                    Kind = CodeMethodKind.Setter,
                    Description = $"Sets the {propertyOriginalName} property value. {currentProperty.Description}",
                    AccessedProperty = currentProperty,
                    ReturnType = new CodeType {
                        Name = "void",
                        IsNullable = false,
                        IsExternal = true,
                    },
                }).First();
                setter.Name = $"{setterPrefix}{accessorName}"; // so we don't get an exception for duplicate names when no prefix
                currentProperty.Setter = setter;
                
                setter.AddParameter(new CodeParameter {
                    Name = "value",
                    Kind = CodeParameterKind.SetterValue,
                    Description = $"Value to set for the {current.Name} property.",
                    Optional = parameterAsOptional,
                    Type = currentProperty.Type.Clone() as CodeTypeBase,
                });
            }
        }
        CrawlTree(current, x => AddGetterAndSetterMethods(x, propertyKindsToAddAccessors, removeProperty, parameterAsOptional, getterPrefix, setterPrefix));
    }
    protected static void AddConstructorsForDefaultValues(CodeElement current, bool addIfInherited, bool forceAdd = false) {
        if(current is CodeClass currentClass &&
            !currentClass.IsOfKind(CodeClassKind.RequestBuilder, CodeClassKind.QueryParameters) &&
            (forceAdd ||
            currentClass.Properties.Any(x => !string.IsNullOrEmpty(x.DefaultValue)) ||
            addIfInherited && DoesAnyParentHaveAPropertyWithDefaultValue(currentClass)) &&
            !currentClass.Methods.Any(x => x.IsOfKind(CodeMethodKind.ClientConstructor)))
            currentClass.AddMethod(new CodeMethod {
                Name = "constructor",
                Kind = CodeMethodKind.Constructor,
                ReturnType = new CodeType {
                    Name = "void"
                },
                IsAsync = false,
                Description = $"Instantiates a new {current.Name} and sets the default values."
            });
        CrawlTree(current, x => AddConstructorsForDefaultValues(x, addIfInherited, forceAdd));
    }
    protected static void ReplaceReservedNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement, HashSet<Type> codeElementExceptions = null, Func<CodeElement, bool> shouldReplaceCallback = null) {
        var shouldReplace = shouldReplaceCallback?.Invoke(current) ?? true;
        var isNotInExceptions = !codeElementExceptions?.Contains(current.GetType()) ?? true;
        if(current is CodeClass currentClass && 
            isNotInExceptions &&
            shouldReplace &&
            currentClass.StartBlock is ClassDeclaration currentDeclaration)
            ReplaceReservedCodeUsings(currentDeclaration, provider, replacement);
        else if(current is CodeNamespace currentNamespace &&
            isNotInExceptions &&
            shouldReplace &&
            !string.IsNullOrEmpty(currentNamespace.Name))
            ReplaceReservedNamespaceSegments(currentNamespace, provider, replacement);
        else if(current is CodeMethod currentMethod &&
            isNotInExceptions &&
            shouldReplace) {
            if(currentMethod.ReturnType is CodeType returnType &&
                !returnType.IsExternal &&
                provider.ReservedNames.Contains(returnType.Name))
                returnType.Name = replacement.Invoke(returnType.Name);
            if(provider.ReservedNames.Contains(currentMethod.Name))
                currentMethod.Name = replacement.Invoke(currentMethod.Name);
            if(currentMethod.ErrorMappings.Select(x => x.Value.Name).Any(x => provider.ReservedNames.Contains(x)))
                ReplaceMappingNames(currentMethod.ErrorMappings, provider, replacement);
            if(currentMethod.DiscriminatorMappings.Select(x => x.Value.Name).Any(x => provider.ReservedNames.Contains(x)))
                ReplaceMappingNames(currentMethod.DiscriminatorMappings, provider, replacement);
            ReplaceReservedParameterNamesTypes(currentMethod, provider, replacement);
        } else if (current is CodeProperty currentProperty &&
                isNotInExceptions &&
                shouldReplace &&
                currentProperty.Type is CodeType propertyType &&
                !propertyType.IsExternal &&
                provider.ReservedNames.Contains(currentProperty.Type.Name))
            propertyType.Name = replacement.Invoke(propertyType.Name);
        // Check if the current name meets the following conditions to be replaced
        // 1. In the list of reserved names
        // 2. If it is a reserved name, make sure that the CodeElement type is worth replacing(not on the blocklist)
        // 3. There's not a very specific condition preventing from replacement
        if (provider.ReservedNames.Contains(current.Name) &&
            isNotInExceptions &&
            shouldReplace) {
            if(current is CodeProperty currentProperty &&
                currentProperty.IsOfKind(CodePropertyKind.Custom)) {
                currentProperty.SerializationName = currentProperty.Name;
                currentProperty.IsNameEscaped = true;
            }
            current.Name = replacement.Invoke(current.Name);
        }

        CrawlTree(current, x => ReplaceReservedNames(x, provider, replacement, codeElementExceptions, shouldReplaceCallback));
    }
    private static void ReplaceReservedCodeUsings(ClassDeclaration currentDeclaration, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        currentDeclaration.Usings
                        .Select(x => x.Declaration)
                        .Where(x => x != null && !x.IsExternal)
                        .Join(provider.ReservedNames, x => x.Name, y => y, (x, y) => x)
                        .ToList()
                        .ForEach(x => {
                            x.Name = replacement.Invoke(x.Name);
                        });
    }
    private static void ReplaceReservedNamespaceSegments(CodeNamespace currentNamespace, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        var segments = currentNamespace.Name.Split('.');
        if(segments.Any(x => provider.ReservedNames.Contains(x)))
            currentNamespace.Name = segments.Select(x => provider.ReservedNames.Contains(x) ?
                                                            replacement.Invoke(x) :
                                                            x)
                                            .Aggregate((x, y) => $"{x}.{y}");
    }
    private static void ReplaceMappingNames(IEnumerable<KeyValuePair<string, CodeTypeBase>> mappings, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        mappings.Where(x => provider.ReservedNames.Contains(x.Value.Name))
                                        .ToList()
                                        .ForEach(x => x.Value.Name = replacement.Invoke(x.Value.Name));
    }
    private static void ReplaceReservedParameterNamesTypes(CodeMethod currentMethod, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        currentMethod.Parameters.Where(x => x.Type is CodeType parameterType &&
                                            !parameterType.IsExternal &&
                                            provider.ReservedNames.Contains(parameterType.Name))
                                            .ToList()
                                            .ForEach(x => {
                                                x.Type.Name = replacement.Invoke(x.Type.Name);
                                            });
    }
    private static IEnumerable<CodeUsing> usingSelector(AdditionalUsingEvaluator x) =>
    x.ImportSymbols.Select(y => 
        new CodeUsing
        {
            Name = y,
            Declaration = new CodeType { Name = x.NamespaceName, IsExternal = true },
        });
    protected static void AddDefaultImports(CodeElement current, IEnumerable<AdditionalUsingEvaluator> evaluators) {
        var usingsToAdd = evaluators.Where(x => x.CodeElementEvaluator.Invoke(current))
                        .SelectMany(x => usingSelector(x))
                        .ToArray();
        if(usingsToAdd.Any()) {
                var parentBlock = current.GetImmediateParentOfType<IBlock>();
                var targetBlock = parentBlock.Parent is CodeClass parentClassParent ? parentClassParent : parentBlock;
                targetBlock.AddUsing(usingsToAdd);
            }
        CrawlTree(current, c => AddDefaultImports(c, evaluators));
    }
    private const string BinaryType = "binary";
    protected static void ReplaceBinaryByNativeType(CodeElement currentElement, string symbol, string ns, bool addDeclaration = false) {
        if(currentElement is CodeMethod currentMethod) {
            var parentClass = currentMethod.Parent as CodeClass;
            var shouldInsertUsing = false;
            if(BinaryType.Equals(currentMethod.ReturnType?.Name)) {
                currentMethod.ReturnType.Name = symbol;
                shouldInsertUsing = !string.IsNullOrWhiteSpace(ns);
            }
            var binaryParameter = currentMethod.Parameters.FirstOrDefault(x => x.Type.Name.Equals(BinaryType));
            if(binaryParameter != null) {
                binaryParameter.Type.Name = symbol;
                shouldInsertUsing = !string.IsNullOrWhiteSpace(ns);
            }
            if(shouldInsertUsing) {
                var newUsing = new CodeUsing {
                    Name = addDeclaration ? symbol : ns,
                };
                if(addDeclaration)
                    newUsing.Declaration = new CodeType {
                        Name = ns,
                        IsExternal = true,
                    };
                parentClass.AddUsing(newUsing);
            }
        }
        CrawlTree(currentElement, c => ReplaceBinaryByNativeType(c, symbol, ns, addDeclaration));
    }
    protected static void ConvertUnionTypesToWrapper(CodeElement currentElement, bool usesBackingStore, bool supportInnerClasses = true) {
        var parentClass = currentElement.Parent as CodeClass;
        if(currentElement is CodeMethod currentMethod) {
            if(currentMethod.ReturnType is CodeUnionType currentUnionType)
                currentMethod.ReturnType = ConvertUnionTypeToWrapper(parentClass, currentUnionType, usesBackingStore, supportInnerClasses);
            if(currentMethod.Parameters.Any(x => x.Type is CodeUnionType))
                foreach(var currentParameter in currentMethod.Parameters.Where(x => x.Type is CodeUnionType))
                    currentParameter.Type = ConvertUnionTypeToWrapper(parentClass, currentParameter.Type as CodeUnionType, usesBackingStore, supportInnerClasses);
        }
        else if (currentElement is CodeIndexer currentIndexer && currentIndexer.ReturnType is CodeUnionType currentUnionType)
            currentIndexer.ReturnType = ConvertUnionTypeToWrapper(parentClass, currentUnionType, usesBackingStore);
        else if(currentElement is CodeProperty currentProperty && currentProperty.Type is CodeUnionType currentPropUnionType)
            currentProperty.Type = ConvertUnionTypeToWrapper(parentClass, currentPropUnionType, usesBackingStore, supportInnerClasses);

        CrawlTree(currentElement, x => ConvertUnionTypesToWrapper(x, usesBackingStore, supportInnerClasses));
    }
    private static CodeTypeBase ConvertUnionTypeToWrapper(CodeClass codeClass, CodeUnionType codeUnionType, bool usesBackingStore, bool supportsInnerClasses = true)
    {
        if(codeClass == null) throw new ArgumentNullException(nameof(codeClass));
        if(codeUnionType == null) throw new ArgumentNullException(nameof(codeUnionType));
        CodeClass newClass;
        var description =
            $"Union type wrapper for classes {codeUnionType.Types.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}";
        if (!supportsInnerClasses)
        {
            var @namespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
            newClass = @namespace.AddClass(new CodeClass()
            {
                Name = codeUnionType.Name,
                Description = description
            }).Last();
        }
        else {
            newClass = codeClass.AddInnerClass(new CodeClass {
            Name = codeUnionType.Name,
            Description = description}).First();
        }
        newClass.AddProperty(codeUnionType
                                .Types
                                .Select(x => new CodeProperty {
                                    Name = x.Name,
                                    Type = x,
                                    Description = $"Union type representation for type {x.Name}"
                                }).ToArray());
        if(codeUnionType.Types.All(x => x.TypeDefinition is CodeClass targetClass && targetClass.IsOfKind(CodeClassKind.Model) ||
                                x.TypeDefinition is CodeEnum))
        {
            KiotaBuilder.AddSerializationMembers(newClass, true, usesBackingStore);
            newClass.Kind = CodeClassKind.Model;
        }
        // Add the discrimnator function to the wrapper as it will be referenced. 
        var factoryMethod = newClass.AddMethod(new CodeMethod
        {
            Name = "CreateFromDiscriminatorValue",
            ReturnType = new CodeType { TypeDefinition = newClass, Name = newClass.Name, IsNullable = false },
            Kind = CodeMethodKind.Factory,
            IsStatic = true,
            IsAsync = false,
        }).First();
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Optional = false,
            Type = new CodeType { Name = "IParseNode", IsExternal = true },
        });
        return new CodeType {
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
        if(currentElement is CodeIndexer currentIndexer &&
            currentElement.Parent is CodeClass currentParentClass) {
            currentParentClass.RemoveChildElement(currentElement);
            foreach(var returnType in currentIndexer.ReturnType.AllTypes)
                AddIndexerMethod(rootNamespace,
                                currentParentClass,
                                returnType.TypeDefinition as CodeClass,
                                methodNameSuffix,
                                parameterNullable,
                                currentIndexer);
        }
        CrawlTree(currentElement, c => ReplaceIndexersByMethodsWithParameter(c, rootNamespace, parameterNullable, methodNameSuffix));
    }
    private static void AddIndexerMethod(CodeElement currentElement, CodeClass targetClass, CodeClass indexerClass, string methodNameSuffix, bool parameterNullable, CodeIndexer currentIndexer) {
        if(currentElement is CodeProperty currentProperty && currentProperty.Type.AllTypes.Any(x => x.TypeDefinition == targetClass)) {
            var parentClass = currentElement.Parent as CodeClass;
            var method = new CodeMethod {
                IsAsync = false,
                IsStatic = false,
                Access = AccessModifier.Public,
                Kind = CodeMethodKind.IndexerBackwardCompatibility,
                Name = currentIndexer.PathSegment + methodNameSuffix,
                Description = currentIndexer.Description,
                ReturnType = new CodeType {
                    IsNullable = false,
                    TypeDefinition = indexerClass,
                    Name = indexerClass.Name,
                },
                OriginalIndexer = currentIndexer,
            };
            var parameter = new CodeParameter {
                Name = "id",
                Optional = false,
                Kind = CodeParameterKind.Custom,
                Description = "Unique identifier of the item",
                Type = new CodeType {
                    Name = "String",
                    IsNullable = parameterNullable,
                    IsExternal = true,
                },
            };
            method.AddParameter(parameter);
            parentClass.AddMethod(method);
        }
        CrawlTree(currentElement, c => AddIndexerMethod(c, targetClass, indexerClass, methodNameSuffix, parameterNullable, currentIndexer));
    }
    internal void AddInnerClasses(CodeElement current, bool prefixClassNameWithParentName, string queryParametersBaseClassName = "QueryParametersBase") {
        if(current is CodeClass currentClass) {
            foreach(var innerClass in currentClass
                                    .Methods
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
                if(!string.IsNullOrEmpty(queryParametersBaseClassName))
                    innerClass.StartBlock.Inherits = new CodeType { Name = queryParametersBaseClassName, IsExternal = true };
            }
        }
        CrawlTree(current, x => AddInnerClasses(x, prefixClassNameWithParentName, queryParametersBaseClassName));
    }
    private static readonly CodeUsingComparer usingComparerWithDeclarations = new(true);
    private static readonly CodeUsingComparer usingComparerWithoutDeclarations = new(false);
    protected readonly GenerationConfiguration _configuration;

    protected static void AddPropertiesAndMethodTypesImports(CodeElement current, bool includeParentNamespaces, bool includeCurrentNamespace, bool compareOnDeclaration) {
        if(current is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration currentClassDeclaration) {
            var currentClassNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
            var currentClassChildren = currentClass.GetChildElements(true);
            var inheritTypes = currentClassDeclaration.Inherits?.AllTypes ?? Enumerable.Empty<CodeType>();
            var propertiesTypes = currentClass
                                .Properties
                                .Select(x => x.Type)
                                .Distinct();
            var methods = currentClass.Methods;
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
            var errorTypes = currentClassChildren
                                .OfType<CodeMethod>()
                                .Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor))
                                .SelectMany(x => x.ErrorMappings)
                                .Select(x => x.Value)
                                .Distinct();
            var usingsToAdd = propertiesTypes
                                .Union(methodsParametersTypes)
                                .Union(methodsReturnTypes)
                                .Union(indexerTypes)
                                .Union(inheritTypes)
                                .Union(errorTypes)
                                .Where(x => x != null)
                                .SelectMany(x => x?.AllTypes?.Select(y => new Tuple<CodeType, CodeNamespace>(y, y?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())))
                                .Where(x => x.Item2 != null && (includeCurrentNamespace || x.Item2 != currentClassNamespace))
                                .Where(x => includeParentNamespaces || !currentClassNamespace.IsChildOf(x.Item2))
                                .Select(x => new CodeUsing { Name = x.Item2.Name, Declaration = x.Item1 })
                                .Where(x => x.Declaration?.TypeDefinition != current)
                                .Distinct(compareOnDeclaration ? usingComparerWithDeclarations : usingComparerWithoutDeclarations)
                                .ToArray();
            if(usingsToAdd.Any())
                (currentClass.Parent is CodeClass parentClass ? parentClass : currentClass).AddUsing(usingsToAdd); //lots of languages do not support imports on nested classes
        }
        CrawlTree(current, (x) => AddPropertiesAndMethodTypesImports(x, includeParentNamespaces, includeCurrentNamespace, compareOnDeclaration));
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
    protected static void CorrectCoreType(CodeElement currentElement, Action<CodeMethod> correctMethodType, Action<CodeProperty> correctPropertyType, Action<ProprietableBlockDeclaration> correctImplements = default) {
        switch(currentElement) {
            case CodeProperty property:
                correctPropertyType?.Invoke(property);
                break;
            case CodeMethod method:
                correctMethodType?.Invoke(method);
                break;
            case ProprietableBlockDeclaration block:
                correctImplements?.Invoke(block);
                break;
        }
        CrawlTree(currentElement, x => CorrectCoreType(x, correctMethodType, correctPropertyType, correctImplements));
    }
    protected static void MakeModelPropertiesNullable(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model))
            currentClass.Properties
                        .Where(x => x.IsOfKind(CodePropertyKind.Custom))
                        .ToList()
                        .ForEach(x => x.Type.IsNullable = true);
        CrawlTree(currentElement, MakeModelPropertiesNullable);
    }
    protected static void AddRawUrlConstructorOverload(CodeElement currentElement) {
        if(currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
            currentElement.Parent is CodeClass parentClass &&
            parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
            var overloadCtor = currentMethod.Clone() as CodeMethod;
            overloadCtor.Kind = CodeMethodKind.RawUrlConstructor;
            overloadCtor.OriginalMethod = currentMethod;
            overloadCtor.RemoveParametersByKind(CodeParameterKind.PathParameters, CodeParameterKind.Path);
            overloadCtor.AddParameter(new CodeParameter {
                Name = "rawUrl",
                Type = new CodeType { Name = "string", IsExternal = true },
                Optional = false,
                Description = "The raw URL to use for the request builder.",
                Kind = CodeParameterKind.RawUrl,
            });
            parentClass.AddMethod(overloadCtor);
        }
        CrawlTree(currentElement, AddRawUrlConstructorOverload);
    }
    protected static void RemoveCancellationParameter(CodeElement currentElement){
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.RequestExecutor)){ 
            currentMethod.RemoveParametersByKind(CodeParameterKind.Cancellation);
        }
        CrawlTree(currentElement, RemoveCancellationParameter);
    }
    
    protected static void AddParsableImplementsForModelClasses(CodeElement currentElement, string className) {
        if(string.IsNullOrEmpty(className)) throw new ArgumentNullException(nameof(className));

        if(currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model)) {
            currentClass.StartBlock.AddImplements(new CodeType {
                IsExternal = true,
                Name = className
            });
        }
        CrawlTree(currentElement, c => AddParsableImplementsForModelClasses(c, className));
    }
    protected static void CorrectDateTypes(CodeClass parentClass, Dictionary<string, (string, CodeUsing)> dateTypesReplacements, params CodeTypeBase[] types) {
        if(parentClass == null)
            return;
        foreach(var type in types.Where(x => x != null && dateTypesReplacements.ContainsKey(x.Name))) {
            var replacement = dateTypesReplacements[type.Name];
            if(replacement.Item1 != null)
                type.Name = replacement.Item1;
            if(replacement.Item2 != null)
                parentClass.AddUsing(replacement.Item2.Clone() as CodeUsing);
        }
    }
    protected static void AddParentClassToErrorClasses(CodeElement currentElement, string parentClassName, string parentClassNamespace) {
        if(currentElement is CodeClass currentClass &&
            currentClass.IsErrorDefinition &&
            currentClass.StartBlock is ClassDeclaration declaration) {
            if(declaration.Inherits != null)
                throw new InvalidOperationException("This error class already inherits from another class. Update the description to remove that inheritance.");
            declaration.Inherits = new CodeType {
                Name = parentClassName,
            };
            declaration.AddUsings(new CodeUsing {
                Name = parentClassName,
                Declaration = new CodeType {
                    Name = parentClassNamespace,
                    IsExternal = true,
                }
            });
        }
        CrawlTree(currentElement, x => AddParentClassToErrorClasses(x, parentClassName, parentClassNamespace));
    }
    protected static void AddDiscriminatorMappingsUsingsToParentClasses(CodeElement currentElement, string parseNodeInterfaceName, bool addFactoryMethodImport = false, bool addUsings = true) {
        if(currentElement is CodeMethod currentMethod &&
            currentMethod.Parent is CodeClass parentClass &&
            parentClass.StartBlock is ClassDeclaration declaration) {
                if(currentMethod.IsOfKind(CodeMethodKind.Factory) &&
                    currentMethod.DiscriminatorMappings != null) {
                        if(addUsings)
                            declaration.AddUsings(currentMethod.DiscriminatorMappings
                                .Select(x => x.Value)
                                .OfType<CodeType>()
                                .Where(x => x.TypeDefinition != null)
                                .Select(x => new CodeUsing {
                                    Name = x.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name,
                                    Declaration = new CodeType {
                                        Name = x.TypeDefinition.Name,
                                        TypeDefinition = x.TypeDefinition,
                                    },
                            }).ToArray());
                        if (currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode, out var parameter))
                            parameter.Type.Name = parseNodeInterfaceName;
                } else if (addFactoryMethodImport &&
                    currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
                    currentMethod.ReturnType is CodeType type &&
                    type.TypeDefinition is CodeClass modelClass &&
                    modelClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory)) is CodeMethod factoryMethod) {
                        declaration.AddUsings(new CodeUsing {
                            Name = modelClass.GetImmediateParentOfType<CodeNamespace>().Name,
                            Declaration = new CodeType {
                                Name = factoryMethod.Name,
                                TypeDefinition = factoryMethod,
                            }
                        });
                }
        }
        CrawlTree(currentElement, x => AddDiscriminatorMappingsUsingsToParentClasses(x, parseNodeInterfaceName, addFactoryMethodImport, addUsings));
    }
    protected static void ReplaceLocalMethodsByGlobalFunctions(CodeElement currentElement, Func<CodeMethod, string> nameUpdateCallback, Func<CodeMethod, CodeUsing[]> usingsCallback, params CodeMethodKind[] kindsToReplace) {
        if(currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(kindsToReplace) &&
            currentMethod.Parent is CodeClass parentClass &&
            parentClass.Parent is CodeNamespace parentNamespace) {
                var usings = usingsCallback?.Invoke(currentMethod);
                var newName = nameUpdateCallback.Invoke(currentMethod);
                parentClass.RemoveChildElement(currentMethod);
                var globalFunction = new CodeFunction(currentMethod) {
                    Name = newName,
                };
                if(usings != null)
                    globalFunction.AddUsing(usings);
                parentNamespace.AddFunction(globalFunction);
            }
        
        CrawlTree(currentElement, x => ReplaceLocalMethodsByGlobalFunctions(x, nameUpdateCallback, usingsCallback, kindsToReplace));
    }
    protected static void AddStaticMethodsUsingsForDeserializer(CodeElement currentElement, Func<CodeType, string> functionNameCallback) {
        if(currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.Deserializer) &&
            currentMethod.Parent is CodeClass parentClass) {
                foreach(var property in parentClass.GetChildElements(true).OfType<CodeProperty>()) {
                    if (property.Type is not CodeType propertyType || propertyType.TypeDefinition == null)
                        continue;
                    var staticMethodName = functionNameCallback.Invoke(propertyType);
                    var staticMethodNS = propertyType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>();
                    var staticMethod = staticMethodNS.FindChildByName<CodeFunction>(staticMethodName);
                    if(staticMethod == null)
                        continue;
                    parentClass.AddUsing(new CodeUsing{
                        Name = staticMethodName,
                        Declaration = new CodeType {
                            Name = staticMethodName,
                            TypeDefinition = staticMethod,
                        }
                    });
                }
            }
        CrawlTree(currentElement, x => AddStaticMethodsUsingsForDeserializer(x, functionNameCallback));
    }
    protected static void AddStaticMethodsUsingsForRequestExecutor(CodeElement currentElement, Func<CodeType, string> functionNameCallback) {
        if(currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
            currentMethod.Parent is CodeClass parentClass) {
                if(currentMethod.ErrorMappings.Any())
                    currentMethod.ErrorMappings.Select(x => x.Value).OfType<CodeType>().ToList().ForEach(x => AddStaticMethodImportToClass(parentClass, x, functionNameCallback));
                if(currentMethod.ReturnType is CodeType returnType &&
                    returnType.TypeDefinition != null)
                    AddStaticMethodImportToClass(parentClass, returnType, functionNameCallback);
            }
        CrawlTree(currentElement, x => AddStaticMethodsUsingsForRequestExecutor(x, functionNameCallback));
    }
    private static void AddStaticMethodImportToClass(CodeClass parentClass, CodeType returnType, Func<CodeType, string> functionNameCallback) {
        var staticMethodName = functionNameCallback.Invoke(returnType);
        var staticMethodNS = returnType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>();
        var staticMethod = staticMethodNS.FindChildByName<CodeFunction>(staticMethodName);
        if(staticMethod != null)
            parentClass.AddUsing(new CodeUsing{
                Name = staticMethodName,
                Declaration = new CodeType {
                    Name = staticMethodName,
                    TypeDefinition = staticMethod,
                }
            });
    }
    protected static void CopyModelClassesAsInterfaces(CodeElement currentElement, Func<CodeClass, string> interfaceNamingCallback) {
        if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
            CopyClassAsInterface(currentClass, interfaceNamingCallback);
        else if (currentElement is CodeProperty codeProperty &&
                codeProperty.IsOfKind(CodePropertyKind.RequestBody) &&
                codeProperty.Type is CodeType type &&
                type.TypeDefinition is CodeClass modelClass &&
                modelClass.IsOfKind(CodeClassKind.Model))
        {
            SetTypeAndAddUsing(CopyClassAsInterface(modelClass, interfaceNamingCallback), type, codeProperty);
        } else if (currentElement is CodeMethod codeMethod &&
                codeMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
                codeMethod.ReturnType is CodeType returnType &&
                returnType.TypeDefinition is CodeClass returnClass &&
                returnClass.IsOfKind(CodeClassKind.Model))
        {
            SetTypeAndAddUsing(CopyClassAsInterface(returnClass, interfaceNamingCallback), returnType, codeMethod);
        }
        
        CrawlTree(currentElement, x => CopyModelClassesAsInterfaces(x, interfaceNamingCallback));
    }
    private static void SetTypeAndAddUsing(CodeInterface inter, CodeType elemType, CodeElement targetElement) {
        elemType.Name = inter.Name;
        elemType.TypeDefinition = inter;
        var interNS = inter.GetImmediateParentOfType<CodeNamespace>();
        if(interNS != targetElement.GetImmediateParentOfType<CodeNamespace>())
        {
            var targetClass = targetElement.GetImmediateParentOfType<CodeClass>();
            if(targetClass.Parent is CodeClass parentClass)
                targetClass = parentClass;
            targetClass.AddUsing(new CodeUsing {
                Name = interNS.Name,
                Declaration = new CodeType {
                    Name = inter.Name,
                    TypeDefinition = inter,
                },
            });
        }
    }
    private static CodeInterface CopyClassAsInterface(CodeClass modelClass, Func<CodeClass, string> interfaceNamingCallback) {
        var interfaceName = interfaceNamingCallback.Invoke(modelClass);
        var targetNS = modelClass.GetImmediateParentOfType<CodeNamespace>();
        var existing = targetNS.FindChildByName<CodeInterface>(interfaceName, false);
        if(existing != null)
            return existing;
        var parentClass = modelClass.Parent as CodeClass;
        var shouldInsertUnderParentClass = parentClass != null;
        var insertValue = new CodeInterface {
                    Name = interfaceName,
                    Kind = CodeInterfaceKind.Model,
        };
        var inter = shouldInsertUnderParentClass ? 
                        parentClass.AddInnerInterface(insertValue).First() :
                        targetNS.AddInterface(insertValue).First();
        var targetUsingBlock = shouldInsertUnderParentClass ? parentClass.StartBlock as ProprietableBlockDeclaration : inter.StartBlock;
        var usingsToRemove = new List<string>();
        var usingsToAdd = new List<CodeUsing>();
        if(modelClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass) {
            var parentInterface = CopyClassAsInterface(baseClass, interfaceNamingCallback);
            inter.StartBlock.AddImplements(new CodeType {
                Name = parentInterface.Name,
                TypeDefinition = parentInterface,
            });
            var parentInterfaceNS = parentInterface.GetImmediateParentOfType<CodeNamespace>();
            if(parentInterfaceNS != targetNS)
                usingsToAdd.Add(new CodeUsing {
                    Name = parentInterfaceNS.Name,
                    Declaration = new CodeType {
                        Name = parentInterface.Name,
                        TypeDefinition = parentInterface,
                    },
                });
        }
        if (modelClass.StartBlock.Implements.Any()) {
            var originalImplements = modelClass.StartBlock.Implements.Where(x => x.TypeDefinition != inter).ToArray();
            inter.StartBlock.AddImplements(originalImplements
                                                        .Select(x => x.Clone() as CodeType)
                                                        .ToArray());
            modelClass.StartBlock.RemoveImplements(originalImplements);
        }
        modelClass.StartBlock.AddImplements(new CodeType {
            Name = interfaceName,
            TypeDefinition = inter,
        });
        var classModelChildItems = modelClass.GetChildElements(true);
        foreach(var method in classModelChildItems.OfType<CodeMethod>()
                                                    .Where(x => x.IsOfKind(CodeMethodKind.Getter, 
                                                                        CodeMethodKind.Setter,
                                                                        CodeMethodKind.Factory) &&
                                                                !(x.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false))) {
            if(method.ReturnType is CodeType methodReturnType &&
                !methodReturnType.IsExternal) {
                if (methodReturnType.TypeDefinition is CodeClass methodTypeClass) {
                    var resultType = ReplaceTypeByInterfaceType(methodTypeClass, methodReturnType, usingsToRemove, interfaceNamingCallback);
                    modelClass.AddUsing(resultType);
                    if(resultType.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>() != targetNS)
                        targetUsingBlock.AddUsings(resultType.Clone() as CodeUsing);
                } else if (methodReturnType.TypeDefinition is CodeEnum methodEnumType)
                    targetUsingBlock.AddUsings(new CodeUsing {
                        Name = methodEnumType.Parent.Name,
                        Declaration = new CodeType {
                            Name = methodEnumType.Name,
                            TypeDefinition = methodEnumType,
                        }
                    });
            }

            foreach(var parameter in method.Parameters)
                if(parameter.Type is CodeType parameterType &&
                    !parameterType.IsExternal) {
                    if(parameterType.TypeDefinition is CodeClass parameterTypeClass) {
                        var resultType = ReplaceTypeByInterfaceType(parameterTypeClass, parameterType, usingsToRemove, interfaceNamingCallback);
                        modelClass.AddUsing(resultType);
                        if(resultType.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>() != targetNS)
                            targetUsingBlock.AddUsings(resultType.Clone() as CodeUsing);
                    } else if(parameterType.TypeDefinition is CodeEnum parameterEnumType)
                        targetUsingBlock.AddUsings(new CodeUsing {
                            Name = parameterEnumType.Parent.Name,
                            Declaration = new CodeType {
                                Name = parameterEnumType.Name,
                                TypeDefinition = parameterEnumType,
                            }
                        });
                }

            if(!method.IsStatic)
                inter.AddMethod(method.Clone() as CodeMethod);
        }
        foreach(var mProp in classModelChildItems.OfType<CodeProperty>())
            if (mProp.Type is CodeType propertyType &&
                !propertyType.IsExternal &&
                propertyType.TypeDefinition is CodeClass propertyClass) {
                    modelClass.AddUsing(ReplaceTypeByInterfaceType(propertyClass, propertyType, usingsToRemove, interfaceNamingCallback));
                }

        modelClass.RemoveUsingsByDeclarationName(usingsToRemove.ToArray());
        var externalTypesOnInter = inter.Methods.Select(x => x.ReturnType).OfType<CodeType>().Where(x => x.IsExternal)
                                    .Union(inter.StartBlock.Implements.Where(x => x.IsExternal))
                                    .Union(inter.Methods.SelectMany(x => x.Parameters).Select(x => x.Type).OfType<CodeType>().Where(x => x.IsExternal))
                                    .Select(x => x.Name)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        usingsToAdd.AddRange(modelClass.Usings.Where(x => x.IsExternal && externalTypesOnInter.Contains(x.Name)));
        if(shouldInsertUnderParentClass)
            usingsToAdd.AddRange(parentClass.Usings.Where(x => x.IsExternal && externalTypesOnInter.Contains(x.Name)));
        targetUsingBlock.AddUsings(usingsToAdd.ToArray());
        return inter;
    }
    private static CodeUsing ReplaceTypeByInterfaceType(CodeClass sourceClass, CodeType originalType, List<string> usingsToRemove, Func<CodeClass, string> interfaceNamingCallback) {
        var propertyInterfaceType = CopyClassAsInterface(sourceClass, interfaceNamingCallback);
        originalType.Name = propertyInterfaceType.Name;
        originalType.TypeDefinition = propertyInterfaceType;
        usingsToRemove.Add(sourceClass.Name);
        return new CodeUsing {
            Name = propertyInterfaceType.Parent.Name,
            Declaration = new CodeType {
                Name = propertyInterfaceType.Name,
                TypeDefinition = propertyInterfaceType,
            }
        };
    }
}
