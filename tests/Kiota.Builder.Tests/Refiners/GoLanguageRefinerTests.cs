using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;
public class GoLanguageRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLangRefinerTests
     [Fact]
    public void AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            ClassKind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        var declaration = model.StartBlock as CodeClass.Declaration;

        Assert.Contains("ApiError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", declaration.Inherits.Name);
    }
    [Fact]
    public void FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            ClassKind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock as CodeClass.Declaration;
        declaration.Inherits = new CodeType {
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
            ClassKind = CodeClassKind.Model,
        }); // so move to models namespace finds the models namespace
        var requestBuilder = main.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            ClassKind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = models.AddNamespace($"{models.Name}.subns"); // otherwise the import gets trimmed
        var errorClass = subNS.AddClass(new CodeClass {
            Name = "Error4XX",
            ClassKind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod {
            Name = "get",
            MethodKind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            },
            ErrorMappings = new () {
                { "4XX", new CodeType {
                        Name = "Error4XX",
                        TypeDefinition = errorClass,
                    } 
                },
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        var declaration = requestBuilder.StartBlock as CodeClass.Declaration;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public void AddsUsingsForDiscriminatorTypes() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            ClassKind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            ClassKind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as CodeClass.Declaration).Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            MethodKind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            DiscriminatorMappings = new() {
                { "ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    }
                },
            }
        }).First();
        var parentModelDeclaration = parentModel.StartBlock as CodeClass.Declaration;
        Assert.Empty(parentModelDeclaration.Usings);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(childModel, parentModelDeclaration.Usings.First(x => x.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
        Assert.Null(parentModelDeclaration.Usings.FirstOrDefault(x => x.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public void AddsUsingsForFactoryMethods() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            ClassKind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            ClassKind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as CodeClass.Declaration).Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            MethodKind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            DiscriminatorMappings = new() {
                { "ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    }
                },
            }
        }).First();
        var requestBuilderClass = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            ClassKind = CodeClassKind.RequestBuilder,
        }).First();
        var requestExecutor = requestBuilderClass.AddMethod(new CodeMethod {
            Name = "get",
            MethodKind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = parentModel.Name,
                TypeDefinition = parentModel,
            },
        }).First();
        var requestBuilderDeclaration = requestBuilderClass.StartBlock as CodeClass.Declaration;
        Assert.Empty(requestBuilderDeclaration.Usings);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(factoryMethod, requestBuilderDeclaration.Usings.First(x => x.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
        Assert.Null(requestBuilderDeclaration.Usings.FirstOrDefault(x => x.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public void DoesNotKeepCancellationParametersInRequestExecutors()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            ClassKind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "getMethod",
            MethodKind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        var cancellationParam = new CodeParameter
        {
            Name = "cancelletionToken",
            Optional = true,
            ParameterKind = CodeParameterKind.Cancellation,
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
            ClassKind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "DateTimeOffset"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesDateOnlyByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "DateOnly"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnly", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesTimeOnlyByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "TimeOnly"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnly", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesDurationByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "TimeSpan"
            },
        }).First();
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
            ClassKind = CodeClassKind.QueryParameters
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
            ClassKind = CodeClassKind.Model
        }).First();
        var property = model.AddProperty(new CodeProperty {
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("select_escaped", property.Name);
        Assert.True(property.IsNameEscaped);
    }
    [Fact]
    public void ReplacesRequestBuilderPropertiesByMethods() {
        var model = root.AddClass(new CodeClass {
            Name = "someModel",
            ClassKind = CodeClassKind.RequestBuilder
        }).First();
        var rb = model.AddProperty(new CodeProperty {
            Name = "someProperty",
            PropertyKind = CodePropertyKind.RequestBuilder,
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
        const string addiationalDataDefaultName = "new Dictionary<string, object>()";
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        model.AddProperty(new () {
            Name = "core",
            PropertyKind = CodePropertyKind.RequestAdapter,
            Type = new CodeType {
                Name = requestAdapterDefaultName
            }
        }, new () {
            Name = "someDate",
            PropertyKind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = dateTimeOffsetDefaultName,
            }
        }, new () {
            Name = "additionalData",
            PropertyKind = CodePropertyKind.AdditionalData,
            Type = new CodeType {
                Name = addiationalDataDefaultName
            }
        });
        const string handlerDefaultName = "IResponseHandler";
        const string headersDefaultName = "IDictionary<string, string>";
        var executorMethod = model.AddMethod(new CodeMethod {
            Name = "executor",
            MethodKind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }, new () {
            Name = "deserializeFields",
            ReturnType = new CodeType {
                Name = deserializeDefaultName,
            },
            MethodKind = CodeMethodKind.Deserializer
        }).First();
        executorMethod.AddParameter(new () {
            Name = "handler",
            ParameterKind = CodeParameterKind.ResponseHandler,
            Type = new CodeType {
                Name = handlerDefaultName,
            }
        }, new () {
            Name = "headers",
            ParameterKind = CodeParameterKind.Headers,
            Type = new CodeType {
                Name = headersDefaultName
            }
        });
        const string serializerDefaultName = "ISerializationWriter";
        var serializationMethod = model.AddMethod(new CodeMethod {
            Name = "seriailization",
            MethodKind = CodeMethodKind.Serializer,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        serializationMethod.AddParameter(new CodeParameter {
            Name = "handler",
            ParameterKind = CodeParameterKind.Serializer,
            Type = new CodeType {
                Name = serializerDefaultName,
            }
        });
        var constructorMethod = model.AddMethod(new CodeMethod {
            Name = "constructor",
            MethodKind = CodeMethodKind.Constructor,
            ReturnType = new CodeType {
                Name = "void"
            }
        }).First();
        var rawUrlParam = new CodeParameter {
            Name = "rawUrl",
            ParameterKind = CodeParameterKind.RawUrl,
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
            PropertyKind = CodePropertyKind.PathParameters,
            DefaultValue = "wrongDefaultValue"
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(model.Properties.Where(x => requestAdapterDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => factoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => addiationalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Equal("make(map[string]string)", pathParamsProp.DefaultValue);
        Assert.Equal("map[string]string", pathParamsProp.Type.Name);
        Assert.False(rawUrlParam.Type.IsNullable);
    }
    #endregion
}
