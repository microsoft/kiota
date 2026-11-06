using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class RustLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);

        var declaration = model.StartBlock;

        Assert.Contains("ApiError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", declaration.Inherits.Name);
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns");
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task EscapesReservedKeywordsInInternalDeclaration()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        var nUsing = new CodeUsing
        {
            Name = "some.ns",
        };
        nUsing.Declaration = new CodeType
        {
            IsExternal = false,
            TypeDefinition = model
        };
        model.AddUsing(nUsing);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        // Rust escapes reserved words with r# prefix
        Assert.NotEqual("break", nUsing.Declaration.Name, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task EscapesReservedKeywords()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        Assert.NotEqual("break", model.Name, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetWithChrono()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "DateTimeOffset"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Contains("chrono", model.StartBlock.Usings.Select(x => x.Name));
    }
    [Fact]
    public async Task ReplacesGuidWithUuid()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "Guid"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Contains("uuid", model.StartBlock.Usings.Select(x => x.Name));
    }
    [Fact]
    public async Task ReplacesIndexersByMethods()
    {
        var collectionNS = root.AddNamespace("collection");
        var itemsNs = collectionNS.AddNamespace($"{collectionNS.Name}.items");
        var requestBuilder = itemsNs.AddClass(new CodeClass
        {
            Name = "requestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        requestBuilder.AddProperty(new CodeProperty
        {
            Name = "urlTemplate",
            DefaultValue = "path",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "string",
            }
        });
        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "idx",
            ReturnType = new CodeType
            {
                Name = requestBuilder.Name,
                TypeDefinition = requestBuilder,
            },
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string",
                },
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        Assert.Single(requestBuilder.Methods, x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility));
    }
    [Fact]
    public async Task RemovesCancellationParameter()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "getAction",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "cancellationToken",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "CancellationToken"
            },
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(method.Parameters, x => x.IsOfKind(CodeParameterKind.Cancellation));
    }
    [Fact]
    public async Task AddsDefaultImports()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model,
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "name",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string" },
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Rust }, root, TestContext.Current.CancellationToken);
        Assert.Contains("Parsable", model.StartBlock.Usings.Select(x => x.Name));
    }
    #endregion
}
