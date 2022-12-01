using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class JavaLanguageRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task ReplacesReservedEnumOptions()
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "model",
        }).First();
        var option = new CodeEnumOption {
            Name = "break", // this a keyword
        };
        model.AddOption(option);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal("break_escaped", option.Name);
        Assert.Equal("break", option.SerializationName);
    }
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
    }
    [Fact]
    public async Task FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock;
        declaration.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor() {
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task EscapesReservedKeywordsInInternalDeclaration() {
        var model = root.AddClass(new CodeClass {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        var nUsing = new CodeUsing {
            Name = "some.ns",
        };
        nUsing.Declaration = new CodeType {
            Name = "break",
            IsExternal = false,
        };
        model.AddUsing(nUsing);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("break", nUsing.Declaration.Name);
        Assert.Contains("escaped", nUsing.Declaration.Name);
    }
    [Fact]
    public async Task EscapesReservedKeywords() {
        var model = root.AddClass(new CodeClass {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("break", model.Name);
        Assert.Contains("escaped", model.Name);
    }
    [Fact]
    public async Task AddsDefaultImports() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "rb",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        requestBuilder.AddMethod(new CodeMethod {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string",
            },
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEmpty(requestBuilder.StartBlock.Usings);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("OffsetDateTime", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("LocalDate", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("LocalTime", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Period", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesBinaryByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "binary"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEqual("binary", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesIndexersByMethodsWithParameter() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "requestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        requestBuilder.AddProperty(new CodeProperty {
            Name = "urlTemplate",
            DefaultValue = "path",
            Kind = CodePropertyKind.UrlTemplate,
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Single(requestBuilder.Properties);
        Assert.Empty(requestBuilder.GetChildElements(true).OfType<CodeIndexer>());
        Assert.Single(collectionRequestBuilder.Methods.Where(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)));
        Assert.Single(collectionRequestBuilder.Properties);
    }
    [Fact]
    public async Task DoesNotKeepCancellationParametersInRequestExecutors()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    #endregion
    #region JavaLanguageRefinerTests
    [Fact]
    public async Task AddsEnumSetImport() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings.Where(x => "EnumSet".Equals(x.Name)));
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
        }, new () {
            Name = "headers",
            Kind = CodePropertyKind.Headers,
            Type = new CodeType
            {
                Name = headersDefaultName
            }
        });
        const string additionalDataHolderDefaultName = "IAdditionalDataHolder";
        model.StartBlock.AddImplements(new CodeType {
            Name = additionalDataHolderDefaultName,
            IsExternal = true,
        });
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Empty(model.Properties.Where(x => requestAdapterDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => factoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => additionalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => headersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.StartBlock.Implements.Where(x => additionalDataHolderDefaultName.Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
        Assert.Contains( additionalDataHolderDefaultName[1..], model.StartBlock.Implements.Select(x => x.Name).ToList());
    }
    [Fact]
    public async Task AddsMethodsOverloads() {
        var builder = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var executor = builder.AddMethod(new CodeMethod {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        executor.AddParameter(new() {
            Name = "config",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType {
                Name = "string"
            }
        },
        new() {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType {
                Name = "string"
            }
        });
        var generator = builder.AddMethod(new CodeMethod {
            Name = "generator",
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        generator.AddParameter(executor.Parameters.ToArray());
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        var childMethods = builder.Methods;
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 1);//only the body
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 1);//only the body
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 2);// body + query config
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 2);// body + query config
        Assert.Equal(4, childMethods.Count());
        Assert.Equal(2, childMethods.Count(x => x.IsOverload));
    }
    [Fact]
    public async Task SplitsLongRefiners() {
        var model = new CodeClass {
            Kind = CodeClassKind.Model,
            Name = "model",
        };
        model.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";

        var otherModel = new CodeClass {
            Kind = CodeClassKind.Model,
            Name = "otherModel"
        };
        root.AddClass(otherModel);

        Enumerable.Range(0, 1500).ToList().ForEach(x => model.DiscriminatorInformation.AddDiscriminatorMapping($"#microsoft.graph.{x}", new CodeType {
            Name = $"microsoft.graph.{x}",
            TypeDefinition = otherModel,
        }));
        model.AddMethod(new CodeMethod {
            Kind = CodeMethodKind.Factory,
            Name = "factory",
            ReturnType = new CodeType {
                Name = "model",
                TypeDefinition = model,
            },
            IsAsync = false,
            IsStatic = true,
        });
        root.AddClass(model);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal(4, model.Methods.Count());
        Assert.Equal("String", model.Methods.First(static x => x.IsOverload).Parameters.First().Type.Name);
    }
    #endregion
}
