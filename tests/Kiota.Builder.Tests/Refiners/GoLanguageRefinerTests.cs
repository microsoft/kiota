using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;
public class GoLanguageRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLangRefinerTests
    [Fact]
    public void ReplacesModelsByInterfaces() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        var executorMethod = requestBuilder.AddMethod(new CodeMethod {
            Name = "Execute",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = model.Name,
                TypeDefinition = model,
            },
        }).First();
        var executorParameter = new CodeParameter {
            Name = "requestBody",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType {
                Name = model.Name,
                TypeDefinition = model,
            },
        };
        executorMethod.AddParameter(executorParameter);
        var property = model.AddProperty(new CodeProperty{
            Name = "someProp",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = model.Name,
                TypeDefinition = model,
            },
        }).First();
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Single(root.GetChildElements(true).OfType<CodeInterface>());
        var inter = root.GetChildElements(true).OfType<CodeInterface>().First();

        Assert.NotEqual(model.Name, inter.Name);
        var propertyType = property.Type as CodeType;
        Assert.NotNull(propertyType);
        Assert.Equal(inter, propertyType.TypeDefinition);
        var executorParameterType = executorParameter.Type as CodeType;
        Assert.NotNull(executorParameterType);
        Assert.Equal(inter, executorParameterType.TypeDefinition);
        var executorMethodReturnType = executorMethod.ReturnType as CodeType;
        Assert.NotNull(executorMethodReturnType);
        Assert.Equal(inter, executorMethodReturnType.TypeDefinition);
    }
    [Fact]
    public void ReplacesModelsByInnerInterfaces() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var responseModel = requestBuilder.AddInnerClass(new CodeClass {
                Name = "someresponsemodel",
                Kind = CodeClassKind.Model,
        }).First();
        

        var executorMethod = requestBuilder.AddMethod(new CodeMethod {
            Name = "Execute",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = responseModel.Name,
                TypeDefinition = responseModel,
            },
        }).First();
        var executorParameter = new CodeParameter {
            Name = "requestBody",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType {
                Name = model.Name,
                TypeDefinition = model,
            },
        };
        executorMethod.AddParameter(executorParameter);
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Single(root.GetChildElements(true).OfType<CodeInterface>());
        var responseInter = requestBuilder.GetChildElements(true).OfType<CodeInterface>().LastOrDefault();
        Assert.NotNull(responseInter);
    }
    [Fact]
    public void AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Contains("ApiError", model.StartBlock.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", model.StartBlock.Inherits.Name);
    }
    [Fact]
    public void FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        model.StartBlock.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        Assert.Throws<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root));
    }
    [Fact]
    public void AddsUsingsForErrorTypesForRequestExecutor() {
        var main = root.AddNamespace("main");
        var models = main.AddNamespace($"{main.Name}.models");
        models.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }); // so move to models namespace finds the models namespace
        var requestBuilder = main.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = models.AddNamespace($"{models.Name}.subns"); // otherwise the import gets trimmed
        var errorClass = subNS.AddClass(new CodeClass {
            Name = "Error4XX",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            },
        }).First();
        requestExecutor.AddErrorMapping("4XX", new CodeType {
                        Name = "Error4XX",
                        TypeDefinition = errorClass,
                    });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Contains("Error4XX", requestBuilder.StartBlock.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public void AddsUsingsForDiscriminatorTypes() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as ClassDeclaration).Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
        }).First();
        factoryMethod.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        Assert.Empty(parentModel.StartBlock.Usings);
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(childModel, parentModel.StartBlock.Usings.First(x => x.Declaration.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
        Assert.Null(parentModel.StartBlock.Usings.FirstOrDefault(x => x.Declaration.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public void AddsUsingsForFactoryMethods() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        childModel.StartBlock.Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
        }).First();
        factoryMethod.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        var requestBuilderClass = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var requestExecutor = requestBuilderClass.AddMethod(new CodeMethod {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = parentModel.Name,
                TypeDefinition = parentModel,
            },
        }).First();
        Assert.Empty(requestBuilderClass.StartBlock.Usings);
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(factoryMethod, requestBuilderClass.StartBlock.Usings.First(x => x.Declaration.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
    }
    [Fact]
    public void DoesNotKeepCancellationParametersInRequestExecutors()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "getMethod",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        var cancellationParam = new CodeParameter
        {
            Name = "cancelletionToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Description = "Cancellation token to use when cancelling requests",
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public void ReplacesDateTimeOffsetByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "DateTimeOffset"
            },
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesDateOnlyByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "DateOnly"
            },
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnly", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesTimeOnlyByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "TimeOnly"
            },
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnly", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesDurationByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "TimeSpan"
            },
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("ISODuration", method.ReturnType.Name);
    }
    #endregion

    #region GoRefinerTests
    [Fact]
    public void DoesNotEscapePublicPropertiesReservedKeywordsForQueryParameters() {
        var model = root.AddClass(new CodeClass {
            Name = "SomeClass",
            Kind = CodeClassKind.QueryParameters
        }).First();
        var property = model.AddProperty(new CodeProperty {
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("select", property.Name);
        Assert.False(property.IsNameEscaped);
    }
    [Fact]
    public void EscapesPublicPropertiesReservedKeywordsForModels() {
        var model = root.AddClass(new CodeClass {
            Name = "SomeClass",
            Kind = CodeClassKind.Model
        }).First();
        var property = model.AddProperty(new CodeProperty {
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("select_escaped", property.Name);
        Assert.True(property.IsNameEscaped);
    }
    [Fact]
    public void ReplacesRequestBuilderPropertiesByMethods() {
        var model = root.AddClass(new CodeClass {
            Name = "someModel",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var rb = model.AddProperty(new CodeProperty {
            Name = "someProperty",
            Kind = CodePropertyKind.RequestBuilder,
        }).First();
        rb.Type = new CodeType {
            Name = "someType",
        };
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(model.Properties);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.RequestBuilderBackwardCompatibility)));
    }
    [Fact]
    public void AddsErrorImportForEnums() {
        var testEnum = root.AddEnum(new CodeEnum {
            Name = "TestEnum",

        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Single(testEnum.Usings.Where(x => x.Name == "errors"));
    }
    [Fact]
    public void CorrectsCoreType() {
        const string requestAdapterDefaultName = "IRequestAdapter";
        const string factoryDefaultName = "ISerializationWriterFactory";
        const string deserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
        const string dateTimeOffsetDefaultName = "DateTimeOffset";
        const string additionalDataDefaultName = "new Dictionary<string, object>()";
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new () {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType {
                Name = requestAdapterDefaultName
            }
        }, new () {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = dateTimeOffsetDefaultName,
            }
        }, new () {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType {
                Name = additionalDataDefaultName
            }
        });
        const string handlerDefaultName = "IResponseHandler";
        const string headersDefaultName = "IDictionary<string, string>";
        var executorMethod = model.AddMethod(new CodeMethod {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }, new () {
            Name = "deserializeFields",
            ReturnType = new CodeType {
                Name = deserializeDefaultName,
            },
            Kind = CodeMethodKind.Deserializer
        }).First();
        executorMethod.AddParameter(new () {
            Name = "handler",
            Kind = CodeParameterKind.ResponseHandler,
            Type = new CodeType {
                Name = handlerDefaultName,
            }
        }, new () {
            Name = "headers",
            Kind = CodeParameterKind.Headers,
            Type = new CodeType {
                Name = headersDefaultName
            }
        });
        const string serializerDefaultName = "ISerializationWriter";
        var serializationMethod = model.AddMethod(new CodeMethod {
            Name = "seriailization",
            Kind = CodeMethodKind.Serializer,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        serializationMethod.AddParameter(new CodeParameter {
            Name = "handler",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType {
                Name = serializerDefaultName,
            }
        });
        var constructorMethod = model.AddMethod(new CodeMethod {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType {
                Name = "void"
            }
        }).First();
        var rawUrlParam = new CodeParameter {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType {
                Name = "string",
                IsNullable = true,
                IsExternal = true
            }
        };
        constructorMethod.AddParameter(rawUrlParam);
        var pathParamsProp = model.AddProperty(new CodeProperty {
            Name = "name",
            Type = new CodeType {
                Name = "string",
                IsExternal = true
            },
            Kind = CodePropertyKind.PathParameters,
            DefaultValue = "wrongDefaultValue"
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(model.Properties.Where(x => requestAdapterDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => factoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => additionalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Equal("make(map[string]string)", pathParamsProp.DefaultValue);
        Assert.Equal("map[string]string", pathParamsProp.Type.Name);
        Assert.False(rawUrlParam.Type.IsNullable);
    }
    [Fact]
    public void RemovesPropertyRelyingOnSubModules() {
        var models = root.AddNamespace("ApiSdk.models");
        var submodels = models.AddNamespace($"{models.Name}.submodels");
        var propertyModel = submodels.AddClass(new CodeClass {
            Name = "propertyModel",
            Kind = CodeClassKind.Model
        }).First();
        var mainModel = models.AddClass(new CodeClass {
            Name = "mainModel",
            Kind = CodeClassKind.Model
        }).First();
        var property = mainModel.AddProperty(new CodeProperty {
            Name = "property",
            Type = new CodeType {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            },
            Kind = CodePropertyKind.Custom
        }).First();
        mainModel.AddMethod(new CodeMethod{
            Name = $"get{property.Name}",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            },
            AccessedProperty = property
        });
        var setter = mainModel.AddMethod(new CodeMethod{
            Name = $"get{property.Name}",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType {
                Name = "void",
                IsExternal = true,
            },
            AccessedProperty = property
        }).First();
        setter.AddParameter(new CodeParameter{
            Name = "value",
            Type = new CodeType {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            }
        });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(mainModel.Properties);
        Assert.Empty(mainModel.Methods.Where(x => x.IsAccessor));
    }
    #endregion
}
