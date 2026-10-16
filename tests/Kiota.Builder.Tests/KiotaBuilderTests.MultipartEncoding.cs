using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("{}", true)]
    [InlineData("""{"template":{"contentType":"text/html"}}""", true)]
    [InlineData("""{"info":{}}""", true)]
    [InlineData("""{"info":{"contentType":"application/json"}}""", true)]
    [InlineData("""{"info":{"contentType":"application/x-custom"}}""", false)]
    public async Task PreservesMultipartModelsWithPartialEncodingAsync(string encoding, bool expectedModel)
    {
        var description = """
        {"openapi":"3.0.3","info":{"title":"Multipart encoding","version":"1.0"},
        "paths":{"/users":{"post":{"requestBody":{"content":{"multipart/form-data":{
        "schema":{"type":"object","properties":{"info":{"$ref":"#/components/schemas/UserInfo"},
        "template":{"type":"string","format":"binary"}}},"encoding":ENCODING}}},
        "responses":{"201":{"description":"Created"}}}}},
        "components":{"schemas":{"UserInfo":{"type":"object","properties":{"firstName":{"type":"string"}}}}}}
        """.Replace("ENCODING", encoding);
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("UsersRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, x => x.Kind == CodeMethodKind.RequestExecutor);
        var body = Assert.Single(executor.Parameters, x => x.Kind == CodeParameterKind.RequestBody);
        Assert.Equal("MultipartBody", body.Type.Name);
        var info = model.FindChildByName<CodeClass>("UserInfo");
        if (expectedModel)
        {
            Assert.NotNull(info);
            Assert.Contains(info.Properties, x => x.Name == "firstName");
        }
        else
            Assert.Null(info);
    }

    [Theory]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Python, false)]
    public async Task GeneratesTypedRequestBodyForFlatMultipartFileCollectionsAsync(GenerationLanguage language, bool expectedTypedBody)
    {
        const string description = """
        {
          "openapi": "3.0.4",
          "info": { "title": "Multipart API", "version": "1.0.0" },
          "paths": {
            "/forms/form-file-multiple": {
              "post": {
                "operationId": "uploadFiles",
                "requestBody": {
                  "required": true,
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "required": ["resume", "files"],
                        "properties": {
                          "resume": { "type": "string", "format": "binary" },
                          "files": {
                            "type": "array",
                            "items": { "type": "string", "format": "binary" }
                          }
                        }
                      }
                    }
                  }
                },
                "responses": { "204": { "description": "No Content" } }
              }
            }
          }
        }
        """;

        // Arrange
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { Language = language }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));

        // Assert
        var requestBuilder = model.FindChildByName<CodeClass>("FormFileMultipleRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, x => x.Kind == CodeMethodKind.RequestExecutor);
        var body = Assert.Single(executor.Parameters, x => x.Kind == CodeParameterKind.RequestBody);
        if (expectedTypedBody)
        {
            Assert.NotEqual("MultipartBody", body.Type.Name);
            var bodyType = Assert.IsType<CodeType>(body.Type);
            var bodyClass = Assert.IsType<CodeClass>(bodyType.TypeDefinition);
            Assert.Contains(bodyClass.Properties, x => x.Name == "resume");
            Assert.Contains(bodyClass.Properties, x => x.Name == "files");
            Assert.Contains(bodyClass.Properties, x => x.Kind == CodePropertyKind.AdditionalData);
        }
        else
            Assert.Equal("MultipartBody", body.Type.Name);
    }

    [Fact]
    public async Task KeepsMultipartBodyFallbackForFlatFileCollectionsWithNonBinaryFieldsAsync()
    {
        const string description = """
        {
          "openapi": "3.0.4",
          "info": { "title": "Multipart API", "version": "1.0.0" },
          "paths": {
            "/forms/form-file-multiple": {
              "post": {
                "requestBody": {
                  "required": true,
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "files": {
                            "type": "array",
                            "items": { "type": "string", "format": "binary" }
                          },
                          "description": { "type": "string" }
                        }
                      }
                    }
                  }
                },
                "responses": { "204": { "description": "No Content" } }
              }
            }
          }
        }
        """;

        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));

        var requestBuilder = model.FindChildByName<CodeClass>("FormFileMultipleRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, x => x.Kind == CodeMethodKind.RequestExecutor);
        var body = Assert.Single(executor.Parameters, x => x.Kind == CodeParameterKind.RequestBody);
        Assert.Equal("MultipartBody", body.Type.Name);
    }
    [Fact]
    public async Task KeepsMultipartBodyFallbackForComposedFileCollectionsAsync()
    {
        const string description = """
        {
          "openapi": "3.0.4",
          "info": { "title": "Multipart API", "version": "1.0.0" },
          "paths": {
            "/forms": {
              "post": {
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "files": { "type": "array", "items": { "type": "string", "format": "binary" } }
                        },
                        "allOf": [{ "$ref": "#/components/schemas/Details" }]
                      }
                    }
                  }
                },
                "responses": { "204": { "description": "No Content" } }
              }
            }
          },
          "components": {
            "schemas": {
              "Details": { "type": "object", "properties": { "description": { "type": "string" } } }
            }
          }
        }
        """;

        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("FormsRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, x => x.Kind == CodeMethodKind.RequestExecutor);
        var body = Assert.Single(executor.Parameters, x => x.Kind == CodeParameterKind.RequestBody);
        Assert.Equal("MultipartBody", body.Type.Name);
    }
    [Fact]
    public async Task KeepsMultipartBodyFallbackForAdditionalPropertiesAsync()
    {
        const string description = """
        {
          "openapi": "3.0.4",
          "info": { "title": "Multipart API", "version": "1.0.0" },
          "paths": {
            "/forms": {
              "post": {
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "files": { "type": "array", "items": { "type": "string", "format": "binary" } }
                        },
                        "additionalProperties": { "type": "string" }
                      }
                    }
                  }
                },
                "responses": { "204": { "description": "No Content" } }
              }
            }
          }
        }
        """;

        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance,
            new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var requestBuilder = model.FindChildByName<CodeClass>("FormsRequestBuilder");
        Assert.NotNull(requestBuilder);
        var executor = Assert.Single(requestBuilder.Methods, x => x.Kind == CodeMethodKind.RequestExecutor);
        var body = Assert.Single(executor.Parameters, x => x.Kind == CodeParameterKind.RequestBody);
        Assert.Equal("MultipartBody", body.Type.Name);
    }
}
