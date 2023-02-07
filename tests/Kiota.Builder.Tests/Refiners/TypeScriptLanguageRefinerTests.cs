using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class TypeScriptLanguageRefinerTests
{
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    public TypeScriptLanguageRefinerTests()
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

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.QueryParametersMapper)));
    }
    [Fact]
    public async Task AddStaticMethodsUsingsForDeserializer()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        model.AddMethod(new CodeMethod
        {
            Name = "Deserialize",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "void",
                IsExternal = true,
            },
        });

        var subNs = graphNS.AddNamespace($"{graphNS.Name}.subns");

        var propertyModel = subNs.AddClass(new CodeClass
        {
            Name = "somepropertyModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        propertyModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "void",
                IsExternal = true,
            },
        });

        model.AddProperty(new CodeProperty
        {
            Name = "someProperty",
            Type = new CodeType
            {
                Name = "somepropertyModel",
                TypeDefinition = propertyModel,
            },
        });

        Assert.Empty(graphNS.GetChildElements(true).OfType<CodeFunction>());
        Assert.Single(model.GetChildElements(true).OfType<CodeMethod>());

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().Where(x => x.IsOfKind(CodeMethodKind.Factory)));
        Assert.Single(subNs.GetChildElements(true).OfType<CodeFunction>());

        var function = subNs.GetChildElements(true).OfType<CodeFunction>().First();
        Assert.Single(model.Usings.Where(x => !x.IsExternal && x.Declaration.TypeDefinition == function));

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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", declaration.Inherits.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("otherProp"));
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task AddsUsingsForDiscriminatorTypes()
    {
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        Assert.False(factoryMethod.Parent is CodeFunction);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.Equal(childModel, (factoryMethod.Parent as CodeFunction).StartBlock.Usings.First(x => x.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
    }
    #endregion
    #region typescript
    private const string HttpCoreDefaultName = "IRequestAdapter";
    private const string FactoryDefaultName = "ISerializationWriterFactory";
    private const string DeserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
    private const string PathParametersDefaultName = "Dictionary<string, object>";
    private const string PathParametersDefaultValue = "new Dictionary<string, object>";
    private const string DateTimeOffsetDefaultName = "DateTimeOffset";
    private const string AdditionalDataDefaultName = "new Dictionary<string, object>()";
    private const string HandlerDefaultName = "IResponseHandler";
    [Fact]
    public async Task EscapesReservedKeywords()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEqual("break", model.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Escaped", model.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnly", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnly", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Duration", method.ReturnType.Name);
    }
    [Fact]
    public async Task AliasesDuplicateUsingSymbols()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var modelsNS = graphNS.AddNamespace($"{graphNS.Name}.models");
        var source1 = modelsNS.AddClass(new CodeClass
        {
            Name = "source",
            Kind = CodeClassKind.Model
        }).First();
        var submodelsNS = modelsNS.AddNamespace($"{modelsNS.Name}.submodels");
        var source2 = submodelsNS.AddClass(new CodeClass
        {
            Name = "source",
            Kind = CodeClassKind.Model
        }).First();

        var using1 = new CodeUsing
        {
            Name = modelsNS.Name,
            Declaration = new CodeType
            {
                Name = source1.Name,
                TypeDefinition = source1,
                IsExternal = false,
            }
        };
        var using2 = new CodeUsing
        {
            Name = submodelsNS.Name,
            Declaration = new CodeType
            {
                Name = source2.Name,
                TypeDefinition = source2,
                IsExternal = false,
            }
        };
        model.AddUsing(using1);
        model.AddUsing(using2);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(using1.Alias);
        Assert.NotEmpty(using2.Alias);
        Assert.NotEqual(using1.Alias, using2.Alias);
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
            Documentation = new()
            {
                Description = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    #endregion
}
