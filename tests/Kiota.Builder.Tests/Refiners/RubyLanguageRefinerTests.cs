using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class RubyLanguageRefinerTests
{

    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    public RubyLanguageRefinerTests()
    {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
        parentClass = new()
        {
            Name = "parentClass"
        };
        graphNS.AddClass(parentClass);
    }
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task DoesNotKeepCancellationParametersInRequestExecutorsAsync()
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
                DescriptionTemplate = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task AddsDefaultImportsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "rb",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEmpty(requestBuilder.StartBlock.Usings);

    }
    #endregion
    #region RubyLanguageRefinerTests
    [Fact]
    public async Task CorrectsCoreTypesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "rb",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var property = model.AddProperty(new CodeProperty
        {
            Name = "name",
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
            Kind = CodePropertyKind.PathParameters,
            DefaultValue = "wrongDefaultValue"
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.Equal("Hash.new", property.DefaultValue);
    }
    [Fact]
    public async Task EscapesReservedKeywordsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.NotEqual("break", model.Name);
        Assert.Contains("escaped", model.Name);
    }
    [Fact]
    public async Task ConvertEnumsToPascalCaseAsync()
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "foo_bar"
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEqual("foo_bar", model.Name);
        Assert.Contains("FooBar", model.Name);
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeTypeAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateTime", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeTypeAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeTypeAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDurationByNativeTypeAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("MicrosoftKiotaAbstractions::ISODuration", method.ReturnType.Name);
    }
    [Fact]
    public async Task AddNamespaceModuleImportsAsync()
    {
        var declaration = parentClass.StartBlock;
        var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        subNS.AddClass(messageClassDef);
        declaration.AddUsings(new CodeUsing
        {
            Name = messageClassDef.Name,
            Declaration = new()
            {
                Name = messageClassDef.Name,
                TypeDefinition = messageClassDef,
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.Single(declaration.Usings, static x => "Message".Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Single(declaration.Usings, static x => "graph".Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task ShortensLongNamespaceNamesAsync()
    {
        var subNS = graphNS.AddNamespace($"{graphNS.Name}.microsoftGraphDoesUserHaveAccessUserIdUserIdTenantIdTenantIdUserPrincipalNameUserPrincipalName");
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        subNS.AddClass(messageClassDef);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.Null(root.FindNamespaceByName($"{graphNS.Name}.microsoftGraphDoesUserHaveAccessUserIdUserIdTenantIdTenantIdUserPrincipalNameUserPrincipalName"));
        Assert.NotNull(root.FindNamespaceByName($"{graphNS.Name}.i7f5f9550ce583c5b890fd039add74646312e8d1fcdadf26872765e05988073b0"));
        Assert.Equal($"{graphNS.Name}.i7f5f9550ce583c5b890fd039add74646312e8d1fcdadf26872765e05988073b0", subNS.Name);
    }
    [Fact]
    public async Task AddsQueryParameterMapperMethodAsync()
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

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, graphNS);
        Assert.Single(model.Methods, x => x.IsOfKind(CodeMethodKind.QueryParametersMapper));
    }
    [Fact]
    public async Task AddsQueryParameterMapperMethodAfterManglingAsync()
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

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, graphNS);
        Assert.Single(model.Properties, x => x.Name.Equals("if_exists"));
        Assert.Single(model.Properties, x => x.IsNameEscaped);
        Assert.Single(model.Methods, x => x.IsOfKind(CodeMethodKind.QueryParametersMapper));
    }
    [Fact]
    public async Task FlattensModelsNamespaceAsync()
    {
        var config = new GenerationConfiguration { Language = GenerationLanguage.Ruby };
        var modelsNS = root.AddNamespace(config.ModelsNamespaceName);
        var subModelsNS = modelsNS.AddNamespace($"{config.ModelsNamespaceName}.submodels");
        subModelsNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        });
        subModelsNS.AddEnum(new CodeEnum
        {
            Name = "someenum",
        });
        Assert.Empty(modelsNS.Classes);
        Assert.Single(subModelsNS.Classes);
        Assert.Empty(modelsNS.Enums);
        Assert.Single(subModelsNS.Enums);
        await ILanguageRefiner.RefineAsync(config, root);
        Assert.Single(modelsNS.Classes);
        Assert.Empty(subModelsNS.Classes);
        Assert.Single(modelsNS.Enums);
        Assert.Empty(subModelsNS.Enums);
    }
    [Fact]
    public async Task DoesNotCorrectNamesWhenCollisionOccursAsync()
    {
        var config = new GenerationConfiguration { Language = GenerationLanguage.Ruby };
        var modelsNS = root.AddNamespace(config.ModelsNamespaceName);
        modelsNS.AddClass(new CodeClass
        {
            Name = "SomeModel",
            Kind = CodeClassKind.Model,
        });
        modelsNS.AddEnum(new CodeEnum
        {
            Name = "some_model",
        });
        await ILanguageRefiner.RefineAsync(config, root);
        Assert.Single(modelsNS.Classes, x => x.Name.Equals("SomeModel"));
        Assert.Single(modelsNS.Enums, x => x.Name.Equals("some_model"));
    }
    #endregion
}
