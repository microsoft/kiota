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
        if (ReplaceSerializationModules(generatedCode, static x => x.SerializerModules, (x, y) => x.SerializerModules = y, defaultValues, newModuleNames))
            return;
        CrawlTree(generatedCode, x => ReplaceDefaultSerializationModules(x, defaultValues, newModuleNames));
    }
    protected static void ReplaceDefaultDeserializationModules(CodeElement generatedCode, HashSet<string> defaultValues, HashSet<string> newModuleNames)
    {
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
    protected static void CorrectCoreTypesForBackingStore(CodeElement currentElement, string defaultPropertyValue, Boolean hasPrefix = true)
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
                backingStoreProperty.NamePrefix = hasPrefix ? backingStoreProperty.NamePrefix : String.Empty;
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
    protected static void ReplacePropertyNames(CodeElement current, HashSet<CodePropertyKind> propertyKindsToReplace, Func<string, string> refineAccessorName)
    {
        if (!(propertyKindsToReplace?.Any() ?? true)) return;
        if (current is CodeProperty currentProperty &&
            !currentProperty.ExistsInBaseType &&
            propertyKindsToReplace!.Contains(currentProperty.Kind) &&
            current.Parent is CodeClass parentClass &&
            !parentClass.IsOfKind(CodeClassKind.QueryParameters) &&
            currentProperty.Access == AccessModifier.Public)
        {
            if (string.IsNullOrEmpty(currentProperty.SerializationName))
                currentProperty.SerializationName = currentProperty.Name;
            currentProperty.Name = refineAccessorName(currentProperty.Name);
        }
        CrawlTree(current, x => ReplacePropertyNames(x, propertyKindsToReplace!, refineAccessorName));
    }
    protected static void AddGetterAndSetterMethods(CodeElement current, HashSet<CodePropertyKind> propertyKindsToAddAccessors, Func<string, string> refineAccessorName, bool removeProperty, bool parameterAsOptional, string getterPrefix, string setterPrefix, string fieldPrefix = "_")
    {
        if (!(propertyKindsToAddAccessors?.Any() ?? true)) return;
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
                currentProperty.Access = AccessModifier.Private;
                if (!string.IsNullOrEmpty(fieldPrefix))
                    currentProperty.NamePrefix = fieldPrefix;
            }
            var propertyOriginalName = (currentProperty.IsNameEscaped ? currentProperty.SerializationName : current.Name)
                                        .ToFirstCharacterLowerCase();
            var accessorName = refineAccessorName(propertyOriginalName.CleanupSymbolName().ToFirstCharacterUpperCase());
            currentProperty.Getter = parentClass.AddMethod(new CodeMethod
            {
                Name = $"get-{accessorName}",
                Access = AccessModifier.Public,
                IsAsync = false,
                Kind = CodeMethodKind.Getter,
                ReturnType = (CodeTypeBase)currentProperty.Type.Clone(),
                Documentation = new()
                {
                    Description = $"Gets the {propertyOriginalName} property value. {currentProperty.Documentation.Description}",
                },
                AccessedProperty = currentProperty,
            }).First();
            currentProperty.Getter.Name = $"{getterPrefix}{accessorName}"; // so we don't get an exception for duplicate names when no prefix
            var setter = parentClass.AddMethod(new CodeMethod
            {
                Name = $"set-{accessorName}",
                Access = AccessModifier.Public,
                IsAsync = false,
                Kind = CodeMethodKind.Setter,
                Documentation = new()
                {
                    Description = $"Sets the {propertyOriginalName} property value. {currentProperty.Documentation.Description}",
                },
                AccessedProperty = currentProperty,
                ReturnType = new CodeType
                {
                    Name = "void",
                    IsNullable = false,
                    IsExternal = true,
                },
            }).First();
            setter.Name = $"{setterPrefix}{accessorName}"; // so we don't get an exception for duplicate names when no prefix
            currentProperty.Setter = setter;

            setter.AddParameter(new CodeParameter
            {
                Name = "value",
                Kind = CodeParameterKind.SetterValue,
                Documentation = new()
                {
                    Description = $"Value to set for the {current.Name} property.",
                },
                Optional = parameterAsOptional,
                Type = (CodeTypeBase)currentProperty.Type.Clone(),
            });
        }
        CrawlTree(current, x => AddGetterAndSetterMethods(x, propertyKindsToAddAccessors!, refineAccessorName, removeProperty, parameterAsOptional, getterPrefix, setterPrefix, fieldPrefix));
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
                Documentation = new()
                {
                    Description = $"Instantiates a new {current.Name} and sets the default values.",
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
                                                || codeElement is CodeEnum codeEnum && provider.ReservedNames.Contains(codeEnum.Name) // only replace enum type names not enum member names
                                                || (codeElement is CodeProperty currentProperty && currentProperty.Type is CodeType propertyType && !propertyType.IsExternal && provider.ReservedNames.Contains(propertyType.Name)));// only replace property type names not property names

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
    protected static void ReplaceReservedNames(CodeElement current, IReservedNamesProvider provider, Func<string, string> replacement, HashSet<Type>? codeElementExceptions = null, Func<CodeElement, bool>? shouldReplaceCallback = null)
    {
        var shouldReplace = shouldReplaceCallback?.Invoke(current) ?? true;
        var isNotInExceptions = !codeElementExceptions?.Contains(current.GetType()) ?? true;
        if (current is CodeClass currentClass &&
            isNotInExceptions &&
            shouldReplace &&
            currentClass.StartBlock is ClassDeclaration currentDeclaration)
        {
            replacement = CheckReplacementNameIsNotAlreadyInUse(currentClass.GetImmediateParentOfType<CodeNamespace>(), current, replacement);
            ReplaceReservedCodeUsingDeclarationNames(currentDeclaration, provider, replacement);
            // if we are don't have a CodeNamespace exception, the namespace segments are also being replaced
            // in the CodeNamespace if-block so we also need to update the using references
            if (!codeElementExceptions?.Contains(typeof(CodeNamespace)) ?? true)
                ReplaceReservedCodeUsingNamespaceSegmentNames(currentDeclaration, provider, replacement);
            if (currentDeclaration.Inherits?.Name is string inheritName && provider.ReservedNames.Contains(inheritName))
                currentDeclaration.Inherits.Name = replacement(currentDeclaration.Inherits.Name);
            if (currentClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Value.Name).Any(provider.ReservedNames.Contains))
                ReplaceMappingNames(currentClass.DiscriminatorInformation.DiscriminatorMappings, provider, replacement);
        }
        else if (current is CodeNamespace currentNamespace &&
            isNotInExceptions &&
            shouldReplace &&
            !string.IsNullOrEmpty(currentNamespace.Name))
            ReplaceReservedNamespaceSegments(currentNamespace, provider, replacement);
        else if (current is CodeMethod currentMethod &&
            isNotInExceptions &&
            shouldReplace)
        {
            if (currentMethod.ReturnType is CodeType returnType &&
                !returnType.IsExternal &&
                provider.ReservedNames.Contains(returnType.Name))
                returnType.Name = replacement.Invoke(returnType.Name);
            if (provider.ReservedNames.Contains(currentMethod.Name))
                currentMethod.Name = replacement.Invoke(currentMethod.Name);
            if (currentMethod.ErrorMappings.Select(x => x.Value.Name).Any(x => provider.ReservedNames.Contains(x)))
                ReplaceMappingNames(currentMethod.ErrorMappings, provider, replacement);
            ReplaceReservedParameterNamesTypes(currentMethod, provider, replacement);
        }
        else if (current is CodeProperty currentProperty &&
                isNotInExceptions &&
                shouldReplace &&
                currentProperty.Type is CodeType propertyType &&
                !propertyType.IsExternal &&
                provider.ReservedNames.Contains(currentProperty.Type.Name))
            propertyType.Name = replacement.Invoke(propertyType.Name);
        else if (current is CodeEnum currentEnum &&
                isNotInExceptions &&
                shouldReplace &&
                currentEnum.Options.Any(x => provider.ReservedNames.Contains(x.Name)))
            ReplaceReservedEnumNames(currentEnum, provider, replacement);
        // Check if the current name meets the following conditions to be replaced
        // 1. In the list of reserved names
        // 2. If it is a reserved name, make sure that the CodeElement type is worth replacing(not on the blocklist)
        // 3. There's not a very specific condition preventing from replacement
        if (provider.ReservedNames.Contains(current.Name) &&
            isNotInExceptions &&
            shouldReplace)
        {
            if (current is CodeProperty currentProperty &&
                currentProperty.IsOfKind(CodePropertyKind.Custom) &&
                string.IsNullOrEmpty(currentProperty.SerializationName))
            {
                currentProperty.SerializationName = currentProperty.Name;
            }
            var newDeclarationName = replacement.Invoke(current.Name);

            /* Update element name in the InnerChildElements property of CodeBlock.
             */
            if (current.Parent is CodeNamespace  || current.Parent is CodeClass) 
            {
                UpdateReservedNameReplacementInParent(current, newDeclarationName);
            }
            current.Name = newDeclarationName;
        }

        CrawlTree(current, x => ReplaceReservedNames(x, provider, replacement, codeElementExceptions, shouldReplaceCallback));
    }

    private static void UpdateReservedNameReplacementInParent(CodeElement codeElement , string newDeclarationName)
    {
        if (codeElement.Parent is CodeNamespace codeNamespace)
        {
            codeNamespace.UpdateChildElement(codeElement.Name, newDeclarationName);
        }
        else if (codeElement.Parent is CodeClass codeClass)
        {
            codeClass.UpdateChildElement(codeElement.Name, newDeclarationName);
        }
    }
    private static void ReplaceReservedEnumNames(CodeEnum currentEnum, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        currentEnum.Options
                    .Where(x => provider.ReservedNames.Contains(x.Name))
                    .ToList()
                    .ForEach(x =>
                    {
                        x.SerializationName = x.Name;
                        x.Name = replacement.Invoke(x.Name);
                    });
    }
    private static void ReplaceReservedCodeUsingDeclarationNames(ClassDeclaration currentDeclaration, IReservedNamesProvider provider, Func<string, string> replacement)
    {
        // replace the using declaration type names that are internally defined by the generator
        currentDeclaration.Usings
                        .Select(x => x.Declaration)
                        .Where(x => x != null && !x.IsExternal)
                        .Join(provider.ReservedNames, static x => x!.Name, static y => y, static (x, y) => x)
                        .ToList()
                        .ForEach(x =>
                        {
                            x!.Name = replacement.Invoke(x.Name);
                        });
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
        if (segments.Any(x => provider.ReservedNames.Contains(x)))
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
                                            .ForEach(x =>
                                            {
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
    protected static void AddDefaultImports(CodeElement current, IEnumerable<AdditionalUsingEvaluator> evaluators)
    {
        var usingsToAdd = evaluators.Where(x => x.CodeElementEvaluator.Invoke(current))
                        .SelectMany(x => usingSelector(x))
                        .ToArray();
        if (usingsToAdd.Any())
        {
            var parentBlock = current.GetImmediateParentOfType<IBlock>();
            var targetBlock = parentBlock.Parent is CodeClass parentClassParent ? parentClassParent : parentBlock;
            targetBlock.AddUsing(usingsToAdd);
        }
        CrawlTree(current, c => AddDefaultImports(c, evaluators));
    }
    private const string BinaryType = "binary";
    protected static void ReplaceBinaryByNativeType(CodeElement currentElement, string symbol, string ns, bool addDeclaration = false, bool isNullable = false)
    {
        if (currentElement is CodeMethod currentMethod)
        {
            var shouldInsertUsing = false;
            if (BinaryType.Equals(currentMethod.ReturnType?.Name))
            {
                currentMethod.ReturnType.Name = symbol;
                currentMethod.ReturnType.IsNullable = isNullable;
                shouldInsertUsing = !string.IsNullOrWhiteSpace(ns);
            }
            var binaryParameter = currentMethod.Parameters.FirstOrDefault(static x => x.Type?.Name?.Equals(BinaryType) ?? false);
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
    protected static void ConvertUnionTypesToWrapper(CodeElement currentElement, bool usesBackingStore, bool supportInnerClasses = true)
    {
        if (currentElement.Parent is CodeClass parentClass)
        {
            if (currentElement is CodeMethod currentMethod)
            {
                if (currentMethod.ReturnType is CodeComposedTypeBase currentUnionType)
                    currentMethod.ReturnType = ConvertComposedTypeToWrapper(parentClass, currentUnionType, usesBackingStore, supportInnerClasses);
                if (currentMethod.Parameters.Any(static x => x.Type is CodeComposedTypeBase))
                    foreach (var currentParameter in currentMethod.Parameters.Where(static x => x.Type is CodeComposedTypeBase))
                        currentParameter.Type = ConvertComposedTypeToWrapper(parentClass, (CodeComposedTypeBase)currentParameter.Type, usesBackingStore, supportInnerClasses);
                if (currentMethod.ErrorMappings.Select(static x => x.Value).OfType<CodeComposedTypeBase>().Any())
                    foreach (var errorUnionType in currentMethod.ErrorMappings.Select(static x => x.Value).OfType<CodeComposedTypeBase>())
                        currentMethod.ReplaceErrorMapping(errorUnionType, ConvertComposedTypeToWrapper(parentClass, errorUnionType, usesBackingStore, supportInnerClasses));
            }
            else if (currentElement is CodeIndexer currentIndexer && currentIndexer.ReturnType is CodeComposedTypeBase currentUnionType)
                currentIndexer.ReturnType = ConvertComposedTypeToWrapper(parentClass, currentUnionType, usesBackingStore);
            else if (currentElement is CodeProperty currentProperty && currentProperty.Type is CodeComposedTypeBase currentPropUnionType)
                currentProperty.Type = ConvertComposedTypeToWrapper(parentClass, currentPropUnionType, usesBackingStore, supportInnerClasses);
        }
        CrawlTree(currentElement, x => ConvertUnionTypesToWrapper(x, usesBackingStore, supportInnerClasses));
    }
    private static CodeTypeBase ConvertComposedTypeToWrapper(CodeClass codeClass, CodeComposedTypeBase codeComposedType, bool usesBackingStore, bool supportsInnerClasses = true)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        ArgumentNullException.ThrowIfNull(codeComposedType);
        CodeClass newClass;
        var description =
            $"Composed type wrapper for classes {codeComposedType.Types.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}";
        if (!supportsInnerClasses)
        {
            var @namespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
            newClass = @namespace.AddClass(new CodeClass
            {
                Name = codeComposedType.Name,
                Documentation = new()
                {
                    Description = description,
                },
            }).Last();
        }
        else if (codeComposedType.TargetNamespace is CodeNamespace targetNamespace)
        {
            newClass = targetNamespace.AddClass(new CodeClass
            {
                Name = codeComposedType.Name,
                Documentation = new()
                {
                    Description = description
                },
            })
                                .First();
            newClass.AddUsing(codeClass.Usings
                                        .Where(static x => x.IsExternal)
                                        .Select(static x => (CodeUsing)x.Clone())
                                        .ToArray());
        }
        else
        {
            if (codeComposedType.Name.Equals(codeClass.Name, StringComparison.OrdinalIgnoreCase))
                codeComposedType.Name = $"{codeComposedType.Name}Wrapper";
            newClass = codeClass.AddInnerClass(new CodeClass
            {
                Name = codeComposedType.Name,
                Documentation = new()
                {
                    Description = description
                },
            })
                                .First();
        }
        newClass.AddProperty(codeComposedType
                                .Types
                                .Select(x => new CodeProperty
                                {
                                    Name = x.Name,
                                    Type = x,
                                    Documentation = new()
                                    {
                                        Description = $"Composed type representation for type {x.Name}"
                                    },
                                }).ToArray());
        if (codeComposedType.Types.All(static x => x.TypeDefinition is CodeClass targetClass && targetClass.IsOfKind(CodeClassKind.Model) ||
                                x.TypeDefinition is CodeEnum || x.TypeDefinition is null))
        {
            KiotaBuilder.AddSerializationMembers(newClass, true, usesBackingStore);
            newClass.AddProperty(new CodeProperty
            {
                Name = "serializationHint",
                Type = new CodeType
                {
                    Name = "string",
                    IsExternal = true,
                },
                Documentation = new()
                {
                    Description = "Serialization hint for the current wrapper.",
                },
                Access = AccessModifier.Public,
                Kind = CodePropertyKind.SerializationHint,
            });
            newClass.Kind = CodeClassKind.Model;
        }
        newClass.OriginalComposedType = codeComposedType;
        // Add the discriminator function to the wrapper as it will be referenced. 
        KiotaBuilder.AddDiscriminatorMethod(newClass, codeComposedType.DiscriminatorInformation.DiscriminatorPropertyName, codeComposedType.DiscriminatorInformation.DiscriminatorMappings);
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
    protected static void ReplaceIndexersByMethodsWithParameter(CodeElement currentElement, bool parameterNullable, string? methodNameSuffix = default)
    {
        if (currentElement is CodeIndexer currentIndexer &&
            currentElement.Parent is CodeClass indexerParentClass &&
            indexerParentClass.Parent is CodeNamespace indexerNamespace &&
            indexerNamespace.Parent is CodeNamespace lookupNamespace)
        {
            indexerParentClass.RemoveChildElement(currentElement);
            AddIndexerMethod(indexerParentClass,
                            methodNameSuffix,
                            parameterNullable,
                            currentIndexer,
                            lookupNamespace);
        }
        CrawlTree(currentElement, c => ReplaceIndexersByMethodsWithParameter(c, parameterNullable, methodNameSuffix));
    }
    private static void AddIndexerMethod(CodeClass indexerParentClass, string? methodNameSuffix, bool parameterNullable, CodeIndexer currentIndexer, CodeNamespace lookupNamespace)
    {
        if (lookupNamespace.Classes
                            .Where(static x => x.IsOfKind(CodeClassKind.RequestBuilder))
                            .SelectMany(static x => x.Properties)
                            .Where(x => x.IsOfKind(CodePropertyKind.RequestBuilder) &&
                                    x.Type is CodeType xType &&
                                    xType.TypeDefinition == indexerParentClass)
                            .Select(static x => x.Parent)
                            .OfType<CodeClass>()
                            .FirstOrDefault() is CodeClass parentClassForProperty)
        {
            parentClassForProperty.AddMethod(CodeMethod.FromIndexer(currentIndexer, methodNameSuffix, parameterNullable));
        }
        else if (lookupNamespace.Classes
                            .Where(static x => x.IsOfKind(CodeClassKind.RequestBuilder))
                            .SelectMany(static x => x.Methods)
                            .Where(x => x.IsOfKind(CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.RequestBuilderBackwardCompatibility) &&
                                        x.ReturnType is CodeType xMethodType &&
                                        xMethodType.TypeDefinition == indexerParentClass)
                            .Select(static x => x.Parent)
                            .OfType<CodeClass>()
                            .FirstOrDefault() is CodeClass parentClassForMethod)
        {
            parentClassForMethod.AddMethod(CodeMethod.FromIndexer(currentIndexer, methodNameSuffix, parameterNullable));
        }
        else if (lookupNamespace.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace parentNamespace &&
            parentNamespace.Classes
                            .Where(static x => x.IsOfKind(CodeClassKind.RequestBuilder))
                            .SelectMany(static x => x.Methods)
                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) &&
                                x.ReturnType is CodeType xMethodType &&
                                xMethodType.TypeDefinition == indexerParentClass)
                            is not null)
        {
            indexerParentClass.AddMethod(CodeMethod.FromIndexer(currentIndexer, methodNameSuffix, parameterNullable)); //we already went one up with the previous indexer
        }
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
                                    .OfType<CodeClass>();

            // ensure we do not miss out the types present in request configuration objects i.e. the query parameters
            var nestedQueryParameters = innerClasses
                                    .SelectMany(static x => x.Properties)
                                    .Where(static x => x.IsOfKind(CodePropertyKind.QueryParameters))
                                    .SelectMany(static x => x.Type.AllTypes)
                                    .Select(static x => x.TypeDefinition)
                                    .OfType<CodeClass>();

            var nestedClasses = new List<CodeClass>();
            nestedClasses.AddRange(innerClasses);
            nestedClasses.AddRange(nestedQueryParameters);

            foreach (var innerClass in nestedClasses)
            {
                var originalClassName = innerClass.Name;

                if (nameFactory != default)
                    innerClass.Name = nameFactory(currentClass.Name, innerClass.Name);
                else if (prefixClassNameWithParentName && !innerClass.Name.StartsWith(currentClass.Name, StringComparison.OrdinalIgnoreCase))
                    innerClass.Name = $"{currentClass.Name}{innerClass.Name}";

                if (addToParentNamespace && parentNamespace.FindChildByName<CodeClass>(innerClass.Name, false) == null)
                { // the query parameters class is already a child of the request executor method parent class
                    parentNamespace.AddClass(innerClass);
                    currentClass.RemoveChildElementByName(originalClassName);
                }
                else if (!addToParentNamespace && innerClass.Parent == null && currentClass.FindChildByName<CodeClass>(innerClass.Name, false) == null) //failsafe
                    currentClass.AddInnerClass(innerClass);

                if (!string.IsNullOrEmpty(queryParametersBaseClassName))
                    innerClass.StartBlock.Inherits = new CodeType { Name = queryParametersBaseClassName, IsExternal = true };
            }
        }
        CrawlTree(current, x => AddInnerClasses(x, prefixClassNameWithParentName, queryParametersBaseClassName, addToParentNamespace, nameFactory));
    }

    private static readonly CodeUsingComparer usingComparerWithDeclarations = new(true);
    private static readonly CodeUsingComparer usingComparerWithoutDeclarations = new(false);
    protected readonly GenerationConfiguration _configuration;

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
                                .Where(static x => x.IsOfKind(CodeParameterKind.Custom, CodeParameterKind.RequestBody, CodeParameterKind.RequestConfiguration))
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


            if (usingsToAdd.Any())
                (currentClass.Parent is CodeClass parentClass ? parentClass : currentClass).AddUsing(usingsToAdd); //lots of languages do not support imports on nested classes
        }
        CrawlTree(current, x => AddPropertiesAndMethodTypesImports(x, includeParentNamespaces, includeCurrentNamespace, compareOnDeclaration, codeTypeFilter));
    }
    protected static void CrawlTree(CodeElement currentElement, Action<CodeElement> function)
    {
        foreach (var childElement in currentElement.GetChildElements())
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
        CrawlTree(currentElement, x => CorrectCoreType(x, correctMethodType, correctPropertyType, correctImplements));
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
    protected static void AddRawUrlConstructorOverload(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
            currentElement.Parent is CodeClass parentClass &&
            parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var overloadCtor = (CodeMethod)currentMethod.Clone();
            overloadCtor.Kind = CodeMethodKind.RawUrlConstructor;
            overloadCtor.OriginalMethod = currentMethod;
            overloadCtor.RemoveParametersByKind(CodeParameterKind.PathParameters, CodeParameterKind.Path);
            overloadCtor.AddParameter(new CodeParameter
            {
                Name = "rawUrl",
                Type = new CodeType { Name = "string", IsExternal = true },
                Optional = false,
                Documentation = new()
                {
                    Description = "The raw URL to use for the request builder.",
                },
                Kind = CodeParameterKind.RawUrl,
            });
            parentClass.AddMethod(overloadCtor);
        }
        CrawlTree(currentElement, AddRawUrlConstructorOverload);
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
    protected static void AddParentClassToErrorClasses(CodeElement currentElement, string parentClassName, string parentClassNamespace, bool addNamespaceToInheritDeclaration = false)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.IsErrorDefinition &&
            currentClass.StartBlock is ClassDeclaration declaration)
        {
            if (declaration.Inherits != null)
                throw new InvalidOperationException("This error class already inherits from another class. Update the description to remove that inheritance.");
            declaration.Inherits = new CodeType
            {
                Name = parentClassName,
            };
            if (addNamespaceToInheritDeclaration)
            {
                declaration.Inherits.TypeDefinition = new CodeType
                {
                    Name = parentClassNamespace,
                    IsExternal = true,
                };
            }
            declaration.AddUsings(new CodeUsing
            {
                Name = parentClassName,
                Declaration = new CodeType
                {
                    Name = parentClassNamespace,
                    IsExternal = true,
                }
            });
        }
        CrawlTree(currentElement, x => AddParentClassToErrorClasses(x, parentClassName, parentClassNamespace, addNamespaceToInheritDeclaration));
    }
    protected static void AddDiscriminatorMappingsUsingsToParentClasses(CodeElement currentElement, string parseNodeInterfaceName, bool addFactoryMethodImport = false, bool addUsings = true)
    {
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.Parent is CodeClass parentClass &&
            parentClass.StartBlock is ClassDeclaration declaration)
        {
            if (currentMethod.IsOfKind(CodeMethodKind.Factory) &&
                (parentClass.DiscriminatorInformation?.HasBasicDiscriminatorInformation ?? false) &&
                parentClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace parentClassNamespace)
            {
                if (addUsings)
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
        CrawlTree(currentElement, x => AddDiscriminatorMappingsUsingsToParentClasses(x, parseNodeInterfaceName, addFactoryMethodImport, addUsings));
    }
    protected static void ReplaceLocalMethodsByGlobalFunctions(CodeElement currentElement, Func<CodeMethod, string> nameUpdateCallback, Func<CodeMethod, CodeUsing[]>? usingsCallback, params CodeMethodKind[] kindsToReplace)
    {
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
        if (currentElement is CodeMethod currentMethod &&
            currentMethod.IsOfKind(CodeMethodKind.Deserializer) &&
            currentMethod.Parent is CodeClass parentClass)
        {
            foreach (var property in parentClass.GetChildElements(true).OfType<CodeProperty>())
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
        elemType.Name = inter.Name;
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
        var existing = targetNS.FindChildByName<CodeInterface>(interfaceName, false);
        if (existing != null)
            return existing;
        var parentClass = modelClass.Parent as CodeClass;
        var insertValue = new CodeInterface
        {
            Name = interfaceName,
            Kind = CodeInterfaceKind.Model,
            OriginalClass = modelClass,
        };
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
        var classModelChildItems = modelClass.GetChildElements(true);
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
        originalType.Name = propertyInterfaceType.Name;
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
            currentClass.Properties.Any(static x => x.IsNameEscaped))
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
                    Description = "Maps the query parameters names to their encoded names for the URI template parsing.",
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
                    Description = "The original query parameter name in the class.",
                },
            });
        }
        CrawlTree(currentElement, x => AddQueryParameterMapperMethod(x, methodName, parameterName));
    }
    protected static CodeMethod? GetMethodClone(CodeMethod currentMethod, params CodeParameterKind[] parameterTypesToExclude)
    {
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
            if (keysToRemove.Any())
                currentClass.DiscriminatorInformation.RemoveDiscriminatorMapping(keysToRemove);
        }
        CrawlTree(currentElement, RemoveDiscriminatorMappingsTargetingSubNamespaces);
    }
    protected void RemoveHandlerFromRequestBuilder(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var codeMethods = currentClass.Methods.Where(x => x.Kind == CodeMethodKind.RequestExecutor);
            foreach (var codeMethod in codeMethods)
            {
                codeMethod.RemoveParametersByKind(CodeParameterKind.ResponseHandler);
            }
        }

        CrawlTree(currentElement, RemoveHandlerFromRequestBuilder);
    }
}
