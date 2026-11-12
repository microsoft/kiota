using System;
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
    private async Task<OpenApiDocument> ReadUnionProtectionDocumentAsync(string composition)
    {
        return await ReadPropertyFilterDocumentAsync($$"""
            openapi: 3.0.3
            info:
              title: Branch-local required properties
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
                    - $ref: '#/components/schemas/A'
                    - $ref: '#/components/schemas/B'
                A:
                  allOf:
                    - type: object
                      required: [id]
                    - type: object
                      properties:
                        id:
                          type: string
                B:
                  type: object
                  properties:
                    id:
                      type: string
                    name:
                      type: string
            """);
    }

    [Theory]
    [InlineData("oneOf", false)]
    [InlineData("oneOf", true)]
    [InlineData("anyOf", false)]
    [InlineData("anyOf", true)]
    public async Task RequiredUnionBranchDoesNotProtectSiblingPropertiesAsync(string composition, bool allowlist)
    {
        var document = await ReadUnionProtectionDocumentAsync(composition);
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = allowlist ? ["B#name"] : [],
            ExcludeProperties = allowlist ? [] : ["B#id"],
        });
        Assert.DoesNotContain("id", document.Components!.Schemas!["B"].Properties!.Keys);
        Assert.Contains("name", document.Components.Schemas["B"].Properties!.Keys);
        Assert.Contains("id", document.Components.Schemas["A"].AllOf![1].Properties!.Keys);
    }

    [Theory]
    [InlineData("oneOf")]
    [InlineData("anyOf")]
    public async Task UnionBranchAllOfConstraintsRemainProtectedAsync(string composition)
    {
        var document = await ReadUnionProtectionDocumentAsync(composition);
        Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(document, new GenerationConfiguration { ExcludeProperties = ["A#id"] }));
        Assert.Contains("id", document.Components!.Schemas!["A"].AllOf![1].Properties!.Keys);
    }

    [Theory]
    [InlineData("oneOf", false)]
    [InlineData("oneOf", true)]
    [InlineData("anyOf", false)]
    [InlineData("anyOf", true)]
    public async Task UnionRootConstraintsStillProtectAllBranchesAsync(string composition, bool discriminator)
    {
        var document = await ReadUnionProtectionDocumentAsync(composition);
        var root = Assert.IsType<OpenApiSchema>(document.Components!.Schemas!["Result"]);
        if (discriminator)
            root.Discriminator = new OpenApiDiscriminator { PropertyName = "id" };
        else
            root.Required = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal) { "id" };
        Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(document, new GenerationConfiguration { ExcludeProperties = ["B#id"] }));
        Assert.Contains("id", document.Components.Schemas["B"].Properties!.Keys);
    }

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
