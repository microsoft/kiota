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
public sealed class OpenApiDescriptionForModelExtensionTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    [Fact]
    public void Parses()
    {
        var oaiValue = new OpenApiString("This is a description");
        var value = OpenApiDescriptionForModelExtension.Parse(oaiValue);
        Assert.NotNull(value);
        Assert.Equal("This is a description", value.Description);
    }
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task ParsesInDocument()
    {
        var documentContent = @"openapi: 3.0.0
info:
  title: Test
  version: 1.0.0
  x-ai-description: This is a description";
        Directory.CreateDirectory(TempDirectory);
        var documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        var mockLogger = new Mock<ILogger<OpenApiDescriptionForModelExtension>>();
        var documentDownloadService = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var generationConfig = new GenerationConfiguration { OutputPath = TempDirectory };
        var (openApiDocumentStream, _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        var document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotNull(document.Info);
        Assert.True(document.Info.Extensions.TryGetValue(OpenApiDescriptionForModelExtension.Name, out var descriptionExtension));
        Assert.IsType<OpenApiDescriptionForModelExtension>(descriptionExtension);
        Assert.Equal("This is a description", ((OpenApiDescriptionForModelExtension)descriptionExtension).Description);
    }
}
