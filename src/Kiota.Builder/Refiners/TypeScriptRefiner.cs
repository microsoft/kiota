using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceIndexersByMethodsWithParameter(generatedCode, false, "ById");
            RemoveCancellationParameter(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
            cancellationToken.ThrowIfCancellationRequested();
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
            ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                },
                static s => s.ToCamelCase(new[] { '_' }),
                _configuration.UsesBackingStore,
                false,
                string.Empty,
                string.Empty);
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
                new[] { $"{AbstractionsPackageName}.registerDefaultSerializer",
                        $"{AbstractionsPackageName}.enableBackingStoreForSerializationWriterFactory",
                        $"{AbstractionsPackageName}.SerializationWriterFactoryRegistry"},
                new[] { $"{AbstractionsPackageName}.registerDefaultDeserializer",
                        $"{AbstractionsPackageName}.ParseNodeFactoryRegistry" });
            cancellationToken.ThrowIfCancellationRequested();
            AddParentClassToErrorClasses(
                    generatedCode,
                    "ApiError",
                    "@microsoft/kiota-abstractions"
            );
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: false
            );
            static string factoryNameCallbackFromTypeName(string? x) => $"create{x.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
            ReplaceLocalMethodsByGlobalFunctions(
                generatedCode,
                static x => factoryNameCallbackFromTypeName(x.Parent?.Name),
                static x =>
                {
                    var result = new List<CodeUsing>() {
                        new() { Name = "ParseNode", Declaration = new() { Name = AbstractionsPackageName, IsExternal = true } }
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
            static string factoryNameCallbackFromType(CodeType x) => factoryNameCallbackFromTypeName(x?.TypeDefinition?.Name);
            cancellationToken.ThrowIfCancellationRequested();
            AddStaticMethodsUsingsForDeserializer(
                generatedCode,
                factoryNameCallbackFromType
            );
            AddStaticMethodsUsingsForRequestExecutor(
                generatedCode,
                factoryNameCallbackFromType
            );
            AddQueryParameterMapperMethod(
                generatedCode
            );
            IntroducesInterfacesAndFunctions(generatedCode, factoryNameCallbackFromType);
            AliasUsingsWithSameSymbol(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }
    private static void AliasCollidingSymbols(IEnumerable<CodeUsing> usings, string currentSymbolName)
    {
        var duplicatedSymbolsUsings = usings.Where(static x => !x.IsExternal)
                                                                .Where(static x => x.Declaration != null && x.Declaration.TypeDefinition != null)
                                                                .GroupBy(static x => x.Declaration!.Name, StringComparer.OrdinalIgnoreCase)
                                                                .Where(static x => x.DistinctBy(static y => y.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>())
                                                                                    .Count() > 1)
                                                                .SelectMany(static x => x)
                                                                .Union(usings
                                                                        .Where(static x => !x.IsExternal && x.Declaration != null)
                                                                        .Where(x => x.Declaration!
                                                                                        .Name
                                                                                        .Equals(currentSymbolName, StringComparison.OrdinalIgnoreCase)))
                                                                .ToArray();
        foreach (var usingElement in duplicatedSymbolsUsings)
            usingElement.Alias = (usingElement.Declaration
                                            ?.TypeDefinition
                                            ?.GetImmediateParentOfType<CodeNamespace>()
                                            .Name +
                                usingElement.Declaration
                                            ?.TypeDefinition
                                            ?.Name)
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
    private const string AbstractionsPackageName = "@microsoft/kiota-abstractions";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            AbstractionsPackageName, "RequestAdapter"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.Options),
            AbstractionsPackageName, "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            AbstractionsPackageName, "HttpMethod", "RequestInformation", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsPackageName, "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            AbstractionsPackageName, "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            AbstractionsPackageName, "ParseNode"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.IndexerBackwardCompatibility),
            AbstractionsPackageName, "getPathParameters"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            AbstractionsPackageName, "Parsable", "ParsableFactory"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            AbstractionsPackageName, "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            AbstractionsPackageName, "AdditionalDataHolder"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            AbstractionsPackageName, "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            AbstractionsPackageName, "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
    };
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.Implements.Where(x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Name = x.Name[1..]); // skipping the I
    }
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            currentProperty.Type.Name = "RequestAdapter";
        else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
            currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
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
                currentProperty.DefaultValue = "{}";
        }
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        if (currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator))
        {
            if (currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
            currentMethod.ReturnType.Name = "Record<string, (node: ParseNode) => void>";
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor))
        {
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.BackingStore))
                .Where(x => x.Type.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
            var urlTplParams = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
            if (urlTplParams != null &&
                urlTplParams.Type is CodeType originalType)
            {
                originalType.Name = "Record<string, unknown>";
                urlTplParams.Documentation.Description = "The raw url or the Url template parameters for the request.";
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
                                })},
    {"DateOnly", (string.Empty, new CodeUsing {
                            Name = "DateOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                        })},
    {"TimeOnly", (string.Empty, new CodeUsing {
                            Name = "TimeOnly",
                            Declaration = new CodeType {
                                Name = AbstractionsPackageName,
                                IsExternal = true,
                            },
                        })},
    };

    private static void ReplaceRequestConfigurationsQueryParamsWithInterfaces(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.QueryParameters, CodeClassKind.RequestConfiguration))
        {

            var targetNS = codeClass.GetImmediateParentOfType<CodeNamespace>();
            var existing = targetNS.FindChildByName<CodeInterface>(codeClass.Name, false);
            if (existing != null)
                return;

            var insertValue = new CodeInterface
            {
                Name = codeClass.Name,
                Kind = codeClass.IsOfKind(CodeClassKind.QueryParameters) ? CodeInterfaceKind.QueryParameters : CodeInterfaceKind.RequestConfiguration
            };
            targetNS.RemoveChildElement(codeClass);
            var codeInterface = targetNS.AddInterface(insertValue).First();

            var props = codeClass.Properties?.ToArray();
            codeInterface.AddProperty(props!);

            var usings = codeClass.Usings?.ToArray();
            codeInterface.AddUsing(usings!);
        }
        CrawlTree(currentElement, x => ReplaceRequestConfigurationsQueryParamsWithInterfaces(x));
    }
    private const string TemporaryInterfaceNameSuffix = "Interface";
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
        AddDeserializerUsingToDisriminatorFactory(generatedCode);
        ReplaceRequestConfigurationsQueryParamsWithInterfaces(generatedCode);
        AddStaticMethodsUsingsToDeserializerFunctions(generatedCode, functionNameCallback);
    }

    private static void CreateSeparateSerializers(CodeElement codeElement)
    {
        if (codeElement is CodeClass codeClass && codeClass.Kind == CodeClassKind.Model)
        {
            CreateSerializationFunctions(codeClass);
        }
        CrawlTree(codeElement, x => CreateSeparateSerializers(x));
    }

    private static void CreateSerializationFunctions(CodeClass modelClass)
    {
        var namespaceOfModel = modelClass.GetImmediateParentOfType<CodeNamespace>();

        var serializerMethod = modelClass.Methods.FirstOrDefault(x => x.Kind == CodeMethodKind.Serializer);
        var deserializerMethod = modelClass.Methods.FirstOrDefault(x => x.Kind == CodeMethodKind.Deserializer);
        if (serializerMethod == null || deserializerMethod == null)
        {
            throw new InvalidOperationException($"The refiner was unable to create local serializer/derserializer method for {modelClass.Name}");
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

        foreach (var codeUsing in modelClass.Usings.Where(x => x.Declaration != null && x.Declaration.IsExternal))
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
            Name = ReturnFinalInterfaceName(modelInterface.Name), // remove the interface suffix
            DefaultValue = "{}",
            Type = new CodeType { Name = ReturnFinalInterfaceName(modelInterface.Name), TypeDefinition = modelInterface }
        });

        codeFunction.AddUsing(new CodeUsing
        {
            Name = modelInterface.Parent?.Name!,
            Declaration = new CodeType
            {
                Name = ReturnFinalInterfaceName(modelInterface.Name),
                TypeDefinition = modelInterface
            }
        });
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
           x => $"{x.Name.ToFirstCharacterUpperCase()}Interface".ToFirstCharacterUpperCase()
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
        if (currentElement is CodeClass currentClass)
        {
            if (currentClass.Kind == CodeClassKind.Model)
            {
                var targetNS = currentClass.GetImmediateParentOfType<CodeNamespace>();

                var existing = targetNS.Classes.FirstOrDefault(x => x == currentClass);
                if (existing == null)
                {
                    return;
                }

                targetNS.RemoveChildElement(currentElement);
            }
            else
            {
                foreach (var codeUsing in currentClass.Usings)
                {
                    RenameModelInterfacesAndRemoveClassesInUsing(codeUsing, currentClass);
                }
            }
        }
        else if (currentElement is CodeInterface modelInterface && modelInterface.IsOfKind(CodeInterfaceKind.Model))
        {
            modelInterface.Name = ReturnFinalInterfaceName(modelInterface.Name);
        }
        else if (currentElement is CodeFunction codeFunction && codeFunction.OriginalLocalMethod.IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer))
        {
            RenameCodeInterfaceParamsInSerializers(codeFunction);
        }

        CrawlTree(currentElement, x => RenameModelInterfacesAndRemoveClasses(x));
    }

    private static void RenameModelInterfacesAndRemoveClassesInUsing(CodeUsing codeUsing, CodeClass parentClass)
    {
        if (codeUsing.Declaration is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface)
        {
            codeType.Name = ReturnFinalInterfaceName(codeInterface.Name);
            codeInterface.Name = ReturnFinalInterfaceName(codeInterface.Name);
        }
        else if (codeUsing.Declaration?.TypeDefinition is CodeClass codeClass && codeClass.Kind == CodeClassKind.Model)
        {
            parentClass?.RemoveUsingsByDeclarationName(codeUsing.Declaration.Name);
        }
    }
    private static void RenameCodeInterfaceParamsInSerializers(CodeFunction codeFunction)
    {
        var param = codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault(x => x.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface);
        if (param != null && param.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface paramInterface)
        {
            paramInterface.Name = ReturnFinalInterfaceName(paramInterface.Name);
            param.Name = ReturnFinalInterfaceName(paramInterface.Name);
        }
    }

    private static string ReturnFinalInterfaceName(string interfaceName)
    {
        return interfaceName.Split(TemporaryInterfaceNameSuffix)[0];
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
            else if (currentClass.Kind == CodeClassKind.RequestBuilder)
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
        if (codeClass.Parent == null)
            throw new InvalidOperationException($"Model class {codeClass}'s parent namespace is null");
        var parentNamespace = codeClass.Parent as CodeNamespace;
        var serializer = parentNamespace?.FindChildByName<CodeFunction>($"{ModelSerializerPrefix}{codeClass.Name.ToFirstCharacterUpperCase()}");
        var deserializer = parentNamespace?.FindChildByName<CodeFunction>($"{ModelDeserializerPrefix}{codeClass.Name.ToFirstCharacterUpperCase()}");
        if (serializer == null || deserializer == null)
        {
            throw new InvalidOperationException($"Serializer or deserializer not found for {codeClass.Name}");
        }
        return (serializer, deserializer);
    }
    private static void AddSerializationUsingToRequestBuilder(CodeClass modelClass, CodeClass targetClass)
    {
        var serializationFunctions = GetSerializationFunctionsForNamespace(modelClass);
        var serializer = serializationFunctions.Item1;
        var deserializer = serializationFunctions.Item2;
        targetClass.AddUsing(new CodeUsing
        {
            Name = serializer.Parent?.Name!,
            Declaration = new CodeType
            {
                Name = serializer.Name,
                TypeDefinition = serializer
            }
        });

        targetClass.AddUsing(new CodeUsing
        {
            Name = deserializer.Parent?.Name!,
            Declaration = new CodeType
            {
                Name = deserializer.Name,
                TypeDefinition = deserializer
            }
        });
    }

    private static void ProcessModelsAssociatedWithMethods(CodeMethod codeMethod, CodeClass requestBuilderClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Setting request body parameter type of request executor to model interface.
         */
        var requestBodyParam = codeMethod.Parameters?.FirstOrDefault(x => x.Kind == CodeParameterKind.RequestBody);
        var requestBodyClass = (requestBodyParam?.Type is CodeType requestBodyType) ? requestBodyType.TypeDefinition as CodeClass : null;
        if (requestBodyParam != null && requestBodyClass != null)
        {
            SetTypeAsModelInterface(CreateModelInterface(requestBodyClass, interfaceNamingCallback), (CodeType)requestBodyParam.Type, requestBuilderClass);
        }
        var parentClass = codeMethod?.GetImmediateParentOfType<CodeClass>();
        if (codeMethod?.ReturnType is CodeType returnType &&
            returnType.TypeDefinition is CodeClass returnClass &&
            returnClass.IsOfKind(CodeClassKind.Model) && parentClass != null && parentClass.Name != returnClass.Name)
        {
            AddSerializationUsingToRequestBuilder(returnClass, parentClass);
            SetTypeAsModelInterface(CreateModelInterface(returnClass, interfaceNamingCallback), returnType, requestBuilderClass);
            if (requestBodyClass != null && requestBodyClass != returnClass)
            {
                AddSerializationUsingToRequestBuilder(requestBodyClass, parentClass);

            }
        }
        if (codeMethod?.ErrorMappings != null && codeMethod.ErrorMappings.Any())
        {
            ProcessModelClassAssociatedWithErrorMappings(codeMethod);
        }

        if (codeMethod?.Kind == CodeMethodKind.RequestGenerator && requestBodyClass != null)
        {
            ProcessModelClassAssociatedWithRequestGenerator(codeMethod, requestBodyClass);
        }

        if (requestBodyClass != null && parentClass != null && parentClass.Name != requestBodyClass.Name)
        {
            var modelInterface = CreateModelInterface(requestBodyClass, interfaceNamingCallback);
            parentClass.AddUsing(
                new CodeUsing
                {
                    Name = modelInterface.Parent?.Name!,
                    Declaration = new CodeType
                    {
                        Name = modelInterface.Name,
                        TypeDefinition = modelInterface
                    }
                });
        }
    }
    private static void ProcessModelClassAssociatedWithErrorMappings(CodeMethod codeMethod)
    {
        foreach (var errorMapping in codeMethod.ErrorMappings)
        {
            if (errorMapping.Value is CodeType codeType && codeType.TypeDefinition is CodeClass errorMappingClass && codeMethod.Parent is CodeClass parentClass)
            {
                AddSerializationUsingToRequestBuilder(errorMappingClass, parentClass);
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
        elemType.Name = interfaceElement.Name.Split(TemporaryInterfaceNameSuffix)[0];
        var interfaceCodeType = new CodeType
        {
            Name = interfaceElement.Name,
            TypeDefinition = interfaceElement,
        };
        requestBuilder.RemoveUsingsByDeclarationName(elemType.Name);
        if (!requestBuilder.Usings.Any(x => x?.Declaration?.TypeDefinition == elemType.TypeDefinition))
        {
            requestBuilder.AddUsing(new CodeUsing
            {
                Name = interfaceElement?.Name!,
                Declaration = interfaceCodeType
            });
        }

        elemType.TypeDefinition = interfaceElement;
        requestBuilder.AddUsing(new CodeUsing
        {
            Name = elemType.Parent?.Name!,
            Declaration = interfaceCodeType
        });
    }

    private static CodeInterface CreateModelInterface(CodeClass modelClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Temporarily name the interface with "Interface" suffix 
         * since adding code elements of the same name in the same namespace causes error. 
         */
        var temporaryInterfaceName = interfaceNamingCallback.Invoke(modelClass);
        var namespaceOfModel = modelClass.GetImmediateParentOfType<CodeNamespace>();
        var existing = namespaceOfModel.FindChildByName<CodeInterface>(temporaryInterfaceName, false);
        if (existing != null)
            return existing;
        var modelParentClass = modelClass.Parent as CodeClass;
        var shouldInsertUnderParentClass = modelParentClass != null;
        var insertValue = new CodeInterface
        {
            Name = temporaryInterfaceName,
            Kind = CodeInterfaceKind.Model,
        };

        var modelInterface = shouldInsertUnderParentClass ?
                       modelParentClass?.AddInnerInterface(insertValue).First() :
                       namespaceOfModel.AddInterface(insertValue).First();

        var classModelChildItems = modelClass.GetChildElements(true);

        var props = classModelChildItems.OfType<CodeProperty>();
        if (modelInterface != null)
        {
            ProcessModelClassDeclaration(modelClass, modelInterface, interfaceNamingCallback);
            ProcessModelClassProperties(modelClass, modelInterface, props, interfaceNamingCallback);
        }
        else
            throw new InvalidOperationException("");
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
                Name = ReturnFinalInterfaceName(parentInterface.Name),
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

            modelInterface.AddUsing(modelClass.Usings.First(x => x.Name == impl.Name));
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

        serializer.AddUsing(new CodeUsing
        {
            Name = parentSerializer.Parent?.Name!,
            Declaration = new CodeType
            {
                Name = parentSerializer.Name,
                TypeDefinition = parentSerializer
            }
        });

        deserializer.AddUsing(new CodeUsing
        {
            Name = parentDeserializer.Parent?.Name!,
            Declaration = new CodeType
            {
                Name = parentDeserializer.Name,
                TypeDefinition = parentDeserializer
            }
        });
    }

    private static void SetUsingInModelInterface(CodeInterface modelInterface, (CodeInterface, CodeUsing) propertyTypeAndUsing)
    {
        if (modelInterface.Name != propertyTypeAndUsing.Item1.Name)
        {
            modelInterface.AddUsing(propertyTypeAndUsing.Item2);
        }
    }

    private static void SetUsingsOfPropertyInSerializationFunctions(string propertySerializerFunctionName, CodeFunction codeFunction, CodeClass property, Func<CodeClass, string> interfaceNamingCallback)
    {
        if (propertySerializerFunctionName != codeFunction.Name)
        {
            var serializationFunction = GetSerializationFunctionsForNamespace(property).Item1;

            if (serializationFunction == null)
            {
                throw new InvalidOperationException($"Serialization function for property {property.Name} not found");
            }
            codeFunction.AddUsing(new CodeUsing
            {
                Name = serializationFunction.Parent?.Name!,
                Declaration = new CodeType
                {
                    Name = serializationFunction.Name,
                    TypeDefinition = serializationFunction

                }
            });

            var interfaceProperty = CreateModelInterface(property, interfaceNamingCallback);
            codeFunction.AddUsing(new CodeUsing
            {
                Name = interfaceProperty.Parent?.Name!,
                Declaration = new CodeType
                {
                    Name = interfaceProperty.Name,
                    TypeDefinition = interfaceProperty
                }
            });
        }
    }

    private static void ProcessModelClassProperties(CodeClass modelClass, CodeInterface modelInterface, IEnumerable<CodeProperty> properties, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Add properties to interfaces
         * Replace model classes by interfaces for property types 
         */
        var serializationFunctions = GetSerializationFunctionsForNamespace(modelClass);
        var serializer = serializationFunctions.Item1;
        var deserializer = serializationFunctions.Item2;

        foreach (var mProp in properties)
        {
            /*
             * The following if-else condition adds the using for a model property.
             */

            // If  the property is of external type or an enum type. 
            if (mProp.Type is CodeType nonModelClassType && (nonModelClassType.IsExternal || (!(nonModelClassType.TypeDefinition is CodeClass))))
            {
                var usingExternal = modelClass.Usings.FirstOrDefault(x => (String.Equals(x.Name, nonModelClassType.Name, StringComparison.OrdinalIgnoreCase) || String.Equals(x.Declaration?.Name, nonModelClassType.Name, StringComparison.OrdinalIgnoreCase)));

                if (usingExternal != null)
                {
                    modelInterface.AddUsing(usingExternal);
                    serializer.AddUsing(usingExternal);
                    deserializer.AddUsing(usingExternal);
                }
            }
            else if (mProp.Type is CodeType propertyType && propertyType.TypeDefinition is CodeClass propertyClass)
            {
                var interfaceTypeAndUsing = ReturnUpdatedModelInterfaceTypeAndUsing(propertyClass, propertyType, interfaceNamingCallback);
                SetUsingInModelInterface(modelInterface, interfaceTypeAndUsing);

                // In case of a serializer function, the object serializer function will hold reference to serializer function of the property type.
                SetUsingsOfPropertyInSerializationFunctions($"{ModelSerializerPrefix}{propertyClass.Name.ToFirstCharacterUpperCase()}", serializer, propertyClass, interfaceNamingCallback);

                // In case of a deserializer function, for creating each object property the Parsable factory will be called. That is, `create{ModelName}FromDiscriminatorValue`.
                SetUsingsOfPropertyInSerializationFunctions($"create{propertyClass.Name.ToFirstCharacterUpperCase()}FromDiscriminatorValue", deserializer, propertyClass, interfaceNamingCallback);
            }

            // Add the property of the model class to the model interface.
            if (mProp.Clone() is CodeProperty newProperty)
                modelInterface.AddProperty(newProperty);
        }
    }

    private static (CodeInterface, CodeUsing) ReturnUpdatedModelInterfaceTypeAndUsing(CodeClass sourceClass, CodeType originalType, Func<CodeClass, string> interfaceNamingCallback)
    {
        var propertyInterfaceType = CreateModelInterface(sourceClass, interfaceNamingCallback);
        originalType.Name = propertyInterfaceType.Name;
        originalType.TypeDefinition = propertyInterfaceType;
        return (propertyInterfaceType, new CodeUsing
        {
            Name = propertyInterfaceType.Parent?.Name!,
            Declaration = new CodeType
            {
                Name = propertyInterfaceType.Name,
                TypeDefinition = propertyInterfaceType,
            }
        });
    }

    protected static void AddStaticMethodsUsingsToDeserializerFunctions(CodeElement currentElement, Func<CodeType, string> functionNameCallback)
    {
        if (currentElement is CodeFunction codeFunction && codeFunction.OriginalLocalMethod is CodeMethod currentMethod && currentMethod.Kind == CodeMethodKind.Deserializer)
        {
            var interfaceParameter = currentMethod.Parameters.FirstOrDefault(x => x.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface);

            if (interfaceParameter?.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface modelInterface)
            {
                var props = modelInterface.GetChildElements(true).OfType<CodeProperty>();
                if (props != null)
                {
                    foreach (var property in props)
                    {
                        AddPropertyFactoryUsingToDeserializer(codeFunction, property, functionNameCallback);
                    }
                }
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
            var staticMethod = staticMethodNS.Functions.FirstOrDefault(x => string.Equals(staticMethodName, x.Name, StringComparison.OrdinalIgnoreCase));
            if (staticMethod != null)
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

    private static void AddDeserializerUsingToDisriminatorFactory(CodeElement codeElement)
    {
        if (codeElement is CodeFunction parsableFactoryFunction && parsableFactoryFunction.OriginalLocalMethod.Kind == CodeMethodKind.Factory
            && parsableFactoryFunction.OriginalLocalMethod?.ReturnType is CodeType codeType && codeType?.TypeDefinition is CodeClass modelReturnClass)
        {
            var modelDeserializerFunction = GetSerializationFunctionsForNamespace(modelReturnClass).Item2;
            parsableFactoryFunction.AddUsing(new CodeUsing
            {
                Name = modelDeserializerFunction.Parent?.Name!,
                Declaration = new CodeType
                {
                    Name = modelDeserializerFunction.Name,
                    TypeDefinition = modelDeserializerFunction
                },
            });
            foreach (var mappedType in parsableFactoryFunction.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                if (mappedType.Value is CodeType type && type.TypeDefinition is CodeClass mappedClass)
                {
                    var deserializer = GetSerializationFunctionsForNamespace(mappedClass).Item2;

                    parsableFactoryFunction.AddUsing(new CodeUsing
                    {
                        Name = deserializer.Parent?.Name!,
                        Declaration = new CodeType
                        {
                            Name = deserializer.Name,
                            TypeDefinition = deserializer
                        },
                    });
                }
            }

        }
        CrawlTree(codeElement, AddDeserializerUsingToDisriminatorFactory);
    }
}
