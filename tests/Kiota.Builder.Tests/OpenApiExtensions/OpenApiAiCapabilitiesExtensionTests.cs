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

public sealed class OpenApiAiCapabilitiesExtensionTest : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(TempDirectory))
        {
            Directory.Delete(TempDirectory, true);
        }
    }

    [Fact]
    public void Parses()
    {
        var oaiValueRepresentation =
        """
        {
            "response_semantics": {
                "data_path": "$.items",
                "static_template": {
                    "title": "Search for items",
                    "body": "Here are the items I found for you."
                },
                "properties": {
                    "title": "Some title",
                    "subtitle": "Some subtitle",
                    "url": "https://example.com",
                    "thumbnail_url": "https://example.com/thumbnail.jpg",
                    "information_protection_label": "confidential"
                },
                "oauth_card_path": "oauthCard.json"
            },
            "confirmation": {
                "type": "modal",
                "title": "Confirm action",
                "body": "Do you want to proceed?"
            },
            "security_info": {
                "data_handling": ["some data handling"]
            }
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        var oaiValue = JsonNode.Parse(stream);
        var value = OpenApiAiCapabilitiesExtension.Parse(oaiValue);

        Assert.NotNull(value);

        var responseSemantics = value.ResponseSemantics;
        var confirmation = value.Confirmation;
        var securityInfo = value.SecurityInfo;
        Assert.NotNull(responseSemantics);
        Assert.NotNull(confirmation);
        Assert.NotNull(securityInfo);

        Assert.Equal("$.items", responseSemantics.DataPath);
        Assert.Equal("oauthCard.json", responseSemantics.OauthCardPath);
        var staticTemplate = responseSemantics.StaticTemplate as JsonObject;
        Assert.NotNull(staticTemplate);
        Assert.Equal("Search for items", staticTemplate["title"]?.ToString());
        Assert.Equal("Here are the items I found for you.", staticTemplate["body"]?.ToString());

        var properties = responseSemantics.Properties;
        Assert.NotNull(properties);
        Assert.Equal("Some title", properties.Title);
        Assert.Equal("Some subtitle", properties.Subtitle);
        Assert.Equal("https://example.com", properties.Url);
        Assert.Equal("https://example.com/thumbnail.jpg", properties.ThumbnailUrl);
        Assert.Equal("confidential", properties.InformationProtectionLabel);

        Assert.Equal("modal", confirmation.Type);
        Assert.Equal("Confirm action", confirmation.Title);
        Assert.Equal("Do you want to proceed?", confirmation.Body);

        Assert.Equal("some data handling", securityInfo.DataHandling[0]);
    }

    [Fact]
    public async Task ParsesInDocumentAsync()
    {
        var documentContent = @"openapi: 3.0.0
info:
  title: Test API
  version: 0.0.0
servers:
  - url: https://api.example.com/v1
    description: Example API
paths:
  /items:
    get:
      operationId: getItems
      parameters: []
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Item'
      x-ai-capabilities:
        response_semantics:
          data_path: $.items
          static_template:
            title: Search for items
            body: Here are the items I found for you.
          properties:
            title: Some title
            subtitle: Some subtitle
            url: https://example.com
            thumbnail_url: https://example.com/thumbnail.jpg
            information_protection_label: confidential
          oauth_card_path: oauthCard.json
        confirmation:
          type: modal
          title: Confirm action
          body: Do you want to proceed?
        security_info:
          data_handling:
            - some data handling
components:
  schemas:
    Item:
      type: object
      properties:
        id:
          type: string
        name:
          type: string";

        Directory.CreateDirectory(TempDirectory);
        var documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        var mockLogger = new Mock<ILogger<OpenApiAiCapabilitiesExtension>>();
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var generationConfig = new GenerationConfiguration { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        var (openApiDocumentStream, _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);

        Assert.NotNull(document);
        Assert.NotNull(document.Paths);
        Assert.NotNull(document.Paths["/items"].Operations.FirstOrDefault().Value.Extensions);
        Assert.True(document.Paths["/items"].Operations.FirstOrDefault().Value.Extensions.TryGetValue(OpenApiAiCapabilitiesExtension.Name, out var capabilitiesExtension));
        Assert.NotNull(capabilitiesExtension);
    }

    [Fact]
    public void Serializes()
    {
        var value = new OpenApiAiCapabilitiesExtension
        {
            ResponseSemantics = new ExtensionResponseSemantics
            {
                DataPath = "$.items",
                StaticTemplate = new JsonObject
                {
                    ["title"] = "Search for items",
                    ["body"] = "Here are the items I found for you."
                },
                Properties = new ExtensionResponseSemanticsProperties
                {
                    Title = "Some title",
                    Subtitle = "Some subtitle",
                    Url = "https://example.com",
                    ThumbnailUrl = "https://example.com/thumbnail.jpg",
                    InformationProtectionLabel = "confidential"
                },
                OauthCardPath = "oauthCard.json"
            },
            Confirmation = new ExtensionConfirmation
            {
                Type = "modal",
                Title = "Confirm action",
                Body = "Do you want to proceed?"
            },
            SecurityInfo = new ExtensionSecurityInfo
            {
                DataHandling = ["some data handling"]
            }
        };
        using var sWriter = new StringWriter();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        var result = sWriter.ToString();

        Assert.Contains("\"response_semantics\":", result);
        Assert.Contains("data_path", result);
        Assert.Contains("$.items", result);
        Assert.Contains("static_template", result);
        Assert.Contains("title", result);
        Assert.Contains("Search for items", result);
        Assert.Contains("body", result);
        Assert.Contains("Here are the items I found for you", result);
        Assert.Contains("properties", result);
        Assert.Contains("title", result);
        Assert.Contains("Some title", result);
        Assert.Contains("subtitle", result);
        Assert.Contains("Some subtitle", result);
        Assert.Contains("url", result);
        Assert.Contains("https://example.com", result);
        Assert.Contains("thumbnail_url", result);
        Assert.Contains("https://example.com/thumbnail.jpg", result);
        Assert.Contains("information_protection_label", result);
        Assert.Contains("confidential", result);
        Assert.Contains("\"oauth_card_path", result);
        Assert.Contains("oauthCard.json", result);
        Assert.Contains("\"confirmation\":", result);
        Assert.Contains("type", result);
        Assert.Contains("modal", result);
        Assert.Contains("title", result);
        Assert.Contains("Confirm action", result);
        Assert.Contains("body", result);
        Assert.Contains("Do you want to proceed?", result);
        Assert.Contains("\"security_info\":", result);
        Assert.Contains("data_handling", result);
        Assert.Contains("some data handling", result);

    }
}
