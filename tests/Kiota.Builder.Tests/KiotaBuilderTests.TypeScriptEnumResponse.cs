using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("/groups", "GroupsRequestBuilder")]
    [InlineData("/groups/{group}", "WithGroupItemRequestBuilder")]
    public async Task ImportsEnumResponseObjectAsync(string path, string builderName)
    {
        var description = """
        {"openapi":"3.0.3","info":{"title":"Enum response","version":"1.0"},
        "paths":{"PATH":{"get":{"responses":{"200":{"description":"Success","content":{
        "application/json":{"schema":{"$ref":"#/components/schemas/Status"}}}}}}}},
        "components":{"schemas":{"Status":{"type":"string","enum":["active","inactive"]}}}}
        """.Replace("PATH", path);
        await using var stream = await GetDocumentStreamAsync(description);
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        await ILanguageRefiner.RefineAsync(configuration, model, TestContext.Current.CancellationToken);
        var requestBuilder = model.FindChildByName<CodeInterface>(builderName);
        Assert.NotNull(requestBuilder);
        var responseEnum = model.FindChildByName<CodeEnum>("Status");
        Assert.NotNull(responseEnum);
        Assert.NotNull(responseEnum.CodeEnumObject);
        Assert.Contains(requestBuilder.Usings, x => x.Declaration?.TypeDefinition == responseEnum.CodeEnumObject && !x.IsErasable);
    }
}
