using System;
using System.Linq;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    public static TheoryData<string, string, bool, string, bool> NullableReferencesToNonModelComponents()
    {
        (string Schema, string TypeName, bool IsCollection)[] targets =
        [
            ("""{"type":"string","maxLength":1024}""", "string", false),
            ("""{"type":"string","format":"uuid"}""", "Guid", false),
            ("""{"type":"string","format":"date-time"}""", "DateTimeOffset", false),
            ("""{"type":"number","format":"double"}""", "double", false),
            ("""{"type":"integer"}""", "integer", false),
            ("""{"type":"integer","format":"int64"}""", "int64", false),
            ("""{"type":"boolean"}""", "boolean", false),
            ("""{"type":"integer","enum":[0,1,2]}""", "integer", false),
            ("""{"type":"integer","oneOf":[{"const":1,"title":"Actual"},{"const":2,"title":"Budget"}]}""", "integer", false),
            ("""{"type":"array","items":{"type":"string"}}""", "string", true),
            ("""{"type":"array","items":{"type":"integer","format":"int32"}}""", "integer", true),
            ("""{"allOf":[{"type":"string"}]}""", "string", false),
            ("""{"allOf":[{"type":"string","format":"date-time"}],"description":"wrapped"}""", "DateTimeOffset", false),
        ];
        var data = new TheoryData<string, string, bool, string, bool>();
        foreach (var (schema, typeName, isCollection) in targets)
            foreach (var keyword in new[] { "oneOf", "anyOf" })
                foreach (var nullFirst in new[] { false, true })
                    data.Add(schema, keyword, nullFirst, typeName, isCollection);
        return data;
    }

    [Theory]
    [MemberData(nameof(NullableReferencesToNonModelComponents))]
    public async Task MapsNullableReferenceToNonModelComponentLikeDirectReferenceAsync(string targetSchema, string keyword, bool nullFirst, string expectedTypeName, bool expectedCollection)
    {
        var codeModel = await CreateCodeModelForNullableReferencesAsync(ThingPath, """
            "Target":TARGET,
            "Thing":{"type":"object","properties":{
              "nullable":NULLABLE,
              "direct":{"$ref":"#/components/schemas/Target"}
            }}
            """.Replace("TARGET", targetSchema).Replace("NULLABLE", NullableReference(keyword, "Target", nullFirst)));
        var thing = codeModel.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        var nullable = Assert.IsType<CodeType>(thing.FindChildByName<CodeProperty>("nullable", false)?.Type);
        var direct = Assert.IsType<CodeType>(thing.FindChildByName<CodeProperty>("direct", false)?.Type);
        Assert.Equal(expectedTypeName, nullable.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedCollection, nullable.IsCollection);
        Assert.True(nullable.IsExternal);
        Assert.Null(nullable.TypeDefinition);
        Assert.Equal(direct.Name, nullable.Name);
        Assert.Equal(direct.CollectionKind, nullable.CollectionKind);
        // no empty model is declared for the component
        Assert.Null(codeModel.FindChildByName<CodeClass>("Target"));
    }

    [Theory]
    [InlineData("oneOf", false)]
    [InlineData("anyOf", true)]
    public async Task NullableReferenceToArrayOfPrimitiveComponentsMapsToPrimitiveCollectionAsync(string keyword, bool nullFirst)
    {
        var codeModel = await CreateCodeModelForNullableReferencesAsync(ThingPath, """
            "Name":{"type":"string","maxLength":1024},
            "Names":{"type":"array","items":{"$ref":"#/components/schemas/Name"}},
            "Thing":{"type":"object","properties":{
              "nullable":NULLABLE,
              "direct":{"$ref":"#/components/schemas/Names"}
            }}
            """.Replace("NULLABLE", NullableReference(keyword, "Names", nullFirst)));
        var thing = codeModel.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        var nullable = Assert.IsType<CodeType>(thing.FindChildByName<CodeProperty>("nullable", false)?.Type);
        var direct = Assert.IsType<CodeType>(thing.FindChildByName<CodeProperty>("direct", false)?.Type);
        Assert.Equal("string", nullable.Name, StringComparer.OrdinalIgnoreCase);
        Assert.True(nullable.IsCollection);
        Assert.Null(nullable.TypeDefinition);
        Assert.Equal(direct.Name, nullable.Name);
        Assert.Equal(direct.CollectionKind, nullable.CollectionKind);
        Assert.Null(codeModel.FindChildByName<CodeClass>("Name"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("Names"));
    }

    [Theory]
    [InlineData("""{"oneOf":[{"type":"string"},{"type":"integer"}]}""")]
    [InlineData("""{"anyOf":[{"type":"string","enum":["a","b"]},{"type":"string"}]}""")]
    public async Task NullableReferenceToComposedComponentWithPrimitiveMembersMapsLikeDirectReferenceAsync(string targetSchema)
    {
        var codeModel = await CreateCodeModelForNullableReferencesAsync(ThingPath, """
            "Target":TARGET,
            "Thing":{"type":"object","properties":{
              "nullable":NULLABLE,
              "direct":{"$ref":"#/components/schemas/Target"}
            }}
            """.Replace("TARGET", targetSchema).Replace("NULLABLE", NullableReference("oneOf", "Target", false)));
        var thing = codeModel.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        var nullable = Assert.IsType<CodeComposedTypeBase>(thing.FindChildByName<CodeProperty>("nullable", false)?.Type, exactMatch: false);
        var direct = Assert.IsType<CodeComposedTypeBase>(thing.FindChildByName<CodeProperty>("direct", false)?.Type, exactMatch: false);
        Assert.Equal(direct.GetType(), nullable.GetType());
        Assert.Equal(direct.Types.Select(static x => x.Name).Order(), nullable.Types.Select(static x => x.Name).Order());
        Assert.Null(codeModel.FindChildByName<CodeClass>("Target"));
    }

    [Fact]
    public async Task MapsNullableReferenceToPrimitiveComponentInRequestAndResponseBodiesAsync()
    {
        var paths = """
            "/count":{"post":{
              "requestBody":{"content":{"application/json":{"schema":REQUEST_BODY}}},
              "responses":{"200":{"description":"ok","content":{"application/json":{"schema":RESPONSE}}}}
            }}
            """.Replace("REQUEST_BODY", NullableReference("anyOf", "Count", true)).Replace("RESPONSE", NullableReference("oneOf", "Count", false));
        var codeModel = await CreateCodeModelForNullableReferencesAsync(paths, """
            "Count":{"type":"integer","format":"int32"}
            """);
        var requestBuilder = codeModel.FindChildByName<CodeClass>("CountRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        var returnType = Assert.IsType<CodeType>(executor.ReturnType);
        Assert.Equal("integer", returnType.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Null(returnType.TypeDefinition);
        var body = Assert.Single(executor.Parameters, static x => x.IsOfKind(CodeParameterKind.RequestBody));
        var bodyType = Assert.IsType<CodeType>(body.Type);
        Assert.Equal("integer", bodyType.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Null(bodyType.TypeDefinition);
        Assert.Null(codeModel.FindChildByName<CodeClass>("Count"));
    }

    [Theory]
    [InlineData("oneOf", false)]
    [InlineData("anyOf", true)]
    public async Task NullableReferenceToStringEnumComponentStaysAnEnumAsync(string keyword, bool nullFirst)
    {
        var codeModel = await CreateCodeModelForNullableReferencesAsync(ThingPath, """
            "Color":{"type":"string","enum":["red","blue"]},
            "Thing":{"type":"object","properties":{
              "color":NULLABLE
            }}
            """.Replace("NULLABLE", NullableReference(keyword, "Color", nullFirst)));
        var thing = codeModel.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        var color = Assert.IsType<CodeType>(thing.FindChildByName<CodeProperty>("color", false)?.Type);
        Assert.False(color.IsCollection);
        var colorEnum = Assert.IsType<CodeEnum>(color.TypeDefinition);
        Assert.Equal("Color", colorEnum.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("""{"oneOf":[{"$ref":"#/components/schemas/Cat"},{"$ref":"#/components/schemas/Dog"}]}""")]
    [InlineData("""{"oneOf":[{"$ref":"#/components/schemas/Cat"},{"$ref":"#/components/schemas/Dog"},{"type":"string"}]}""")]
    public async Task NullableReferenceToUnionComponentWithModelMembersKeepsTheComponentModelAsync(string targetSchema)
    {
        var codeModel = await CreateCodeModelForNullableReferencesAsync(ThingPath, """
            "Cat":{"type":"object","properties":{"meow":{"type":"string"}}},
            "Dog":{"type":"object","properties":{"bark":{"type":"string"}}},
            "Pet":TARGET,
            "Thing":{"type":"object","properties":{
              "pet":NULLABLE
            }}
            """.Replace("TARGET", targetSchema).Replace("NULLABLE", NullableReference("anyOf", "Pet", false)));
        var thing = codeModel.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        var pet = Assert.IsType<CodeType>(thing.FindChildByName<CodeProperty>("pet", false)?.Type);
        var petClass = Assert.IsType<CodeClass>(pet.TypeDefinition);
        Assert.Equal("Pet", petClass.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("""{"oneOf":[{"$ref":"#/components/schemas/Target"},{"type":"integer"},{"type":"null"}]}""")]
    [InlineData("""{"oneOf":[{"$ref":"#/components/schemas/Target"},{"$ref":"#/components/schemas/Other"}]}""")]
    [InlineData("""{"anyOf":[{"$ref":"#/components/schemas/Target"},{"type":"integer"}]}""")]
    public async Task UnionOfAReferenceAndAnotherNonNullMemberIsNotCollapsedAsync(string propertySchema)
    {
        var codeModel = await CreateCodeModelForNullableReferencesAsync(ThingPath, """
            "Target":{"type":"string"},
            "Other":{"type":"integer"},
            "Thing":{"type":"object","properties":{"value":VALUE}}
            """.Replace("VALUE", propertySchema));
        var thing = codeModel.FindChildByName<CodeClass>("Thing");
        Assert.NotNull(thing);
        var value = thing.FindChildByName<CodeProperty>("value", false);
        Assert.NotNull(value);
        Assert.IsType<CodeComposedTypeBase>(value.Type, exactMatch: false);
    }

    private const string ThingPath = """
        "/thing":{"get":{"responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Thing"}}}}}}}
        """;

    private static string NullableReference(string keyword, string componentName, bool nullFirst)
    {
        var entries = nullFirst ?
            """[{"type":"null"},{"$ref":"#/components/schemas/COMPONENT"}]""" :
            """[{"$ref":"#/components/schemas/COMPONENT"},{"type":"null"}]""";
        return """{"KEYWORD":ENTRIES}""".Replace("KEYWORD", keyword).Replace("ENTRIES", entries.Replace("COMPONENT", componentName));
    }

    private async Task<CodeNamespace> CreateCodeModelForNullableReferencesAsync(string paths, string schemas)
    {
        var description = """
            {
              "openapi":"3.1.0",
              "info":{"title":"Nullable references","version":"1.0"},
              "paths":{PATHS},
              "components":{"schemas":{SCHEMAS}}
            }
            """.Replace("PATHS", paths).Replace("SCHEMAS", schemas);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        return builder.CreateSourceModel(builder.CreateUriSpace(document));
    }
}
