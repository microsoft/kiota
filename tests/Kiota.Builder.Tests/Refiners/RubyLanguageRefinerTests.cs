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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task AddsDefaultImports()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEmpty(requestBuilder.StartBlock.Usings);

    }
    #endregion
    #region RubyLanguageRefinerTests
    [Fact]
    public async Task CorrectsCoreTypes()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.Equal("Hash.new", property.DefaultValue);
    }
    [Fact]
    public async Task EscapesReservedKeywords()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.NotEqual("break", model.Name);
        Assert.Contains("escaped", model.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateTime", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("MicrosoftKiotaAbstractions::ISODuration", method.ReturnType.Name);
    }
    [Fact]
    public async Task AddNamespaceModuleImports()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
        Assert.Single(declaration.Usings.Where(static x => "Message".Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase)));
        Assert.Single(declaration.Usings.Where(static x => "graph".Equals(x.Declaration.Name, StringComparison.OrdinalIgnoreCase)));
    }
    #endregion
}
