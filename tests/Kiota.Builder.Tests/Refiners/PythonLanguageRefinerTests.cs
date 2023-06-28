using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class PythonLanguageRefinerTests
{
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    public PythonLanguageRefinerTests()
    {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
        parentClass = new()
        {
            Name = "parentClass"
        };
        graphNS.AddClass(parentClass);
    }
    #region commonrefiner
    [Fact]
    public async Task AddsDefaultImports()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "someModel",
            Kind = CodeClassKind.Model
        }).First();

        Assert.Empty(model.Methods);
        var declaration = model.StartBlock;
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Contains("annotations", declaration.Usings.Select(x => x.Name));
    }
    [Fact]
    public async Task AddsQueryParameterMapperMethod()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty
        {
            Name = "Select",
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string"
            },
        });

        Assert.Empty(model.Methods);

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.QueryParametersMapper)));
    }
    [Fact]
    public async Task AddsQueryParameterMapperMethodAfterMangling()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty
        {
            Name = "ifExists",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.QueryParameter
        });

        Assert.Empty(model.Methods);

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(model.Properties.Where(x => x.Name.Equals("if_exists")));
        Assert.Single(model.Properties.Where(x => x.IsNameEscaped));
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.QueryParametersMapper)));
    }
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        var declaration = model.StartBlock;

        Assert.Contains("APIError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("APIError", declaration.Inherits.Name);
    }
    [Fact]
    public async Task InlineParentOnErrorClassesWhichAlreadyInherit()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        var otherModel = root.AddClass(new CodeClass
        {
            Name = "otherModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = false,
        }).First();
        otherModel.AddProperty(
        new CodeProperty
        {
            Name = "otherProp",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        otherModel.AddMethod(
        new CodeMethod
        {
            Name = "otherMethod",
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
        otherModel.AddUsing(
        new CodeUsing
        {
            Name = "otherNs",
        });
        var declaration = model.StartBlock;
        declaration.Inherits = new CodeType
        {
            TypeDefinition = otherModel
        };
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("other_prop"));
        Assert.Contains(model.Methods, x => x.Name.Equals("otherMethod"));
        Assert.Contains(model.Usings, x => x.Name.Equals("otherNs"));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var errorClass = subNS.AddClass(new CodeClass
        {
            Name = "Error4XX",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();
        requestExecutor.AddErrorMapping("4XX", new CodeType
        {
            Name = "Error4XX",
            TypeDefinition = errorClass,
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

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
    private const string AdditionalDataDefaultName = "Dictionary<string, object>";
    private const string HandlerDefaultName = "IResponseHandler";
    [Fact]
    public async Task EscapesReservedKeywords()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        var voidMethod = model.AddMethod(new CodeMethod
        {
            Name = "continue",// this is a keyword
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEqual("break", model.Name);
        Assert.EndsWith("_", voidMethod.Name);
    }
    [Fact]
    public async Task EscapesExceptionPropertiesNames()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "Error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        exception.AddProperty(new CodeProperty
        {
            Name = "with_traceback",
            Type = new CodeType
            {
                Name = "boolean"
            }

        },
        new CodeProperty
        {
            Type = new CodeType
            {
                Name = "integer"
            },
            Name = "response_status_code",
        }
        ).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        var declaration = exception.StartBlock;

        Assert.Contains("APIError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("APIError", declaration.Inherits.Name);
        Assert.Contains("with_traceback_", exception.Properties.Select(x => x.Name));
        Assert.Contains("response_status_code_", exception.Properties.Select(x => x.Name));
    }
    [Fact]
    public async Task CorrectsCoreType()
    {

        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = HttpCoreDefaultName
            }
        }, new()
        {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = DateTimeOffsetDefaultName,
            }
        }, new()
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = AdditionalDataDefaultName
            }
        }, new()
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = PathParametersDefaultName
            },
            DefaultValue = PathParametersDefaultValue
        });
        var executorMethod = model.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        executorMethod.AddParameter(new CodeParameter
        {
            Name = "handler",
            Kind = CodeParameterKind.ResponseHandler,
            Type = new CodeType
            {
                Name = HandlerDefaultName,
            }
        });
        const string serializerDefaultName = "ISerializationWriter";
        var serializationMethod = model.AddMethod(new CodeMethod
        {
            Name = "seriailization",
            Kind = CodeMethodKind.Serializer,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        serializationMethod.AddParameter(new CodeParameter
        {
            Name = "handler",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = serializerDefaultName,
            }
        });
        var constructorMethod = model.AddMethod(new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        constructorMethod.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = PathParametersDefaultName
            },
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.Empty(model.Properties.Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => FactoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => AdditionalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => PathParametersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => PathParametersDefaultValue.Equals(x.DefaultValue)));
        Assert.Empty(model.Methods.Where(x => DeserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => HandlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Single(constructorMethod.Parameters.Where(x => x.Type is CodeComposedTypeBase));
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeType()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateTimeOffset"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.datetime", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeType()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateOnly"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.date", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeType()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeOnly"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.time", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDurationByNativeType()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeSpan"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.timedelta", method.ReturnType.Name);
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
            Name = "cancellationToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Documentation = new()
            {
                Description = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task AddsPropertiesAndMethodTypesImportsPython()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty
        {
            Name = "Select",
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string"
            },
        });
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();

        Assert.Empty(model.Methods);
        var declaration = model.StartBlock;
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(requestBuilder.Methods.Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor)));
        Assert.DoesNotContain("QueryParameters", declaration.Usings.Select(x => x.Name));
    }
    #endregion
}
