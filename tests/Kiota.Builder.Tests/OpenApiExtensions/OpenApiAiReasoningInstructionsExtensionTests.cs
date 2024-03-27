using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiAiReasoningInstructionsExtensionTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    [Fact]
    public void Parses()
    {
        var oaiValue = new OpenApiArray {
            new OpenApiString("This is a description"),
            new OpenApiString("This is a description 2"),
        };
        var value = OpenApiAiReasoningInstructionsExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.Equal("This is a description", value.ReasoningInstructions[0]);
    }
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task ParsesInDocument()
    {
        var documentContent = @"openapi: 3.0.0
info:
  title: Test
  version: 1.0.0
  x-ai-reasoning-instructions:
    - This is a description
    - This is a description 2";
        Directory.CreateDirectory(TempDirectory);
        var documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        var mockLogger = new Mock<ILogger<OpenApiAiReasoningInstructionsExtension>>();
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var generationConfig = new GenerationConfiguration { OutputPath = TempDirectory };
        var (openApiDocumentStream, _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotNull(document.Info);
        Assert.True(document.Info.Extensions.TryGetValue(OpenApiAiReasoningInstructionsExtension.Name, out var descriptionExtension));
        Assert.IsType<OpenApiAiReasoningInstructionsExtension>(descriptionExtension);
        Assert.Equal("This is a description", ((OpenApiAiReasoningInstructionsExtension)descriptionExtension).ReasoningInstructions[0]);
    }
}
