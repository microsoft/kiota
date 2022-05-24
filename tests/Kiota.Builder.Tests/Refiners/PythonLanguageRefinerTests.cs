using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;
public class PythonLanguageRefinerTests {
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    public PythonLanguageRefinerTests() {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
        parentClass = new () {
            Name = "parentClass"
        };
        graphNS.AddClass(parentClass);
    }
#region commonrefiner
    [Fact]
    public void AddsQueryParameterMapperMethod() {
        var model = graphNS.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty {
            Name = "Select",
            SerializationName = "%24select",
            Type = new CodeType {
                Name = "string"
            },
        });

        Assert.Empty(model.Methods);

        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.QueryParametersMapper)));
    }
    [Fact]
    public void AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", declaration.Inherits.Name);
    }
    [Fact]
    public void FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock;
        declaration.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        Assert.Throws<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root));
    }
    [Fact]
    public void AddsUsingsForErrorTypesForRequestExecutor() {
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
#endregion
#region python
    private const string HttpCoreDefaultName = "IRequestAdapter";
    private const string FactoryDefaultName = "ISerializationWriterFactory";
    private const string DeserializeDefaultName = "Dict[str, Callable[[ParseNode], None]]";
    private const string PathParametersDefaultName = "Dictionary<string, object>";
    private const string PathParametersDefaultValue = "new Dictionary<string, object>";
    private const string DateTimeOffsetDefaultName = "DateTimeOffset";
    private const string AddiationalDataDefaultName = "Dictionary<string, object>";
    private const string HandlerDefaultName = "IResponseHandler";
    [Fact]
    public void EscapesReservedKeywords() {
        var model = root.AddClass(new CodeClass {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEqual("break", model.Name);
        Assert.Contains("escaped", model.Name);
    }
    [Fact]
    public void CorrectsCoreType() {

        var model = root.AddClass(new CodeClass () {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty() {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType {
                Name = HttpCoreDefaultName
            }
        }, new () {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = DateTimeOffsetDefaultName,
            }
        }, new () {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType {
                Name = AddiationalDataDefaultName
            }
        }, new () {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType {
                Name = PathParametersDefaultName
            },
            DefaultValue = PathParametersDefaultValue
        });
        var executorMethod = model.AddMethod(new CodeMethod {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        executorMethod.AddParameter(new CodeParameter {
            Name = "handler",
            Kind = CodeParameterKind.ResponseHandler,
            Type = new CodeType {
                Name = HandlerDefaultName,
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
        constructorMethod.AddParameter(new CodeParameter {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType {
                Name = PathParametersDefaultName
            },
        });
        ILanguageRefiner.Refine(new GenerationConfiguration{ Language = GenerationLanguage.Python }, root);
        Assert.Empty(model.Properties.Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => FactoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => AddiationalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => PathParametersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => PathParametersDefaultValue.Equals(x.DefaultValue)));
        Assert.Empty(model.Methods.Where(x => DeserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => HandlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Single(constructorMethod.Parameters.Where(x => x.Type is CodeUnionType));
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime", method.ReturnType.Name);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("date", method.ReturnType.Name);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("time", method.ReturnType.Name);
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("timedelta", method.ReturnType.Name);
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
            Name = "cancellationToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Description = "Cancellation token to use when cancelling requests",
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
#endregion
}
