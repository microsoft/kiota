using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("anyOf", false)]
    [InlineData("anyOf", true)]
    [InlineData("oneOf", false)]
    [InlineData("oneOf", true)]
    public async Task PreservesNullableArrayQueryParametersAsync(string keyword, bool nullFirst)
    {
        var array = """{"type":"array","items":{"type":"string"}}""";
        var nullSchema = """{"type":"null"}""";
        var members = nullFirst ? $"{nullSchema},{array}" : $"{array},{nullSchema}";
        var type = await GetQueryParameterTypeAsync($"{{\"{keyword}\":[{members}]}}");
        Assert.True(type.IsArray);
        Assert.Equal("string", type.Name);
    }

    [Theory]
    [InlineData("""{"type":"array","items":{"type":"string"}}""", true)]
    [InlineData("""{"anyOf":[{"oneOf":[{"type":"null"},{"type":"array","items":{"type":"string"}}]},{"type":"null"}]}""", true)]
    [InlineData("""{"anyOf":[{"type":"array","items":{"type":"string"}},{"type":"string"}]}""", false)]
    [InlineData("""{"type":"string"}""", false)]
    public async Task PreservesQueryArrayControlsAsync(string schema, bool isArray)
    {
        var type = await GetQueryParameterTypeAsync(schema);
        Assert.Equal(isArray, type.IsArray);
        Assert.Equal("string", type.Name);
    }

    [Fact]
    public async Task PreservesNullableEnumArrayQueryParameterAsync()
    {
        var type = await GetQueryParameterTypeAsync("""{"anyOf":[{"type":"array","items":{"type":"string","enum":["active","inactive"]}},{"type":"null"}]}""");
        Assert.True(type.IsArray);
        var definition = Assert.IsType<CodeEnum>(type.TypeDefinition);
        Assert.Equal(2, definition.Options.Count());
    }

    private async Task<CodeType> GetQueryParameterTypeAsync(string schema)
    {
        var description = """
        {"openapi":"3.1.0","info":{"title":"Nullable query","version":"1.0"},
        "paths":{"/test":{"get":{"parameters":[{"name":"ids","in":"query","schema":SCHEMA}],
        "responses":{"204":{"description":"Success"}}}}}}
        """.Replace("SCHEMA", schema);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp, ExcludeBackwardCompatible = true }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var parameters = model.FindChildByName<CodeClass>("TestRequestBuilderGetQueryParameters");
        Assert.NotNull(parameters);
        var property = Assert.Single(parameters.Properties, static x => x.Kind == CodePropertyKind.QueryParameter);
        return Assert.IsType<CodeType>(property.Type);
    }
}
