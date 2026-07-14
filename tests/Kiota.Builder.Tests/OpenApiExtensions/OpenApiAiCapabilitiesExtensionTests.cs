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
                "body": "Do you want to proceed?",
                "isNonConsequential": true
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
        var staticTemplate = responseSemantics.StaticTemplate?.Template;
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
        Assert.True(confirmation.IsNonConsequential);

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
        await File.WriteAllTextAsync(documentPath, documentContent, cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<OpenApiAiCapabilitiesExtension>>();
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var generationConfig = new GenerationConfiguration { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        var (openApiDocumentStream, _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig, cancellationToken: TestContext.Current.CancellationToken);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken);

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
                StaticTemplate = new ExtensionResponseSemanticsStaticTemplate
                {
                    Template = new JsonObject
                    {
                        ["title"] = "Search for items",
                        ["body"] = "Here are the items I found for you."
                    }
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
                Body = "Do you want to proceed?",
                IsNonConsequential = true
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
        Assert.Contains("\"isNonConsequential\":true", result);
        Assert.Contains("\"security_info\":", result);
        Assert.Contains("data_handling", result);
        Assert.Contains("some data handling", result);

    }

    [Theory]
    // Safe relative references inside the manifest package.
    [InlineData("card.json", true)]
    [InlineData("./adaptiveCards/card.json", true)]
    [InlineData("adaptiveCards/card.json", true)]
    [InlineData("a/b/c.json", true)]
    // Empty / whitespace are not usable references.
    [InlineData("", false)]
    [InlineData("   ", false)]
    // Parent-directory traversal (CWE-22).
    [InlineData("../card.json", false)]
    [InlineData("../../../../etc/passwd", false)]
    [InlineData("..\\..\\windows\\system32\\config\\sam", false)]
    [InlineData("adaptiveCards/../../secret.json", false)]
    // Rooted POSIX / UNC paths.
    [InlineData("/etc/passwd", false)]
    [InlineData("//server/share/card.json", false)]
    // Windows drive-qualified paths.
    [InlineData("C:\\Windows\\System32\\drivers\\etc\\hosts", false)]
    [InlineData("C:card.json", false)]
    // Absolute URIs (CWE-829).
    [InlineData("http://attacker.example/exfil", false)]
    [InlineData("https://attacker.example/card.json", false)]
    [InlineData("file:///etc/passwd", false)]
    // Percent-encoded traversal / URIs must be decoded before validation (CWE-22 / CWE-829).
    [InlineData("%2e%2e/card.json", false)]
    [InlineData("..%2f..%2f..%2f..%2f..%2f..%2fetc%2fpasswd", false)]
    [InlineData("file%3A%2F%2F%2Fetc%2Fpasswd", false)]
    [InlineData("%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd", false)]
    // Encoding hardening variants.
    [InlineData("%2E%2E/card.json", false)]
    [InlineData("..%5c..%5csecret.json", false)]
    [InlineData("https%3A%2F%2Fattacker.example%2Fcard.json", false)]
    [InlineData("%252e%252e%252fcard.json", false)]
    // A benign filename containing an encoded space stays safe after decoding.
    [InlineData("card%20name.json", true)]
    // Encoded NUL / control characters must be rejected (truncation + segment-check evasion).
    [InlineData("card%00.json", false)]
    [InlineData("safe.json%00%2e%2e%2fetc%2fpasswd", false)]
    // Encoding deeper than the decode budget must fail closed rather than pass residual %XX through.
    [InlineData("%25252525252e%25252525252e%25252525252fx", false)]
    [InlineData("%2525252525252e%2525252525252e%2525252525252fx", false)]
    // Unicode full-width homoglyph traversal (literal and percent-encoded UTF-8) is folded and rejected.
    [InlineData("\uFF0E\uFF0E/card.json", false)]
    [InlineData("%EF%BC%8E%EF%BC%8E/card.json", false)]
    public void StaticTemplateIsSafeFileReferenceValidatesPaths(string file, bool expectedSafe)
    {
        Assert.Equal(expectedSafe, ExtensionResponseSemanticsStaticTemplate.IsSafeFileReference(file));
    }

    [Fact]
    public void StaticTemplateIsSafeFileReferenceRejectsNull()
    {
        Assert.False(ExtensionResponseSemanticsStaticTemplate.IsSafeFileReference(null));
    }

    [Fact]
    public void StaticTemplateExposesFileReferenceAndFlagsUnsafeOnes()
    {
        var unsafeTemplate = new ExtensionResponseSemanticsStaticTemplate
        {
            Template = new JsonObject { ["file"] = "../../../../etc/passwd" }
        };
        Assert.Equal("../../../../etc/passwd", unsafeTemplate.File);
        Assert.True(unsafeTemplate.HasUnsafeFileReference);

        var safeTemplate = new ExtensionResponseSemanticsStaticTemplate
        {
            Template = new JsonObject { ["file"] = "./adaptiveCards/card.json" }
        };
        Assert.Equal("./adaptiveCards/card.json", safeTemplate.File);
        Assert.False(safeTemplate.HasUnsafeFileReference);
    }

    [Fact]
    public void StaticTemplateTreatsInlineCardAsSafePassthrough()
    {
        // An inlined Adaptive Card has no "file" property; it must never be treated as an unsafe file reference.
        var inlineCard = new ExtensionResponseSemanticsStaticTemplate
        {
            Template = new JsonObject
            {
                ["type"] = "AdaptiveCard",
                ["body"] = new JsonArray()
            }
        };
        Assert.Null(inlineCard.File);
        Assert.False(inlineCard.HasUnsafeFileReference);
    }
}
