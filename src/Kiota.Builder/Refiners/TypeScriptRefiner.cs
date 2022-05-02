using System.Linq;
using System;
using Kiota.Builder.Extensions;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;
public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public TypeScriptRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override void Refine(CodeNamespace generatedCode)
    {
        AddDefaultImports(generatedCode, defaultUsingEvaluators);
        ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
        RemoveCancellationParameter(generatedCode);
        CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
        CorrectCoreTypesForBackingStore(generatedCode, "BackingStoreFactorySingleton.instance.createBackingStore()");
        AddInnerClasses(generatedCode,
            true,
            string.Empty,
            true);
        DisableActionOf(generatedCode,
            CodeParameterKind.QueryParameter);
        AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        AliasUsingsWithSameSymbol(generatedCode);
        AddParsableImplementsForModelClasses(generatedCode, "Parsable");
        ReplaceBinaryByNativeType(generatedCode, "ArrayBuffer", null);
        ReplaceReservedNames(generatedCode, new TypeScriptReservedNamesProvider(), x => $"{x}_escaped");
        //AddGetterAndSetterMethods(generatedCode,
        //    new() {
        //        CodePropertyKind.Custom,
        //        CodePropertyKind.AdditionalData,
        //    },
        //    _configuration.UsesBackingStore,
        //    false,
        //    string.Empty,
        //    string.Empty);
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
    }

    private static void AddModelsInterfaces(CodeElement generatedCode)
    {
        CopyModelClassesAsInterfacesTS(
           generatedCode,
           x => $"{x.Name.ToFirstCharacterUpperCase()}Interface".ToFirstCharacterUpperCase()
       );

        RenameInterfacesAndModels(generatedCode);

    }

    private static void RenameInterfacesAndModels(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
        {
            currentClass.Name = currentClass.Name + "Impl";
        }
        if (currentElement is CodeInterface modelInterface && modelInterface.IsOfKind(CodeInterfaceKind.Model))
        {
            if (modelInterface.Name.EndsWith("Interface"))
            {
                modelInterface.Name = modelInterface.Name.Split("Interface")[0];
            }
        }
        //else if (currentElement is CodeProperty codeProperty &&
        //        codeProperty.IsOfKind(CodePropertyKind.RequestBody) &&
        //        codeProperty.Type is CodeType type &&
        //        type.TypeDefinition is CodeInterface modelInterface)
        //{
        //    if (modelInterface.Name.EndsWith("Interface"))
        //    {
        //        modelInterface.Name = modelInterface.Name.Split("Interface")[0];
        //    }
        //}
        //else if (currentElement is CodeMethod codeMethod &&
        //      codeMethod.IsOfKind(CodeMethodKind.RequestExecutor) &&
        //      codeMethod.ReturnType is CodeType returnType &&
        //      returnType.TypeDefinition is CodeInterface inter)
        //{
        //    if (inter.Name.EndsWith("Interface"))
        //    {
        //        inter.Name = inter.Name.Split("Interface")[0];
        //    }
        //}
        //else if (currentElement is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model))
        //{

        //    foreach (var impl in codeClass.StartBlock.Implements)
        //    {
        //        if (impl.Name.EndsWith("Interface"))
        //        {
        //            impl.Name = impl.Name.Split("Interface")[0];
        //        }
        //    }
        //}
        //else if (currentElement is CodeInterface interBase)
        //{

        //    if (interBase.StartBlock?.inherits.TypeDefinition is CodeInterface interParent)

        //        if (interParent.Name.EndsWith("Interface"))
        //        {
        //            interParent.Name = interParent.Name.Split("Interface")[0];
        //        }
        //}

        CrawlTree(currentElement, x => RenameInterfacesAndModels(x));
    }

    private static void CopyModelClassesAsInterfacesTS(CodeElement currentElement, Func<CodeClass, string> interfaceNamingCallback)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
        {
            CopyClassAsInterface(currentClass, interfaceNamingCallback);
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
            SetTypeAndAddUsing(CopyClassAsInterface(modelClass, interfaceNamingCallback), type, codeProperty);
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
                    SetTypeAndAddUsing(CopyClassAsInterface(type1.TypeDefinition as CodeClass, interfaceNamingCallback), type1, requestBodyParam);
                }
                SetTypeAndAddUsing(CopyClassAsInterface(returnClass, interfaceNamingCallback), returnType, codeMethod);

               var parentClass =  codeMethod.GetImmediateParentOfType<CodeClass>();

                if (parentClass != null && parentClass.Name != returnClass.Name) 
                {
                    parentClass.AddUsing(new CodeUsing { Name = returnClass.Parent.Name, Declaration = new CodeType { Name = returnClass.Name,TypeDefinition =returnClass } });
                  
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
                    SetTypeAndAddUsing(CopyClassAsInterface(codeClass, interfaceNamingCallback), type1, requestBodyParam1);


                    var parentClass = codeMethod.GetImmediateParentOfType<CodeClass>();

                    if (parentClass != null && parentClass.Name != codeClass.Name)
                    {
                        parentClass.AddUsing(new CodeUsing { Name = codeClass.Parent.Name, Declaration = new CodeType { Name = codeClass.Name, TypeDefinition = codeClass } });

                    }
                }
            }
        }

        CrawlTree(currentElement, x => CopyModelClassesAsInterfacesTS(x, interfaceNamingCallback));
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
    private static CodeInterface CopyClassAsInterface(CodeClass modelClass, Func<CodeClass, string> interfaceNamingCallback)
    {
        var finalInterfaceName = modelClass.Name.ToFirstCharacterUpperCase();
        var temporaryInterfaceName = interfaceNamingCallback.Invoke(modelClass);
        var targetNS = modelClass.GetImmediateParentOfType<CodeNamespace>();
        var existing = targetNS.FindChildByName<CodeInterface>(temporaryInterfaceName, false);
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
                       targetNS.AddInterface(insertValue).First();

        //var modelInterface = targetNS.AddInterface(insertValue).First();
       // var usingsToRemove = new List<string>();

        var classModelChildItems = modelClass.GetChildElements(true);

       // var targetUsingBlock = shouldInsertUnderParentClass ? modelParentClass.StartBlock as ProprietableBlockDeclaration : modelInterface.StartBlock;
        var targetUsingBlock = modelInterface.StartBlock;
        var usingsToAdd = new List<CodeUsing>();

        /**
         * If a child model class inherits from parent model class, the child interface extends from the parent interface.
         */
        if (modelClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            var parentInterface = CopyClassAsInterface(baseClass, interfaceNamingCallback);
            modelInterface.StartBlock.inherits = new CodeType
            {
                Name = parentInterface.Name,
                TypeDefinition = parentInterface,
            };
            var parentInterfaceNS = parentInterface.GetImmediateParentOfType<CodeNamespace>();
            //if (parentInterfaceNS != targetNS)
                modelInterface.AddUsing(new CodeUsing
                {
                    Name = parentInterfaceNS.Name,
                    Declaration = new CodeType
                    {
                        //Name = parentInterface.Name.EndsWith("Interface")? parentInterface.Name.Split("Interface")[0]: parentInterface.Name,
                        Name = parentInterface.Name,
                        TypeDefinition = parentInterface,
                    },
                });
        }

        /**
         * Replace model classes by interfaces for property types 
         */
        foreach (var mProp in classModelChildItems.OfType<CodeProperty>())
        {
            if (mProp.Type is CodeType propertyType && propertyType.TypeDefinition is CodeClass propertyClass)
            {
                var codeUsing = ReplaceTypeByInterfaceType(propertyClass, propertyType, interfaceNamingCallback);//, usingsToRemove);

                if (modelInterface.Name != codeUsing.Item1.Name) {
                    modelInterface.AddUsing(codeUsing.Item2);
                }
                modelClass.AddUsing(codeUsing.Item2);

                /***
                 * Add "impl" or model classes in class usings as they will be required by the serializer method. 
                 */
                if (modelClass.Name != propertyClass.Name) {
                    modelClass.AddUsing(new CodeUsing
                    {
                        Name = mProp.Parent.Name,
                        Declaration = new CodeType
                        {
                            Name = propertyClass.Name + "Impl",
                            TypeDefinition = propertyClass,
                        }
                    });
                }
            }
            else if (mProp.Type is CodeType nonClassPropertyType && !nonClassPropertyType.IsExternal && nonClassPropertyType is CodeType nonClassTypeDef)
            {
                modelInterface.AddUsing(new CodeUsing
                {
                    Name = mProp.Parent.Name,
                    Declaration = new CodeType
                    {
                        Name = nonClassTypeDef.Name,
                        TypeDefinition = nonClassTypeDef,
                    }
                });

            }
            modelInterface.AddProperty(mProp);
        }

        /*
         * Set the model interface in startblock implements of the model class
         */
        if (modelClass.StartBlock.Implements.Any())
        {
            var originalImplements = modelClass.StartBlock.Implements.Where(x => x.TypeDefinition != modelInterface).ToArray();
            modelInterface.StartBlock.AddImplements(originalImplements
                                                        .Select(x => x.Clone() as CodeType)
                                                        .ToArray());
            modelClass.StartBlock.RemoveImplements(originalImplements);
        }

        modelClass.StartBlock.AddImplements(new CodeType
        {
            Name = finalInterfaceName,
            TypeDefinition = modelInterface,
        });

        /**
         * Set a parameter of model interface type in the model class constructor
         */
        var constructor = modelClass.Methods.FirstOrDefault(x => x is CodeMethod method1 && method1.IsOfKind(CodeMethodKind.Constructor));

        constructor.AddParameter(new CodeParameter
        {
            Name = finalInterfaceName.ToFirstCharacterLowerCase(),
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
                if (methodReturnType.TypeDefinition is CodeClass methodTypeClass)
                {
                    var resultType = ReplaceTypeByInterfaceType(methodTypeClass, methodReturnType, interfaceNamingCallback);
                    modelClass.AddUsing(resultType.Item2);
                    if (resultType.Item2.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>() != targetNS)
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
                        if (resultType.Item2.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>() != targetNS)
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

        //modelClass.RemoveUsingsByDeclarationName(usingsToRemove.ToArray());
        var externalTypesOnInter = modelInterface.Methods.Select(x => x.ReturnType).OfType<CodeType>().Where(x => x.IsExternal)
                                    .Union(modelInterface.StartBlock.Implements.Where(x => x.IsExternal))
                                    .Union(modelInterface.Methods.SelectMany(x => x.Parameters).Select(x => x.Type).OfType<CodeType>().Where(x => x.IsExternal))
                                    .Select(x => x.Name)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        //if (shouldInsertUnderParentClass)
        //    usingsToAdd.AddRange(modelParentClass.Usings.Where(x => x.IsExternal && externalTypesOnInter.Contains(x.Name)));
     //   targetUsingBlock.AddUsings(usingsToAdd.ToArray());
        return modelInterface;
    }
    private static (CodeInterface, CodeUsing) ReplaceTypeByInterfaceType(CodeClass sourceClass, CodeType originalType, Func<CodeClass, string> interfaceNamingCallback, List<string> usingsToRemove = null)
    {
        var propertyInterfaceType = CopyClassAsInterface(sourceClass, interfaceNamingCallback);
        originalType.Name = propertyInterfaceType.Name;
        originalType.TypeDefinition = propertyInterfaceType;
        //if (usingsToRemove != null) 
        //{
        //    usingsToRemove.Add(sourceClass.Name);
        //}
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
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "RequestOption[]");
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Headers)).ToList().ForEach(x => { x.Type.Name = "Record<string, string>"; x.Type.ActionOf = false; });
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
