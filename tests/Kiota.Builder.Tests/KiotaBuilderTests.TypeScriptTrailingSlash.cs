using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Fact]
    public async Task PreservesTypeScriptRequestBuildersForTrailingSlashAsync()
    {
        const string description = """
        {"openapi":"3.0.3","info":{"title":"Trailing slash","version":"1"},
        "paths":{"/token":{"get":{"responses":{"200":{"description":"ok","content":{"text/plain":{"schema":{"type":"string"}}}}}}},
        "/token/":{"post":{"requestBody":{"content":{"application/json":{"schema":{"type":"object","properties":{"value":{"type":"string"}}}}}},
        "responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"type":"object","properties":{"result":{"type":"string"}}}}}}}}}}}
        """;
        await using var stream = await GetDocumentStreamAsync(description);
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        await ILanguageRefiner.RefineAsync(configuration, model, TestContext.Current.CancellationToken);
        var ns = model.FindNamespaceByName($"{configuration.ClientNamespaceName}.token");
        Assert.NotNull(ns);
        var file = Assert.Single(ns.Files);
        Assert.Equal(2, file.Interfaces.Count(x => x.Kind == CodeInterfaceKind.RequestBuilder));
        Assert.Contains(file.Interfaces, x => x.Name.Equals("PostRequestBody", System.StringComparison.OrdinalIgnoreCase));
        Assert.Contains(file.Interfaces, x => x.Name.Equals("PostResponse", System.StringComparison.OrdinalIgnoreCase));
        foreach (var metadata in file.Constants.Where(x => x.Kind == CodeConstantKind.RequestsMetadata))
        {
            using var output = new StringWriter();
            var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, "./", "client");
            writer.SetTextWriter(output);
            writer.Write(metadata);
            var expectedTemplate = file.Constants.Single(x => x.Kind == CodeConstantKind.UriTemplate && x.OriginalCodeElement == metadata.OriginalCodeElement);
            Assert.Contains($"uriTemplate: {char.ToUpperInvariant(expectedTemplate.Name[0])}{expectedTemplate.Name[1..]}", output.ToString());
        }
    }
}
