using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public abstract class CommonLanguageRefiner : ILanguageRefiner
{
    protected static readonly char[] UnderscoreArray = new[] { '_' };
    protected CommonLanguageRefiner(GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }
    public abstract Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken);
    /// <summary>
    ///     This method adds the imports for the default serializers and deserializers to the api client class.
    ///     It also updates the module names to replace the fully qualified class name by the class name without the namespace.
    /// </summary>
    protected void AddSerializationModulesImport(CodeElement generatedCode, string[]? serializationWriterFactoryInterfaceAndRegistrationFullName = default, string[]? parseNodeFactoryInterfaceAndRegistrationFullName = default, char separator = '.')
    {
        serializationWriterFactoryInterfaceAndRegistrationFullName ??= Array.Empty<string>();
        parseNodeFactoryInterfaceAndRegistrationFullName ??= Array.Empty<string>();
        if (generatedCode is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.ClientConstructor) &&
            currentMethod.Parent is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration declaration)
        {
            var cumulatedSymbols = currentMethod.DeserializerModules
                                                .Union(currentMethod.SerializerModules)
                                                .Union(serializationWriterFactoryInterfaceAndRegistrationFullName)
                                                .Union(parseNodeFactoryInterfaceAndRegistrationFullName)
                                                .Where(x => !string.IsNullOrEmpty(x))
                                                .ToList();
            currentMethod.DeserializerModules = currentMethod.DeserializerModules.Select(x => x.Split(separator).Last()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            currentMethod.SerializerModules = currentMethod.SerializerModules.Select(x => x.Split(separator).Last()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            declaration.AddUsings(cumulatedSymbols.Select(x => new CodeUsing
            {
                Name = x.Split(separator).Last(),
                Declaration = new CodeType
                {
                    Name = x.Split(separator).SkipLast(1).Aggregate((x, y) => $"{x}{separator}{y}"),
                    IsExternal = true,
                }
            }).ToArray());
            return;
        }
        CrawlTree(generatedCode, x => AddSerializationModulesImport(x, serializationWriterFactoryInterfaceAndRegistrationFullName, parseNodeFactoryInterfaceAndRegistrationFullName, separator));
    }
    protected static void ReplaceDefaultSerializationModules(CodeElement generatedCode, HashSet<string> defaultValues, HashSet<string> newModuleNames)
    {
        ArgumentNullException.ThrowIfNull(defaultValues);
        if (ReplaceSerializationModules(generatedCode, static x => x.SerializerModules, (x, y) => x.SerializerModules = y, defaultValues, newModuleNames))
            return;
        CrawlTree(generatedCode, x => ReplaceDefaultSerializationModules(x, defaultValues, newModuleNames));
    }
    protected static void ReplaceDefaultDeserializationModules(CodeElement generatedCode, HashSet<string> defaultValues, HashSet<string> newModuleNames)
    {
        ArgumentNullException.ThrowIfNull(defaultValues);
        if (ReplaceSerializationModules(generatedCode, static x => x.DeserializerModules, (x, y) => x.DeserializerModules = y, defaultValues, newModuleNames))
            return;
        CrawlTree(generatedCode, x => ReplaceDefaultDeserializationModules(x, defaultValues, newModuleNames));
    }
    private static bool ReplaceSerializationModules(CodeElement generatedCode, Func<CodeMethod, HashSet<string>> propertyGetter, Action<CodeMethod, HashSet<string>> propertySetter, HashSet<string> initialNames, HashSet<string> moduleNames)
    {
        if (generatedCode is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.ClientConstructor))
        {
            var modules = propertyGetter.Invoke(currentMethod);
            if (modules.Count == initialNames.Count &&
                modules.All(initialNames.Contains))
            {
                propertySetter.Invoke(currentMethod, moduleNames);
                return true;
            }
        }

        return false;
    }
    protected static void CorrectCoreTypesForBackingStore(CodeElement currentElement, string defaultPropertyValue, bool hasPrefix = true)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder)
            && currentClass.StartBlock is ClassDeclaration currentDeclaration)
        {
            var backedModelImplements = currentDeclaration.Implements.FirstOrDefault(x => "IBackedModel".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
            if (backedModelImplements != null)
                backedModelImplements.Name = backedModelImplements.Name[1..]; //removing the "I"
            var backingStoreProperty = currentClass.GetPropertyOfKind(CodePropertyKind.BackingStore);
            if (backingStoreProperty != null)
            {
                backingStoreProperty.DefaultValue = defaultPropertyValue;
                backingStoreProperty.NamePrefix = hasPrefix ? backingStoreProperty.NamePrefix : string.Empty;
            }

        }
        CrawlTree(currentElement, x => CorrectCoreTypesForBackingStore(x, defaultPropertyValue, hasPrefix));
    }
    private static bool DoesAnyParentHaveAPropertyWithDefaultValue(CodeClass current)
    {
        if (current.StartBlock is ClassDeclaration currentDeclaration &&
           currentDeclaration.Inherits?.TypeDefinition is CodeClass parentClass)
        {
            if (parentClass.Properties.Any(static x => !string.IsNullOrEmpty(x.DefaultValue)))
                return true;
            return DoesAnyParentHaveAPropertyWithDefaultValue(parentClass);
        }

        return false;
    }
    protected static void CorrectNames(CodeElement current, Func<string, string> refineName,
        bool classNames = true,
        bool enumNames = true)
    {
        ArgumentNullException.ThrowIfNull(refineName);
        if (current is CodeClass currentClass && classNames &&
            refineName(currentClass.Name) is string refinedClassName &&
            !currentClass.Name.Equals(refinedClassName, StringComparison.Ordinal) &&
            currentClass.Parent is IBlock parentBlock)
        {
            parentBlock.RenameChildElement(currentClass.Name, refinedClassName);
        }
        else if (current is CodeEnum currentEnum &&
            enumNames &&
            refineName(currentEnum.Name) is string refinedEnumName &&
            !currentEnum.Name.Equals(refinedEnumName, StringComparison.Ordinal) &&
            currentEnum.Parent is IBlock parentBlock2)
        {
            parentBlock2.RenameChildElement(currentEnum.Name, refinedEnumName);
        }
        CrawlTree(current, x => CorrectNames(x, refineName));
    }
    protected static void ReplacePropertyNames(CodeElement current, HashSet<CodePropertyKind> propertyKindsToReplace, Func<string, string> refineAccessorName)
    {
        ArgumentNullException.ThrowIfNull(refineAccessorName);
        if (propertyKindsToReplace is null || propertyKindsToReplace.Count == 0) return;
        if (current is CodeProperty currentProperty &&
            !currentProperty.ExistsInBaseType &&
            propertyKindsToReplace!.Contains(currentProperty.Kind) &&
            current.Parent is CodeClass parentClass &&
            currentProperty.Access == AccessModifier.Public)
        {
            var refinedName = refineAccessorName(currentProperty.Name);

            if (!refinedName.Equals(currentProperty.Name, StringComparison.Ordinal) &&
                !parentClass.Properties.Any(property => !currentProperty.Name.Equals(property.Name, StringComparison.Ordinal) &&
                    refinedName.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))// ensure the refinement won't generate a duplicate
            {
                if (string.IsNullOrEmpty(currentProperty.SerializationName))
                    currentProperty.SerializationName = currentProperty.Name;

                parentClass.RenameChildElement(currentProperty.Name, refinedName);
            }
        }
        CrawlTree(current, x => ReplacePropertyNames(x, propertyKindsToReplace!, refineAccessorName));
    }
    protected static void AddGetterAndSetterMethods(CodeElement current, HashSet<CodePropertyKind> propertyKindsToAddAccessors, Func<CodeElement, string, string> refineAccessorName, bool removeProperty, bool parameterAsOptional, string getterPrefix, string setterPrefix, string fieldPrefix = "_", AccessModifier propertyAccessModifier = AccessModifier.Private)
    {
        ArgumentNullException.ThrowIfNull(refineAccessorName);
        var isSetterPrefixEmpty = string.IsNullOrEmpty(setterPrefix);
        var isGetterPrefixEmpty = string.IsNullOrEmpty(getterPrefix);
        if (propertyKindsToAddAccessors is null || propertyKindsToAddAccessors.Count == 0) return;
        if (current is CodeProperty currentProperty &&
            !currentProperty.ExistsInBaseType &&
            propertyKindsToAddAccessors!.Contains(currentProperty.Kind) &&
            current.Parent is CodeClass parentClass &&
            !parentClass.IsOfKind(CodeClassKind.QueryParameters))
        {
            if (removeProperty && currentProperty.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.AdditionalData)) // we never want to remove backing stores
                parentClass.RemoveChildElement(currentProperty);
            else
            {
                currentProperty.Access = propertyAccessModifier;
                if (!string.IsNullOrEmpty(fieldPrefix))
                    currentProperty.NamePrefix = fieldPrefix;
            }
            var accessorName = refineAccessorName(current, currentProperty.Name.ToFirstCharacterUpperCase());

            currentProperty.Getter = parentClass.AddMethod(new CodeMethod
            {
                Name = $"{(isGetterPrefixEmpty ? "get-" : getterPrefix)}{accessorName}",
                Access = AccessModifier.Public,
                IsAsync = false,
                Kind = CodeMethodKind.Getter,
                ReturnType = (CodeTypeBase)currentProperty.Type.Clone(),
                Documentation = new(currentProperty.Documentation.TypeReferences.ToDictionary(static x => x.Key, static x => x.Value))
                {
                    DescriptionTemplate = $"Gets the {currentProperty.WireName} property value. {currentProperty.Documentation.DescriptionTemplate}",
                },
                AccessedProperty = currentProperty,
                Deprecation = currentProperty.Deprecation,
            }).First();
            if (isGetterPrefixEmpty)
                currentProperty.Getter.Name = $"{getterPrefix}{accessorName}"; // so we don't get an exception for duplicate names when no prefix
            currentProperty.Setter = parentClass.AddMethod(new CodeMethod
            {
                Name = $"{(isSetterPrefixEmpty ? "set-" : setterPrefix)}{accessorName}",
                Access = AccessModifier.Public,
                IsAsync = false,
                Kind = CodeMethodKind.Setter,
                Documentation = new(currentProperty.Documentation.TypeReferences.ToDictionary(static x => x.Key, static x => x.Value))
                {
                    DescriptionTemplate = $"Sets the {currentProperty.WireName} property value. {currentProperty.Documentation.DescriptionTemplate}",
                },
                AccessedProperty = currentProperty,
                ReturnType = new CodeType
                {
                    Name = "void",
                    IsNullable = false,
                    IsExternal = true,
                },
                Deprecation = currentProperty.Deprecation,
            }).First();
            if (isSetterPrefixEmpty)
                currentProperty.Setter.Name = $"{setterPrefix}{accessorName}"; // so we don't get an exception for duplicate names when no prefix

            currentProperty.Setter.AddParameter(new CodeParameter
            {
                Name = "value",
                Kind = CodeParameterKind.SetterValue,
                Documentation = new()
                {
                    DescriptionTemplate = $"Value to set for the {currentProperty.WireName} property.",
                },
                Optional = parameterAsOptional,
                Type = (CodeTypeBase)currentProperty.Type.Clone(),
            });
        }
        CrawlTree(current, x => AddGetterAndSetterMethods(x, propertyKindsToAddAccessors!, refineAccessorName, removeProperty, parameterAsOptional, getterPrefix, setterPrefix, fieldPrefix, propertyAccessModifier));
    }
    protected static void AddConstructorsForDefaultValues(CodeElement current, bool addIfInherited, bool forceAdd = false, CodeClassKind[]? classKindsToExclude = null)
    {
        if (current is CodeClass currentClass &&
            !currentClass.IsOfKind(CodeClassKind.RequestBuilder, CodeClassKind.QueryParameters) &&
            (classKindsToExclude == null || !currentClass.IsOfKind(classKindsToExclude)) &&
            (forceAdd ||
            currentClass.Properties.Any(static x => !string.IsNullOrEmpty(x.DefaultValue)) ||
            addIfInherited && DoesAnyParentHaveAPropertyWithDefaultValue(currentClass)) &&
            !currentClass.Methods.Any(x => x.IsOfKind(CodeMethodKind.ClientConstructor)))
            currentClass.AddMethod(new CodeMethod
            {
                Name = "constructor",
                Kind = CodeMethodKind.Constructor,
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                IsAsync = false,
                Documentation = new(new() {
                    { "TypeName", new CodeType() {
                        IsExternal = false,
                        TypeDefinition = current,
                    }}
                })
                {
                    DescriptionTemplate = "Instantiates a new {TypeName} and sets the default values.",
                },
            });
        CrawlTree(current, x => AddConstructorsForDefaultValues(x, addIfInherited, forceAdd, classKindsToExclude));
    }

    protected static void ReplaceReservedModelTypes(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement) =>
        ReplaceReservedNames(current,
            provider,
            replacement,
            codeElementExceptions: new HashSet<Type> { typeof(CodeNamespace) },
            shouldReplaceCallback: codeElement => codeElement is CodeClass
                                                || codeElement is CodeMethod
                                                || codeElement is CodeEnum codeEnum && provider.ReservedNames.Contains(codeEnum.Name)); // only replace enum type names not enum member names

    protected static void ReplaceReservedNamespaceTypeNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement) =>
        ReplaceReservedNames(current, provider, replacement, shouldReplaceCallback: codeElement => codeElement is CodeNamespace || codeElement is CodeClass);

    private static Func<string, string> CheckReplacementNameIsNotAlreadyInUse(CodeNamespace parentNamespace, CodeElement originalItem, Func<string, string> replacement)
    {
        var newReplacement = replacement;
        var index = 0;
        while (true)
        {
            if (index > 0)
                newReplacement = name => $"{replacement(name)}{index}";
            if (parentNamespace.FindChildByName<CodeElement>(newReplacement(originalItem.Name), false) is null)
                return newReplacement;
            index++;
        }
    }
    protected static void ReplaceReservedExceptionPropertyNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        ReplaceReservedNames(
            current,
            provider,
            replacement,
            null,
            static x => ((x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom)) || x is CodeMethod) && x.Parent is CodeClass parent && parent.IsOfKind(CodeClassKind.Model) && parent.IsErrorDefinition
        );
    }
    protected static void ReplaceReservedNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement, HashSet<Type>? codeElementExceptions = null, Func<CodeElement, bool>? shouldReplaceCallback = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(replacement);
        var shouldReplace = shouldReplaceCallback?.Invoke(current) ?? true;
        var isNotInExceptions = !codeElementExceptions?.Contains(current.GetType()) ?? true;
        if (current is CodeClass currentClass &&
            isNotInExceptions &&
            shouldReplace &&
            currentClass.StartBlock is ClassDeclaration currentDeclaration)
        {
            replacement = CheckReplacementNameIsNotAlreadyInUse(currentClass.GetImmediateParentOfType<CodeNamespace>(), current, replacement);
            // if we are don't have a CodeNamespace exception, the namespace segments are also being replaced
            // in the CodeNamespace if-block so we also need to update the using references
            if (!codeElementExceptions?.Contains(typeof(CodeNamespace)) ?? true)
                ReplaceReservedCodeUsingNamespaceSegmentNames(currentDeclaration, provider, replacement);
            // we don't need to rename the inheritance name as it's either external and shouldn't change or it's generated and the code type maps directly to the source
        }
        else if (current is CodeNamespace currentNamespace &&
            isNotInExceptions &&
            shouldReplace &&
            !string.IsNullOrEmpty(currentNamespace.Name))
            ReplaceReservedNamespaceSegments(currentNamespace, provider, replacement);
        else if (current is CodeMethod currentMethod &&
            isNotInExceptions &&
            shouldReplace &&
            provider.ReservedNames.Contains(currentMethod.Name) &&
            current.Parent is IBlock parentBlock)
        {
            parentBlock.RenameChildElement(current.Name, replacement.Invoke(currentMethod.Name));
        }
        // we don't need to property type name as it's either external and shouldn't change or it's generated and the code type maps directly to the source

        // Check if the current name meets the following conditions to be replaced
        // 1. In the list of reserved names
        // 2. If it is a reserved name, make sure that the CodeElement type is worth replacing(not on the blocklist)
        // 3. There's not a very specific condition preventing from replacement
        if (provider.ReservedNames.Contains(current.Name) &&
            isNotInExceptions &&
            (shouldReplaceCallback?.Invoke(current) ?? true))// re-invoke the callback if present as conditions above may have renamed dependencies.
        {
            if (current is CodeProperty currentProperty &&
                currentProperty.IsOfKind(CodePropertyKind.Custom) &&
                string.IsNullOrEmpty(currentProperty.SerializationName))
            {
                currentProperty.SerializationName = currentProperty.Name;
            }
            if (current is CodeEnumOption currentEnumOption &&
                string.IsNullOrEmpty(currentEnumOption.SerializationName))
            {
                currentEnumOption.SerializationName = currentEnumOption.Name;
            }

            var replacementName = replacement.Invoke(current.Name);
            if (current.Parent is IBlock parentBlock)
                parentBlock.RenameChildElement(current.Name, replacementName);
            else
                current.Name = replacementName;
        }

        CrawlTree(current, x => ReplaceReservedNames(x, provider, replacement, codeElementExceptions, shouldReplaceCallback));
    }
    private static void ReplaceReservedCodeUsingNamespaceSegmentNames(ClassDeclaration currentDeclaration, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        // replace the using namespace segment names that are internally defined by the generator
        currentDeclaration.Usings
            .Where(static codeUsing => codeUsing is { IsExternal: false })
            .Select(static codeUsing => new Tuple<CodeUsing, string[]>(codeUsing, codeUsing.Name.Split('.')))
            .Where(tuple => tuple.Item2.Any(x => provider.ReservedNames.Contains(x)))
            .ToList()
            .ForEach(tuple =>
            {
                tuple.Item1.Name = tuple.Item2.Select(x => provider.ReservedNames.Contains(x) ? replacement.Invoke(x) : x)
                                              .Aggregate(static (x, y) => $"{x}.{y}");
            });
    }


    private static void ReplaceReservedNamespaceSegments(CodeNamespace currentNamespace, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        var segments = currentNamespace.Name.Split('.');
        if (Array.Exists(segments, provider.ReservedNames.Contains) && currentNamespace.Parent is CodeNamespace parentNamespace)
        {
            parentNamespace.RenameChildElement(currentNamespace.Name, segments.Select(x => provider.ReservedNames.Contains(x) ?
                                                            replacement.Invoke(x) :
                                                            x)
                                            .Aggregate((x, y) => $"{x}.{y}"));
        }
    }
    private static IEnumerable<CodeUsing> usingSelector(AdditionalUsingEvaluator x) =>
    x.ImportSymbols.Select(y =>
        new CodeUsing
        {
            Name = y,
            Declaration = new CodeType { Name = x.NamespaceName, IsExternal = true },
            IsErasable = x.IsErasable,
        });
    protected static void AddDefaultImports(CodeElement current, IEnumerable<AdditionalUsingEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(current);
        var usingsToAdd = evaluators.Where(x => x.CodeElementEvaluator.Invoke(current))
                        .SelectMany(usingSelector)
                        .ToArray();
        if (usingsToAdd.Length != 0)
        {
            var parentBlock = current.GetImmediateParentOfType<IBlock>();
            var targetBlock = parentBlock.Parent is CodeClass parentClassParent ? parentClassParent : parentBlock;
            targetBlock.AddUsing(usingsToAdd);
        }
        CrawlTree(current, c => AddDefaultImports(c, evaluators));
    }
    private static readonly HashSet<string> BinaryTypes = new(StringComparer.OrdinalIgnoreCase) { "binary", "base64", "base64url" };
    protected static void ReplaceBinaryByNativeType(CodeElement currentElement, string symbol, string ns, bool addDeclaration = false, bool isNullable = false)
    {
        if (currentElement is CodeMethod currentMethod)
        {
            var shouldInsertUsing = false;
            if (!string.IsNullOrEmpty(currentMethod.ReturnType?.Name) && BinaryTypes.Contains(currentMethod.ReturnType.Name))
            {
                currentMethod.ReturnType.Name = symbol;
                currentMethod.ReturnType.IsNullable = isNullable;
                shouldInsertUsing = !string.IsNullOrWhiteSpace(ns);
            }
            var binaryParameter = currentMethod.Parameters.FirstOrDefault(static x => BinaryTypes.Contains(x.Type?.Name ?? string.Empty));
            if (binaryParameter != null)
            {
                binaryParameter.Type.Name = symbol;
                binaryParameter.Type.IsNullable = isNullable;
                shouldInsertUsing = !string.IsNullOrWhiteSpace(ns);
            }
            if (shouldInsertUsing && currentMethod.Parent is CodeClass parentClass)
            {
                var newUsing = new CodeUsing
                {
                    Name = addDeclaration ? symbol : ns,
                };
                if (addDeclaration)
                    newUsing.Declaration = new CodeType
                    {
                        Name = ns,
                        IsExternal = true,
                    };
                parentClass.AddUsing(newUsing);
            }
        }
        CrawlTree(currentElement, c => ReplaceBinaryByNativeType(c, symbol, ns, addDeclaration, isNullable));
    }
    protected static void ConvertUnionTypesToWrapper(CodeElement currentElement, bool usesBackingStore, Func<string, string> refineMethodName, bool supportInnerClasses = true, string markerInterfaceNamespace = "", string markerInterfaceName = "", string markerMethodName = "")
    {
        ArgumentNullException.ThrowIfNull(currentElement);
        ArgumentNullException.ThrowIfNull(refineMethodName);
        if (currentElement.Parent is CodeClass parentClass)
        {
            if (currentElement is CodeMethod currentMethod)
            {
                currentMethod.Name = refineMethodName(currentMethod.Name);

                if (currentMethod.ReturnType is CodeComposedTypeBase currentUnionType)
                    currentMethod.ReturnType = ConvertComposedTypeToWrapper(parentClass, currentUnionType, usesBackingStore, refineMethodName, supportInnerClasses, markerInterfaceNamespace, markerInterfaceName, markerMethodName);
                if (currentMethod.Parameters.Any(static x => x.Type is CodeComposedTypeBase))
                    foreach (var currentParameter in currentMethod.Parameters.Where(static x => x.Type is CodeComposedTypeBase))
                        currentParameter.Type = ConvertComposedTypeToWrapper(parentClass, (CodeComposedTypeBase)currentParameter.Type, usesBackingStore, refineMethodName, supportInnerClasses, markerInterfaceNamespace, markerInterfaceName, markerMethodName);
                if (currentMethod.ErrorMappings.Select(static x => x.Value).OfType<CodeComposedTypeBase>().Any())
                    foreach (var errorUnionType in currentMethod.ErrorMappings.Select(static x => x.Value).OfType<CodeComposedTypeBase>())
                        currentMethod.ReplaceErrorMapping(errorUnionType, ConvertComposedTypeToWrapper(parentClass, errorUnionType, usesBackingStore, refineMethodName, supportInnerClasses, markerInterfaceNamespace, markerInterfaceName, markerMethodName));
            }
            else if (currentElement is CodeIndexer currentIndexer && currentIndexer.ReturnType is CodeComposedTypeBase currentUnionType)
                currentIndexer.ReturnType = ConvertComposedTypeToWrapper(parentClass, currentUnionType, usesBackingStore, refineMethodName, supportInnerClasses, markerInterfaceNamespace, markerInterfaceName, markerMethodName);
            else if (currentElement is CodeProperty currentProperty && currentProperty.Type is CodeComposedTypeBase currentPropUnionType)
                currentProperty.Type = ConvertComposedTypeToWrapper(parentClass, currentPropUnionType, usesBackingStore, refineMethodName, supportInnerClasses, markerInterfaceNamespace, markerInterfaceName, markerMethodName);
        }
        CrawlTree(currentElement, x => ConvertUnionTypesToWrapper(x, usesBackingStore, refineMethodName, supportInnerClasses, markerInterfaceNamespace, markerInterfaceName, markerMethodName));
    }
    private static CodeType ConvertComposedTypeToWrapper(CodeClass codeClass, CodeComposedTypeBase codeComposedType, bool usesBackingStore, Func<string, string> refineMethodName, bool supportsInnerClasses, string markerInterfaceNamespace, string markerInterfaceName, string markerMethodName)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        ArgumentNullException.ThrowIfNull(codeComposedType);
        CodeClass newClass;
        var description =
            "Composed type wrapper for classes {TypesList}";
        if (!supportsInnerClasses)
        {
            var @namespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
            if (@namespace.FindChildByName<CodeClass>(codeComposedType.Name, false) is CodeClass { OriginalComposedType: null })
                codeComposedType.Name = $"{codeComposedType.Name}Wrapper";
            newClass = @namespace.AddClass(new CodeClass
            {
                Name = codeComposedType.Name,
                Documentation = new(new() {
                    { "TypesList", codeComposedType }
                })
                {
                    DescriptionTemplate = description,
                },
                Deprecation = codeComposedType.Deprecation,
            }).Last();
        }
        else if (codeComposedType.TargetNamespace is CodeNamespace targetNamespace)
        {
            newClass = targetNamespace.AddClass(new CodeClass
            {
                Name = codeComposedType.Name,
                Documentation = new(new() {
                    { "TypesList", codeComposedType }
                })
                {
                    DescriptionTemplate = description
                },
            })
            .First();

            newClass.AddUsing(codeComposedType.AllTypes
                .SelectMany(static c => (c.TypeDefinition as CodeClass)?.Usings ?? Enumerable.Empty<CodeUsing>())
                .Where(static x => x.IsExternal)
                .Select(static u => (CodeUsing)u.Clone())
                .ToArray());
        }
        else
        {
            if (codeComposedType.Name.Equals(codeClass.Name, StringComparison.OrdinalIgnoreCase) || codeClass.FindChildByName<CodeProperty>(codeComposedType.Name, false) is not null)
                codeComposedType.Name = $"{codeComposedType.Name}Wrapper";
            newClass = codeClass.AddInnerClass(new CodeClass
            {
                Name = codeComposedType.Name,
                Documentation = new(new() {
                    { "TypesList", codeComposedType }
                })
                {
                    DescriptionTemplate = description
                },
            })
                                .First();
        }
        newClass.AddProperty(codeComposedType
                                .Types
                                .Select(static x => new CodeProperty
                                {
                                    Name = x.Name,
                                    Type = x,
                                    Documentation = new(new() {
                                        { "TypeName", x }
                                    })
                                    {
                                        DescriptionTemplate = "Composed type representation for type {TypeName}"
                                    },
                                }).ToArray());
        if (codeComposedType.Types.All(static x => x.TypeDefinition is CodeClass targetClass && targetClass.IsOfKind(CodeClassKind.Model) ||
                                x.TypeDefinition is CodeEnum || x.TypeDefinition is null))
        {
            KiotaBuilder.AddSerializationMembers(newClass, false, usesBackingStore, refineMethodName);
            newClass.Kind = CodeClassKind.Model;
        }
        newClass.OriginalComposedType = codeComposedType;
        if (!string.IsNullOrEmpty(markerInterfaceName) && !string.IsNullOrEmpty(markerInterfaceNamespace))
        {
            newClass.StartBlock.AddImplements(new CodeType
            {
                Name = markerInterfaceName
            });
            newClass.AddUsing(new CodeUsing
            {
                Name = markerInterfaceName,
                Declaration = new()
                {
                    Name = markerInterfaceNamespace,
                    IsExternal = true,
                }
            });
        }
        if (!string.IsNullOrEmpty(markerMethodName))
        {
            newClass.AddMethod(new CodeMethod
            {
                Name = markerMethodName,
                ReturnType = new CodeType
                {
                    Name = "boolean",
                    IsNullable = false,
                },
                Kind = CodeMethodKind.ComposedTypeMarker,
                Access = AccessModifier.Public,
                IsAsync = false,
                IsStatic = false,
                Documentation = new()
                {
                    DescriptionTemplate = "Determines if the current object is a wrapper around a composed type",
                },
            });
        }
        // Add the discriminator function to the wrapper as it will be referenced. 
        KiotaBuilder.AddDiscriminatorMethod(newClass, codeComposedType.DiscriminatorInformation.DiscriminatorPropertyName, codeComposedType.DiscriminatorInformation.DiscriminatorMappings, refineMethodName);
        return new CodeType
        {
            Name = newClass.Name,
            TypeDefinition = newClass,
            CollectionKind = codeComposedType.CollectionKind,
            IsNullable = codeComposedType.IsNullable,
            ActionOf = codeComposedType.ActionOf,
        };
    }
    protected static void MoveClassesWithNamespaceNamesUnderNamespace(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            !string.IsNullOrEmpty(currentClass.Name) &&
            currentClass.Parent is CodeNamespace parentNamespace)
        {
            var childNamespaceWithClassName = parentNamespace.GetChildElements(true)
                                                            .OfType<CodeNamespace>()
                                                            .FirstOrDefault(x => x.Name
                                                                                .EndsWith(currentClass.Name, StringComparison.OrdinalIgnoreCase));
            if (childNamespaceWithClassName != null)
            {
                parentNamespace.RemoveChildElement(currentClass);
                childNamespaceWithClassName.AddClass(currentClass);
            }
        }
        CrawlTree(currentElement, MoveClassesWithNamespaceNamesUnderNamespace);
    }
    protected static void ReplaceIndexersByMethodsWithParameter(CodeElement currentElement, bool parameterNullable, Func<string, string> methodNameCallback, Func<string, string> parameterNameCallback, GenerationLanguage language)
    {
        if (currentElement is CodeIndexer currentIndexer &&
            currentElement.Parent is CodeClass indexerParentClass)
        {
            if (indexerParentClass.ContainsMember(currentElement.Name)) // TODO remove condition for v2 necessary because of the second case of Go block
                indexerParentClass.RemoveChildElement(currentElement);
            //TODO remove whole block except for last else if body for v2
            if (language == GenerationLanguage.Go)
            {
                if (currentIndexer.IsLegacyIndexer)
                {
                    if (indexerParentClass.Indexer is CodeIndexer specificIndexer && specificIndexer != currentIndexer && !specificIndexer.IsLegacyIndexer)
                    {
                        indexerParentClass.RemoveChildElement(specificIndexer);
                        indexerParentClass.AddMethod(CodeMethod.FromIndexer(specificIndexer, methodNameCallback, parameterNameCallback, parameterNullable, true));
                    }
                    indexerParentClass.AddMethod(CodeMethod.FromIndexer(currentIndexer, methodNameCallback, parameterNameCallback, parameterNullable));
                }
                else
                {
                    var foundLegacyIndexer = indexerParentClass.Methods.Any(x => x.Kind is CodeMethodKind.IndexerBackwardCompatibility && x.OriginalIndexer is not null && x.OriginalIndexer.IsLegacyIndexer);
                    if (!foundLegacyIndexer && indexerParentClass.GetChildElements(true).OfType<CodeIndexer>().FirstOrDefault(static x => x.IsLegacyIndexer) is CodeIndexer legacyIndexer)
                    {
                        indexerParentClass.RemoveChildElement(legacyIndexer);
                        indexerParentClass.AddMethod(CodeMethod.FromIndexer(legacyIndexer, methodNameCallback, parameterNameCallback, parameterNullable));
                        foundLegacyIndexer = true;
                    }
                    indexerParentClass.AddMethod(CodeMethod.FromIndexer(currentIndexer, methodNameCallback, parameterNameCallback, parameterNullable, foundLegacyIndexer));
                }
            }
            else if (!currentIndexer.IsLegacyIndexer)
                indexerParentClass.AddMethod(CodeMethod.FromIndexer(currentIndexer, methodNameCallback, parameterNameCallback, parameterNullable));

        }
        CrawlTree(currentElement, c => ReplaceIndexersByMethodsWithParameter(c, parameterNullable, methodNameCallback, parameterNameCallback, language));
    }
    internal void DisableActionOf(CodeElement current, params CodeParameterKind[] kinds)
    {
        if (current is CodeMethod currentMethod)
            foreach (var parameter in currentMethod.Parameters.Where(x => x.Type.ActionOf && x.IsOfKind(kinds)))
                parameter.Type.ActionOf = false;

        CrawlTree(current, x => DisableActionOf(x, kinds));
    }
    internal void AddInnerClasses(CodeElement current, bool prefixClassNameWithParentName, string queryParametersBaseClassName = "", bool addToParentNamespace = false, Func<string, string, string>? nameFactory = default)
    {
        if (current is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var parentNamespace = currentClass.GetImmediateParentOfType<CodeNamespace>();
            var innerClasses = currentClass
                                    .Methods
                                    .SelectMany(static x => x.Parameters)
                                    .Where(static x => x.Type.ActionOf && x.IsOfKind(CodeParameterKind.RequestConfiguration))
                                    .SelectMany(static x => x.Type.AllTypes)
                                    .Select(static x => x.TypeDefinition)
                                    .OfType<CodeClass>()
                                    .Distinct();

            // ensure we do not miss out the types present in request configuration objects i.e. the query parameters
            var nestedQueryParameters = innerClasses
                                    .SelectMany(static x => x.Properties)
                                    .Where(static x => x.IsOfKind(CodePropertyKind.QueryParameters))
                                    .SelectMany(static x => x.Type.AllTypes)
                                    .Select(static x => x.TypeDefinition)
                                    .OfType<CodeClass>()
                                    .Distinct();

            var nestedClasses = new List<CodeClass>();
            nestedClasses.AddRange(innerClasses);
            nestedClasses.AddRange(nestedQueryParameters);

            foreach (var nestedClass in nestedClasses)
            {
                if (nestedClass.Parent is not CodeClass parentClass) continue;

                if (nameFactory != default)
                    parentClass.RenameChildElement(nestedClass.Name, nameFactory(currentClass.Name, nestedClass.Name));
                else if (prefixClassNameWithParentName && !nestedClass.Name.StartsWith(currentClass.Name, StringComparison.OrdinalIgnoreCase))
                    parentClass.RenameChildElement(nestedClass.Name, $"{currentClass.Name}{nestedClass.Name}");

                if (addToParentNamespace && parentNamespace.FindChildByName<CodeClass>(nestedClass.Name, false) == null)
                { // the query parameters class is already a child of the request executor method parent class
                    parentNamespace.AddClass(nestedClass);
                    currentClass.RemoveChildElementByName(nestedClass.Name);
                }
                else if (!addToParentNamespace && currentClass.FindChildByName<CodeClass>(nestedClass.Name, false) == null) //failsafe
                    currentClass.AddInnerClass(nestedClass);

                if (!string.IsNullOrEmpty(queryParametersBaseClassName))
                    nestedClass.StartBlock.Inherits = new CodeType { Name = queryParametersBaseClassName, IsExternal = true };
            }
        }
        CrawlTree(current, x => AddInnerClasses(x, prefixClassNameWithParentName, queryParametersBaseClassName, addToParentNamespace, nameFactory));
    }

    private static readonly CodeUsingComparer usingComparerWithDeclarations = new(true);
    private static readonly CodeUsingComparer usingComparerWithoutDeclarations = new(false);
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly GenerationConfiguration _configuration;
#pragma warning restore CA1051 // Do not declare visible instance fields

    protected static void AddPropertiesAndMethodTypesImports(CodeElement current, bool includeParentNamespaces, bool includeCurrentNamespace, bool compareOnDeclaration, Func<IEnumerable<CodeTypeBase>, IEnumerable<CodeTypeBase>>? codeTypeFilter = default)
    {
        if (current is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration currentClassDeclaration &&
            currentClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentClassNamespace)
        {
            var currentClassChildren = currentClass.GetChildElements(true);
            var inheritTypes = currentClassDeclaration.Inherits?.AllTypes ?? Enumerable.Empty<CodeType>();
            var propertiesTypes = currentClass
                                .Properties
                                .Where(static x => !x.ExistsInBaseType)
                                .Select(static x => x.Type)
                                .Distinct();
            var methods = currentClass.Methods;
            var methodsReturnTypes = methods
                                .Select(static x => x.ReturnType)
                                .Distinct();
            var methodsParametersTypes = methods
                                .SelectMany(static x => x.Parameters)
                                .Where(static x => x.IsOfKind(CodeParameterKind.Custom, CodeParameterKind.RequestBody, CodeParameterKind.RequestConfiguration, CodeParameterKind.QueryParameter))
                                .Select(static x => x.Type)
                                .Distinct();
            var indexerTypes = currentClassChildren
                                .OfType<CodeIndexer>()
                                .Select(static x => x.ReturnType)
                                .Distinct();
            var errorTypes = currentClassChildren
                                .OfType<CodeMethod>()
                                .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor))
                                .SelectMany(static x => x.ErrorMappings)
                                .Select(static x => x.Value)
                                .Distinct();
            var typesCollection = propertiesTypes
                                .Union(methodsParametersTypes)
                                .Union(methodsReturnTypes)
                                .Union(indexerTypes)
                                .Union(inheritTypes)
                                .Union(errorTypes)
                                .Where(static x => x != null);

            if (codeTypeFilter != default)
            {
                typesCollection = codeTypeFilter.Invoke(typesCollection);
            }

            var usingsToAdd = typesCollection
                            .SelectMany(static x => x.AllTypes.Select(static y => (type: y, ns: y.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>())))
                            .Where(x => x.ns != null && (includeCurrentNamespace || x.ns != currentClassNamespace))
                            .Where(x => includeParentNamespaces || !currentClassNamespace.IsChildOf(x.ns!))
                            .Select(static x => new CodeUsing { Name = x.ns!.Name, Declaration = x.type })
                            .Where(x => x.Declaration?.TypeDefinition != current)
                            .Distinct(compareOnDeclaration ? usingComparerWithDeclarations : usingComparerWithoutDeclarations)
                            .ToArray();


            if (usingsToAdd.Length != 0)
                (currentClass.Parent is CodeClass parentClass ? parentClass : currentClass).AddUsing(usingsToAdd); //lots of languages do not support imports on nested classes
        }
        CrawlTree(current, x => AddPropertiesAndMethodTypesImports(x, includeParentNamespaces, includeCurrentNamespace, compareOnDeclaration, codeTypeFilter));
    }
    protected static void CrawlTree(CodeElement currentElement, Action<CodeElement> function, bool innerOnly = true)
    {
        ArgumentNullException.ThrowIfNull(currentElement);
        ArgumentNullException.ThrowIfNull(function);
        foreach (var childElement in currentElement.GetChildElements(innerOnly))
            function.Invoke(childElement);
    }
    protected static void CorrectCoreType(CodeElement currentElement, Action<CodeMethod>? correctMethodType, Action<CodeProperty>? correctPropertyType, Action<ProprietableBlockDeclaration>? correctImplements = default)
    {
        switch (currentElement)
        {
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
        CrawlTree(currentElement, x => CorrectCoreType(x, correctMethodType, correctPropertyType, correctImplements), false);
    }
    protected static void MakeModelPropertiesNullable(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model))
            currentClass.Properties
                        .Where(static x => x.IsOfKind(CodePropertyKind.Custom))
                        .ToList()
                        .ForEach(static x => x.Type.IsNullable = true);
        CrawlTree(currentElement, MakeModelPropertiesNullable);
    }
    protected static void RemoveMethodByKind(CodeElement currentElement, CodeMethodKind kind, params CodeMethodKind[] additionalKinds)
    {
        RemoveMethodByKindImpl(currentElement, new List<CodeMethodKind>(additionalKinds) { kind }.ToArray());
    }
    private static void RemoveMethodByKindImpl(CodeElement currentElement, CodeMethodKind[] kinds)
    {
        if (currentElement is CodeMethod codeMethod &&
            currentElement.Parent is CodeClass parentClass &&
            codeMethod.IsOfKind(kinds))
        {
            parentClass.RemoveMethodByKinds(codeMethod.Kind);
        }
        CrawlTree(currentElement, x => RemoveMethodByKindImpl(x, kinds));
    }
    protected static void RemoveCancellationParameter(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
        {
            currentMethod.RemoveParametersByKind(CodeParameterKind.Cancellation);
        }
        CrawlTree(currentElement, RemoveCancellationParameter);
    }

    protected static void AddParsableImplementsForModelClasses(CodeElement currentElement, string className)
    {
        ArgumentException.ThrowIfNullOrEmpty(className);

        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.Model))
        {
            currentClass.StartBlock.AddImplements(new CodeType
            {
                IsExternal = true,
                Name = className
            });
        }
        CrawlTree(currentElement, c => AddParsableImplementsForModelClasses(c, className));
    }
    protected static void CorrectCoreTypes(CodeClass? parentClass, Dictionary<string, (string, CodeUsing?)> coreTypesReplacements, params CodeTypeBase[] types)
    {
        ArgumentNullException.ThrowIfNull(coreTypesReplacements);
        if (parentClass == null)
            return;
        foreach (var type in types.Where(x => x != null && !string.IsNullOrEmpty(x.Name) && coreTypesReplacements.ContainsKey(x.Name)))
        {
            var replacement = coreTypesReplacements[type.Name];
            if (!string.IsNullOrEmpty(replacement.Item1))
                type.Name = replacement.Item1;
            if (replacement.Item2 != null)
                parentClass.AddUsing((CodeUsing)replacement.Item2.Clone());
        }
    }
    protected static void InlineParentClasses(CodeElement currentElement, CodeElement parent)
    {
        if (currentElement is CodeClass currentClass &&
            parent is CodeType parentType &&
            parentType.TypeDefinition is CodeClass parentClass)
        {
            foreach (var currentParent in parentClass.GetInheritanceTree())
            {
                foreach (var p in currentParent
                    .Properties
                    .Where(pp =>
                        !currentClass.ContainsMember(pp.Name) &&
                        !currentClass.Properties.Any(cp => cp.Name.Equals(pp.Name, StringComparison.OrdinalIgnoreCase))))
                {
                    var newP = (CodeProperty)p.Clone();
                    newP.Parent = currentClass;
                    currentClass.AddProperty(newP);
                    if (newP.Setter != null)
                    {
                        newP.Setter.AccessedProperty = newP;
                        currentClass.AddMethod(newP.Setter);
                    }
                    if (newP.Getter != null)
                    {
                        newP.Getter.AccessedProperty = newP;
                        currentClass.AddMethod(newP.Getter);
                    }
                }

                foreach (var m in currentParent
                    .Methods
                    .Where(pm =>
                        !currentClass.ContainsMember(pm.Name) &&
                        !currentClass.Methods.Any(cm => cm.Name.Equals(pm.Name, StringComparison.OrdinalIgnoreCase))))
                {
                    var newM = (CodeMethod)m.Clone();
                    newM.Parent = currentClass;
                    currentClass.AddMethod(newM);
                }

                foreach (var u in currentParent
                    .Usings
                    .Where(pu => !currentClass.Usings.Any(cu => cu.Name.Equals(pu.Name, StringComparison.OrdinalIgnoreCase))))
                {
                    var newU = (CodeUsing)u.Clone();
                    newU.Parent = currentClass;
                    currentClass.AddUsing(newU);
                }
                foreach (var implement in currentParent
                             .StartBlock
                             .Implements
                             .Where(pi => !currentClass.Usings.Any(ci => ci.Name.Equals(pi.Name, StringComparison.OrdinalIgnoreCase))))
                {
                    currentClass.StartBlock.AddImplements((CodeType)implement.Clone());
                }
            }
        }
    }
    protected static void AddParentClassToErrorClasses(CodeElement currentElement, string parentClassName, string parentClassNamespace, bool addNamespaceToInheritDeclaration = false, bool isInterface = false, bool isErasable = false)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsErrorDefinition &&
            currentClass.StartBlock is ClassDeclaration declaration)
        {
            if (isInterface)
            {
                declaration.AddImplements(new CodeType
                {
                    Name = parentClassName,
                    IsExternal = true,
                });
            }
            else
            {
                if (declaration.Inherits is CodeElement parentElement)
                {
                    // Need to remove inheritance before fixing up the child elements
                    declaration.Inherits = null;
                    InlineParentClasses(currentClass, parentElement);
                }

                declaration.Inherits = new CodeType
                {
                    Name = parentClassName,
                    IsExternal = true,
                };
                if (addNamespaceToInheritDeclaration)
                {
                    declaration.Inherits.TypeDefinition = new CodeType
                    {
                        Name = parentClassNamespace,
                        IsExternal = true,
                    };
                }
            }
            declaration.AddUsings(new CodeUsing
            {
                Name = parentClassName,
                Declaration = new CodeType
                {
                    Name = parentClassNamespace,
                    IsExternal = true,
                },
                IsErasable = isErasable
            });
        }
        CrawlTree(currentElement, x => AddParentClassToErrorClasses(x, parentClassName, parentClassNamespace, addNamespaceToInheritDeclaration, isInterface, isErasable));
    }
    protected static void AddDiscriminatorMappingsUsingsToParentClasses(CodeElement currentElement, string parseNodeInterfaceName, bool addFactoryMethodImport = false, bool addUsings = true, bool includeParentNamespace = false)
    {
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.Parent is CodeClass parentClass &&
            parentClass.StartBlock is ClassDeclaration declaration)
        {
            if (currentMethod.IsOfKind(CodeMethodKind.Factory) &&
                (parentClass.DiscriminatorInformation?.HasBasicDiscriminatorInformation ?? false) &&
                parentClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace parentClassNamespace)
            {
                if (addUsings && includeParentNamespace)
                    declaration.AddUsings(parentClass.DiscriminatorInformation.DiscriminatorMappings
                        .Select(static x => x.Value)
                        .OfType<CodeType>()
                        .Where(static x => x.TypeDefinition != null)
                        .Select(x => new CodeUsing
                        {
                            Name = x.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>().Name,
                            Declaration = new CodeType
                            {
                                Name = x.TypeDefinition.Name,
                                TypeDefinition = x.TypeDefinition,
                            },
                        }).ToArray());
                else if (addUsings && !includeParentNamespace)
                    declaration.AddUsings(parentClass.DiscriminatorInformation.DiscriminatorMappings
                        .Select(static x => x.Value)
                        .OfType<CodeType>()
                        .Where(static x => x.TypeDefinition != null)
                        .Where(x => x.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>() != parentClassNamespace)
                        .Select(x => new CodeUsing
                        {
                            Name = x.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>().Name,
                            Declaration = new CodeType
                            {
                                Name = x.TypeDefinition.Name,
                                TypeDefinition = x.TypeDefinition,
                            },
                        }).ToArray());
                if (currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parameter)
                    parameter.Type.Name = parseNodeInterfaceName;
            }
            else if (addFactoryMethodImport &&
                currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
                currentMethod.ReturnType is CodeType type &&
                type.TypeDefinition is CodeClass modelClass &&
                modelClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory)) is CodeMethod factoryMethod)
            {
                declaration.AddUsings(new CodeUsing
                {
                    Name = modelClass.GetImmediateParentOfType<CodeNamespace>().Name,
                    Declaration = new CodeType
                    {
                        Name = factoryMethod.Name,
                        TypeDefinition = factoryMethod,
                    }
                });
            }
        }
        CrawlTree(currentElement, x => AddDiscriminatorMappingsUsingsToParentClasses(x, parseNodeInterfaceName, addFactoryMethodImport, addUsings, includeParentNamespace));
    }
    protected static void ReplaceLocalMethodsByGlobalFunctions(CodeElement currentElement, Func<CodeMethod, string> nameUpdateCallback, Func<CodeMethod, CodeUsing[]>? usingsCallback, params CodeMethodKind[] kindsToReplace)
    {
        ArgumentNullException.ThrowIfNull(nameUpdateCallback);
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(kindsToReplace) &&
            currentMethod.Parent is CodeClass parentClass &&
            parentClass.Parent is CodeNamespace parentNamespace)
        {
            var usings = usingsCallback?.Invoke(currentMethod);
            var newName = nameUpdateCallback.Invoke(currentMethod);
            parentClass.RemoveChildElement(currentMethod);
            var globalFunction = new CodeFunction(currentMethod)
            {
                Name = newName,
            };
            if (usings != null)
                globalFunction.AddUsing(usings);
            parentNamespace.AddFunction(globalFunction);
        }

        CrawlTree(currentElement, x => ReplaceLocalMethodsByGlobalFunctions(x, nameUpdateCallback, usingsCallback, kindsToReplace));
    }
    protected static void AddStaticMethodsUsingsForDeserializer(CodeElement currentElement, Func<CodeType, string> functionNameCallback)
    {
        ArgumentNullException.ThrowIfNull(functionNameCallback);
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.Deserializer) &&
            currentMethod.Parent is CodeClass parentClass)
        {
            foreach (var property in parentClass.UnorderedProperties)
            {
                if (property.Type is not CodeType propertyType || propertyType.TypeDefinition == null)
                    continue;
                var staticMethodName = functionNameCallback.Invoke(propertyType);
                var staticMethodNS = propertyType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>();
                var staticMethod = staticMethodNS.FindChildByName<CodeFunction>(staticMethodName, false);
                if (staticMethod == null)
                    continue;
                parentClass.AddUsing(new CodeUsing
                {
                    Name = staticMethodName,
                    Declaration = new CodeType
                    {
                        Name = staticMethodName,
                        TypeDefinition = staticMethod,
                    }
                });
            }
        }
        CrawlTree(currentElement, x => AddStaticMethodsUsingsForDeserializer(x, functionNameCallback));
    }
    protected static void AddStaticMethodsUsingsForRequestExecutor(CodeElement currentElement, Func<CodeType, string> functionNameCallback)
    {
        ArgumentNullException.ThrowIfNull(functionNameCallback);
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
            currentMethod.Parent is CodeClass parentClass)
        {
            if (currentMethod.ErrorMappings.Any())
                currentMethod.ErrorMappings.Select(x => x.Value).OfType<CodeType>().ToList().ForEach(x => AddStaticMethodImportToClass(parentClass, x, functionNameCallback));
            if (currentMethod.ReturnType is CodeType returnType &&
                returnType.TypeDefinition != null)
                AddStaticMethodImportToClass(parentClass, returnType, functionNameCallback);
        }
        CrawlTree(currentElement, x => AddStaticMethodsUsingsForRequestExecutor(x, functionNameCallback));
    }
    private static void AddStaticMethodImportToClass(CodeClass parentClass, CodeType returnType, Func<CodeType, string> functionNameCallback)
    {
        var staticMethodName = functionNameCallback.Invoke(returnType);
        if (returnType.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace staticMethodNS &&
            staticMethodNS.FindChildByName<CodeFunction>(staticMethodName, false) is CodeFunction staticMethod)
            parentClass.AddUsing(new CodeUsing
            {
                Name = staticMethodName,
                Declaration = new CodeType
                {
                    Name = staticMethodName,
                    TypeDefinition = staticMethod,
                }
            });
    }
    protected static void CopyModelClassesAsInterfaces(CodeElement currentElement, Func<CodeClass, string> interfaceNamingCallback)
    {
        ArgumentNullException.ThrowIfNull(interfaceNamingCallback);
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
            CopyClassAsInterface(currentClass, interfaceNamingCallback);
        else if (currentElement is CodeProperty codeProperty &&
                codeProperty.IsOfKind(CodePropertyKind.RequestBody) &&
                codeProperty.Type is CodeType type &&
                type.TypeDefinition is CodeClass modelClass &&
                modelClass.IsOfKind(CodeClassKind.Model))
        {
            SetTypeAndAddUsing(CopyClassAsInterface(modelClass, interfaceNamingCallback), type, codeProperty);
        }
        else if (currentElement is CodeMethod codeMethod &&
                codeMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator))
        {
            if (codeMethod.ReturnType is CodeType returnType &&
                returnType.TypeDefinition is CodeClass returnClass &&
                returnClass.IsOfKind(CodeClassKind.Model))
            {
                SetTypeAndAddUsing(CopyClassAsInterface(returnClass, interfaceNamingCallback), returnType, codeMethod);
            }
            if (codeMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.RequestBody)) is CodeParameter requestBodyParameter &&
                requestBodyParameter.Type is CodeType parameterType &&
                parameterType.TypeDefinition is CodeClass parameterClass &&
                parameterClass.IsOfKind(CodeClassKind.Model))
            {
                SetTypeAndAddUsing(CopyClassAsInterface(parameterClass, interfaceNamingCallback), parameterType, codeMethod);
            }
        }

        CrawlTree(currentElement, x => CopyModelClassesAsInterfaces(x, interfaceNamingCallback));
    }
    private static void SetTypeAndAddUsing(CodeInterface inter, CodeType elemType, CodeElement targetElement)
    {
        elemType.TypeDefinition = inter;
        var interNS = inter.GetImmediateParentOfType<CodeNamespace>();
        if (interNS != targetElement.GetImmediateParentOfType<CodeNamespace>())
        {
            var targetClass = targetElement.GetImmediateParentOfType<CodeClass>();
            if (targetClass.Parent is CodeClass parentClass)
                targetClass = parentClass;
            targetClass.AddUsing(new CodeUsing
            {
                Name = interNS.Name,
                Declaration = new CodeType
                {
                    Name = inter.Name,
                    TypeDefinition = inter,
                },
            });
        }
    }
    private static CodeInterface CopyClassAsInterface(CodeClass modelClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        var interfaceName = interfaceNamingCallback.Invoke(modelClass);
        var targetNS = modelClass.GetImmediateParentOfType<CodeNamespace>();
        if (targetNS.FindChildByName<CodeInterface>(interfaceName, false) is { } existing)
            return existing;
        var parentClass = modelClass.Parent as CodeClass;
        var insertValue = new CodeInterface
        {
            Name = interfaceName,
            Kind = CodeInterfaceKind.Model,
            OriginalClass = modelClass,
        };
        modelClass.AssociatedInterface = insertValue;
        var inter = parentClass != null ?
                        parentClass.AddInnerInterface(insertValue).First() :
                        targetNS.AddInterface(insertValue).First();
        var targetUsingBlock = parentClass != null ? (ProprietableBlockDeclaration)parentClass.StartBlock : inter.StartBlock;
        var usingsToRemove = new List<string>();
        var usingsToAdd = new List<CodeUsing>();
        if (modelClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            var parentInterface = CopyClassAsInterface(baseClass, interfaceNamingCallback);
            inter.StartBlock.AddImplements(new CodeType
            {
                Name = parentInterface.Name,
                TypeDefinition = parentInterface,
            });
            var parentInterfaceNS = parentInterface.GetImmediateParentOfType<CodeNamespace>();
            if (parentInterfaceNS != targetNS)
                usingsToAdd.Add(new CodeUsing
                {
                    Name = parentInterfaceNS.Name,
                    Declaration = new CodeType
                    {
                        Name = parentInterface.Name,
                        TypeDefinition = parentInterface,
                    },
                });
        }
        if (modelClass.StartBlock.Implements.Any())
        {
            var originalImplements = modelClass.StartBlock.Implements.Where(x => x.TypeDefinition != inter).ToArray();
            inter.StartBlock.AddImplements(originalImplements
                                                        .Select(static x => (CodeType)x.Clone())
                                                        .ToArray());
            modelClass.StartBlock.RemoveImplements(originalImplements);
        }
        modelClass.StartBlock.AddImplements(new CodeType
        {
            Name = interfaceName,
            TypeDefinition = inter,
        });
        var classModelChildItems = modelClass.GetChildElements(true)?.ToArray();
        if (classModelChildItems is not null)
        {
            foreach (var method in classModelChildItems.OfType<CodeMethod>()
                                                        .Where(x => x.IsOfKind(CodeMethodKind.Getter,
                                                                            CodeMethodKind.Setter,
                                                                            CodeMethodKind.Factory) &&
                                                                    !(x.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false)))
            {
                if (method.ReturnType is CodeType methodReturnType &&
                    !methodReturnType.IsExternal)
                {
                    if (methodReturnType.TypeDefinition is CodeClass methodTypeClass)
                    {
                        var resultType = ReplaceTypeByInterfaceType(methodTypeClass, methodReturnType, usingsToRemove, interfaceNamingCallback);
                        modelClass.AddUsing(resultType);
                        if (resultType.Declaration?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() != targetNS)
                            targetUsingBlock.AddUsings((CodeUsing)resultType.Clone());
                    }
                    else if (methodReturnType.TypeDefinition is CodeEnum methodEnumType &&
                                methodEnumType.Parent is not null)
                        targetUsingBlock.AddUsings(new CodeUsing
                        {
                            Name = methodEnumType.Parent.Name,
                            Declaration = new CodeType
                            {
                                Name = methodEnumType.Name,
                                TypeDefinition = methodEnumType,
                            }
                        });
                }

                foreach (var parameter in method.Parameters)
                    if (parameter.Type is CodeType parameterType &&
                        !parameterType.IsExternal)
                    {
                        if (parameterType.TypeDefinition is CodeClass parameterTypeClass)
                        {
                            var resultType = ReplaceTypeByInterfaceType(parameterTypeClass, parameterType, usingsToRemove, interfaceNamingCallback);
                            modelClass.AddUsing(resultType);
                            if (resultType.Declaration?.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() != targetNS)
                                targetUsingBlock.AddUsings((CodeUsing)resultType.Clone());
                        }
                        else if (parameterType.TypeDefinition is CodeEnum parameterEnumType &&
                                parameterEnumType.Parent is not null)
                            targetUsingBlock.AddUsings(new CodeUsing
                            {
                                Name = parameterEnumType.Parent.Name,
                                Declaration = new CodeType
                                {
                                    Name = parameterEnumType.Name,
                                    TypeDefinition = parameterEnumType,
                                }
                            });
                    }

                if (!method.IsStatic)
                    inter.AddMethod((CodeMethod)method.Clone());
            }
            foreach (var mProp in classModelChildItems.OfType<CodeProperty>())
                if (mProp.Type is CodeType propertyType &&
                    !propertyType.IsExternal &&
                    propertyType.TypeDefinition is CodeClass propertyClass)
                {
                    modelClass.AddUsing(ReplaceTypeByInterfaceType(propertyClass, propertyType, usingsToRemove, interfaceNamingCallback));
                }
        }

        modelClass.RemoveUsingsByDeclarationName(usingsToRemove.ToArray());
        var externalTypesOnInter = inter.Methods.Select(static x => x.ReturnType).OfType<CodeType>().Where(static x => x.IsExternal)
                                    .Union(inter.StartBlock.Implements.Where(static x => x.IsExternal))
                                    .Union(inter.Methods.SelectMany(static x => x.Parameters).Select(static x => x.Type).OfType<CodeType>().Where(static x => x.IsExternal))
                                    .Select(static x => x.Name)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        usingsToAdd.AddRange(modelClass.Usings.Where(x => x.IsExternal && externalTypesOnInter.Contains(x.Name)));
        if (parentClass != null)
            usingsToAdd.AddRange(parentClass.Usings.Where(x => x.IsExternal && externalTypesOnInter.Contains(x.Name)));
        targetUsingBlock.AddUsings(usingsToAdd.ToArray());
        return inter;
    }
    private static CodeUsing ReplaceTypeByInterfaceType(CodeClass sourceClass, CodeType originalType, List<string> usingsToRemove, Func<CodeClass, string> interfaceNamingCallback)
    {
        var propertyInterfaceType = CopyClassAsInterface(sourceClass, interfaceNamingCallback);
        originalType.TypeDefinition = propertyInterfaceType;
        usingsToRemove.Add(sourceClass.Name);
        return new CodeUsing
        {
            Name = propertyInterfaceType.Parent?.Name ?? throw new InvalidOperationException("Interface parent is null"),
            Declaration = new CodeType
            {
                Name = propertyInterfaceType.Name,
                TypeDefinition = propertyInterfaceType,
            }
        };
    }
    public void AddQueryParameterMapperMethod(CodeElement currentElement, string methodName = "getQueryParameter", string parameterName = "originalName")
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsOfKind(CodeClassKind.QueryParameters) &&
            currentClass.Properties.Any(static x => x.IsNameEscaped && !x.SerializationName.Equals(x.Name, StringComparison.Ordinal)))
        {
            var method = currentClass.AddMethod(new CodeMethod
            {
                Name = methodName,
                Access = AccessModifier.Public,
                ReturnType = new CodeType
                {
                    Name = "string",
                    IsNullable = false,
                },
                IsAsync = false,
                IsStatic = false,
                Kind = CodeMethodKind.QueryParametersMapper,
                Documentation = new()
                {
                    DescriptionTemplate = "Maps the query parameters names to their encoded names for the URI template parsing.",
                },
            }).First();
            method.AddParameter(new CodeParameter
            {
                Name = parameterName,
                Kind = CodeParameterKind.QueryParametersMapperParameter,
                Type = new CodeType
                {
                    Name = "string",
                    IsNullable = true,
                },
                Optional = false,
                Documentation = new()
                {
                    DescriptionTemplate = "The original query parameter name in the class.",
                },
            });
        }
        CrawlTree(currentElement, x => AddQueryParameterMapperMethod(x, methodName, parameterName));
    }
    protected static CodeMethod? GetMethodClone(CodeMethod currentMethod, params CodeParameterKind[] parameterTypesToExclude)
    {
        ArgumentNullException.ThrowIfNull(currentMethod);
        if (currentMethod.Parameters.Any(x => x.IsOfKind(parameterTypesToExclude)) &&
            currentMethod.Clone() is CodeMethod cloneMethod)
        {
            cloneMethod.RemoveParametersByKind(parameterTypesToExclude);
            cloneMethod.OriginalMethod = currentMethod;
            return cloneMethod;
        }

        return null;
    }
    protected void RemoveDiscriminatorMappingsTargetingSubNamespaces(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.Factory) &&
            currentMethod.Parent is CodeClass currentClass &&
            currentClass.DiscriminatorInformation.DiscriminatorMappings.Any())
        {
            var currentNamespace = currentMethod.GetImmediateParentOfType<CodeNamespace>();
            var keysToRemove = currentClass.DiscriminatorInformation.DiscriminatorMappings
                                            .Where(x => x.Value is CodeType mappingType &&
                                                        mappingType.TypeDefinition is CodeClass mappingClass &&
                                                        mappingClass.Parent is CodeNamespace mappingNamespace &&
                                                        currentNamespace.IsParentOf(mappingNamespace) &&
                                                        mappingClass.StartBlock.InheritsFrom(currentClass))
                                            .Select(x => x.Key)
                                            .ToArray();
            if (keysToRemove.Length != 0)
                currentClass.DiscriminatorInformation.RemoveDiscriminatorMapping(keysToRemove);
        }
        CrawlTree(currentElement, RemoveDiscriminatorMappingsTargetingSubNamespaces);
    }
    protected static void MoveRequestBuilderPropertiesToBaseType(CodeElement currentElement, CodeUsing baseTypeUsing, AccessModifier? accessModifier = null, bool addCurrentTypeAsGenericTypeParameter = false)
    {
        ArgumentNullException.ThrowIfNull(baseTypeUsing);
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            if (currentClass.StartBlock.Inherits == null)
            {
                currentClass.StartBlock.Inherits = new CodeType
                {
                    Name = baseTypeUsing.Name,
                    IsExternal = true,
                };
                if (addCurrentTypeAsGenericTypeParameter)
                {
                    currentClass.StartBlock.Inherits.AddGenericTypeParameterValue(new CodeType
                    {
                        TypeDefinition = currentClass,
                    });
                }
                currentClass.AddUsing(baseTypeUsing);
            }

            var properties = currentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.PathParameters, CodePropertyKind.UrlTemplate, CodePropertyKind.RequestAdapter));
            foreach (var property in properties)
            {
                property.ExistsInExternalBaseType = true;
                if (accessModifier.HasValue)
                    property.Access = accessModifier.Value;
            }
        }

        CrawlTree(currentElement, x => MoveRequestBuilderPropertiesToBaseType(x, baseTypeUsing, accessModifier, addCurrentTypeAsGenericTypeParameter));
    }
    protected static void RemoveRequestConfigurationClassesCommonProperties(CodeElement currentElement, CodeUsing baseTypeUsing)
    {
        ArgumentNullException.ThrowIfNull(baseTypeUsing);
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestConfiguration))
        {
            if (currentClass.StartBlock.Inherits == null)
            {
                currentClass.StartBlock.Inherits = new CodeType
                {
                    Name = baseTypeUsing.Name,
                    IsExternal = true,
                };
                currentClass.AddUsing(baseTypeUsing);
            }
            currentClass.RemovePropertiesOfKind(CodePropertyKind.Headers, CodePropertyKind.Options);
        }

        CrawlTree(currentElement, x => RemoveRequestConfigurationClassesCommonProperties(x, baseTypeUsing));
    }
    protected static void RemoveUntypedNodePropertyValues(CodeElement currentElement)
    {
        if (currentElement is CodeProperty currentProperty
            && currentElement.Parent is CodeClass parentClass
            && currentProperty.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))
        {
            parentClass.RemoveChildElement(currentProperty);
        }
        CrawlTree(currentElement, RemoveUntypedNodePropertyValues);
    }
    protected static void RemoveRequestConfigurationClasses(CodeElement currentElement, CodeUsing? configurationParameterTypeUsing = null, CodeType? defaultValueForGenericTypeParam = null, bool keepRequestConfigurationClass = false, bool addDeprecation = false, CodeUsing? usingForDefaultGenericParameter = null)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestConfiguration) &&
            currentClass.Parent is CodeClass parentClass)
        {
            if (addDeprecation && keepRequestConfigurationClass)
                currentClass.Deprecation = new DeprecationInformation("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.");
            else if (!keepRequestConfigurationClass)
                parentClass.RemoveChildElement(currentClass);
            var configurationParameters = parentClass.Methods
                                                    .SelectMany(static x => x.Parameters)
                                                    .Where(x => x.IsOfKind(CodeParameterKind.RequestConfiguration) && x.Type is CodeType type && type.TypeDefinition == currentClass)
                                                    .ToArray();
            var genericTypeParamValue = currentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.QueryParameters)?.Type as CodeType ?? defaultValueForGenericTypeParam;
            if (configurationParameterTypeUsing != null && genericTypeParamValue != null && configurationParameters.Length != 0)
            {
                parentClass.AddUsing(configurationParameterTypeUsing);
                if (usingForDefaultGenericParameter != null)
                    parentClass.AddUsing(usingForDefaultGenericParameter);
                var configurationParameterType = new CodeType
                {
                    Name = configurationParameterTypeUsing.Name,
                    IsExternal = true,
                };
                if (addDeprecation && keepRequestConfigurationClass)
                {
                    currentClass.RemovePropertiesOfKind(CodePropertyKind.Headers, CodePropertyKind.Options, CodePropertyKind.QueryParameters);
                    currentClass.StartBlock.Inherits = GetGenericTypeForRequestConfiguration(configurationParameterType, genericTypeParamValue);
                }
                foreach (var configurationParameter in configurationParameters)
                {
                    var newType = GetGenericTypeForRequestConfiguration(configurationParameterType, genericTypeParamValue);
                    newType.ActionOf = configurationParameter.Type.ActionOf;
                    configurationParameter.Type = newType;
                }
            }
        }

        CrawlTree(currentElement, x => RemoveRequestConfigurationClasses(x, configurationParameterTypeUsing, defaultValueForGenericTypeParam, keepRequestConfigurationClass, addDeprecation, usingForDefaultGenericParameter));
    }
    private static CodeType GetGenericTypeForRequestConfiguration(CodeType configurationParameterType, CodeType genericTypeParamValue)
    {
        var newType = (CodeType)configurationParameterType.Clone();
        newType.AddGenericTypeParameterValue(genericTypeParamValue);
        return newType;
    }

    internal static void AddPrimaryErrorMessage(CodeElement currentElement, string name, Func<CodeType> type, bool asProperty = false)
    {
        if (currentElement is CodeClass { IsErrorDefinition: true } currentClass)
        {
            if (asProperty)
            {
                currentClass.AddProperty(new CodeProperty
                {
                    Name = name,
                    Access = AccessModifier.Public,
                    Kind = CodePropertyKind.ErrorMessageOverride,
                    Type = type(),
                    Documentation = new()
                    {
                        DescriptionTemplate = "The primary error message.",
                    },
                });
            }
            else
            {
                currentClass.AddMethod(new CodeMethod
                {
                    Name = name,
                    Access = AccessModifier.Public,
                    Kind = CodeMethodKind.ErrorMessageOverride,
                    ReturnType = type(),
                    IsAsync = false,
                    IsStatic = false,
                    Documentation = new()
                    {
                        DescriptionTemplate = "The primary error message.",
                    },
                });
            }
        }
        CrawlTree(currentElement, x => AddPrimaryErrorMessage(x, name, type, asProperty));
    }
    protected static void DeduplicateErrorMappings(CodeElement codeElement)
    {
        if (codeElement is CodeMethod { Kind: CodeMethodKind.RequestExecutor } requestExecutorMethod)
        {
            requestExecutorMethod.DeduplicateErrorMappings();
        }
        CrawlTree(codeElement, DeduplicateErrorMappings);
    }
}
