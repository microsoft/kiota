using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("{}", true)]
    [InlineData("""{"template":{"contentType":"text/html"}}""", true)]
    [InlineData("""{"info":{}}""", true)]
    [InlineData("""{"info":{"contentType":"application/json"}}""", true)]
    [InlineData("""{"info":{"contentType":"application/x-custom"}}""", false)]
    public async Task PreservesMultipartModelsWithPartialEncodingAsync(string encoding, bool expectedModel)
    {
        var description = """
        {"openapi":"3.0.3","info":{"title":"Multipart encoding","version":"1.0"},
        "paths":{"/users":{"post":{"requestBody":{"content":{"multipart/form-data":{
        "schema":{"type":"object","properties":{"info":{"$ref":"#/components/schemas/UserInfo"},
        "template":{"type":"string","format":"binary"}}},"encoding":ENCODING}}},
        "responses":{"201":{"description":"Created"}}}}},
        "components":{"schemas":{"UserInfo":{"type":"object","properties":{"firstName":{"type":"string"}}}}}}
        """.Replace("ENCODING", encoding);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("UsersRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, x => x.Kind == CodeMethodKind.RequestExecutor);
        var body = Assert.Single(executor.Parameters, x => x.Kind == CodeParameterKind.RequestBody);
        Assert.Equal("MultipartBody", body.Type.Name);
        var info = model.FindChildByName<CodeClass>("UserInfo");
        if (expectedModel)
        {
            Assert.NotNull(info);
            Assert.Contains(info.Properties, x => x.Name == "firstName");
        }
        else
            Assert.Null(info);
    }
}
