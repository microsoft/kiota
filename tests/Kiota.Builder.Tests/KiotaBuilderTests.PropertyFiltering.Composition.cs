using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("oneOf", false)]
    [InlineData("anyOf", false)]
    [InlineData("oneOf", true)]
    [InlineData("anyOf", true)]
    public async Task TrimmingLastUnionObjectPropertyKeepsBothResponseTypesAsync(string composition, bool nullableObject)
    {
        var description = $$"""
            openapi: 3.0.3
            info:
              title: Trimmed union
              version: 1.0.0
            paths:
              /result:
                get:
                  responses:
                    '200':
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Result'
            components:
              schemas:
                Result:
                  {{composition}}:
                    - type: object
                      properties:
                        metadata:
                          type: string
                    - type: string
            """;
        var document = await ReadPropertyFilterDocumentAsync(description);
        var resultSchema = document.Components!.Schemas!["Result"];
        var objectSchema = Assert.IsType<OpenApiSchema>((resultSchema.OneOf ?? resultSchema.AnyOf)![0]);
        if (nullableObject)
            objectSchema.Type = JsonSchemaType.Object | JsonSchemaType.Null;
        var configuration = new GenerationConfiguration { ExcludeProperties = ["Result#metadata"] };
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, configuration);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        builder.SetOpenApiDocument(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = Assert.IsType<CodeClass>(model.FindChildByName<CodeClass>("ResultRequestBuilder", true));
        var executor = Assert.Single(requestBuilder.Methods, static x => x.Kind == CodeMethodKind.RequestExecutor);
        var composed = Assert.IsAssignableFrom<CodeComposedTypeBase>(executor.ReturnType);
        Assert.Equal(2, composed.Types.Count());
        Assert.Contains(composed.Types, static x => x.IsExternal && x.Name == "string");
        var objectModel = Assert.IsType<CodeClass>(Assert.Single(composed.Types, static x => !x.IsExternal).TypeDefinition);
        Assert.DoesNotContain(objectModel.Properties, static x => x.Kind == CodePropertyKind.Custom);
        Assert.Empty(objectSchema.Properties!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TrimmingLastDerivedRootPropertyKeepsItsBaseTypeAsync(bool trimBase)
    {
        var document = await ReadPropertyFilterDocumentAsync("""
            openapi: 3.0.3
            info:
              title: Trimmed derived root
              version: 1.0.0
            paths:
              /derived:
                get:
                  responses:
                    '200':
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Derived'
            components:
              schemas:
                Base:
                  type: object
                  properties:
                    id:
                      type: string
                Derived:
                  allOf:
                    - $ref: '#/components/schemas/Base'
                  properties:
                    name:
                      type: string
            """);
        var configuration = new GenerationConfiguration
        {
            ExcludeProperties = trimBase ? ["Derived#name", "Base#id"] : ["Derived#name"],
        };
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, configuration);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        builder.SetOpenApiDocument(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var derived = Assert.IsType<CodeClass>(model.FindChildByName<CodeClass>("Derived", true));
        var parent = Assert.IsType<CodeClass>(derived.StartBlock.Inherits?.TypeDefinition);
        Assert.Equal("Base", parent.Name);
        Assert.Same(parent, model.FindChildByName<CodeClass>("Base", true));
        Assert.DoesNotContain(derived.Properties, static x => x.Kind == CodePropertyKind.Custom);
        if (trimBase)
            Assert.DoesNotContain(parent.Properties, static x => x.Kind == CodePropertyKind.Custom);
        else
            Assert.Contains(parent.Properties, static x => x.Kind == CodePropertyKind.Custom && x.Name == "id");
    }
}
