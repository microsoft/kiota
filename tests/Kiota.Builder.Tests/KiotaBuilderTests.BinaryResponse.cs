using System.IO;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("3.0.3", "*/*", false)]
    [InlineData("3.1.0", "*/*", false)]
    [InlineData("3.0.3", "application/octet-stream", false)]
    [InlineData("3.1.0", "application/octet-stream", false)]
    [InlineData("3.0.3", "*/*", true)]
    [InlineData("3.1.0", "*/*", true)]
    [InlineData("3.0.3", "application/octet-stream", true)]
    [InlineData("3.1.0", "application/octet-stream", true)]
    public async Task GeneratesTypeScriptBinaryResponseAsync(string openApiVersion, string mediaType, bool includeOtherResponses)
    {
        // The 200-only wildcard case is the reproduction from #8466.
        var otherResponses = includeOtherResponses ? $$$$"""
            ,"203": {
              "description": "Raw file from a proxy",
              "content": {
                "{{{{mediaType}}}}": {"schema": {"type": "string", "format": "binary"}}
              }
            },
            "400": {
              "description": "Error",
              "content": {
                "application/json": {
                  "schema": {"type": "object", "properties": {"message": {"type": "string"}}}
                },
                "text/plain": {"schema": {"type": "string"}}
              }
            }
            """ : string.Empty;
        var description = $$$$"""
            {
              "openapi": "{{{{openApiVersion}}}}",
              "info": {"title": "Binary file with dynamic media type", "version": "1"},
              "servers": [{"url": "https://example.invalid"}],
              "paths": {
                "/file": {
                  "get": {
                    "operationId": "downloadFile",
                    "responses": {
                      "200": {
                        "description": "Raw file",
                        "content": {
                          "{{{{mediaType}}}}": {"schema": {"type": "string", "format": "binary"}}
                        }
                      }
                      {{{{otherResponses}}}}
                    }
                  }
                }
              }
            }
            """;
        var outputDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var configuration = new GenerationConfiguration
            {
                Language = GenerationLanguage.TypeScript,
                ClientClassName = "FileClient",
                OutputPath = outputDirectory.FullName,
            };
            var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
            await using var stream = await GetDocumentStreamAsync(description);
            var result = await builder.CreateOpenApiDocumentWithResultAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.NotNull(result.Document);
            Assert.Empty(result.Diagnostic.Errors);
            var model = builder.CreateSourceModel(builder.CreateUriSpace(result.Document));
            var requestBuilder = model.FindChildByName<CodeClass>("FileRequestBuilder");
            Assert.NotNull(requestBuilder);
            var executor = Assert.Single(requestBuilder.Methods, static method => method.Kind == CodeMethodKind.RequestExecutor);
            var returnType = Assert.IsType<CodeType>(executor.ReturnType);
            Assert.Equal("binary", returnType.Name);
            Assert.True(returnType.IsExternal);
            if (includeOtherResponses)
            {
                var errorMapping = Assert.Single(executor.ErrorMappings);
                Assert.Equal("400", errorMapping.Key);
                var errorType = Assert.IsType<CodeType>(errorMapping.Value);
                Assert.True(Assert.IsType<CodeClass>(errorType.TypeDefinition).IsErrorDefinition);
            }
            else
                Assert.Empty(executor.ErrorMappings);

            await builder.ApplyLanguageRefinementAsync(configuration, model, TestContext.Current.CancellationToken);
            await builder.CreateLanguageSourceFilesAsync(configuration.Language, model, TestContext.Current.CancellationToken);
            var source = await File.ReadAllTextAsync(Path.Combine(outputDirectory.FullName, "file", "index.ts"), TestContext.Current.CancellationToken);
            Assert.Matches(@"get\([^\r\n]*\)\s*:\s*Promise<ArrayBuffer \| undefined>;", source);
            Assert.Contains("adapterMethodName: \"sendPrimitive\"", source);
            Assert.Matches("responseBodyFactory:\\s*\"ArrayBuffer\"", source);
            if (includeOtherResponses)
            {
                // Structured error media types are also advertised in the Accept header.
                Assert.Contains($"responseBodyContentType: \"{mediaType}, ", source);
                Assert.Contains("errorMappings: {", source);
            }
            else
                Assert.Contains($"responseBodyContentType: \"{mediaType}\"", source);
            Assert.DoesNotContain("sendNoResponseContent", source);
        }
        finally
        {
            outputDirectory.Delete(true);
        }
    }
}
