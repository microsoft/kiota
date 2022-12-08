using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class GoLanguageRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLangRefinerTests
    [Fact]
    public async Task AddsInnerClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method1",
            ReturnType = new CodeType {
                Name = "string",
                IsExternal = true
            }
        }).First();
        var parameter = new CodeParameter {
            Name = "param1",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType {
                Name = "SomeCustomType",
                ActionOf = true,
                TypeDefinition = new CodeClass {
                    Name = "SomeCustomType"
                }
            }
        };
        method.AddParameter(parameter);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(2, model.GetChildElements(true).Count());
    }
    [Fact]
    public async Task TrimsCircularDiscriminatorReferences() {
        var modelsNS = root.AddNamespace("ApiSdk.models");
        var baseModel = modelsNS.AddClass(new CodeClass {
            Kind = CodeClassKind.Model,
            Name = "BaseModel",
        }).First();
        baseModel.AddProperty(new CodeProperty {
            Name = "Discriminator",
            Type = new CodeType {Name = "string"},
        });
        var subNamespace = modelsNS.AddNamespace($"{modelsNS.Name}.sub");
        var derivedModel = subNamespace.AddClass(new CodeClass{
            Kind = CodeClassKind.Model,
            Name = "DerivedModel",
        }).First();
        derivedModel.StartBlock.Inherits = new CodeType {
            Name = baseModel.Name,
            TypeDefinition = baseModel,
        };
        var factoryMethod = baseModel.AddMethod(new CodeMethod {
            Kind = CodeMethodKind.Factory,
            Name = "factory",
            ReturnType = new CodeType {
                Name = baseModel.Name,
                TypeDefinition = baseModel,
            },
        }).First();
        baseModel.DiscriminatorInformation.DiscriminatorPropertyName = "Discriminator";
        baseModel.DiscriminatorInformation.AddDiscriminatorMapping("DerivedModel", new CodeType{ Name = derivedModel.Name, TypeDefinition = derivedModel });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(baseModel.DiscriminatorInformation.DiscriminatorMappings);
        Assert.Empty(baseModel.Usings.Where(x => x.Name.Equals("models.sub", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task ReplacesModelsByInterfaces() {
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
        var property = model.AddProperty(new CodeProperty {
            Name = "someProp",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = model.Name,
                TypeDefinition = model,
            },
        }).First();
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
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
    public async Task ReplacesModelsByInnerInterfaces() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Single(root.GetChildElements(true).OfType<CodeInterface>());
        var responseInter = requestBuilder.GetChildElements(true).OfType<CodeInterface>().LastOrDefault();
        Assert.NotNull(responseInter);
    }
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Contains("ApiError", model.StartBlock.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", model.StartBlock.Inherits.Name);
    }
    [Fact]
    public async Task FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        model.StartBlock.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.DoesNotContain("Error4XX", requestBuilder.StartBlock.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task AddsUsingsForDiscriminatorTypes() {
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
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "foo";
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        Assert.Empty(parentModel.StartBlock.Usings);
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(childModel, parentModel.StartBlock.Usings.First(x => x.Declaration.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
        Assert.Null(parentModel.StartBlock.Usings.FirstOrDefault(x => x.Declaration.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task AddsUsingsForFactoryMethods() {
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
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(factoryMethod, requestBuilderClass.StartBlock.Usings.First(x => x.Declaration.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
    }
    [Fact]
    public async Task RenamesCancellationParametersInRequestExecutors()
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
            Documentation = new() {
                Description = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.True(method.Parameters.Any());
        Assert.Contains(cancellationParam, method.Parameters);
        Assert.Equal("ctx", cancellationParam.Name);
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeType() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeType() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnly", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeType() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnly", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDurationByNativeType() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("ISODuration", method.ReturnType.Name);
    }
    #endregion

    #region GoRefinerTests
    [Fact]
    public async Task DoesNotEscapePublicPropertiesReservedKeywordsForQueryParameters() {
        var model = root.AddClass(new CodeClass {
            Name = "SomeClass",
            Kind = CodeClassKind.QueryParameters
        }).First();
        var property = model.AddProperty(new CodeProperty {
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("select", property.Name);
        Assert.False(property.IsNameEscaped);
    }
    [Fact]
    public async Task EscapesPublicPropertiesReservedKeywordsForModels() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("select_escaped", property.Name);
        Assert.True(property.IsNameEscaped);
    }
    [Fact]
    public async Task ReplacesRequestBuilderPropertiesByMethods() {
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(model.Properties);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.RequestBuilderBackwardCompatibility)));
    }
    [Fact]
    public async Task AddsErrorImportForEnums() {
        var testEnum = root.AddEnum(new CodeEnum {
            Name = "TestEnum",

        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Single(testEnum.Usings.Where(x => x.Name == "errors"));
    }
    [Fact]
    public async Task CorrectsCoreType() {
        const string requestAdapterDefaultName = "IRequestAdapter";
        const string factoryDefaultName = "ISerializationWriterFactory";
        const string deserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
        const string dateTimeOffsetDefaultName = "DateTimeOffset";
        const string additionalDataDefaultName = "new Dictionary<string, object>()";
        const string headersDefaultName = "IDictionary<string, string>";
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new() {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType {
                Name = requestAdapterDefaultName
            }
        }, new() {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = dateTimeOffsetDefaultName,
            }
        }, new() {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType {
                Name = additionalDataDefaultName
            }
        }, new() {
            Name = "headers",
            Kind = CodePropertyKind.Headers,
            Type = new CodeType {
                Name = headersDefaultName
            }
        });
        var executorMethod = model.AddMethod(new CodeMethod {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }, new() {
            Name = "deserializeFields",
            ReturnType = new CodeType {
                Name = deserializeDefaultName,
            },
            Kind = CodeMethodKind.Deserializer
        }).First();
        const string serializerDefaultName = "ISerializationWriter";
        var serializationMethod = model.AddMethod(new CodeMethod {
            Name = "serialization",
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(model.Properties.Where(x => requestAdapterDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => factoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => additionalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => headersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Equal("make(map[string]string)", pathParamsProp.DefaultValue);
        Assert.Equal("map[string]string", pathParamsProp.Type.Name);
        Assert.False(rawUrlParam.Type.IsNullable);
    }
    [Fact]
    public async Task RemovesPropertyRelyingOnSubModules() {
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
        mainModel.AddMethod(new CodeMethod {
            Name = $"get{property.Name}",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            },
            AccessedProperty = property
        });
        var setter = mainModel.AddMethod(new CodeMethod {
            Name = $"get{property.Name}",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType {
                Name = "void",
                IsExternal = true,
            },
            AccessedProperty = property
        }).First();
        setter.AddParameter(new CodeParameter {
            Name = "value",
            Type = new CodeType {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            }
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(mainModel.Properties);
        Assert.Empty(mainModel.Methods.Where(x => x.IsAccessor));
    }
    [Fact]
    public async Task AddsMethodsOverloads()
    {
        var builder = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var executor = builder.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        executor.AddParameter(new()
        {
            Name = "handler",
            Kind = CodeParameterKind.ResponseHandler,
            Type = new CodeType
            {
                Name = "string"
            }
        },
        new()
        {
            Name = "config",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        },
        new()
        {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        var generator = builder.AddMethod(new CodeMethod
        {
            Name = "generator",
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        generator.AddParameter(executor.Parameters.Where(x => !x.IsOfKind(CodeParameterKind.ResponseHandler)).ToArray());
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        var childMethods = builder.Methods;
        Assert.DoesNotContain(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor)); // no executor overloads
        Assert.Empty(childMethods.Where(x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator))); // no generator overloads
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 2);// body + query
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 3);// ctx + body + query config
        Assert.Equal(2, childMethods.Count());
    }
    #endregion
}
