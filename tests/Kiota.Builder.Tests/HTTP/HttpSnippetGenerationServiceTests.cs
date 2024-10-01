using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Kiota.Builder.Tests.OpenApiSampleFiles;
using System.Threading;

namespace Kiota.Builder.Tests.http;

public sealed class HttpSnippetGenerationServiceTests : IDisposable
{
    private readonly HttpClient _httpClient = new();

    public void Dispose()
    {
        _httpClient.Dispose();
    }


    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new HttpSnippetGenerationService(null, new()));
        Assert.Throws<ArgumentNullException>(() => new HttpSnippetGenerationService(new(), null));
    }

    [Fact]
    public async Task GeneratesHttpSnippetAsync()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, Posts.OpenApiYaml);
        var mockLogger = new Mock<ILogger<HttpSnippetGenerationService>>();
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);

        var httpSnippetGenerationService = new HttpSnippetGenerationService(openApiDocument, generationConfiguration);
        await httpSnippetGenerationService.GenerateHttpSnippetAsync();

        var fileNames = openApiDocument.Paths
                .Where(x => x.Value.Operations.Any())
                .Select(x => x.Key)
                .Select(x => Path.Combine(outputDirectory, x.TrimStart('/')+".http"))
                .ToList();

        foreach (var file in fileNames)
        {
            Assert.True(File.Exists(file));
        }

        var httpSnipetContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "posts.http"));

        Assert.Contains("GET {{url}}/posts HTTP/1.1", httpSnipetContent);
        Assert.Contains("POST {{url}}/posts HTTP/1.1", httpSnipetContent);
        Assert.Contains("Content-Type: application/json", httpSnipetContent);
        Assert.Contains("\"userId\": 0", httpSnipetContent);
        Assert.Contains("\"id\": 0", httpSnipetContent);
        Assert.Contains("\"title\": \"string\",", httpSnipetContent);
        Assert.Contains("\"body\": \"string\"", httpSnipetContent);
        Assert.Contains("}", httpSnipetContent);
    }
}
