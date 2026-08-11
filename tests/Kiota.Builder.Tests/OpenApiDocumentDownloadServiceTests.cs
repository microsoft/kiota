using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using kiota.Rpc;
using Kiota.Builder.Configuration;
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
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

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
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        Assert.NotNull(document);
        //There should be no log entry for the no server rule
        var logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Empty(logEntryForNoServerRule);
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
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        Assert.NotNull(document);
        //There should be no log entry for the no server rule
        var logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Empty(logEntryForNoServerRule);
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
}
