using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;
public class JavaLanguageRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public void AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            ClassKind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        var declaration = model.StartBlock as CodeClass.Declaration;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
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
        Assert.Throws<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root));
    }
    [Fact]
    public void AddsUsingsForErrorTypesForRequestExecutor() {
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            ClassKind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
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
        }).First();
        requestExecutor.ErrorMappings.TryAdd("4XX", new CodeType {
                        Name = "Error4XX",
                        TypeDefinition = errorClass,
                    });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        var declaration = requestBuilder.StartBlock as CodeClass.Declaration;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public void EscapesReservedKeywordsInInternalDeclaration() {
        var model = root.AddClass(new CodeClass {
            Name = "break",
            ClassKind = CodeClassKind.Model
        }).First();
        var nUsing = new CodeUsing {
            Name = "some.ns",
        };
        nUsing.Declaration = new CodeType {
            Name = "break",
            IsExternal = false,
        };
        model.AddUsing(nUsing);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("break", nUsing.Declaration.Name);
        Assert.Contains("escaped", nUsing.Declaration.Name);
    }
    [Fact]
    public void EscapesReservedKeywords() {
        var model = root.AddClass(new CodeClass {
            Name = "break",
            ClassKind = CodeClassKind.Model
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("break", model.Name);
        Assert.Contains("escaped", model.Name);
    }
    [Fact]
    public void AddsDefaultImports() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "rb",
            ClassKind = CodeClassKind.RequestBuilder,
        }).First();
        requestBuilder.AddMethod(new CodeMethod {
            Name = "get",
            MethodKind = CodeMethodKind.RequestExecutor,
        });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEmpty(requestBuilder.StartBlock.Usings);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("OffsetDateTime", method.ReturnType.Name);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("LocalDate", method.ReturnType.Name);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("LocalTime", method.ReturnType.Name);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Period", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesBinaryByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "binary"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEqual("binary", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesIndexersByMethodsWithParameter() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "requestBuilder",
            ClassKind = CodeClassKind.RequestBuilder
        }).First();
        requestBuilder.AddProperty(new CodeProperty {
            Name = "urlTemplate",
            DefaultValue = "path",
            PropertyKind = CodePropertyKind.UrlTemplate,
            Type = new CodeType {
                Name = "string",
            }
        });
        requestBuilder.SetIndexer(new CodeIndexer {
            Name = "idx",
            ReturnType = new CodeType {
                Name = "model",
                TypeDefinition = model,
            },
        });
        var collectionRequestBuilder = root.AddClass(new CodeClass {
            Name = "CollectionRequestBUilder",
        }).First();
        collectionRequestBuilder.AddProperty(new CodeProperty {
            Name = "collection",
            Type = new CodeType {
                Name = "requestBuilder",
                TypeDefinition = requestBuilder,
            },
        });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Single(requestBuilder.Properties);
        Assert.Empty(requestBuilder.GetChildElements(true).OfType<CodeIndexer>());
        Assert.Single(collectionRequestBuilder.Methods.Where(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)));
        Assert.Single(collectionRequestBuilder.Properties);
    }
    [Fact]
    public void AddsInnerClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
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
            ParameterKind = CodeParameterKind.QueryParameter,
            Type = new CodeType {
                Name = "SomeCustomType",
                ActionOf = true,
                TypeDefinition = new CodeClass {
                    Name = "SomeCustomType"
                }
            }
        };
        method.AddParameter(parameter);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal(2, model.GetChildElements(true).Count());
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    #endregion
    #region JavaLanguageRefinerTests
    [Fact]
    public void AddsEnumSetImport() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty{
            Name = "prop1",
            Type = new CodeType {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum {
                    Name = "SomeEnum",
                    Flags = true,
                    Parent = root,
                }
            }
        });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty((model.StartBlock as CodeClass.Declaration).Usings.Where(x => "EnumSet".Equals(x.Name)));
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
                Name = additionalDataDefaultName
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
            Type = new CodeType() {
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Empty(model.Properties.Where(x => requestAdapterDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => factoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => additionalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
    }
    [Fact]
    public void AddsMethodsOverloads() {
        var builder = root.AddClass(new CodeClass {
            Name = "model",
            ClassKind = CodeClassKind.RequestBuilder
        }).First();
        var executor = builder.AddMethod(new CodeMethod {
            Name = "executor",
            MethodKind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        executor.AddParameter(new CodeParameter {
            Name = "handler",
            ParameterKind = CodeParameterKind.ResponseHandler,
            Type = new CodeType {
                Name = "string"
            }
        });
        executor.AddParameter(new CodeParameter {
            Name = "headers",
            ParameterKind = CodeParameterKind.Headers,
            Type = new CodeType {
                Name = "string"
            }
        });
        executor.AddParameter(new CodeParameter {
            Name = "query",
            ParameterKind = CodeParameterKind.QueryParameter,
            Type = new CodeType {
                Name = "string"
            }
        });
        executor.AddParameter(new CodeParameter {
            Name = "body",
            ParameterKind = CodeParameterKind.RequestBody,
            Type = new CodeType {
                Name = "string"
            }
        });
        executor.AddParameter(new CodeParameter {
            Name = "options",
            ParameterKind = CodeParameterKind.Options,
            Type = new CodeType {
                Name = "string"
            }
        });
        var generator = builder.AddMethod(new CodeMethod {
            Name = "generator",
            MethodKind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        generator.AddParameter(executor.Parameters.Where(x => !x.IsOfKind(CodeParameterKind.ResponseHandler)).ToArray());
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        var childMethods = builder.Methods;
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 1);//only the body
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 1);//only the body
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 2);// body + query params
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 2);// body + query params
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 3);// body + query params + headers
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 3);// body + query params + headers
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 4);// body + query params + headers + options
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 4);// body + query params + headers + options
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 5);// body + query params + headers + options + response handler
        Assert.Equal(9, childMethods.Count());
        Assert.Equal(7, childMethods.Count(x => x.IsOverload));
    }
    #endregion
}
