using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.Refiners;
public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public static readonly string BackingStoreEnabledKey = "backingStoreEnabled";

    public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeduplicateErrorMappings(generatedCode);
            RemoveMethodByKind(generatedCode, CodeMethodKind.RawUrlConstructor, CodeMethodKind.RawUrlBuilder);
            // Invoke the ConvertUnionTypesToWrapper method to maintain a consistent CodeDOM structure. 
            // Note that in the later stages, specifically within the GenerateModelCodeFile() function, the introduced wrapper interface is disregarded. 
            // Instead, a ComposedType is created, which has its own writer, along with the associated Factory, Serializer, and Deserializer functions 
            // that are incorporated into the CodeFile.
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                s => s.ToFirstCharacterLowerCase(),
                false
            );
            ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), static x => $"{x}Escaped");
            ReplaceReservedExceptionPropertyNames(generatedCode, new TypeScriptExceptionsReservedNamesProvider(), static x => $"{x}Escaped");
            MoveRequestBuilderPropertiesToBaseType(generatedCode,
            new CodeUsing
            {
                Name = "BaseRequestBuilder",
                IsErasable = true,
                Declaration = new CodeType
                {
                    Name = AbstractionsPackageName,
                    IsExternal = true
                }
            }, addCurrentTypeAsGenericTypeParameter: true);
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterLowerCase(),
                GenerationLanguage.TypeScript);
            RemoveCancellationParameter(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
            cancellationToken.ThrowIfCancellationRequested();
            RemoveRequestConfigurationClasses(generatedCode,
                new CodeUsing
                {
                    Name = "RequestConfiguration",
                    IsErasable = true,
                    Declaration = new CodeType
                    {
                        Name = AbstractionsPackageName,
                        IsExternal = true,
                    }
                },
                new CodeType
                {
                    Name = "object",
                    IsExternal = true,
                });
            AddInnerClasses(generatedCode,
                true,
                string.Empty,
                true);
            // `AddInnerClasses` will have inner classes moved to their own files, so  we add the imports after so that the files don't miss anything.
            // This is because imports are added at the file level so nested classes would potentially use the higher level imports.
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            DisableActionOf(generatedCode,
                CodeParameterKind.RequestConfiguration);
            cancellationToken.ThrowIfCancellationRequested();
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            ReplaceBinaryByNativeType(generatedCode, "ArrayBuffer", string.Empty, isNullable: true);
            cancellationToken.ThrowIfCancellationRequested();
            AddParentClassToErrorClasses(
                generatedCode,
                "ApiError",
                "@microsoft/kiota-abstractions",
                false,
                true,
                true
            );
            AddConstructorsForDefaultValues(generatedCode, true);
            cancellationToken.ThrowIfCancellationRequested();
            var defaultConfiguration = new GenerationConfiguration();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "@microsoft/kiota-serialization-json.JsonSerializationWriterFactory",
                    "@microsoft/kiota-serialization-text.TextSerializationWriterFactory",
                    "@microsoft/kiota-serialization-form.FormSerializationWriterFactory",
                    "@microsoft/kiota-serialization-multipart.MultipartSerializationWriterFactory",
                }
            );
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new(StringComparer.OrdinalIgnoreCase) {
                    "@microsoft/kiota-serialization-json.JsonParseNodeFactory",
                    "@microsoft/kiota-serialization-text.TextParseNodeFactory",
                    "@microsoft/kiota-serialization-form.FormParseNodeFactory",
                }
            );
            AddSerializationModulesImport(generatedCode,
                [$"{AbstractionsPackageName}.registerDefaultSerializer"],
                [$"{AbstractionsPackageName}.registerDefaultDeserializer"]);
            cancellationToken.ThrowIfCancellationRequested();
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: false
            );
            ReplaceLocalMethodsByGlobalFunctions(
                generatedCode,
                static x => GetFactoryFunctionNameFromTypeName(x.Parent?.Name),
                static x =>
                {
                    var result = new List<CodeUsing>() {
                        new() { Name = "ParseNode", Declaration = new() { Name = AbstractionsPackageName, IsExternal = true }, IsErasable = true }
                    };
                    if (x.Parent?.Parent != null)
                        result.Add(new() { Name = x.Parent.Parent.Name, Declaration = new() { Name = x.Parent.Name, TypeDefinition = x.Parent } });

                    if (x.Parent is CodeClass parentClass && parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation)
                        result.AddRange(parentClass.DiscriminatorInformation
                                        .DiscriminatorMappings
                                        .Select(static y => y.Value)
                                        .OfType<CodeType>()
                                        .Select(static y => new CodeUsing { Name = y.Name, Declaration = y }));
                    return result.ToArray();
                },
                CodeMethodKind.Factory
            );
            static string factoryNameCallbackFromType(CodeType x) => GetFactoryFunctionNameFromTypeName(x.TypeDefinition?.Name);
            cancellationToken.ThrowIfCancellationRequested();
            AddStaticMethodsUsingsForDeserializer(
                generatedCode,
                factoryNameCallbackFromType
            );

            AddStaticMethodsUsingsForRequestExecutor(
                generatedCode,
                factoryNameCallbackFromType
            );
            ReplacePropertyNames(generatedCode,
                [
                    CodePropertyKind.Custom,
                    CodePropertyKind.QueryParameter,
                ],
                static s => s.ToCamelCase(UnderscoreArray));
            IntroducesInterfacesAndFunctions(generatedCode, factoryNameCallbackFromType);
            GenerateEnumObjects(generatedCode);
            AliasUsingsWithSameSymbol(generatedCode);
            var modelsNamespace = generatedCode.FindOrAddNamespace(_configuration.ModelsNamespaceName); // ensuring we have a models namespace in case we don't have any reusable model
            GenerateReusableModelsCodeFiles(modelsNamespace);
            GenerateRequestBuilderCodeFiles(modelsNamespace);
            GroupReusableModelsInSingleFile(modelsNamespace);
            MergeConflictingBuilderCodeFiles(modelsNamespace);
            RemoveSelfReferencingUsings(generatedCode);
            AddAliasToCodeFileUsings(generatedCode);
            CorrectSerializerParameters(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }

    private static void CorrectSerializerParameters(CodeElement currentElement)
    {
        if (currentElement is CodeFunction currentFunction &&
            currentFunction.OriginalLocalMethod.Kind is CodeMethodKind.Serializer)
        {
            foreach (var parameter in currentFunction.OriginalLocalMethod.Parameters
                         .Where(p => GetOriginalComposedType(p.Type) is CodeComposedTypeBase composedType &&
                                     composedType.IsComposedOfObjectsAndPrimitives(IsPrimitiveType)))
            {
                var composedType = GetOriginalComposedType(parameter.Type)!;
                var newType = (CodeComposedTypeBase)composedType.Clone();
                var nonPrimitiveTypes = composedType.Types.Where(x => !IsPrimitiveType(x, composedType)).ToArray();
                newType.SetTypes(nonPrimitiveTypes);
                parameter.Type = newType;
            }
        }

        CrawlTree(currentElement, CorrectSerializerParameters);
    }

    private static void AddAliasToCodeFileUsings(CodeElement currentElement)
    {
        if (currentElement is CodeFile codeFile)
        {
            var enumeratedUsings = codeFile.GetChildElements(true).SelectMany(GetUsingsFromCodeElement).ToArray();
            var duplicatedUsings = enumeratedUsings.Where(static x => !x.IsExternal)
                .Where(static x => x.Declaration != null && x.Declaration.TypeDefinition != null)
                .Where(static x => string.IsNullOrEmpty(x.Alias))
                .GroupBy(static x => x.Declaration!.Name, StringComparer.OrdinalIgnoreCase)
                .Where(static x => x.Count() > 1)
                .Where(static x => x.DistinctBy(static y => y.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>())
                    .Count() > 1)
                .SelectMany(static x => x)
                .ToArray();

            if (duplicatedUsings.Length > 0)
                foreach (var usingElement in duplicatedUsings)
                    usingElement.Alias = (usingElement.Declaration
                                              ?.TypeDefinition
                                              ?.GetImmediateParentOfType<CodeNamespace>()
                                              .Name +
                                          usingElement.Declaration
                                              ?.TypeDefinition
                                              ?.Name.ToFirstCharacterUpperCase())
                        .GetNamespaceImportSymbol()
                        .ToFirstCharacterUpperCase();
        }

        CrawlTree(currentElement, AddAliasToCodeFileUsings);
    }

    private static void GenerateEnumObjects(CodeElement currentElement)
    {
        AddEnumObject(currentElement);
        AddEnumObjectUsings(currentElement);
    }
    private const string FileNameForModels = "index";
    private static void GroupReusableModelsInSingleFile(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace codeNamespace)
        {
            var targetFile = codeNamespace.TryAddCodeFile(FileNameForModels);
            foreach (var otherFile in codeNamespace.Files.Except([targetFile]))
            {
                targetFile.AddUsing(otherFile.Usings.ToArray());
                targetFile.AddElements(otherFile.GetChildElements(true).ToArray());
                codeNamespace.RemoveChildElement(otherFile);
            }
            if (codeNamespace.Enums.ToArray() is { Length: > 0 } enums)
            {
                var enumObjects = enums.Select(static x => x.CodeEnumObject).OfType<CodeConstant>().ToArray();
                targetFile.AddElements(enumObjects);
                targetFile.AddElements(enums);
                codeNamespace.RemoveChildElement(enumObjects);
                codeNamespace.RemoveChildElement(enums);
            }
            RemoveSelfReferencingUsingForFile(targetFile, codeNamespace);
            var childElements = targetFile.GetChildElements(true).ToArray();
            AliasCollidingSymbols(childElements.SelectMany(GetUsingsFromCodeElement).Distinct(), childElements.Select(static x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
        }
        CrawlTree(currentElement, GroupReusableModelsInSingleFile);
    }
    private void GenerateReusableModelsCodeFiles(CodeElement currentElement)
    {
        if (currentElement.Parent is CodeNamespace codeNamespace && currentElement is CodeInterface codeInterface && codeInterface.IsOfKind(CodeInterfaceKind.Model))
            GenerateModelCodeFile(codeInterface, codeNamespace);
        CrawlTree(currentElement, GenerateReusableModelsCodeFiles);
    }
    private static void GenerateRequestBuilderCodeFiles(CodeNamespace modelsNamespace)
    {
        if (modelsNamespace.Parent is not CodeNamespace mainNamespace) return;
        var elementsToConsider = mainNamespace.Namespaces.Except([modelsNamespace]).OfType<CodeElement>().Union(mainNamespace.Classes).ToArray();
        foreach (var element in elementsToConsider)
            GenerateRequestBuilderCodeFilesForElement(element);

        foreach (var element in elementsToConsider)
            AddDownwardsConstantsImports(element);
    }
    private static void GenerateRequestBuilderCodeFilesForElement(CodeElement currentElement)
    {
        if (currentElement.Parent is CodeNamespace codeNamespace && currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
            GenerateRequestBuilderCodeFile(ReplaceRequestBuilderClassByInterface(currentClass, codeNamespace), codeNamespace);
        CrawlTree(currentElement, GenerateRequestBuilderCodeFilesForElement);
    }
    private static void AddDownwardsConstantsImports(CodeElement currentElement)
    {
        if (currentElement is CodeInterface currentInterface &&
            currentInterface.Kind is CodeInterfaceKind.RequestBuilder &&
            currentElement.Parent is CodeFile codeFile &&
            codeFile.Parent is CodeNamespace parentNamespace &&
            parentNamespace.Parent is CodeNamespace parentLevelNamespace &&
            parentLevelNamespace.Files.SelectMany(static x => x.Interfaces).FirstOrDefault(static x => x.Kind is CodeInterfaceKind.RequestBuilder) is CodeInterface parentLevelInterface &&
            codeFile.Constants
                .Where(static x => x.Kind is CodeConstantKind.NavigationMetadata or CodeConstantKind.RequestsMetadata)
                .Select(static x => new CodeUsing
                {
                    Name = x.Name,
                    Declaration = new CodeType
                    {
                        TypeDefinition = x,
                    },
                })
                .ToArray() is { Length: > 0 } constantUsings)
            parentLevelInterface.AddUsing(constantUsings);
        CrawlTree(currentElement, AddDownwardsConstantsImports);
    }

    private static void MergeConflictingBuilderCodeFiles(CodeNamespace modelsNamespace)
    {
        if (modelsNamespace.Parent is not CodeNamespace mainNamespace) return;
        var elementsToConsider = mainNamespace.Namespaces.Except([modelsNamespace]).OfType<CodeElement>().ToArray();
        foreach (var element in elementsToConsider)
            MergeConflictingBuilderCodeFilesForElement(element);
    }
    private static void MergeConflictingBuilderCodeFilesForElement(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace currentNamespace && currentNamespace.Files.ToArray() is { Length: > 1 } codeFiles)
        {
            var targetFile = codeFiles.First();
            foreach (var fileToMerge in codeFiles.Skip(1))
            {
                if (fileToMerge.Classes.Any())
                    targetFile.AddElements(fileToMerge.Classes.ToArray());
                if (fileToMerge.Constants.Any())
                    targetFile.AddElements(fileToMerge.Constants.ToArray());
                if (fileToMerge.Enums.Any())
                    targetFile.AddElements(fileToMerge.Enums.ToArray());
                if (fileToMerge.Interfaces.Any())
                    targetFile.AddElements(fileToMerge.Interfaces.ToArray());
                if (fileToMerge.Usings.Any())
                    targetFile.AddElements(fileToMerge.Usings.ToArray());
                currentNamespace.RemoveChildElement(fileToMerge);
            }
        }
        CrawlTree(currentElement, MergeConflictingBuilderCodeFilesForElement);
    }

    private static CodeFile? GenerateModelCodeFile(CodeInterface codeInterface, CodeNamespace codeNamespace)
    {
        var functions = GetSerializationAndFactoryFunctions(codeInterface, codeNamespace).ToArray();

        if (functions.Length == 0)
            return null;

        var composedType = GetOriginalComposedType(codeInterface);
        var elements = composedType is null ? new List<CodeElement> { codeInterface }.Concat(functions) : GetCodeFileElementsForComposedType(codeInterface, codeNamespace, composedType, functions);

        return codeNamespace.TryAddCodeFile(codeInterface.Name, elements.ToArray());
    }

    private static IEnumerable<CodeFunction> GetSerializationAndFactoryFunctions(CodeInterface codeInterface, CodeNamespace codeNamespace)
    {
        return codeNamespace.GetChildElements(true)
            .OfType<CodeFunction>()
            .Where(codeFunction =>
                IsDeserializerOrSerializerFunction(codeFunction, codeInterface) ||
                IsFactoryFunction(codeFunction, codeInterface, codeNamespace));
    }

    private static bool IsDeserializerOrSerializerFunction(CodeFunction codeFunction, CodeInterface codeInterface)
    {
        return codeFunction.OriginalLocalMethod.Kind is CodeMethodKind.Deserializer or CodeMethodKind.Serializer &&
            codeFunction.OriginalLocalMethod.Parameters.Any(x => x.Type is CodeType codeType && codeType.TypeDefinition == codeInterface);
    }

    private static bool IsFactoryFunction(CodeFunction codeFunction, CodeInterface codeInterface, CodeNamespace codeNamespace)
    {
        return codeFunction.OriginalLocalMethod.Kind is CodeMethodKind.Factory &&
            codeInterface.Name.EqualsIgnoreCase(codeFunction.OriginalMethodParentClass.Name) &&
            codeFunction.OriginalMethodParentClass.IsChildOf(codeNamespace);
    }

    private static List<CodeElement> GetCodeFileElementsForComposedType(CodeInterface codeInterface, CodeNamespace codeNamespace, CodeComposedTypeBase composedType, CodeFunction[] functions)
    {
        var children = new List<CodeElement>(functions)
        {
            // Add the composed type, The writer will output the composed type as a type definition e.g export type Pet = Cat | Dog
            composedType
        };

        ReplaceFactoryMethodForComposedType(composedType, children);
        ReplaceSerializerMethodForComposedType(composedType, children);
        ReplaceDeserializerMethodForComposedType(codeInterface, codeNamespace, composedType, children);

        return children;
    }

    private static CodeFunction? FindFunctionOfKind(List<CodeElement> elements, CodeMethodKind kind)
    {
        return elements.OfType<CodeFunction>().FirstOrDefault(function => function.OriginalLocalMethod.IsOfKind(kind));
    }

    private static void RemoveUnusedDeserializerImport(List<CodeElement> children, CodeFunction factoryFunction)
    {
        if (FindFunctionOfKind(children, CodeMethodKind.Deserializer) is { } deserializerMethod)
            factoryFunction.RemoveUsingsByDeclarationName(deserializerMethod.Name);
    }

    private static void ReplaceFactoryMethodForComposedType(CodeComposedTypeBase composedType, List<CodeElement> children)
    {
        if (composedType is null || FindFunctionOfKind(children, CodeMethodKind.Factory) is not { } function) return;

        if (composedType.IsComposedOfPrimitives(IsPrimitiveType))
        {
            function.OriginalLocalMethod.ReturnType = composedType;
            // Remove the deserializer import statement if its not being used
            RemoveUnusedDeserializerImport(children, function);
        }
    }

    private static void ReplaceSerializerMethodForComposedType(CodeComposedTypeBase composedType, List<CodeElement> children)
    {
        if (FindFunctionOfKind(children, CodeMethodKind.Serializer) is not { } function) return;

        // Add the key parameter if the composed type is a union of primitive values
        if (composedType.IsComposedOfPrimitives(IsPrimitiveType))
            function.OriginalLocalMethod.AddParameter(CreateKeyParameter());

        // Add code usings for each individual item since the functions can be invoked to serialize/deserialize the contained classes/interfaces
        AddSerializationUsingsForCodeComposed(composedType, function, CodeMethodKind.Serializer);
    }

    private static void AddSerializationUsingsForCodeComposed(CodeComposedTypeBase composedType, CodeFunction function, CodeMethodKind kind)
    {
        // Add code usings for each individual item since the functions can be invoked to serialize/deserialize the contained classes/interfaces
        foreach (var codeClass in composedType.Types.Where(x => !IsPrimitiveType(x, composedType))
                .Select(static x => x.TypeDefinition)
                     .OfType<CodeInterface>()
                     .Select(static x => x.OriginalClass)
                     .OfType<CodeClass>())
        {
            var (serializer, deserializer) = GetSerializationFunctionsForNamespace(codeClass);
            if (kind == CodeMethodKind.Serializer)
                AddSerializationUsingsToFunction(function, serializer);
            if (kind == CodeMethodKind.Deserializer)
                AddSerializationUsingsToFunction(function, deserializer);
        }
    }

    private static void AddSerializationUsingsToFunction(CodeFunction function, CodeFunction serializationFunction)
    {
        if (serializationFunction.Parent is not null)
        {
            function.AddUsing(new CodeUsing
            {
                Name = serializationFunction.Parent.Name,
                Declaration = new CodeType
                {
                    Name = serializationFunction.Name,
                    TypeDefinition = serializationFunction
                }
            });
        }
    }

    private static void ReplaceDeserializerMethodForComposedType(CodeInterface codeInterface, CodeNamespace codeNamespace, CodeComposedTypeBase composedType, List<CodeElement> children)
    {
        if (FindFunctionOfKind(children, CodeMethodKind.Deserializer) is not { } deserializerMethod) return;

        // Deserializer function is not required for primitive values
        if (composedType.IsComposedOfPrimitives(IsPrimitiveType))
        {
            children.Remove(deserializerMethod);
            codeInterface.RemoveChildElement(deserializerMethod);
            codeNamespace.RemoveChildElement(deserializerMethod);
        }

        // Add code usings for each individual item since the functions can be invoked to serialize/deserialize the contained classes/interfaces
        AddSerializationUsingsForCodeComposed(composedType, deserializerMethod, CodeMethodKind.Deserializer);
    }

    private static CodeParameter CreateKeyParameter()
    {
        return new CodeParameter
        {
            Name = "key",
            Type = new CodeType { Name = "string", IsExternal = true, IsNullable = false },
            Optional = false,
            Documentation = new()
            {
                DescriptionTemplate = "The name of the property to write in the serialization.",
            },
        };
    }

    public static CodeComposedTypeBase? GetOriginalComposedType(CodeElement element)
    {
        return element switch
        {
            CodeParameter param => GetOriginalComposedType(param.Type),
            CodeType codeType when codeType.TypeDefinition is not null => GetOriginalComposedType(codeType.TypeDefinition),
            CodeClass codeClass => codeClass.OriginalComposedType,
            CodeInterface codeInterface => codeInterface.OriginalClass.OriginalComposedType,
            CodeComposedTypeBase composedType => composedType,
            _ => null,
        };
    }

    private static readonly CodeUsing[] navigationMetadataUsings = [
        new CodeUsing
        {
            Name = "NavigationMetadata",
            IsErasable = true,
            Declaration = new CodeType
            {
                Name = AbstractionsPackageName,
                IsExternal = true,
            },
        },
        new CodeUsing
        {
            Name = "KeysToExcludeForNavigationMetadata",
            IsErasable = true,
            Declaration = new CodeType
            {
                Name = AbstractionsPackageName,
                IsExternal = true,
            },
        }];
    private static readonly CodeUsing[] requestMetadataUsings = [
        new CodeUsing
        {
            Name = "RequestsMetadata",
            IsErasable = true,
            Declaration = new CodeType
            {
                Name = AbstractionsPackageName,
                IsExternal = true,
            },
        },
    ];
    private static CodeInterface ReplaceRequestBuilderClassByInterface(CodeClass codeClass, CodeNamespace codeNamespace)
    {
        if (CodeConstant.FromRequestBuilderToRequestsMetadata(codeClass, requestMetadataUsings) is CodeConstant requestsMetadataConstant)
            codeNamespace.AddConstant(requestsMetadataConstant);
        if (CodeConstant.FromRequestBuilderToNavigationMetadata(codeClass, navigationMetadataUsings) is CodeConstant navigationConstant)
            codeNamespace.AddConstant(navigationConstant);
        if (CodeConstant.FromRequestBuilderClassToUriTemplate(codeClass) is CodeConstant uriTemplateConstant)
            codeNamespace.AddConstant(uriTemplateConstant);
        if (codeClass.Methods.FirstOrDefault(static x => x.Kind is CodeMethodKind.ClientConstructor) is CodeMethod clientConstructor)
        {
            clientConstructor.IsStatic = true;
            clientConstructor.Name = $"Create{codeClass.Name.ToFirstCharacterUpperCase()}";

            codeNamespace.AddFunction(new CodeFunction(clientConstructor)).First().AddUsing(new CodeUsing
            {
                Name = "apiClientProxifier",
                Declaration = new CodeType
                {
                    Name = AbstractionsPackageName,
                    IsExternal = true,
                },
            });
        }
        var interfaceDeclaration = CodeInterface.FromRequestBuilder(codeClass);
        codeNamespace.RemoveChildElement(codeClass);
        codeNamespace.AddInterface(interfaceDeclaration);
        return interfaceDeclaration;

    }
    private static void GenerateRequestBuilderCodeFile(CodeInterface codeInterface, CodeNamespace codeNamespace)
    {
        var executorMethods = codeInterface.Methods
            .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor))
            .ToArray();

        var inlineEnums = codeNamespace
            .Enums
            .ToArray();

        var enumObjects = inlineEnums
            .Select(static x => x.CodeEnumObject)
            .OfType<CodeConstant>()
            .ToArray();

        var queryParameterInterfaces = executorMethods
            .SelectMany(static x => x.Parameters)
            .Where(static x => x.IsOfKind(CodeParameterKind.RequestConfiguration))
            .Select(static x => x.Type)
            .OfType<CodeType>()
            .Select(static x => x.GenericTypeParameterValues.FirstOrDefault()?.Name)
            .OfType<string>()
            .Select(x => codeNamespace.FindChildByName<CodeInterface>(x, false))
            .OfType<CodeInterface>()
            .ToArray();

        var inlineRequestAndResponseBodyFiles = codeNamespace.Interfaces
            .Where(static x => x.Kind is CodeInterfaceKind.Model)
            .Select(x => GenerateModelCodeFile(x, codeNamespace))
            .OfType<CodeFile>()
            .ToArray();

        var queryParametersMapperConstants = queryParameterInterfaces
            .Select(static x => $"{x.Name.ToFirstCharacterLowerCase()}Mapper")
            .Select(x => codeNamespace.FindChildByName<CodeConstant>(x, false))
            .OfType<CodeConstant>()
            .ToArray();

        var navigationConstant = codeNamespace.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterLowerCase()}{CodeConstant.NavigationMetadataSuffix}", false);
        var requestsMetadataConstant = codeNamespace.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterLowerCase()}{CodeConstant.RequestsMetadataSuffix}", false);
        var uriTemplateConstant = codeNamespace.FindChildByName<CodeConstant>($"{codeInterface.Name.ToFirstCharacterLowerCase()}{CodeConstant.UriTemplateSuffix}", false);

        var proxyConstants = new[] { navigationConstant, requestsMetadataConstant, uriTemplateConstant }
            .OfType<CodeConstant>()
            .ToArray();

        var clientConstructorFunction = codeNamespace.FindChildByName<CodeFunction>($"Create{codeInterface.Name.ToFirstCharacterUpperCase()}", false);

        codeNamespace.RemoveChildElement(inlineRequestAndResponseBodyFiles);
        var elements = new CodeElement[] { codeInterface }
                            .Union(clientConstructorFunction is not null ? new[] { clientConstructorFunction } : Array.Empty<CodeElement>())
                            .Union(proxyConstants)
                            .Union(queryParameterInterfaces)
                            .Union(queryParametersMapperConstants)
                            .Union(inlineRequestAndResponseBodyFiles.SelectMany(static x => x.GetChildElements(true)))
                            .Union(inlineEnums)
                            .Union(enumObjects)
                            .Distinct()
                            .ToArray();

        codeNamespace.TryAddCodeFile(codeInterface.Name, elements);
    }

    public static IEnumerable<CodeUsing> GetUsingsFromCodeElement(CodeElement codeElement)
    {
        return codeElement switch
        {
            CodeFunction f => f.StartBlock.Usings,
            CodeInterface ci => ci.Usings,
            CodeEnum ce => ce.Usings,
            CodeClass cc => cc.Usings,
            CodeConstant codeConstant => codeConstant.StartBlock.Usings,
            _ => Enumerable.Empty<CodeUsing>()
        };
    }
    private static void RemoveSelfReferencingUsingForFile(CodeFile codeFile, CodeNamespace codeNamespace)
    {
        // correct the using values
        // eliminate the using referring the elements in the same file
        var elementSet = codeFile.GetChildElements(true).Select(static x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fileElements = codeFile.GetChildElements(true).ToArray();
        foreach (var element in fileElements)
        {
            var foundUsingsNames = GetUsingsFromCodeElement(element)
                .Select(static x => x.Declaration?.TypeDefinition)
                .OfType<CodeElement>()
                .Where(x => x.GetImmediateParentOfType<CodeNamespace>() == codeNamespace)
                .Where(x => elementSet.Contains(x.Name))
                .Select(static x => x.Name);

            foreach (var x in foundUsingsNames)
            {
                switch (element)
                {
                    case CodeFunction ci:
                        ci.RemoveUsingsByDeclarationName(x);
                        break;
                    case CodeInterface ci:
                        ci.RemoveUsingsByDeclarationName(x);
                        break;
                    case CodeEnum ci:
                        ci.RemoveUsingsByDeclarationName(x);
                        break;
                    case CodeClass ci:
                        ci.RemoveUsingsByDeclarationName(x);
                        break;
                }
            }
        }
    }
    private static void RemoveSelfReferencingUsings(CodeElement currentElement)
    {
        if (currentElement is CodeFile { Parent: CodeNamespace codeNamespace } codeFile)
            RemoveSelfReferencingUsingForFile(codeFile, codeNamespace);

        CrawlTree(currentElement, RemoveSelfReferencingUsings);
    }

    private static void AliasCollidingSymbols(IEnumerable<CodeUsing> usings, string currentSymbolName)
    {
        AliasCollidingSymbols(usings, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { currentSymbolName });
    }
    private static void AliasCollidingSymbols(IEnumerable<CodeUsing> usings, HashSet<string> currentSymbolNames)
    {
        var enumeratedUsings = usings.ToArray();
        var duplicatedSymbolsUsings = enumeratedUsings.Where(static x => !x.IsExternal)
                                                                .Where(static x => x.Declaration != null && x.Declaration.TypeDefinition != null)
                                                                .GroupBy(static x => x.Declaration!.Name, StringComparer.OrdinalIgnoreCase)
                                                                .Where(static x => x.DistinctBy(static y => y.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>())
                                                                                    .Count() > 1)
                                                                .SelectMany(static x => x)
                                                                .Union(enumeratedUsings
                                                                        .Where(static x => !x.IsExternal && x.Declaration != null)
                                                                        .Where(x => currentSymbolNames.Contains(x.Declaration!.Name)))
                                                                .ToArray();
        foreach (var usingElement in duplicatedSymbolsUsings)
            usingElement.Alias = (usingElement.Declaration
                                            ?.TypeDefinition
                                            ?.GetImmediateParentOfType<CodeNamespace>()
                                            .Name +
                                usingElement.Declaration
                                            ?.TypeDefinition
                                            ?.Name.ToFirstCharacterUpperCase())
                                .GetNamespaceImportSymbol()
                                .ToFirstCharacterUpperCase();
    }
    private static void AliasUsingsWithSameSymbol(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration currentDeclaration &&
            currentDeclaration.Usings.Any(static x => !x.IsExternal))
        {
            AliasCollidingSymbols(currentDeclaration.Usings, currentClass.Name);
        }
        else if (currentElement is CodeFunction currentFunction &&
                    currentFunction.StartBlock is BlockDeclaration currentFunctionDeclaration &&
                    currentFunctionDeclaration.Usings.Any(static x => !x.IsExternal))
        {
            AliasCollidingSymbols(currentFunctionDeclaration.Usings, currentFunction.Name);
        }
        else if (currentElement is CodeInterface currentInterface &&
                    currentInterface.StartBlock is InterfaceDeclaration interfaceDeclaration &&
                    interfaceDeclaration.Usings.Any(static x => !x.IsExternal))
        {
            AliasCollidingSymbols(interfaceDeclaration.Usings, currentInterface.Name);
        }
        CrawlTree(currentElement, AliasUsingsWithSameSymbol);
    }
    private const string GuidPackageName = "guid-typescript";
    private const string AbstractionsPackageName = "@microsoft/kiota-abstractions";
    // A helper method to check if a parameter is a multipart body
    private static bool IsMultipartBody(CodeParameter p) =>
        p.IsOfKind(CodeParameterKind.RequestBody) &&
        p.Type.Name.Equals(MultipartBodyClassName, StringComparison.OrdinalIgnoreCase);

    // A helper method to check if a method has a multipart body parameter
    private static bool HasMultipartBody(CodeMethod m) =>
        m.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator) &&
        m.Parameters.Any(IsMultipartBody);
    // for Kiota abstraction library if the code is not required for runtime purposes e.g. interfaces then the IsErasable flag is set to true
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (static x => x is CodeMethod method && method.Kind is CodeMethodKind.ClientConstructor,
            AbstractionsPackageName, true, "RequestAdapter"),
        new (static x => x is CodeMethod method && method.Kind is CodeMethodKind.RequestGenerator,
            AbstractionsPackageName, true, "RequestInformation"),
        new (static x => x is CodeMethod method && method.Kind is CodeMethodKind.Serializer,
            AbstractionsPackageName, true,"SerializationWriter"),
        new (static x => x is CodeMethod method && method.Kind is CodeMethodKind.Deserializer or CodeMethodKind.Factory,
            AbstractionsPackageName, true, "ParseNode"),
        new (static x => x is CodeMethod method && method.Kind is CodeMethodKind.RequestExecutor,
            AbstractionsPackageName, true, "Parsable", "ParsableFactory"),
        new (static x => x is CodeClass @class && @class.Kind is CodeClassKind.Model,
            AbstractionsPackageName, true, "Parsable"),
        new (static x => x is CodeClass @class && @class.Kind is CodeClassKind.Model && @class.Properties.Any(static x => x.Kind is CodePropertyKind.AdditionalData),
            AbstractionsPackageName, true, "AdditionalDataHolder"),
        new (static x => x is CodeMethod method && method.Kind is CodeMethodKind.ClientConstructor &&
                    method.Parameters.Any(static y => y.Kind is CodeParameterKind.BackingStore),
            AbstractionsPackageName, true, "BackingStoreFactory"),
        new (static x => x is CodeProperty prop && prop.Kind is CodePropertyKind.BackingStore,
            AbstractionsPackageName, true, "BackingStore", "BackedModel"),
        new (static x => x is CodeMethod m && HasMultipartBody(m),
            AbstractionsPackageName, MultipartBodyClassName, $"serialize{MultipartBodyClassName}"),
        new (static x => (x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))
                         || (x is CodeMethod method && (method.Parameters.Any(param => param.Kind is CodeParameterKind.RequestBody && param.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)) || method.ReturnType.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))),
            AbstractionsPackageName, "createUntypedNodeFromDiscriminatorValue"),
        new (static x => (x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Custom) && prop.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))
                         || (x is CodeMethod method && (method.Parameters.Any(param => param.Kind is CodeParameterKind.RequestBody && param.Type.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)) || method.ReturnType.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))),
            AbstractionsPackageName, true, KiotaBuilder.UntypedNodeName),
    };
    private const string MultipartBodyClassName = "MultipartBody";
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            currentProperty.Type.Name = "RequestAdapter";
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
        {
            currentProperty.Type.Name = "boolean";
            currentProperty.Name = BackingStoreEnabledKey;
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.Type.Name = "RequestOption[]";
        else if (currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.Type.Name = "Record<string, string[]>";
        else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
        {
            currentProperty.Type.Name = "Record<string, unknown>";
            currentProperty.DefaultValue = "{}";
        }
        else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
        {
            currentProperty.Type.IsNullable = false;
            currentProperty.Type.Name = "Record<string, unknown>";
            if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = string.Empty;
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator))
        {
            if (currentMethod.Parameters.OfKind(CodeParameterKind.RequestBody) is CodeParameter requestBodyParam)
                requestBodyParam.Type.IsNullable = false;
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith('I')).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
            currentMethod.ReturnType.Name = "Record<string, (node: ParseNode) => void>";
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            if (currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters)) is CodeParameter urlTplParams && urlTplParams.Type is CodeType originalType)
            {
                originalType.Name = "Record<string, unknown>";
                urlTplParams.Documentation.DescriptionTemplate = "The raw url or the Url template parameters for the request.";
                var unionType = new CodeUnionType
                {
                    Name = "rawUrlOrTemplateParameters",
                    IsNullable = true,
                };
                unionType.AddType(originalType, new()
                {
                    Name = "string",
                    IsNullable = true,
                    IsExternal = true,
                });
                urlTplParams.Type = unionType;
            }
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Factory) && currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
        else if (currentMethod.IsOfKind(CodeMethodKind.RawUrlBuilder) && currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl) is { } parameter)
        {
            parameter.Type.IsNullable = false;
        }
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
    {"DateTimeOffset", ("Date", null)},
    {"TimeSpan", ("Duration", new CodeUsing {
                                    Name = "Duration",
                                    Declaration = new CodeType {
                                        Name = AbstractionsPackageName,
                                        IsExternal = true,
                                    },
                                    IsErasable = true, // the import is used only for the type, not for the value
                                })},
    {"DateOnly", (string.Empty, new CodeUsing {
                            Name = "DateOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                            IsErasable = true, // the import is used only for the type, not for the value
                        })},
    {"TimeOnly", (string.Empty, new CodeUsing {
                            Name = "TimeOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                            IsErasable = true, // the import is used only for the type, not for the value
                        })},
    {"Guid", (string.Empty, new CodeUsing {
                            Name = "Guid",
                            Declaration = new CodeType {
                                Name = GuidPackageName,
                                IsExternal = true,
                            },
                            IsErasable = true, // the import is used only for the type, not for the value
                        })},
    };

    private static void ReplaceRequestQueryParamsWithInterfaces(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass &&
            codeClass.IsOfKind(CodeClassKind.QueryParameters) &&
            codeClass.Parent is CodeClass parentClass &&
            codeClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace targetNS &&
            targetNS.FindChildByName<CodeInterface>(codeClass.Name, false) is null)
        {
            var insertValue = new CodeInterface
            {
                Name = codeClass.Name,
                Kind = CodeInterfaceKind.QueryParameters,
                Documentation = codeClass.Documentation,
                Deprecation = codeClass.Deprecation,
                OriginalClass = codeClass
            };
            parentClass.RemoveChildElement(codeClass);
            var codeInterface = targetNS.AddInterface(insertValue).First();

            var props = codeClass.Properties.ToArray();
            codeInterface.AddProperty(props);

            if (CodeConstant.FromQueryParametersMapping(codeInterface) is CodeConstant constant)
                targetNS.AddConstant(constant);

            var usings = codeClass.Usings.ToArray();
            codeInterface.AddUsing(usings);
        }
        CrawlTree(currentElement, ReplaceRequestQueryParamsWithInterfaces);
    }

    private const string ModelSerializerPrefix = "serialize";
    private const string ModelDeserializerPrefix = "deserializeInto";

    /// <summary>
    /// TypeScript generation outputs models as interfaces 
    /// and serializers and deserializers as javascript functions. 
    /// </summary>
    /// <param name="generatedCode"></param>
    private static void IntroducesInterfacesAndFunctions(CodeElement generatedCode, Func<CodeType, string> functionNameCallback)
    {
        CreateSeparateSerializers(generatedCode);
        CreateInterfaceModels(generatedCode);
        AddDeserializerUsingToDiscriminatorFactory(generatedCode);
        ReplaceRequestQueryParamsWithInterfaces(generatedCode);
        AddStaticMethodsUsingsToDeserializerFunctions(generatedCode, functionNameCallback);
    }

    private static void CreateSeparateSerializers(CodeElement codeElement)
    {
        if (codeElement is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model))
        {
            CreateSerializationFunctions(codeClass);
        }
        CrawlTree(codeElement, CreateSeparateSerializers);
    }

    private static void CreateSerializationFunctions(CodeClass modelClass)
    {
        var namespaceOfModel = modelClass.GetImmediateParentOfType<CodeNamespace>();
        if (modelClass.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Serializer)) is not CodeMethod serializerMethod || modelClass.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Deserializer)) is not CodeMethod deserializerMethod)
        {
            throw new InvalidOperationException($"The refiner was unable to create local serializer/deserializer method for {modelClass.Name}");
        }
        serializerMethod.IsStatic = true;
        deserializerMethod.IsStatic = true;

        var serializerFunction = new CodeFunction(serializerMethod)
        {
            Name = $"{ModelSerializerPrefix}{modelClass.Name.ToFirstCharacterUpperCase()}",
        };

        var deserializerFunction = new CodeFunction(deserializerMethod)
        {
            Name = $"{ModelDeserializerPrefix}{modelClass.Name.ToFirstCharacterUpperCase()}",
        };

        foreach (var codeUsing in modelClass.Usings.Where(static x => x.Declaration is not null && x.Declaration.IsExternal))
        {
            deserializerFunction.AddUsing(codeUsing);
            serializerFunction.AddUsing(codeUsing);
        }

        namespaceOfModel.AddFunction(deserializerFunction);
        namespaceOfModel.AddFunction(serializerFunction);
    }

    private static void AddInterfaceParamToSerializer(CodeInterface modelInterface, CodeFunction codeFunction)
    {
        var method = codeFunction.OriginalLocalMethod;

        method.AddParameter(new CodeParameter
        {
            Name = GetFinalInterfaceName(modelInterface),
            DefaultValue = "{}",
            Type = new CodeType { Name = GetFinalInterfaceName(modelInterface), TypeDefinition = modelInterface },
            Kind = CodeParameterKind.DeserializationTarget,
        });

        if (modelInterface.Parent is not null)
        {
            codeFunction.AddUsing(new CodeUsing
            {
                Name = modelInterface.Parent.Name,
                Declaration = new CodeType
                {
                    Name = GetFinalInterfaceName(modelInterface),
                    TypeDefinition = modelInterface
                }
            });
        }
    }

    /// <summary>
    /// Convert models from CodeClass to CodeInterface 
    /// and removes model classes from the generated code.
    /// </summary>
    /// <param name="generatedCode"></param>
    private static void CreateInterfaceModels(CodeElement generatedCode)
    {
        GenerateModelInterfaces(
           generatedCode,
           static x => $"{x.Name.ToFirstCharacterUpperCase()}Interface".ToFirstCharacterUpperCase()
       );

        RenameModelInterfacesAndRemoveClasses(generatedCode);
    }

    /// <summary>
    /// Removes the "Interface" suffix temporarily added to model interface names
    /// Adds the "Impl" suffix to model class names.
    /// </summary>
    /// <param name="currentElement"></param>
    private static void RenameModelInterfacesAndRemoveClasses(CodeElement currentElement)
    {
        if (currentElement is CodeInterface modelInterface && modelInterface.IsOfKind(CodeInterfaceKind.Model) && modelInterface.Parent is CodeNamespace parentNS)
        {
            var finalName = GetFinalInterfaceName(modelInterface);
            if (!finalName.Equals(modelInterface.Name, StringComparison.Ordinal))
            {
                if (parentNS.FindChildByName<CodeClass>(finalName, false) is CodeClass existingClassToRemove)
                    parentNS.RemoveChildElement(existingClassToRemove);
                parentNS.RenameChildElement(modelInterface.Name, finalName);
            }
        }
        else if (currentElement is CodeFunction codeFunction && codeFunction.OriginalLocalMethod.IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer))
        {
            RenameCodeInterfaceParamsInSerializers(codeFunction);
        }

        CrawlTree(currentElement, RenameModelInterfacesAndRemoveClasses);
    }

    private static void RenameCodeInterfaceParamsInSerializers(CodeFunction codeFunction)
    {
        if (codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault(static x => x.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface) is CodeParameter param && param.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface paramInterface)
        {
            param.Name = GetFinalInterfaceName(paramInterface);
        }
    }

    private static string GetFinalInterfaceName(CodeInterface codeInterface)
    {
        return codeInterface.OriginalClass.Name.ToFirstCharacterUpperCase();
    }

    private static void GenerateModelInterfaces(CodeElement currentElement, Func<CodeClass, string> interfaceNamingCallback)
    {
        if (currentElement is CodeClass currentClass)
        {
            if (currentClass.IsOfKind(CodeClassKind.Model))
            {
                var modelInterface = CreateModelInterface(currentClass, interfaceNamingCallback);

                var serializationFunctions = GetSerializationFunctionsForNamespace(currentClass);

                AddInterfaceParamToSerializer(modelInterface, serializationFunctions.Item1);

                AddInterfaceParamToSerializer(modelInterface, serializationFunctions.Item2);
            }
            else if (currentClass.IsOfKind(CodeClassKind.RequestBuilder))
            {
                ProcessorRequestBuilders(currentClass, interfaceNamingCallback);
            }
        }

        CrawlTree(currentElement, x => GenerateModelInterfaces(x, interfaceNamingCallback));
    }

    private static void ProcessorRequestBuilders(CodeClass requestBuilderClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        foreach (var codeMethod in requestBuilderClass.Methods)
        {
            ProcessModelsAssociatedWithMethods(codeMethod, requestBuilderClass, interfaceNamingCallback);
        }
    }
    private static (CodeFunction, CodeFunction) GetSerializationFunctionsForNamespace(CodeClass codeClass)
    {
        if (codeClass.Parent is not CodeNamespace parentNamespace)
            throw new InvalidOperationException($"Model class {codeClass}'s parent namespace is null");
        var serializer = parentNamespace.FindChildByName<CodeFunction>($"{ModelSerializerPrefix}{codeClass.Name.ToFirstCharacterUpperCase()}") ??
            throw new InvalidOperationException($"Serializer not found for {codeClass.Name}");
        var deserializer = parentNamespace.FindChildByName<CodeFunction>($"{ModelDeserializerPrefix}{codeClass.Name.ToFirstCharacterUpperCase()}") ??
            throw new InvalidOperationException($"Deserializer not found for {codeClass.Name}");
        return (serializer, deserializer);
    }
    private static void AddSerializationUsingToRequestBuilder(CodeClass modelClass, CodeClass targetClass)
    {
        var (serializer, _) = GetSerializationFunctionsForNamespace(modelClass);
        if (serializer.Parent is not null)
        {
            targetClass.AddUsing(new CodeUsing
            {
                Name = serializer.Parent.Name,
                Declaration = new CodeType
                {
                    Name = serializer.Name,
                    TypeDefinition = serializer
                }
            });
        }
    }

    private static void ProcessModelsAssociatedWithMethods(CodeMethod codeMethod, CodeClass requestBuilderClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Setting request body parameter type of request executor to model interface.
         */
        if (codeMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody)) is CodeParameter requestBodyParam &&
            requestBodyParam.Type is CodeType requestBodyType && requestBodyType.TypeDefinition is CodeClass requestBodyClass)
        {
            SetTypeAsModelInterface(CreateModelInterface(requestBodyClass, interfaceNamingCallback), requestBodyType, requestBuilderClass);

            if (codeMethod.IsOfKind(CodeMethodKind.RequestGenerator))
            {
                ProcessModelClassAssociatedWithRequestGenerator(codeMethod, requestBodyClass);
            }

            if (codeMethod.ReturnType is CodeType returnType &&
                returnType.TypeDefinition is CodeClass returnClass &&
                codeMethod.GetImmediateParentOfType<CodeClass>() is CodeClass parentClass &&
                returnClass.IsOfKind(CodeClassKind.Model) && !parentClass.Name.Equals(returnClass.Name, StringComparison.Ordinal))
            {
                AddSerializationUsingToRequestBuilder(returnClass, parentClass);
                SetTypeAsModelInterface(CreateModelInterface(returnClass, interfaceNamingCallback), returnType, requestBuilderClass);
                if (requestBodyClass != returnClass)
                {
                    AddSerializationUsingToRequestBuilder(requestBodyClass, parentClass);
                }

                if (!parentClass.Name.Equals(requestBodyClass.Name, StringComparison.OrdinalIgnoreCase) && CreateModelInterface(requestBodyClass, interfaceNamingCallback) is CodeInterface modelInterface && modelInterface.Parent is not null)
                {
                    parentClass.AddUsing(new CodeUsing
                    {
                        Name = modelInterface.Parent.Name,
                        Declaration = new CodeType
                        {
                            Name = modelInterface.Name,
                            TypeDefinition = modelInterface
                        }
                    });
                }
            }
        }
    }

    private static void ProcessModelClassAssociatedWithRequestGenerator(CodeMethod codeMethod, CodeClass requestBodyClass)
    {
        if (codeMethod.Parent is CodeClass parentClass)
        {
            AddSerializationUsingToRequestBuilder(requestBodyClass, parentClass);
        }
    }

    private static void SetTypeAsModelInterface(CodeInterface interfaceElement, CodeType elemType, CodeClass requestBuilder)
    {
        var interfaceCodeType = new CodeType
        {
            Name = interfaceElement.Name,
            TypeDefinition = interfaceElement,
        };
        requestBuilder.RemoveUsingsByDeclarationName(GetFinalInterfaceName(interfaceElement));
        if (!requestBuilder.Usings.Any(x => x.Declaration?.TypeDefinition == elemType.TypeDefinition))
        {
            requestBuilder.AddUsing(new CodeUsing
            {
                Name = interfaceElement.Name,
                Declaration = interfaceCodeType
            });
        }

        elemType.TypeDefinition = interfaceElement;
        if (elemType.Parent is not null)
        {
            requestBuilder.AddUsing(new CodeUsing
            {
                Name = elemType.Parent.Name,
                Declaration = interfaceCodeType
            });
        }
    }

    private static CodeInterface CreateModelInterface(CodeClass modelClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Temporarily name the interface with "Interface" suffix 
         * since adding code elements of the same name in the same namespace causes error. 
         */
        var temporaryInterfaceName = interfaceNamingCallback.Invoke(modelClass);
        var namespaceOfModel = modelClass.GetImmediateParentOfType<CodeNamespace>();
        if (namespaceOfModel.FindChildByName<CodeInterface>(temporaryInterfaceName, false) is CodeInterface existing)
            return existing;

        var i = 1;
        while (namespaceOfModel.FindChildByName<CodeClass>(temporaryInterfaceName, false) != null)
        {// We already know an Interface doesn't exist with the name. Make sure we don't collide with an existing class name in the namespace.
            temporaryInterfaceName = $"{temporaryInterfaceName}{i++}";
        }

        var insertValue = new CodeInterface
        {
            Name = temporaryInterfaceName,
            Kind = CodeInterfaceKind.Model,
            Documentation = modelClass.Documentation,
            Deprecation = modelClass.Deprecation,
            OriginalClass = modelClass,
        };

        var modelInterface = modelClass.Parent is CodeClass modelParentClass ?
                       modelParentClass.AddInnerInterface(insertValue).First() :
                       namespaceOfModel.AddInterface(insertValue).First();
        var classModelChildItems = modelClass.GetChildElements(true);

        var props = classModelChildItems.OfType<CodeProperty>();
        ProcessModelClassDeclaration(modelClass, modelInterface, interfaceNamingCallback);
        ProcessModelClassProperties(modelClass, modelInterface, props, interfaceNamingCallback);
        modelClass.AssociatedInterface = modelInterface;
        return modelInterface;
    }

    private static void ProcessModelClassDeclaration(CodeClass modelClass, CodeInterface modelInterface, Func<CodeClass, string> tempInterfaceNamingCallback)
    {
        /*
         * If a child model class inherits from parent model class, the child interface extends from the parent interface.
         */
        if (modelClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            var parentInterface = CreateModelInterface(baseClass, tempInterfaceNamingCallback);
            var codeType = new CodeType
            {
                Name = GetFinalInterfaceName(parentInterface),
                TypeDefinition = parentInterface,
            };
            modelInterface.StartBlock.AddImplements(codeType);
            var parentInterfaceNS = parentInterface.GetImmediateParentOfType<CodeNamespace>();

            modelInterface.AddUsing(new CodeUsing
            {
                Name = parentInterfaceNS.Name,
                Declaration = codeType
            });
            AddSuperUsingsInSerializerFunction(modelClass, baseClass);
        }

        // Adding external implementations such as Parsable, AdditionalDataHolder from model class to the model interface.
        foreach (var impl in modelClass.StartBlock.Implements)
        {
            modelInterface.StartBlock.AddImplements(new CodeType
            {
                Name = impl.Name,
                TypeDefinition = impl.TypeDefinition,
            });

            modelInterface.AddUsing(modelClass.Usings.First(x => x.Name.Equals(impl.Name, StringComparison.Ordinal)));
        }
    }

    private static void AddSuperUsingsInSerializerFunction(CodeClass childClass, CodeClass parentClass)
    {
        var serializationFunctions = GetSerializationFunctionsForNamespace(childClass);
        var serializer = serializationFunctions.Item1;
        var deserializer = serializationFunctions.Item2;

        var parentSerializationFunctions = GetSerializationFunctionsForNamespace(parentClass);
        var parentSerializer = parentSerializationFunctions.Item1;
        var parentDeserializer = parentSerializationFunctions.Item2;

        if (parentSerializer.Parent is not null)
        {
            serializer.AddUsing(new CodeUsing
            {
                Name = parentSerializer.Parent.Name,
                Declaration = new CodeType
                {
                    Name = parentSerializer.Name,
                    TypeDefinition = parentSerializer
                }
            });
        }

        if (parentDeserializer.Parent is not null)
        {
            deserializer.AddUsing(new CodeUsing
            {
                Name = parentDeserializer.Parent.Name,
                Declaration = new CodeType
                {
                    Name = parentDeserializer.Name,
                    TypeDefinition = parentDeserializer
                }
            });
        }
    }

    private static void SetUsingInModelInterface(CodeInterface modelInterface, (CodeInterface?, CodeUsing?) propertyTypeAndUsing)
    {
        if (propertyTypeAndUsing.Item1 is not null && propertyTypeAndUsing.Item2 is not null && !modelInterface.Name.Equals(propertyTypeAndUsing.Item1.Name, StringComparison.OrdinalIgnoreCase))
        {
            modelInterface.AddUsing(propertyTypeAndUsing.Item2);
        }
    }

    private static void SetUsingsOfPropertyInSerializationFunctions(string propertySerializerFunctionName, CodeFunction codeFunction, CodeClass property, Func<CodeClass, string> interfaceNamingCallback)
    {
        if (!propertySerializerFunctionName.EqualsIgnoreCase(codeFunction.Name))
        {
            if (GetSerializationFunctionsForNamespace(property).Item1 is CodeFunction serializationFunction && serializationFunction.Parent is not null)
            {
                codeFunction.AddUsing(new CodeUsing
                {
                    Name = serializationFunction.Parent.Name,
                    Declaration = new CodeType
                    {
                        Name = serializationFunction.Name,
                        TypeDefinition = serializationFunction

                    }
                });
            }

            var interfaceProperty = CreateModelInterface(property, interfaceNamingCallback);
            if (interfaceProperty.Parent is not null)
            {
                codeFunction.AddUsing(new CodeUsing
                {
                    Name = interfaceProperty.Parent.Name,
                    Declaration = new CodeType
                    {
                        Name = interfaceProperty.Name,
                        TypeDefinition = interfaceProperty
                    }
                });
            }
        }
    }

    protected static void AddEnumObject(CodeElement currentElement)
    {
        if (currentElement is CodeEnum codeEnum && CodeConstant.FromCodeEnum(codeEnum) is CodeConstant constant)
        {
            codeEnum.CodeEnumObject = constant;
            var nameSpace = codeEnum.GetImmediateParentOfType<CodeNamespace>();
            nameSpace.AddConstant(constant);
        }
        CrawlTree(currentElement, AddEnumObject);
    }
    protected static void AddEnumObjectUsings(CodeElement currentElement)
    {
        if (currentElement is CodeProperty codeProperty && codeProperty.Kind is CodePropertyKind.RequestBuilder && codeProperty.Type is CodeType codeType && codeType.TypeDefinition is CodeClass codeClass)
        {
            foreach (var propertyMethod in codeClass.Methods)
            {
                if (propertyMethod.ReturnType is CodeType ct && ct.TypeDefinition is CodeEnum codeEnum)
                {
                    codeClass.AddUsing(new CodeUsing
                    {
                        Name = codeEnum.Name,
                        Declaration = new CodeType
                        {
                            TypeDefinition = codeEnum.CodeEnumObject
                        }
                    });
                }
            }
        }
        if (currentElement is CodeFunction codeFunction && codeFunction.OriginalLocalMethod.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Serializer))
        {
            foreach (var propertyEnum in codeFunction.OriginalMethodParentClass.Properties.Select(static x => x.Type).OfType<CodeType>().Select(static x => x.TypeDefinition).OfType<CodeEnum>())
            {
                codeFunction.AddUsing(new CodeUsing
                {
                    Name = propertyEnum.Name,
                    Declaration = new CodeType
                    {
                        TypeDefinition = propertyEnum.CodeEnumObject
                    }
                });
            }
        }
        CrawlTree(currentElement, AddEnumObjectUsings);
    }

    private static void AddCodeUsingForComposedTypeProperty(CodeType propertyType, CodeInterface modelInterface, Func<CodeClass, string> interfaceNamingCallback)
    {
        // If the property is a composed type then add code using for each of the classes contained in the composed type
        if (GetOriginalComposedType(propertyType) is not { } composedTypeProperty) return;
        foreach (var composedType in composedTypeProperty.AllTypes)
        {
            if (composedType.TypeDefinition is CodeClass composedTypePropertyClass)
            {
                var composedTypePropertyInterfaceTypeAndUsing = GetUpdatedModelInterfaceAndCodeUsing(composedTypePropertyClass, composedType, interfaceNamingCallback);
                SetUsingInModelInterface(modelInterface, composedTypePropertyInterfaceTypeAndUsing);
            }
        }
    }

    private static void ProcessModelClassProperties(CodeClass modelClass, CodeInterface modelInterface, IEnumerable<CodeProperty> properties, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Add properties to interfaces
         * Replace model classes by interfaces for property types 
         */
        var (serializer, deserializer) = GetSerializationFunctionsForNamespace(modelClass);

        foreach (var mProp in properties)
        {
            /*
             * The following if-else condition adds the using for a model property.
             */

            // If  the property is of external type or an enum type. 
            if (mProp.Type is CodeType ct && (ct.IsExternal || ct.TypeDefinition is not CodeClass) && modelClass.Usings.FirstOrDefault(x => x.Name.EqualsIgnoreCase(ct.Name) || (x.Declaration != null && x.Declaration.Name.EqualsIgnoreCase(ct.Name))) is CodeUsing usingExternal)
            {
                modelInterface.AddUsing(usingExternal);
                serializer.AddUsing(usingExternal);
                deserializer.AddUsing(usingExternal);
            }
            else if (mProp.Type is CodeType propertyType && propertyType.TypeDefinition is CodeClass propertyClass)
            {
                AddCodeUsingForComposedTypeProperty(propertyType, modelInterface, interfaceNamingCallback);

                var interfaceTypeAndUsing = GetUpdatedModelInterfaceAndCodeUsing(propertyClass, propertyType, interfaceNamingCallback);
                SetUsingInModelInterface(modelInterface, interfaceTypeAndUsing);

                // In case of a serializer function, the object serializer function will hold reference to serializer function of the property type.
                SetUsingsOfPropertyInSerializationFunctions($"{ModelSerializerPrefix}{propertyClass.Name.ToFirstCharacterUpperCase()}", serializer, propertyClass, interfaceNamingCallback);

                // In case of a deserializer function, for creating each object property the Parsable factory will be called. That is, `create{ModelName}FromDiscriminatorValue`.
                SetUsingsOfPropertyInSerializationFunctions(GetFactoryFunctionNameFromTypeName(propertyClass.Name), deserializer, propertyClass, interfaceNamingCallback);
            }

            // Add the property of the model class to the model interface.
            if (mProp.Clone() is CodeProperty newProperty)
                modelInterface.AddProperty(newProperty);
        }
    }
    private const string FactoryPrefix = "create";
    private const string FactorySuffix = "FromDiscriminatorValue";
    private static string GetFactoryFunctionNameFromTypeName(string? typeName) => string.IsNullOrEmpty(typeName) ? string.Empty : $"{FactoryPrefix}{typeName.ToFirstCharacterUpperCase()}{FactorySuffix}";

    private static (CodeInterface?, CodeUsing?) GetUpdatedModelInterfaceAndCodeUsing(CodeClass sourceClass, CodeType originalType, Func<CodeClass, string> interfaceNamingCallback)
    {
        var propertyInterfaceType = CreateModelInterface(sourceClass, interfaceNamingCallback);
        if (propertyInterfaceType.Parent is null)
            return (null, null);
        originalType.TypeDefinition = propertyInterfaceType;
        return (propertyInterfaceType, new CodeUsing
        {
            Name = propertyInterfaceType.Parent.Name,
            Declaration = new CodeType
            {
                Name = propertyInterfaceType.Name,
                TypeDefinition = propertyInterfaceType,
            }
        });
    }

    private static void AddStaticMethodsUsingsToDeserializerFunctions(CodeElement currentElement, Func<CodeType, string> functionNameCallback)
    {
        if (currentElement is CodeFunction codeFunction && codeFunction.OriginalLocalMethod is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.Deserializer) && currentMethod.Parameters.FirstOrDefault(x => x.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface) is CodeParameter interfaceParameter && interfaceParameter.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface ci)
        {
            foreach (var property in ci.Properties)
            {
                AddPropertyFactoryUsingToDeserializer(codeFunction, property, functionNameCallback);
            }
        }
        CrawlTree(currentElement, x => AddStaticMethodsUsingsToDeserializerFunctions(x, functionNameCallback));
    }

    private static void AddPropertyFactoryUsingToDeserializer(CodeFunction codeFunction, CodeProperty property, Func<CodeType, string> functionNameCallback)
    {
        if (property.Type is CodeType propertyType && propertyType.TypeDefinition != null)
        {
            var staticMethodName = functionNameCallback.Invoke(propertyType);
            var staticMethodNS = propertyType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>();
            if (staticMethodNS.Functions.FirstOrDefault(x => x.Name.EqualsIgnoreCase(staticMethodName)) is CodeFunction staticMethod)
            {
                codeFunction.AddUsing(new CodeUsing
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
    }

    /// <summary>
    /// Adds all the required import statements (CodeUsings) to the deserialization function which have a dependency on ComposedTypes.
    /// Composed types can be comprised of other interfaces/classes.
    /// </summary>
    /// <param name="codeElement">The code element to process.</param>
    private static void AddDeserializerUsingToDiscriminatorFactoryForComposedTypeParameters(CodeElement codeElement)
    {
        if (codeElement is not CodeFunction function) return;

        var composedTypeParam = function.OriginalLocalMethod.Parameters
            .FirstOrDefault(x => GetOriginalComposedType(x) is not null);

        if (composedTypeParam is null) return;

        var composedType = GetOriginalComposedType(composedTypeParam);
        if (composedType is null) return;

        foreach (var type in composedType.AllTypes)
        {
            if (type.TypeDefinition is not CodeInterface codeInterface) continue;

            var modelDeserializerFunction = GetSerializationFunctionsForNamespace(codeInterface.OriginalClass).Item2;
            if (modelDeserializerFunction.Parent is null) continue;

            function.AddUsing(new CodeUsing
            {
                Name = modelDeserializerFunction.Parent.Name,
                Declaration = new CodeType
                {
                    Name = modelDeserializerFunction.Name,
                    TypeDefinition = modelDeserializerFunction
                },
            });
        }
    }

    private static void AddDeserializerUsingToDiscriminatorFactory(CodeElement codeElement)
    {
        AddDeserializerUsingToDiscriminatorFactoryForComposedTypeParameters(codeElement);
        if (codeElement is CodeFunction parsableFactoryFunction && parsableFactoryFunction.OriginalLocalMethod.IsOfKind(CodeMethodKind.Factory) &&
            parsableFactoryFunction.OriginalLocalMethod?.ReturnType is CodeType codeType && codeType.TypeDefinition is CodeClass modelReturnClass)
        {
            var modelDeserializerFunction = GetSerializationFunctionsForNamespace(modelReturnClass).Item2;
            if (modelDeserializerFunction.Parent is not null)
            {
                parsableFactoryFunction.AddUsing(new CodeUsing
                {
                    Name = modelDeserializerFunction.Parent.Name,
                    Declaration = new CodeType
                    {
                        Name = modelDeserializerFunction.Name,
                        TypeDefinition = modelDeserializerFunction
                    },
                });
            }

            foreach (var mappedType in parsableFactoryFunction.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                if (mappedType.Value is CodeType type && type.TypeDefinition is CodeClass mappedClass)
                {
                    var deserializer = GetSerializationFunctionsForNamespace(mappedClass).Item2;

                    if (deserializer.Parent is not null)
                    {
                        parsableFactoryFunction.AddUsing(new CodeUsing
                        {
                            Name = deserializer.Parent.Name,
                            Declaration = new CodeType
                            {
                                Name = deserializer.Name,
                                TypeDefinition = deserializer
                            },
                        });
                    }
                }
            }
        }
        CrawlTree(codeElement, AddDeserializerUsingToDiscriminatorFactory);
    }
}
