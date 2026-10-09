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
    [InlineData("anyOf", false, false)]
    [InlineData("oneOf", false, false)]
    [InlineData("anyOf", true, false)]
    [InlineData("oneOf", true, false)]
    [InlineData("anyOf", false, true)]
    [InlineData("oneOf", false, true)]
    [InlineData("anyOf", true, true)]
    [InlineData("oneOf", true, true)]
    [InlineData("anyOf", false, true, true)]
    [InlineData("oneOf", false, true, true)]
    [InlineData("anyOf", true, true, true)]
    [InlineData("oneOf", true, true, true)]
    public async Task ComposedArrayPreservesAllOfItemPropertiesAsync(string composition, bool nullable, bool inherited, bool inline = false)
    {
        var description = """
        {
          "openapi":"3.1.0", "info":{"title":"Composed arrays","version":"1.0"},
          "paths":{"/things":{"get":{"responses":{"200":{"description":"Success","content":{"application/json":{"schema":{"$ref":"#/components/schemas/TreeResponse"}}}}}}}},
          "components":{"schemas":{
            "TreeResponse":{"type":"object","properties":{"things":{"COMPOSITION":[
              {"type":"array","items":{"$ref":"#/components/schemas/Thing"}}, OTHER
            ]}}},
            "Base":{"type":"object","properties":{"id":{"type":"string"}}},
            "Thing":{"allOf":[BASE,{"type":"object","properties":{"name":{"type":"string"}}}]}
          }}
        }
        """.Replace("COMPOSITION", composition)
            .Replace("OTHER", nullable ? """{"type":"null"}""" : """{"type":"string"}""")
            .Replace("BASE", inherited ? """{"$ref":"#/components/schemas/Base"}""" : """{"type":"object","properties":{"id":{"type":"string"}}}""");
        if (inline)
            description = description.Replace("""{"type":"array","items":{"$ref":"#/components/schemas/Thing"}}""", """{"type":"array","items":{"allOf":[{"$ref":"#/components/schemas/Base"},{"type":"object","properties":{"name":{"type":"string"}}}]}}""");
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var tree = model.FindChildByName<CodeClass>("TreeResponse");
        Assert.NotNull(tree);
        var things = Assert.Single(tree.Properties, static p => p.Name == "things");
        var itemType = nullable ? Assert.IsType<CodeType>(things.Type) :
            Assert.Single(Assert.IsAssignableFrom<CodeComposedTypeBase>(things.Type).Types, static t => t.IsCollection);
        Assert.True(itemType.IsCollection);
        var item = Assert.IsType<CodeClass>(itemType.TypeDefinition);
        Assert.Contains(item.Properties, static p => p.Kind == CodePropertyKind.Custom && p.Name == "name");
        var baseClass = inherited ? Assert.IsType<CodeClass>(item.StartBlock.Inherits?.TypeDefinition) : item;
        Assert.Contains(baseClass.Properties, static p => p.Kind == CodePropertyKind.Custom && p.Name == "id");
    }

    [Theory]
    [InlineData("anyOf", false)]
    [InlineData("anyOf", true)]
    [InlineData("oneOf", false)]
    [InlineData("oneOf", true)]
    [InlineData("anyOf", false, "demo.Thing")]
    [InlineData("anyOf", true, "demo.Thing")]
    [InlineData("anyOf", false, "Thing", true, false)]
    [InlineData("anyOf", true, "Thing", true, false)]
    [InlineData("oneOf", false, "Thing", true, false)]
    [InlineData("oneOf", true, "Thing", true, false)]
    [InlineData("anyOf", false, "demo.Thing", true, true)]
    [InlineData("anyOf", true, "demo.Thing", true, true)]
    [InlineData("oneOf", false, "demo.Thing", true, true)]
    [InlineData("oneOf", true, "demo.Thing", true, true)]
    public async Task ComposedArrayPreservesReferencedModelPropertiesAsync(string composition, bool directFirst, string componentName = "Thing", bool nullable = false, bool nullFirst = false)
    {
        const string directOperation = """
        "post":{"responses":{"201":{"description":"Created","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Thing"}}}}}}
        """;
        const string composedOperation = """
        "get":{"responses":{"200":{"description":"Success","content":{"application/json":{"schema":{"$ref":"#/components/schemas/TreeResponse"}}}}}}
        """;
        var description = """
        {
          "openapi":"3.1.0", "info":{"title":"Composed arrays","version":"1.0"},
          "paths":{"/things":{OPERATIONS}},
          "components":{"schemas":{
            "TreeResponse":{"type":"object","properties":{"things":{"COMPOSITION":[
              MEMBERS
            ]}}},
            "Thing":{"type":"object","properties":{
              "id":{"type":"string","format":"uuid"},
              "name":{"type":"string"}, "count":{"type":"integer"}
            }}
          }}
        }
        """.Replace("COMPOSITION", composition).Replace("OPERATIONS", directFirst ?
            directOperation + "," + composedOperation : composedOperation + "," + directOperation)
            .Replace("MEMBERS", nullFirst ?
                """{"type":"null"},{"type":"array","items":{"$ref":"#/components/schemas/Thing"}}""" :
                """{"type":"array","items":{"$ref":"#/components/schemas/Thing"}},""" + (nullable ?
                    """{"type":"null"}""" : """{"type":"array","items":{"type":"object","additionalProperties":true}}"""))
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
        if (nullable)
        {
            var arrayType = Assert.IsType<CodeType>(things.Type);
            Assert.Same(thing, arrayType.TypeDefinition);
            Assert.True(arrayType.IsCollection);
            Assert.True(arrayType.IsNullable);
        }
        else
        {
            var composedType = Assert.IsAssignableFrom<CodeComposedTypeBase>(things.Type);
            Assert.Contains(composedType.Types, member => member.TypeDefinition == thing && member.IsCollection);
        }
    }
}
