using System.Linq;
using System;
using Kiota.Builder.Extensions;
using System.Collections.Generic;
using Microsoft.OpenApi.Expressions;

namespace Kiota.Builder.Refiners;
public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    private const string ModelClassSuffix = "Impl";
    public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override void Refine(CodeNamespace generatedCode)
    {
        ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
        RemoveCancellationParameter(generatedCode);
        CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
        CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
        AddInnerClasses(generatedCode,
            true,
            string.Empty,
            true);
        // `AddInnerClasses` will have inner classes moved to their own files, so  we add the imports after so that the files don't miss anything.
        // This is because imports are added at the file level so nested classes would potentially use the higher level imports.
        AddDefaultImports(generatedCode, defaultUsingEvaluators);
        DisableActionOf(generatedCode,
            CodeParameterKind.RequestConfiguration);
        AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        AliasUsingsWithSameSymbol(generatedCode);
        AddParsableImplementsForModelClasses(generatedCode, "Parsable");
        ReplaceBinaryByNativeType(generatedCode, "ArrayBuffer", null);
        ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
        AddConstructorsForDefaultValues(generatedCode, true);
        ReplaceDefaultSerializationModules(
            generatedCode,
            "@microsoft/kiota-serialization-json.JsonSerializationWriterFactory",
            "@microsoft/kiota-serialization-text.TextSerializationWriterFactory"
        );
        ReplaceDefaultDeserializationModules(
            generatedCode,
            "@microsoft/kiota-serialization-json.JsonParseNodeFactory",
            "@microsoft/kiota-serialization-text.TextParseNodeFactory"
        );
        AddSerializationModulesImport(generatedCode,
            new[] { $"{AbstractionsPackageName}.registerDefaultSerializer",
                    $"{AbstractionsPackageName}.enableBackingStoreForSerializationWriterFactory",
                    $"{AbstractionsPackageName}.SerializationWriterFactoryRegistry"},
            new[] { $"{AbstractionsPackageName}.registerDefaultDeserializer",
                    $"{AbstractionsPackageName}.ParseNodeFactoryRegistry" });
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
        Func<string, string> factoryNameCallbackFromTypeName = x => $"create{x.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
        ReplaceLocalMethodsByGlobalFunctions(
            generatedCode,
            x => factoryNameCallbackFromTypeName(x.Parent.Name),
            x => new List<CodeUsing>(x.DiscriminatorMappings
                                    .Select(y => y.Value)
                                    .OfType<CodeType>()
                                    .Select(y => new CodeUsing { Name = y.Name, Declaration = y })) {
                    new() { Name = "ParseNode", Declaration = new() { Name = AbstractionsPackageName, IsExternal = true } },
                    new() { Name = x.Parent.Parent.Name, Declaration = new() { Name = x.Parent.Name, TypeDefinition = x.Parent } },
                }.ToArray(),
            CodeMethodKind.Factory
        );
        Func<CodeType, string> factoryNameCallbackFromType = x => factoryNameCallbackFromTypeName(x.Name);
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
        AddModelsInterfaces(generatedCode);
        ReplaceRequestConfigurationsQueryParamsWithInterfaces(generatedCode);
    }

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

            codeInterface.AddProperty(codeClass.Properties.ToArray());
            codeInterface.AddUsing(codeClass.Usings.ToArray());
        }
        CrawlTree(currentElement, x => ReplaceRequestConfigurationsQueryParamsWithInterfaces(x));
    }

    private static void AddModelsInterfaces(CodeElement generatedCode)
    {
        GenerateModelInterfaces(
           generatedCode,
           x => $"{x.Name.ToFirstCharacterUpperCase()}Interface".ToFirstCharacterUpperCase()
       );

        RenameModelInterfacesAndClasses(generatedCode);

    }

    /// <summary>
    /// Removes the "Interface" suffix temporarily added to model interface names
    /// Adds the "Impl" suffix to model class names.
    /// </summary>
    /// <param name="currentElement"></param>
    private static void RenameModelInterfacesAndClasses(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
        {
            currentClass.Name = currentClass.Name + ModelClassSuffix;
        }
        if (currentElement is CodeInterface modelInterface && modelInterface.IsOfKind(CodeInterfaceKind.Model) && modelInterface.Name.EndsWith("Interface"))
        {
            modelInterface.Name = modelInterface.Name.Split("Interface")[0];
        }

        CrawlTree(currentElement, x => RenameModelInterfacesAndClasses(x));
    }

    private static void GenerateModelInterfaces(CodeElement currentElement, Func<CodeClass, string> interfaceNamingCallback)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
        {
            CreateModelInterface(currentClass, interfaceNamingCallback);
        }
        /*
         * Setting object property type to interface type.
         */
        else if (currentElement is CodeProperty codeProperty &&
                codeProperty.IsOfKind(CodePropertyKind.RequestBody) &&
                codeProperty.Type is CodeType type &&
                type.TypeDefinition is CodeClass modelClass &&
                modelClass.IsOfKind(CodeClassKind.Model))
        {
            SetTypeAndAddUsing(CreateModelInterface(modelClass, interfaceNamingCallback), type, codeProperty);
        }
        else if (currentElement is CodeMethod codeMethod)
        {
            /*
             * Setting request body parameter type of request executor to model interface.
             */
            if (codeMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
            codeMethod.ReturnType is CodeType returnType &&
            returnType.TypeDefinition is CodeClass returnClass &&
            returnClass.IsOfKind(CodeClassKind.Model))
            {
                var requestBodyParam = codeMethod.Parameters.OfKind(CodeParameterKind.RequestBody);
                if (requestBodyParam != null && requestBodyParam.Type is CodeType type1)
                {
                    SetTypeAndAddUsing(CreateModelInterface(type1.TypeDefinition as CodeClass, interfaceNamingCallback), type1, requestBodyParam);
                }
                SetTypeAndAddUsing(CreateModelInterface(returnClass, interfaceNamingCallback), returnType, codeMethod);

                var parentClass = codeMethod.GetImmediateParentOfType<CodeClass>();

                if (parentClass != null && parentClass.Name != returnClass.Name)
                {
                    parentClass.AddUsing(new CodeUsing { Name = returnClass.Parent.Name, Declaration = new CodeType { Name = returnClass.Name, TypeDefinition = returnClass } });

                }
            }
            /*
             * Setting request body parameter type of request generator to model interface.
             */
            else if (codeMethod.IsOfKind(CodeMethodKind.RequestGenerator))
            {
                var requestBodyParam1 = codeMethod?.Parameters?.OfKind(CodeParameterKind.RequestBody);
                if (requestBodyParam1 != null && requestBodyParam1.Type is CodeType type1 && type1.TypeDefinition is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model))
                {
                    SetTypeAndAddUsing(CreateModelInterface(codeClass, interfaceNamingCallback), type1, requestBodyParam1);


                    var parentClass = codeMethod.GetImmediateParentOfType<CodeClass>();

                    if (parentClass != null && parentClass.Name != codeClass.Name)
                    {
                        parentClass.AddUsing(new CodeUsing { Name = codeClass.Parent.Name, Declaration = new CodeType { Name = codeClass.Name, TypeDefinition = codeClass } });

                    }
                }
            }
        }

        CrawlTree(currentElement, x => GenerateModelInterfaces(x, interfaceNamingCallback));
    }
    private static void SetTypeAndAddUsing(CodeInterface interfaceElement, CodeType elemType, CodeElement targetElement)
    {
        if (interfaceElement.Name.EndsWith("Interface"))
        {
            elemType.Name = interfaceElement.Name.Split("Interface")[0];
        }
        else
        {
            elemType.Name = interfaceElement.Name;

        }
        elemType.TypeDefinition = interfaceElement;
        var nameSpaceOfInterface = interfaceElement.GetImmediateParentOfType<CodeNamespace>();
        if (nameSpaceOfInterface != targetElement.GetImmediateParentOfType<CodeNamespace>())
        {
            var targetClass = targetElement.GetImmediateParentOfType<CodeClass>();
            if (targetClass.Parent is CodeClass parentClass)
                targetClass = parentClass;
            targetClass.AddUsing(new CodeUsing
            {
                Name = nameSpaceOfInterface.Name,
                Declaration = new CodeType
                {
                    Name = interfaceElement.Name,
                    TypeDefinition = interfaceElement,
                },
            });
        }
    }
    private static CodeInterface CreateModelInterface(CodeClass modelClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        /*
         * Temporarily name the interface with "Interface" suffix 
         * since adding code elements of the same name in the same namespace causes error. 
         */
        var finalInterfaceName = modelClass.Name.ToFirstCharacterUpperCase();
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
                       modelParentClass.AddInnerInterface(insertValue).First() :
                       namespaceOfModel.AddInterface(insertValue).First();

        var classModelChildItems = modelClass.GetChildElements(true);

        var targetUsingBlock = modelInterface.StartBlock;
        var usingsToAdd = new List<CodeUsing>();

        /**
         * If a child model class inherits from parent model class, the child interface extends from the parent interface.
         */
        if (modelClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            var parentInterface = CreateModelInterface(baseClass, interfaceNamingCallback);
            modelInterface.StartBlock.AddInheritsFrom(new CodeType
            {
                Name = parentInterface.Name,
                TypeDefinition = parentInterface,
            });
            var parentInterfaceNS = parentInterface.GetImmediateParentOfType<CodeNamespace>();

            modelInterface.AddUsing(new CodeUsing
            {
                Name = parentInterfaceNS.Name,
                Declaration = new CodeType
                {
                    Name = parentInterface.Name,
                    TypeDefinition = parentInterface,
                },
            });
        }

        /**
         * Add properties to interfaces
         * Replace model classes by interfaces for property types 
         */
        foreach (var mProp in classModelChildItems.OfType<CodeProperty>())
        {
            if (mProp.Type is CodeType externalType && externalType.IsExternal)
            {
                var usingExternal = modelClass.Usings.FirstOrDefault(x => String.Equals(x.Name, externalType.Name, StringComparison.OrdinalIgnoreCase));

                if (usingExternal != null)
                {
                    modelInterface.AddUsing(usingExternal);
                }
            }
            else if (mProp.Type is CodeType propertyType && !propertyType.IsExternal && propertyType.TypeDefinition is CodeClass propertyClass)
            {
                var codeUsing = ReplaceTypeByInterfaceType(propertyClass, propertyType, interfaceNamingCallback);

                if (modelInterface.Name != codeUsing.Item1.Name)
                {
                    modelInterface.AddUsing(codeUsing.Item2);
                }
                modelClass.AddUsing(codeUsing.Item2);

                /***
                 * Append "impl" and add class property type in  the model class usings as they will be required by the serializer method. 
                 */
                if (modelClass.Name != propertyClass.Name)
                {
                    modelClass.AddUsing(new CodeUsing
                    {
                        Name = mProp.Parent.Name,
                        Declaration = new CodeType
                        {
                            Name = propertyClass.Name + ModelClassSuffix,
                            TypeDefinition = propertyClass,
                        }
                    });
                }
            }
            else if (mProp.Type is CodeType nonClassPropertyType && nonClassPropertyType is CodeType nonClassTypeDef && !(nonClassTypeDef.TypeDefinition is CodeClass) && !nonClassPropertyType.IsExternal)
            {
                var usingExternal = modelClass.Usings.FirstOrDefault(x => String.Equals(x.Declaration.Name, nonClassTypeDef.Name, StringComparison.OrdinalIgnoreCase));

                if (usingExternal != null)
                {
                    modelInterface.AddUsing(usingExternal);
                }
            }

            modelInterface.AddProperty(mProp);
        }

        /*
         * Model class should implement the model interface
         */
        modelClass.StartBlock.AddImplements(new CodeType
        {
            Name = finalInterfaceName,
            TypeDefinition = modelInterface,
        });

        /**
         * Set a parameter of model interface type in the model class constructor
         */
        var constructor = modelClass.Methods?.FirstOrDefault(x => x is CodeMethod method1 && method1.IsOfKind(CodeMethodKind.Constructor));
        if (constructor == null)
        {
            constructor = modelClass.AddMethod(new CodeMethod
            {
                Name = "constructor",
                Kind = CodeMethodKind.Constructor,
                IsAsync = false,
                IsStatic = false,
                Description = $"Instantiates a new {modelClass.Name.ToFirstCharacterUpperCase()} and sets the default values.",
                Access = AccessModifier.Public,
            }).First();

        }
        constructor.AddParameter(new CodeParameter
        {
            Name = finalInterfaceName.ToFirstCharacterLowerCase() + "ParameterValue",
            Type = new CodeType { Name = finalInterfaceName, TypeDefinition = modelInterface },
            Optional = true
        });

        modelClass.AddUsing(new CodeUsing
        {

            Name = modelInterface.Parent.Name,
            Declaration = new CodeType
            {
                Name = finalInterfaceName,
                TypeDefinition = modelInterface,
            }
        });

        foreach (var method in classModelChildItems.OfType<CodeMethod>()
                                                    .Where(x => x.IsOfKind(CodeMethodKind.Getter,
                                                                        CodeMethodKind.Setter,
                                                                        CodeMethodKind.Factory) &&
                                                                !(x.AccessedProperty?.IsOfKind(CodePropertyKind.AdditionalData) ?? false)))
        {
            if (method.ReturnType is CodeType methodReturnType &&
                !methodReturnType.IsExternal)
            {
                if (methodReturnType?.TypeDefinition is CodeClass methodTypeClass)
                {
                    var resultType = ReplaceTypeByInterfaceType(methodTypeClass, methodReturnType, interfaceNamingCallback);
                    modelClass.AddUsing(resultType.Item2);
                    if (resultType.Item2.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>() != namespaceOfModel)
                        targetUsingBlock.AddUsings(resultType.Item2.Clone() as CodeUsing);
                }
                else if (methodReturnType.TypeDefinition is CodeEnum methodEnumType)
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
                        var resultType = ReplaceTypeByInterfaceType(parameterTypeClass, parameterType, interfaceNamingCallback);
                        modelClass.AddUsing(resultType.Item2);
                        if (resultType.Item2.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>() != namespaceOfModel)
                            targetUsingBlock.AddUsings(resultType.Item2.Clone() as CodeUsing);
                    }
                    else if (parameterType.TypeDefinition is CodeEnum parameterEnumType)
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
        }
        var externalTypesOnInter = modelInterface.Methods.Select(x => x.ReturnType).OfType<CodeType>().Where(x => x.IsExternal)
                                    .Union(modelInterface.StartBlock.Implements.Where(x => x.IsExternal))
                                    .Union(modelInterface.Methods.SelectMany(x => x.Parameters).Select(x => x.Type).OfType<CodeType>().Where(x => x.IsExternal))
                                    .Select(x => x.Name)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return modelInterface;
    }

    private static (CodeInterface, CodeUsing) ReplaceTypeByInterfaceType(CodeClass sourceClass, CodeType originalType, Func<CodeClass, string> interfaceNamingCallback)
    {
        var propertyInterfaceType = CreateModelInterface(sourceClass, interfaceNamingCallback);
        originalType.Name = propertyInterfaceType.Name;
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

    private static readonly CodeUsingDeclarationNameComparer usingComparer = new();
    private static void AliasUsingsWithSameSymbol(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass &&
            currentClass.StartBlock is ClassDeclaration currentDeclaration &&
            currentDeclaration.Usings.Any(x => !x.IsExternal))
        {
            var duplicatedSymbolsUsings = currentDeclaration.Usings.Where(x => !x.IsExternal)
                                                                    .Distinct(usingComparer)
                                                                    .GroupBy(x => x.Declaration.Name, StringComparer.OrdinalIgnoreCase)
                                                                    .Where(x => x.Count() > 1)
                                                                    .SelectMany(x => x)
                                                                    .Union(currentDeclaration
                                                                            .Usings
                                                                            .Where(x => !x.IsExternal)
                                                                            .Where(x => x.Declaration
                                                                                            .Name
                                                                                            .Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase)));
            foreach (var usingElement in duplicatedSymbolsUsings)
                usingElement.Alias = (usingElement.Declaration
                                                .TypeDefinition
                                                .GetImmediateParentOfType<CodeNamespace>()
                                                .Name +
                                    usingElement.Declaration
                                                .TypeDefinition
                                                .Name)
                                    .GetNamespaceImportSymbol();
        }
        CrawlTree(currentElement, AliasUsingsWithSameSymbol);
    }
    private const string AbstractionsPackageName = "@microsoft/kiota-abstractions";
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] {
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
            currentProperty.Type.Name = "Record<string, string>";
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
        else
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
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
            currentMethod.ReturnType.Name = $"Record<string, (node: ParseNode) => void>";
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
                urlTplParams.Description = "The raw url or the Url template parameters for the request.";
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
        CorrectDateTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new CodeTypeBase[] { currentMethod.ReturnType })
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "DateTimeOffset",
            ("Date", null)
        },
        {
            "TimeSpan",
            ("Duration", new CodeUsing
            {
                Name = "Duration",
                Declaration = new CodeType
                {
                    Name = AbstractionsPackageName,
                    IsExternal = true,
                },
            })
        },
        {
            "DateOnly",
            (null, new CodeUsing
            {
                Name = "DateOnly",
                Declaration = new CodeType
                {
                    Name = AbstractionsPackageName,
                    IsExternal = true,
                },
            })
        },
        {
            "TimeOnly",
            (null, new CodeUsing
            {
                Name = "TimeOnly",
                Declaration = new CodeType
                {
                    Name = AbstractionsPackageName,
                    IsExternal = true,
                },
            })
        },
    };
}
