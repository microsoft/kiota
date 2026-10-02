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
    [InlineData("anyOf", false, "demo.Thing")]
    [InlineData("anyOf", true, "demo.Thing")]
    public async Task ComposedArrayPreservesReferencedModelPropertiesAsync(string composition, bool directFirst, string componentName = "Thing")
    {
        const string directOperation = """
        "post":{"responses":{"201":{"description":"Created","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Thing"}}}}}}
        """;
        const string composedOperation = """
        "get":{"responses":{"200":{"description":"Success","content":{"application/json":{"schema":{"$ref":"#/components/schemas/TreeResponse"}}}}}}
        """;
        var description = """
        {
          "openapi":"3.0.1", "info":{"title":"Composed arrays","version":"1.0"},
          "paths":{"/things":{OPERATIONS}},
          "components":{"schemas":{
            "TreeResponse":{"type":"object","properties":{"things":{"COMPOSITION":[
              {"type":"array","items":{"$ref":"#/components/schemas/Thing"}},
              {"type":"array","items":{"type":"object","additionalProperties":true}}
            ]}}},
            "Thing":{"type":"object","properties":{
              "id":{"type":"string","format":"uuid"},
              "name":{"type":"string"}, "count":{"type":"integer"}
            }}
          }}
        }
        """.Replace("COMPOSITION", composition).Replace("OPERATIONS", directFirst ?
            directOperation + "," + composedOperation : composedOperation + "," + directOperation)
            .Replace("Thing\"", componentName + "\"");
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var thing = model.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        Assert.Equal(new[] { "count", "id", "name" }, thing.Properties
            .Where(static p => p.Kind == CodePropertyKind.Custom)
            .Select(static p => p.Name).OrderBy(static name => name));
        var tree = model.FindChildByName<CodeClass>("TreeResponse");
        Assert.NotNull(tree);
        var things = Assert.Single(tree.Properties, static p => p.Name == "things");
        var composedType = Assert.IsAssignableFrom<CodeComposedTypeBase>(things.Type);
        Assert.Contains(composedType.Types, member => member.TypeDefinition == thing && member.IsCollection);
    }
}
