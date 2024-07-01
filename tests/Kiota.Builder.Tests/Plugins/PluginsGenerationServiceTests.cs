using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Plugins;
using Kiota.Builder.Plugins.Models;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.Plugins.Manifest;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Plugins;
public sealed class PluginsGenerationServiceTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new PluginsGenerationService(null, OpenApiUrlTreeNode.Create(), new(), "foo"));
        Assert.Throws<ArgumentNullException>(() => new PluginsGenerationService(new(), null, new(), "foo"));
        Assert.Throws<ArgumentNullException>(() => new PluginsGenerationService(new(), OpenApiUrlTreeNode.Create(), null, "foo"));
        Assert.Throws<ArgumentException>(() => new PluginsGenerationService(new(), OpenApiUrlTreeNode.Create(), new(), string.Empty));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GeneratesManifest()
    {
        var simpleDescriptionContent = @"openapi: 3.0.0
info:
  title: test
  version: 1.0
servers:
  - url: http://localhost/
    description: There's no place like home
paths:
  /test:
    get:
      description: description for test path
      responses:
        '200':
          description: test
  /test/{id}:
    get:
      description: description for test path with id
      operationId: test.WithId
      parameters:
      - name: id
        in: path
        required: true
        description: The id of the test
        schema:
          type: integer
          format: int32
      responses:
        '200':
          description: test";
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
            PluginTypes = [PluginType.APIPlugin, PluginType.APIManifest, PluginType.OpenAI],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "client-apimanifest.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenAIPluginFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, AppManifestFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal(OpenApiFileName, resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Equal(2, resultingManifest.Document.Functions.Count);// all functions are generated despite missing operationIds
        Assert.Empty(resultingManifest.Problems);// no problems are expected with names

        // Validate the v1 plugin
        var v1ManifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, OpenAIPluginFileName));
        using var v1JsonDocument = JsonDocument.Parse(v1ManifestContent);
        var v1Manifest = PluginManifestDocument.Load(v1JsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal(OpenApiFileName, v1Manifest.Document.Api.URL);
        Assert.Empty(v1Manifest.Problems);

        // Validate the manifest file
        var appManifestFile = await File.ReadAllTextAsync(Path.Combine(outputDirectory, AppManifestFileName));
        var appManifestModelObject = JsonSerializer.Deserialize<AppManifestModel>(appManifestFile, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        Assert.Equal("com.microsoft.kiota.plugin.client", appManifestModelObject.PackageName);
        Assert.Equal("client", appManifestModelObject.Name.ShortName);
        Assert.Equal("client", appManifestModelObject.CopilotExtensions.Plugins[0].Id);
        Assert.Equal(ManifestFileName, appManifestModelObject.CopilotExtensions.Plugins[0].File);
    }
    private const string ManifestFileName = "client-apiplugin.json";
    private const string OpenAIPluginFileName = "openai-plugins.json";
    private const string OpenApiFileName = "client-openapi.yml";
    private const string AppManifestFileName = "manifest.json";

    [Fact]
    public async Task GeneratesManifestAndUpdatesExistingAppManifest()
    {
        var simpleDescriptionContent = @"openapi: 3.0.0
info:
  title: test
  version: 1.0
servers:
  - url: http://localhost/
    description: There's no place like home
paths:
  /test/{id}:
    get:
      description: description for test path with id
      operationId: test.WithId
      parameters:
      - name: id
        in: path
        required: true
        description: The id of the test
        schema:
          type: integer
          format: int32
      responses:
        '200':
          description: test";
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "client-apimanifest.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "maniffest.json")));

    }
    [Fact]
    public async Task GeneratesManifestAndCleansUpInputDescription()
    {
        var simpleDescriptionContent = @"openapi: 3.0.0
info:
  title: test
  version: 1.0
x-test-root-extension: test
servers:
  - url: http://localhost/
    description: There's no place like home
paths:
  /test:
    get:
      description: description for test path
      responses:
        '200':
          description: test
        '400':
          description: client error response
  /test/{id}:
    get:
      description: description for test path with id
      operationId: test.WithId
      parameters:
      - name: id
        in: path
        required: true
        description: The id of the test
        schema:
          type: integer
          format: int32
      responses:
        '200':
          description: test
        '500':
          description: api error response
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      required:
        - '@odata.type'
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string";
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal(OpenApiFileName, resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Equal(2, resultingManifest.Document.Functions.Count);// all functions are generated despite missing operationIds
        Assert.Empty(resultingManifest.Problems);// no problems are expected with names

        var openApiReader = new OpenApiStreamReader();

        // Validate the original file.
        var originalOpenApiFile = File.OpenRead(simpleDescriptionPath);
        var originalDocument = openApiReader.Read(originalOpenApiFile, out var originalDiagnostic);
        Assert.Empty(originalDiagnostic.Errors);

        Assert.Single(originalDocument.Components.Schemas);// one schema originally
        Assert.Single(originalDocument.Extensions); // single unsupported extension at root
        Assert.Equal(2, originalDocument.Paths.Count); // document has only two paths
        Assert.Equal(2, originalDocument.Paths["/test"].Operations[OperationType.Get].Responses.Count); // 2 responses originally
        Assert.Equal(2, originalDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses.Count); // 2 responses originally

        // Validate the output open api file
        var resultOpenApiFile = File.OpenRead(Path.Combine(outputDirectory, OpenApiFileName));
        var resultDocument = openApiReader.Read(resultOpenApiFile, out var diagnostic);
        Assert.Empty(diagnostic.Errors);

        // Assertions / validations
        Assert.Empty(resultDocument.Components.Schemas);// no schema is referenced. so ensure they are all removed
        Assert.Empty(resultDocument.Extensions); // no extension at root (unsupported extension is removed)
        Assert.Equal(2, resultDocument.Paths.Count); // document has only two paths
        Assert.Single(resultDocument.Paths["/test"].Operations[OperationType.Get].Responses); // other responses are removed from the document
        Assert.NotEmpty(resultDocument.Paths["/test"].Operations[OperationType.Get].Responses["2XX"].Description); // response description string is not empty
        Assert.Single(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses); // 2 responses originally
        Assert.NotEmpty(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses["2XX"].Description);// response description string is not empty
    }
}
