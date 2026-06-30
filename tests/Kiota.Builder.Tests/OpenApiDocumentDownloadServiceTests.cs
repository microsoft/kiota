using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using kiota.Rpc;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;

public sealed class OpenApiDocumentDownloadServiceTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private const string DocumentContentWithNoServer = @"openapi: 3.0.0
info:
  title: Graph Users
  version: 0.0.0
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
                type: object";

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_IncludeKiotaValidationRulesInConfig()
    {
        var generationConfig = new GenerationConfiguration
        {
            PluginTypes = [PluginType.APIPlugin],
            IncludeKiotaValidationRules = true
        };
        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(document);
        //There should be a log entry for the no server rule
        var logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Single(logEntryForNoServerRule);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_No_IncludeKiotaValidationRulesInConfig()
    {
        var generationConfig = new GenerationConfiguration
        {
            PluginTypes = [PluginType.APIPlugin],
            IncludeKiotaValidationRules = false
        };
        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(document);
        //There should be no log entry for the no server rule
        var logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Empty(logEntryForNoServerRule);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsync_LogsSpecificationPathWhenParsingThrows()
    {
        const string brokenDocument = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Repro API",
    "version": "1.0.0"
  },
  "paths": {},
  "components": {
    "schemas": {
      "ItemStatus": {
        "type": "string",
        "enum": [
          "Active",
          "Archived"
        ],
        "x-ms-enum-flags": []
      }
    }
  }
}
""";

        var generationConfig = new GenerationConfiguration
        {
            OpenAPIFilePath = "repro-broken.json"
        };
        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        using var inputDocumentStream = CreateMemoryStreamFromString(brokenDocument);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken));

        var parsingLogEntry = fakeLogger.LogEntries
            .Where(l => l.message.Contains("Error parsing specification", StringComparison.OrdinalIgnoreCase));

        var logEntry = Assert.Single(parsingLogEntry);
        Assert.Contains("repro-broken.json", logEntry.message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_Default_IncludeKiotaValidationRulesInConfig()
    {
        var generationConfig = new GenerationConfiguration
        {
            PluginTypes = [PluginType.APIPlugin],
        };
        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(document);
        //There should be no log entry for the no server rule
        var logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Empty(logEntryForNoServerRule);
    }

    [Fact]
    public async Task DoesNotLoadExternalReferencesByDefault()
    {
        var generationConfig = new GenerationConfiguration
        {
            OpenAPIFilePath = "https://example.com/openapi.yaml",
        };
        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        using var inputDocumentStream = CreateMemoryStreamFromString("""
openapi: 3.0.0
info:
  title: External refs
  version: 0.0.0
paths: {}
components:
  schemas:
    Pet:
      $ref: 'https://contoso.com/schemas/pet.yaml#/components/schemas/Pet'
""");
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AllowedExternalOriginsLoadMatchingReferences()
    {
        var generationConfig = new GenerationConfiguration
        {
            OpenAPIFilePath = "https://example.com/openapi.yaml",
            AllowedExternalOrigins = ["https://contoso.com/schemas/*"],
        };
        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        using var inputDocumentStream = CreateMemoryStreamFromString("""
openapi: 3.0.0
info:
  title: External refs
  version: 0.0.0
paths: {}
components:
  schemas:
    Pet:
      $ref: 'https://contoso.com/schemas/pet.yaml#/components/schemas/Pet'
""");
        using var httpClient = new HttpClient(new ResponseHandler());
        var documentDownloadService = new OpenApiDocumentDownloadService(httpClient, fakeLogger);

        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(document);
    }

    [Fact]
    public async Task AllowedExternalOriginsStreamLoaderImplementsOpenApiLoader()
    {
        using var httpClient = new HttpClient(new ResponseHandler());
        var loader = (IStreamLoader)new AllowedExternalOriginsStreamLoader(httpClient, ["https://contoso.com/schemas/*"]);

        await using var stream = await loader.LoadAsync(
            new Uri("https://example.com/openapi.yaml"),
            new Uri("https://contoso.com/schemas/pet.yaml"),
            TestContext.Current.CancellationToken);
        Assert.NotNull(stream);
    }

    [Fact]
    public async Task AllowedExternalOriginsWildcardAllowsAnyExternalReference()
    {
        using var httpClient = new HttpClient(new ResponseHandler());
        var loader = (IStreamLoader)new AllowedExternalOriginsStreamLoader(httpClient, ["*"]);

        await using var stream = await loader.LoadAsync(
            new Uri("https://example.com/openapi.yaml"),
            new Uri("https://contoso.com/schemas/pet.yaml"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(stream);
    }

    [Fact]
    public async Task AllowedExternalOriginsStreamLoaderAllowsFullLocalPaths()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var schemaPath = Path.Combine(tempDirectory, "schemas", "pet.yaml");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(schemaPath)!);
            await File.WriteAllTextAsync(schemaPath, "type: object", TestContext.Current.CancellationToken);
            using var httpClient = new HttpClient(new ResponseHandler());
            var loader = (IStreamLoader)new AllowedExternalOriginsStreamLoader(httpClient, [schemaPath]);

            await using var stream = await loader.LoadAsync(
                new Uri(Path.Combine(tempDirectory, "openapi.yaml")),
                new Uri(schemaPath),
                TestContext.Current.CancellationToken);

            Assert.NotNull(stream);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task AllowedExternalOriginsStreamLoaderAllowsRelativePathPatterns()
    {
        var relativeDirectory = Path.GetRandomFileName();
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), relativeDirectory, "schemas", "pet.yaml");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(schemaPath)!);
            await File.WriteAllTextAsync(schemaPath, "type: object", TestContext.Current.CancellationToken);
            using var httpClient = new HttpClient(new ResponseHandler());
            var allowedOrigin = Path.Combine(relativeDirectory, "schemas", "*");
            var loader = (IStreamLoader)new AllowedExternalOriginsStreamLoader(httpClient, [allowedOrigin]);

            await using var stream = await loader.LoadAsync(
                new Uri(Path.Combine(Directory.GetCurrentDirectory(), "openapi.yaml")),
                new Uri($"{relativeDirectory}/schemas/pet.yaml", UriKind.Relative),
                TestContext.Current.CancellationToken);

            Assert.NotNull(stream);
        }
        finally
        {
            var tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), relativeDirectory);
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    private static Stream CreateMemoryStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private sealed class ResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
openapi: 3.0.0
info:
  title: External ref
  version: 0.0.0
paths: {}
components:
  schemas:
    Pet:
      type: object
"""),
            });
        }
    }
}
