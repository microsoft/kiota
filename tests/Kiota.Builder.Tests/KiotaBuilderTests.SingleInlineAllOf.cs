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
    [InlineData("anyOf", true)]
    [InlineData("anyOf", false)]
    [InlineData("oneOf", true)]
    [InlineData("oneOf", false)]
    public async Task SingleInlineAllOfPreservesPropertiesInUnionMembersAsync(string composition, bool referenced)
    {
        const string accountSchema = """
        {"type":"object","properties":{"label":{"type":"string"}},
          "allOf":[{"type":"object","properties":{
            "other":{"$ref":"#/components/schemas/Identification"}
          }}]}
        """;
        var description = """
        {
          "openapi":"3.0.3","info":{"title":"Union member allOf","version":"1.0"},
          "paths":{"/account":{"get":{"responses":{"200":{"description":"Success","content":{
            "application/json":{"schema":{"COMPOSITION":[ACCOUNT_MEMBER,{"$ref":"#/components/schemas/Company"}]}}
          }}}}}},
          "components":{"schemas":{
            "Account":ACCOUNT_SCHEMA,
            "Company":{"type":"object","properties":{"companyName":{"type":"string"}}},
            "Identification":{"type":"object","properties":{"identification":{"type":"string"}}}
          }}
        }
        """.Replace("COMPOSITION", composition)
            .Replace("ACCOUNT_MEMBER", referenced ? """{"$ref":"#/components/schemas/Account"}""" : accountSchema)
            .Replace("ACCOUNT_SCHEMA", accountSchema);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("AccountRequestBuilder");
        Assert.NotNull(requestBuilder);
        var method = Assert.Single(requestBuilder.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && !x.IsOverload);
        CodeComposedTypeBase composedType = composition == "anyOf"
            ? Assert.IsType<CodeIntersectionType>(method.ReturnType)
            : Assert.IsType<CodeUnionType>(method.ReturnType);
        Assert.Equal(2, composedType.Types.Count());
        Assert.Contains(composedType.Types, static x => x.Name == "Company");
        var account = Assert.IsType<CodeClass>(Assert.Single(composedType.Types, static x => x.Name != "Company").TypeDefinition);
        Assert.Contains(account.Properties, static x => x.Name == "label");
        var other = Assert.Single(account.Properties, static x => x.Name == "other");
        var identification = model.FindChildByName<CodeClass>("Identification");
        Assert.NotNull(identification);
        Assert.Same(identification, Assert.IsType<CodeType>(other.Type).TypeDefinition);
        Assert.Contains(identification.Properties, static x => x.Name == "identification");
    }

    [Theory]
    [InlineData("anyOf")]
    [InlineData("oneOf")]
    public async Task SingleInlineAllOfPreservesSiblingUnionAsync(string composition)
    {
        var description = """
        {
          "openapi":"3.0.3","info":{"title":"Mixed composition","version":"1.0"},
          "paths":{"/account":{"get":{"responses":{"200":{"description":"Success","content":{
            "application/json":{"schema":{"$ref":"#/components/schemas/Account"}}
          }}}}}},
          "components":{"schemas":{
            "Account":{
              "allOf":[{"type":"object","properties":{"label":{"type":"string"}}}],
              "COMPOSITION":[{"$ref":"#/components/schemas/Person"},{"$ref":"#/components/schemas/Company"}]
            },
            "Person":{"type":"object","properties":{"firstName":{"type":"string"}}},
            "Company":{"type":"object","properties":{"companyName":{"type":"string"}}}
          }}
        }
        """.Replace("COMPOSITION", composition);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("AccountRequestBuilder");
        Assert.NotNull(requestBuilder);
        var method = Assert.Single(requestBuilder.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && !x.IsOverload);
        CodeComposedTypeBase composedType = composition == "anyOf"
            ? Assert.IsType<CodeIntersectionType>(method.ReturnType)
            : Assert.IsType<CodeUnionType>(method.ReturnType);
        Assert.Equal(2, composedType.Types.Count());
        Assert.Contains(composedType.Types, static x => x.Name == "Person");
        Assert.Contains(composedType.Types, static x => x.Name == "Company");
    }

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
