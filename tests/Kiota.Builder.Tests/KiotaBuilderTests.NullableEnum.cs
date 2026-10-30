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
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExcludesJsonNullSentinelFromNullableEnumOptionsAsync(bool useYaml)
    {
        var description = useYaml ?
        """
        openapi: 3.1.0
        info: {title: Nullable enum, version: 1.0.0}
        paths:
          /ctx:
            get:
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema: {$ref: '#/components/schemas/Ctx'}
        components:
          schemas:
            Ctx:
              type: object
              properties:
                renderTemplate:
                  type: [string, 'null']
                  enum: [standard, compact, null]
        """ :
        """
        {
          "openapi": "3.1.0",
          "info": {"title": "Nullable enum", "version": "1.0.0"},
          "paths": {
            "/ctx": {
              "get": {
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": {
                      "application/json": {
                        "schema": {"$ref": "#/components/schemas/Ctx"}
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Ctx": {
                "type": "object",
                "properties": {
                  "renderTemplate": {
                    "type": ["string", "null"],
                    "enum": ["standard", "compact", null]
                  }
                }
              }
            }
          }
        }
        """;

        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration
            {
                ClientClassName = "ReproClient",
                ClientNamespaceName = "Repro",
            }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);

        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var modelsNamespace = model.FindNamespaceByName("Repro.models");
        Assert.NotNull(modelsNamespace);
        var enumType = Assert.Single(modelsNamespace.Enums);
        Assert.Equal("Ctx_renderTemplate", enumType.Name);
        Assert.Equal(new[] { "standard", "compact" }, enumType.Options.Select(static x => x.SerializationName));

        var contextType = Assert.Single(modelsNamespace.Classes, static x => x.Name == "Ctx");
        var property = Assert.Single(contextType.Properties, static x => x.Name == "renderTemplate");
        Assert.True(property.Type.IsNullable);
        Assert.Same(enumType, Assert.IsType<CodeType>(property.Type).TypeDefinition);
    }
}
