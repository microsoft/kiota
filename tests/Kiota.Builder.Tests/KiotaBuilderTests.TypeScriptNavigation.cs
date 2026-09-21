using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("/api/foo/foo/bar")]
    [InlineData("/api/foo/{id}/foo/bar")]
    [InlineData("/api/foo/foo/foo/bar")]
    public async Task AliasesRepeatedSegmentNavigationMetadataAsync(string path)
    {
        var description = """
        {"openapi":"3.0.3","info":{"title":"Repeated segments","version":"1"},
        "paths":{"PATH":{"get":{"responses":{"200":{"description":"ok","content":{"text/plain":{"schema":{"type":"string"}}}}}}}}}
        """.Replace("PATH", path);
        await using var stream = await GetDocumentStreamAsync(description);
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        await ILanguageRefiner.RefineAsync(configuration, model, TestContext.Current.CancellationToken);
        var outer = model.FindNamespaceByName($"{configuration.ClientNamespaceName}.api.foo")?.Files.SelectMany(x => x.Interfaces).Single(x => x.Name.Equals("FooRequestBuilder", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(outer);
        var file = outer.GetImmediateParentOfType<CodeFile>();
        var constant = Assert.Single(file.Constants, x => x.Kind == CodeConstantKind.NavigationMetadata);
        var imported = outer.Usings.Where(x => x.Declaration?.TypeDefinition is CodeConstant { Kind: CodeConstantKind.NavigationMetadata }).ToArray();
        Assert.NotEmpty(imported);
        using var output = new StringWriter();
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, "./", "client");
        writer.SetTextWriter(output);
        writer.Write(constant);
        var result = output.ToString();
        foreach (var import in imported)
        {
            if (import.Declaration.Name == constant.Name)
                Assert.False(string.IsNullOrEmpty(import.Alias));
            var symbol = string.IsNullOrEmpty(import.Alias) ? import.Declaration.Name.ToFirstCharacterUpperCase() : import.Alias;
            Assert.Contains($"navigationMetadata: {symbol}", result);
        }
    }
}
