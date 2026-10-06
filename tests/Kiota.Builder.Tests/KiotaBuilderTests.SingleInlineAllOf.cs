using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true, true)]
    [InlineData(true, true, false, "anyOf")]
    [InlineData(true, false, false, "anyOf")]
    [InlineData(true, true, true, "anyOf")]
    [InlineData(true, true, false, "oneOf")]
    [InlineData(true, false, false, "oneOf")]
    [InlineData(true, true, true, "oneOf")]
    public async Task SingleInlineAllOfPreservesReferencedPropertiesAsync(bool wrapped, bool explicitType, bool siblingProperty = false, string nullableComposition = null)
    {
        const string inlineSchema = """
        {"type":"object","required":["other"],"properties":{
          "other":{"$ref":"#/components/schemas/Identification"}
        }}
        """;
        var accountSchema = wrapped ? "{" + (explicitType ? "\"type\":\"object\"," : "") +
            (siblingProperty ? "\"properties\":{\"label\":{\"type\":\"string\"}}," : "") +
            "\"allOf\":[" + inlineSchema + "]}" : inlineSchema;
        const string accountReference = """{"$ref":"#/components/schemas/Account"}""";
        var responseSchema = nullableComposition is null ? accountReference :
            "{\"" + nullableComposition + "\":[" + accountReference + ",{\"type\":\"null\"}]}";
        var description = """
        {
          "openapi":"OPENAPI_VERSION","info":{"title":"Single inline allOf","version":"1.0"},
          "paths":{"/account":{"get":{"responses":{"200":{"description":"Success","content":{
            "application/json":{"schema":RESPONSE_SCHEMA}
          }}}}}},
          "components":{"schemas":{
            "Account":ACCOUNT_SCHEMA,
            "Identification":{"type":"object","properties":{"identification":{"type":"string"}}}
          }}
        }
        """.Replace("ACCOUNT_SCHEMA", accountSchema)
            .Replace("RESPONSE_SCHEMA", responseSchema)
            .Replace("OPENAPI_VERSION", nullableComposition is null ? "3.0.3" : "3.1.0");
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var account = model.FindChildByName<CodeClass>("Account");
        Assert.NotNull(account);
        var other = Assert.Single(account.Properties, static p => p.Name == "other");
        Assert.Equal("other", other.Name);
        if (siblingProperty)
            Assert.Contains(account.Properties, static p => p.Name == "label");
        var identification = model.FindChildByName<CodeClass>("Identification");
        Assert.NotNull(identification);
        Assert.Same(identification, Assert.IsType<CodeType>(other.Type).TypeDefinition);
        Assert.Contains(identification.Properties, static p => p.Name == "identification");
    }
}
