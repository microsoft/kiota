using System;
using System.Collections.Generic;
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
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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
        if (Directory.Exists(TempDirectory))
        {
            Directory.Delete(TempDirectory, true);
        }
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_WithOverlaysYamlInConfigWithRelativePath()
    {
        // Assert
        var yaml = """
        overlay: "1.0.0"
        info:
          title: "Test Overlay"
          version: "2.0.0"
        actions:
          - target: "$.info"
            update: 
                title: "Updated Title"
                description: "Updated Description"
        """;


        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        Directory.CreateDirectory(TempDirectory);
        var overlaysPath = Path.Combine(TempDirectory, Path.GetRandomFileName() + "overlays.yaml");
        await File.WriteAllTextAsync(overlaysPath, yaml).ConfigureAwait(false);

        var generationConfig = new GenerationConfiguration
        {
            Overlays = new HashSet<string>() {
                overlaysPath
            }
        };

        //Act
        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        // Assert
        Assert.NotNull(document);
        Assert.Equal("Updated Title", document.Info.Title);
        Assert.Equal("Updated Description", document.Info.Description);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_With2OverlaysYamlInConfigWithRelativePath()
    {
        // Assert
        var yaml = """
        overlay: "1.0.0"
        info:
          title: "Test Overlay"
          version: "2.0.0"
        actions:
          - target: "$.info"
            update: 
                title: "Updated Title"
        """;

        var yaml2 = """
        overlay: "1.0.0"
        info:
          title: "Test Overlay"
          version: "2.0.0"
        actions:
          - target: "$.info"
            update: 
                description: "Updated Description"
        """;


        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        Directory.CreateDirectory(TempDirectory);
        var overlaysPath = Path.Combine(TempDirectory, Path.GetRandomFileName() + "overlays.yaml");
        var overlaysPath2 = Path.Combine(TempDirectory, Path.GetRandomFileName() + "overlays.yaml");
        await File.WriteAllTextAsync(overlaysPath, yaml);
        await File.WriteAllTextAsync(overlaysPath2, yaml2);

        var generationConfig = new GenerationConfiguration
        {
            Overlays = new HashSet<string>() {
                overlaysPath,
                overlaysPath2
            }
        };

        //Act
        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        // Assert
        Assert.NotNull(document);
        Assert.Equal("Updated Title", document.Info.Title);
        Assert.Equal("Updated Description", document.Info.Description);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_WithOverlaysYamlInConfigAbsolutePath()
    {
        // Assert
        var yaml = """
        overlay: "1.0.0"
        info:
          title: "Test Overlay"
          version: "2.0.0"
        actions:
          - target: "$.info"
            update: 
                title: "Updated Title"
                description: "Updated Description"
        """;


        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();


        Directory.CreateDirectory(TempDirectory);
        var overlaysPath = Path.Combine(TempDirectory, Path.GetRandomFileName() + "overlays.yaml");
        await File.WriteAllTextAsync(overlaysPath, yaml);

        var generationConfig = new GenerationConfiguration
        {
            Overlays = new HashSet<string>() {
               overlaysPath
            }
        };

        //Act
        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);


        // Assert
        Assert.NotNull(document);
        Assert.Equal("Updated Title", document.Info.Title);
        Assert.Equal("Updated Description", document.Info.Description);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_WithInvalidUpdatePropertyInOverlays()
    {
        // Assert
        var json = """
        overlay: "1.0.0"
        info:
          title: "Test Overlay"
          version: "2.0.0"
        actions:
          - target: "$.info"
            update: 
                randomProperty: "Updated RandomProperty"
                description: "Updated Description"
        """;


        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();


        Directory.CreateDirectory(TempDirectory);
        var overlaysPath = Path.Combine(TempDirectory, Path.GetRandomFileName() + "overlays.yaml");
        await File.WriteAllTextAsync(overlaysPath, json);

        var generationConfig = new GenerationConfiguration
        {
            Overlays = new HashSet<string>() {
                overlaysPath
            }
        };

        //Act
        using var inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);


        // Assert
        Assert.NotNull(document);
        Assert.Equal("Updated Description", document.Info.Description);
        var diagError = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI error:"));
        Assert.Single(diagError);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_WithOverlaysJsonInConfig()
    {
        var json =
        """
        {
            "openapi": "3.1.0",
            "info": {
                "title": "Test Overlay",
                "version": "2.0.0",
                "description": "Description API"
            },
            "paths": {
                "/test": {
                    "get": {
                        "summary": "Test endpoint",
                        "responses": {
                            "200": {
                                "description": "OK"
                            }
                        }
                    }
                }
            }
        }
        """;

        // Assert
        var jsonOverlays = """
        {
            "overlay": "1.0.0",
            "info": {
                "title": "Test Overlay",
                "version": "2.0.0"
            },
            "extends": "x-extends",
            "actions": [
                {
                "target": "$.info",
                    "update": {
                     "title": "Updated Title YES"
                    }
                },
                {
                    "target": "$.info.description",
                    "remove": true
                }
            ],
         
        }
        """;

        var fakeLogger = new FakeLogger<OpenApiDocumentDownloadService>();

        Directory.CreateDirectory(TempDirectory);
        var overlaysPath = Path.Combine(TempDirectory, Path.GetRandomFileName() + "overlays.yaml");
        await File.WriteAllTextAsync(overlaysPath, jsonOverlays);

        var generationConfig = new GenerationConfiguration
        {
            Overlays = new HashSet<string>() {
                overlaysPath
            }
        };

        //Act
        using var inputDocumentStream = CreateMemoryStreamFromString(json);
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, fakeLogger);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        // Assert
        Assert.NotNull(document);
        Assert.Equal("Updated Title YES", document.Info.Title);
        Assert.Null(document.Info.Description);
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
