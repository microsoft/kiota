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
    [InlineData("""{"$ref":"#/components/schemas/WidgetType"}""", false)]
    [InlineData("""{"description":"Widget type","allOf":[{"$ref":"#/components/schemas/WidgetType"}]}""", false)]
    [InlineData("""{"allOf":[{"allOf":[{"$ref":"#/components/schemas/WidgetType"}]}]}""", false)]
    [InlineData("""{"type":"array","items":{"allOf":[{"$ref":"#/components/schemas/WidgetType"}]}}""", true)]
    [InlineData("""{"allOf":[{"type":"array","items":{"$ref":"#/components/schemas/WidgetType"}}]}""", true)]
    [InlineData("""{"type":"array","items":{"allOf":[{"allOf":[{"$ref":"#/components/schemas/WidgetType"}]}]}}""", true)]
    [InlineData("""{"allOf":[{"type":"array","items":{"allOf":[{"allOf":[{"$ref":"#/components/schemas/WidgetType"}]}]}}]}""", true)]
    public async Task ResolvesSingleAllOfQueryEnumsAsync(string parameterSchema, bool isArray)
    {
        var description = """
        {
          "openapi":"3.0.1",
          "info":{"title":"Query enums","version":"1.0"},
          "paths":{"/widgets":{"get":{
            "parameters":[{"name":"widgetType","in":"query","schema":PARAMETER_SCHEMA}],
            "responses":{"204":{"description":"Success"}}
          }}},
          "components":{"schemas":{"WidgetType":{"type":"string","enum":["Basic","Advanced"]}}}
        }
        """.Replace("PARAMETER_SCHEMA", parameterSchema);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration
            {
                Language = GenerationLanguage.Python,
                ExcludeBackwardCompatible = true,
            }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("WidgetsRequestBuilder");
        Assert.NotNull(requestBuilder);
        var parameters = requestBuilder.FindChildByName<CodeClass>("WidgetsRequestBuilderGetQueryParameters");
        Assert.NotNull(parameters);
        var property = Assert.Single(parameters.Properties, static x => x.Kind == CodePropertyKind.QueryParameter);
        var type = Assert.IsType<CodeType>(property.Type);
        var enumType = Assert.IsType<CodeEnum>(type.TypeDefinition);
        Assert.Equal("WidgetType", enumType.Name);
        Assert.Equal(new[] { "Basic", "Advanced" }, enumType.Options.Select(static x => x.Name));
        Assert.Equal(isArray, type.IsArray);
        Assert.Equal(!isArray, type.IsNullable);
    }
}
