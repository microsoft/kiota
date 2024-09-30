using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Tests.OpenApiSampleFiles;
using Kiota.Builder.Writers.http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Writers.http;
public sealed class HttpSnippetWriterTests : IDisposable
{
    private readonly StringWriter writer;
    private readonly HttpClient _httpClient = new();

    public HttpSnippetWriterTests()
    {
        writer = new StringWriter();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        writer?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WritesHttpSnippetAsync()
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
            OpenAPIFilePath = "openapiPath"
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);
        var httpSnippetWriter = new HttpSnippetWriter(writer);
        httpSnippetWriter.Write(urlTreeNode);

        var result = writer.ToString();

        Assert.Contains("GET {{url}}/posts/{{post-id}} HTTP/1.1", result);
        Assert.Contains("PATCH {{url}}/posts/{{post-id}} HTTP/1.1", result);
        Assert.Contains("Content-Type: application/json", result);
        Assert.Contains("\"userId\": 0", result);
        Assert.Contains("\"id\": 0", result);
        Assert.Contains("\"title\": \"string\",", result);
        Assert.Contains("\"body\": \"string\"", result);
        Assert.Contains("}", result);
    }

}
