using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
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
            "file": "path_to_file",
            "title": "title",
            "url": "https://example.com"
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        var oaiValue = JsonNode.Parse(stream);
        var value = OpenApiAiAdaptiveCardExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.Equal("$.items", value.DataPath);
        Assert.Equal("path_to_file", value.File);
        Assert.Equal("title", value.Title);
        Assert.Equal("https://example.com", value.Url);
        Assert.Null(value.Subtitle);
        Assert.Null(value.ThumbnailUrl);
        Assert.Null(value.InformationProtectionLabel);
    }

    [Fact]
    public void ParsesWithAllProperties()
    {
        var oaiValueRepresentation =
        """
        {
            "data_path": "$.items",
            "file": "path_to_file",
            "title": "title",
            "url": "https://example.com",
            "subtitle": "subtitle text",
            "thumbnail_url": "https://example.com/thumbnail.jpg",
            "information_protection_label": "confidential"
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        var oaiValue = JsonNode.Parse(stream);
        var value = OpenApiAiAdaptiveCardExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.Equal("$.items", value.DataPath);
        Assert.Equal("path_to_file", value.File);
        Assert.Equal("title", value.Title);
        Assert.Equal("https://example.com", value.Url);
        Assert.Equal("subtitle text", value.Subtitle);
        Assert.Equal("https://example.com/thumbnail.jpg", value.ThumbnailUrl);
        Assert.Equal("confidential", value.InformationProtectionLabel);
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
        title: title
        url: https://example.com
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
        title: title
        url: https://example.com
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
            File = "path_to_file",
            Title = "title",
            Url = "https://example.com"
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        Assert.Equal("{\"data_path\":\"$.items\",\"file\":\"path_to_file\",\"title\":\"title\",\"url\":\"https://example.com\"}", result);
    }

    [Fact]
    public void SerializesWithAllProperties()
    {
        var value = new OpenApiAiAdaptiveCardExtension
        {
            DataPath = "$.items",
            File = "path_to_file",
            Title = "title",
            Url = "https://example.com",
            Subtitle = "subtitle text",
            ThumbnailUrl = "https://example.com/thumbnail.jpg",
            InformationProtectionLabel = "confidential"
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });

        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();
        Assert.Equal("{\"data_path\":\"$.items\",\"file\":\"path_to_file\",\"title\":\"title\",\"url\":\"https://example.com\",\"subtitle\":\"subtitle text\",\"thumbnail_url\":\"https://example.com/thumbnail.jpg\",\"information_protection_label\":\"confidential\"}", result);
    }
}
