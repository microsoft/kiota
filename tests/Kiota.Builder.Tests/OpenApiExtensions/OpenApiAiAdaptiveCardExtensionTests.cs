using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Writers;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiAiAdaptiveCardExtensionTest : IDisposable
{
    private readonly HttpClient _httpClient = new();
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    [Fact]
    public void Parses()
    {
        var oaiValueRepresentation =
        """
        {
            "data_path": "$.items",
            "file": "path_to_file"
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        var oaiValue = JsonNode.Parse(stream);
        var value = OpenApiAiAdaptiveCardExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.Equal("$.items", value.DataPath);
        Assert.Equal("path_to_file", value.File);
    }
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task ParsesInDocumentAsync()
    {
        var documentContent = @"openapi: 3.0.0
info:
  title: Graph Users
  version: 0.0.0
servers:
  - url: https://graph.microsoft.com/v1.0
    description: The Microsoft Graph API
tags: []
paths:
  /users:
    get:
      operationId: getUsers
      parameters: []
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/User'
      x-ai-adaptive-card:
        data_path: $.users
        file: path_to_file
  /users/{id}:
    get:
      operationId: getUser
      parameters:
        - name: id
          in: path
          required: true
          description: The user id
          schema:
            type: string
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/User'
      x-ai-adaptive-card:
        data_path: $.user
        file: path_to_file
components:
  schemas:
    User:
      type: object
      required:
        - id
        - displayName
      properties:
        id:
          type: string
        displayName:
          type: string";
        Directory.CreateDirectory(TempDirectory);
        var documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        var mockLogger = new Mock<ILogger<OpenApiAiAdaptiveCardExtension>>();
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var generationConfig = new GenerationConfiguration { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        var (openApiDocumentStream, _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotNull(document.Paths);
        Assert.NotNull(document.Paths["/users"].Operations.FirstOrDefault().Value.Extensions);
        Assert.True(document.Paths["/users"].Operations.FirstOrDefault().Value.Extensions.TryGetValue(OpenApiAiAdaptiveCardExtension.Name, out var adaptiveCardExtension));
        Assert.NotNull(adaptiveCardExtension);
    }

    [Fact]
    public void Serializes()
    {
        var value = new OpenApiAiAdaptiveCardExtension
        {
            DataPath = "$.items",
            File = "path_to_file"
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        Assert.Equal("{\"data_path\":\"$.items\",\"file\":\"path_to_file\"}", result);
    }
}
