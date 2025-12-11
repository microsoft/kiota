using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Plugins;
using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Plugins;

public sealed class PluginsGenerationServiceTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly ILogger<KiotaBuilder> _logger = new Mock<ILogger<KiotaBuilder>>().Object;

    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new PluginsGenerationService(null, OpenApiUrlTreeNode.Create(), new(), "foo", _logger));
        Assert.Throws<ArgumentNullException>(() => new PluginsGenerationService(new(), null, new(), "foo", _logger));
        Assert.Throws<ArgumentNullException>(() => new PluginsGenerationService(new(), OpenApiUrlTreeNode.Create(), null, "foo", _logger));
        Assert.Throws<ArgumentException>(() => new PluginsGenerationService(new(), OpenApiUrlTreeNode.Create(), new(), string.Empty, _logger));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Theory]
    [InlineData("client", "client")]
    [InlineData("Budget Tracker", "BudgetTracker")]//drop the space
    [InlineData("My-Super complex() %@#$& Name", "MySupercomplexName")]//drop the space and special characters
    public async Task GeneratesManifestAsync(string inputPluginName, string expectedPluginName)
    {
        var simpleDescriptionContent =
"""
openapi: 3.0.0
info:
  title: test
  version: 1.0
  description: test description we've created
servers:
  - url: http://localhost/
    description: There's no place like home
tags:
  - name: test
    description: test description we've created
    externalDocs:
      description: external docs for test path
      url: http://localhost/test
      x-random-extension: true
paths:
  /test:
    get:
      tags:
        - test  
      summary: summary for test path
      description: description for test path
      responses:
        '200':
          description: test
  /test/{id}:
    get:
      summary: Summary for test path with id that is longer than 50 characters 
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
""";
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin, PluginType.APIManifest, PluginType.OpenAI],
            ClientClassName = inputPluginName,
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);

        var manifestPaths = await pluginsGenerationService.GenerateManifestAsync();
        Console.WriteLine($"Generated manifest paths: {string.Join(", ", manifestPaths.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");

        // Validate that the dictionary contains the plugin path for API plugin
        var apiPluginPath = Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-apiplugin.json");
        Assert.Equal(manifestPaths[PluginType.APIPlugin], apiPluginPath);
        Assert.True(File.Exists(apiPluginPath), $"The API plugin path '{apiPluginPath}' was not found.");
        Assert.True(File.Exists(Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-apiplugin.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-apimanifest.json")));
        // v1 plugins are not generated anymore
        Assert.False(File.Exists(Path.Combine(outputDirectory, OpenAIPluginFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-openapi.yml")));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "manifest.json")));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "color.png")));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "outline.png")));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-apiplugin.json"));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal($"{expectedPluginName.ToLower()}-openapi.yml", resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Equal(2, resultingManifest.Document.Functions.Count);// all functions are generated despite missing operationIds
        Assert.Contains("description for test path with id", resultingManifest.Document.Functions[1].Description);// Uses the operation description
        Assert.Null(resultingManifest.Document.Capabilities?.ConversationStarters);// conversation starters should not be generated for API plugins
        Assert.Equal(expectedPluginName, resultingManifest.Document.Namespace);// namespace is cleaned up.
        Assert.Empty(resultingManifest.Problems);// no problems are expected with names
        Assert.Equal("test description we've created", resultingManifest.Document.DescriptionForHuman);// description is pulled from info   
    }
    private const string ManifestFileName = "client-apiplugin.json";
    private const string OpenAIPluginFileName = "openai-plugins.json";
    private const string OpenApiFileName = "client-openapi.yml";

    [Fact]
    public async Task ThrowsOnEmptyPathsAfterFilteringAsync()
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
          description: test";
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin, PluginType.APIManifest, PluginType.OpenAI],
            ClientClassName = "testPlugin",
            IncludePatterns = ["test/id"]// this would filter out all paths
        };
        var kiotaBuilder = new KiotaBuilder(new Mock<ILogger<KiotaBuilder>>().Object, generationConfiguration, _httpClient, true);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await kiotaBuilder.GeneratePluginAsync(CancellationToken.None));
        Assert.Equal("No paths found in the OpenAPI document.", exception.Message);
    }

    [Fact]
    public async Task GeneratesManifestAndCleansUpInputDescriptionAsync()
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
      externalDocs:
        description: external docs for test path
        url: http://localhost/test
      x-random-extension: true
      responses:
        '200':
          description: test
        '400':
          description: client error response
  /test/{id}:
    get:
      summary: description for test path with id
      operationId: test.WithId
      x-openai-isConsequential: true
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
          description:
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.message'
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
          anyOf:
          - type: string
          - type: integer
        '@odata.type':
          type: string
    microsoft.graph.message:
      allOf:
      - $ref: '#/components/schemas/microsoft.graph.entity'
      - type: object
        title: message
        properties:
          subject:
            type: string
          body:
            type: string";
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
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
        Assert.Null(resultingManifest.Document.Capabilities?.ConversationStarters);// conversation starters should not be generated for API plugins
        Assert.Empty(resultingManifest.Problems);// no problems are expected with names

        // Validate the original file.
        using var originalOpenApiFile = File.OpenRead(simpleDescriptionPath);
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        var originalResult = await OpenApiDocument.LoadAsync(originalOpenApiFile, "yaml", settings);
        var originalDocument = originalResult.Document;
        Assert.Empty(originalResult.Diagnostic.Errors);

        Assert.Equal(originalDocument.Paths["/test"].Operations[HttpMethod.Get].Description, resultingManifest.Document.Functions[0].Description);// pulls from description
        Assert.Equal(originalDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Summary, resultingManifest.Document.Functions[1].Description);// pulls from summary
        Assert.NotNull(originalDocument.Paths["/test"].Operations[HttpMethod.Get].ExternalDocs); // existing external docs
        Assert.Equal(2, originalDocument.Components.Schemas.Count);// one schema originally
        Assert.Single(originalDocument.Extensions); // single unsupported extension at root
        Assert.Equal(2, originalDocument.Paths.Count); // document has only two paths
        Assert.Equal(2, originalDocument.Paths["/test"].Operations[HttpMethod.Get].Responses.Count); // 2 responses originally
        Assert.Single(originalDocument.Paths["/test"].Operations[HttpMethod.Get].Extensions); // 1 UNsupported extension
        Assert.Equal(2, originalDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses.Count); // 2 responses originally
        Assert.Single(originalDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Extensions); // 1 supported extension
        Assert.Equal(2, originalDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema.AllOf[0].Properties["id"].AnyOf.Count); // anyOf we selected

        // Validate the output open api file
        using var resultOpenApiFile = File.OpenRead(Path.Combine(outputDirectory, OpenApiFileName));
        var resultResult = await OpenApiDocument.LoadAsync(resultOpenApiFile, "yaml", settings);
        var resultDocument = resultResult.Document;
        Assert.Empty(resultResult.Diagnostic.Errors);

        // Assertions / validations
        Assert.Null(resultDocument.Components.Schemas);// no schema is referenced. so ensure they are all removed
        Assert.Null(resultDocument.Extensions); // no extension at root (unsupported extension is removed)
        Assert.Equal(2, resultDocument.Paths.Count); // document has only two paths
        Assert.Equal(originalDocument.Paths["/test"].Operations[HttpMethod.Get].Responses.Count - 1, resultDocument.Paths["/test"].Operations[HttpMethod.Get].Responses.Count); // We removed the error response
        Assert.NotEmpty(resultDocument.Paths["/test"].Operations[HttpMethod.Get].Responses["200"].Description); // response description string is not empty
        Assert.Null(resultDocument.Paths["/test"].Operations[HttpMethod.Get].ExternalDocs); // external docs are removed
        Assert.Null(resultDocument.Paths["/test"].Operations[HttpMethod.Get].Extensions); // NO UNsupported extension
        Assert.Equal(originalDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses.Count - 1, resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses.Count); // Responses are still intact.
        Assert.NotEmpty(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Description);// response description string is not empty
        Assert.Single(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Extensions); // 1 supported extension still present in operation
        Assert.Null(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema.AllOf); // allOf were merged
        Assert.Null(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema.Properties["id"].AnyOf); // anyOf we selected
        Assert.Equal(JsonSchemaType.String, resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema.Properties["id"].Type.Value);
        Assert.DoesNotContain("500", resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses.Keys, StringComparer.OrdinalIgnoreCase); // We removed the error response
    }

    [Fact]
    public async Task GeneratesManifestWithAdaptiveCardExtensionAsync()
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
      externalDocs:
        description: external docs for test path
        url: http://localhost/test
      x-ai-adaptive-card:
        data_path: $.test
        file: path_to_file
        title: title
      responses:
        '200':
          description: test
        '400':
          description: client error response
  /test/{id}:
    get:
      summary: description for test path with id
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
          description:
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
          anyOf:
          - type: string
          - type: integer
        '@odata.type':
          type: string
    microsoft.graph.message:
      allOf:
      - $ref: '#/components/schemas/microsoft.graph.entity'
      - type: object
        title: message
        properties:
          subject:
            type: string
          body:
            type: string";

        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
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
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities);
        Assert.Equal("$.test", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.DataPath);
        string jsonString = "{\"file\":\"path_to_file\"}";
        using JsonDocument doc = JsonDocument.Parse(jsonString);
        JsonElement staticTemplate = doc.RootElement.Clone();
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(staticTemplate.ToString()), JsonNode.Parse(resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.StaticTemplate.ToString())), "adaptive card present");
        Assert.Null(resultingManifest.Document.Functions[1].Capabilities);// no response semantics is added if no adaptive card
    }


    [Fact]
    public async Task GeneratesManifestWithAdaptiveCardWithoutExtensionAsync()
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
      externalDocs:
        description: external docs for test path
        url: http://localhost/test
      responses:
        '200':
          description: test
        '400':
          description: client error response
  /test/{id}:
    get:
      summary: description for test path with id
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
          description:
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.message'
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
          anyOf:
          - type: string
          - type: integer
        '@odata.type':
          type: string
    microsoft.graph.message:
      allOf:
      - $ref: '#/components/schemas/microsoft.graph.entity'
      - type: object
        title: message
        properties:
          subject:
            type: string
          body:
            type: string";

        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
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
        Assert.Null(resultingManifest.Document.Functions[0].Capabilities); // no response semantics is added if no schema
        Assert.NotNull(resultingManifest.Document.Functions[1].Capabilities.ResponseSemantics); // response semantics is added if response has schema
        string jsonString = $"{{\"file\": \"./adaptiveCards/{resultingManifest.Document.Functions[1].Name}.json\"}}";
        using JsonDocument doc = JsonDocument.Parse(jsonString);
        JsonElement staticTemplate = doc.RootElement.Clone();
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(staticTemplate.ToString()), JsonNode.Parse(resultingManifest.Document.Functions[1].Capabilities.ResponseSemantics.StaticTemplate.ToString())), "adaptive card present");

        // validate presence of adaptive card
        var path = Path.Combine(outputDirectory, "adaptiveCards", $"{resultingManifest.Document.Functions[1].Name}.json");
        Assert.True(File.Exists(path));

    }

    [Fact]
    public async Task GeneratesManifestWithEmptyAdaptiveCardExtensionAsync()
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
      summary: description for test path with id
      operationId: test.WithId
      x-ai-adaptive-card: {}
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
          description:
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.message'
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
          anyOf:
          - type: string
          - type: integer
        '@odata.type':
          type: string
    microsoft.graph.message:
      allOf:
      - $ref: '#/components/schemas/microsoft.graph.entity'
      - type: object
        title: message
        properties:
          subject:
            type: string
          body:
            type: string";

        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal(OpenApiFileName, resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Single(resultingManifest.Document.Functions);// all functions are generated despite missing operationIds
        Assert.Null(resultingManifest.Document.Functions[0].Capabilities); // response semantics is added if response has schema but it is not if empty adaptive card extension is present
        // validate adaptive card does not exist
        var path = Path.Combine(outputDirectory, "adaptiveCards", $"{resultingManifest.Document.Functions[0].Name}.json");
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task GeneratesManifestWithDefault200ResponseAsync()
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
      externalDocs:
        description: external docs for test path
        url: http://localhost/test
      responses:
        '400':
          description: client error response
";

        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));

        var (openAPIDocumentStream2, _) = await openAPIDocumentDS.LoadStreamAsync(Path.Combine(outputDirectory, OpenApiFileName), generationConfiguration, null, false);
        var resultingSpec = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream2, generationConfiguration);

        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Single(resultingSpec.Paths["/test"].Operations[HttpMethod.Get].Responses);
    }

    const string ManifestContent1 = @"openapi: 3.0.0
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
      externalDocs:
        description: external docs for test path
        url: http://localhost/test
      responses:
        '200':
          description: test
        '400':
          description: client error response
  /test/{id}:
    get:
      summary: description for test path with id
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
          description:
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.message'
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
          anyOf:
          - type: string
          - type: integer
        '@odata.type':
          type: string
    microsoft.graph.message:
      allOf:
      - $ref: '#/components/schemas/microsoft.graph.entity'
      - type: object
        title: message
        properties:
          subject:
            type: string
          body:
            type: string";

    const string ManifestContent2 = @"openapi: 3.0.0
info:
  title: test
  version: 1.0
servers:
  - url: http://localhost/
    description: There's no place like home
paths:
  /test/{id}:
    get:
      summary: description 2 for test path with id
      operationId: test.WithId
      parameters:
      - name: id
        in: path
        required: true
        description: The id of the test
        schema:
          type: integer
          format: int32
      - name: mode
        in: query
        required: true
        description: The search mode
        schema:
          type: integer
          format: int32
      responses:
        '200':
          description:
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.message'
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
          anyOf:
          - type: string
          - type: integer
        '@odata.type':
          type: string
    microsoft.graph.message:
      allOf:
      - $ref: '#/components/schemas/microsoft.graph.entity'
      - type: object
        title: message
        properties:
          subject:
            type: string
          body:
            type: string";

    [Fact]
    public async Task GenerateAndMergePluginManifestsAsync_SingleFileTest()
    {
        // Arrange
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);

        var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description-partial-1-1.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath1, ManifestContent1);

        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath1,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", // Kiota builder would set this for us
        };

        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath1, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        pluginsGenerationService.DownloadService = openAPIDocumentDS;

        // Act
        var manifestPaths = await pluginsGenerationService.GenerateAndMergeMultipleManifestsAsync();

        // Assert
        Assert.NotNull(manifestPaths);
        string ManifestFileNameMerged = "client-apiplugin.json";
        string manifestPathMerged = Path.Combine(outputDirectory, ManifestFileNameMerged);
        var expectedManifestPaths = new List<string>
            {
                manifestPathMerged
            };
        Assert.Equal(expectedManifestPaths, manifestPaths);
    }

    [Fact]
    public async Task GeneratesManifestWithMultipleRuntimesAsync_TwoFilesTest()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);

        // Write the first description
        var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description-partial-1-2.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath1, ManifestContent1);
        // Write the second description
        var simpleDescriptionPath2 = Path.Combine(workingDirectory, "description-partial-2-2.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath2, ManifestContent2);

        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath1,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        // Generate the first manifest
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath1, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        pluginsGenerationService.DownloadService = openAPIDocumentDS;

        List<string> manifestPaths = await pluginsGenerationService.GenerateAndMergeMultipleManifestsAsync();

        string ManifestFileName1 = "client-apiplugin-partial-1-2.json";
        string OpenApiFileName1 = "client-openapi-partial-1-2.yml";
        string ManifestFileName2 = "client-apiplugin-partial-2-2.json";
        string OpenApiFileName2 = "client-openapi-partial-2-2.yml";
        string ManifestFileNameMerged = "client-apiplugin.json";

        // assert that the paths are correct
        string manifestPath1 = Path.Combine(outputDirectory, ManifestFileName1);
        string manifestPath2 = Path.Combine(outputDirectory, ManifestFileName2);
        string manifestPathMerged = Path.Combine(outputDirectory, ManifestFileNameMerged);
        Assert.NotNull(manifestPaths);
        var expectedManifestPaths = new List<string>
        {
            manifestPath1,
            manifestPath2,
            manifestPathMerged
        };
        Assert.Equal(expectedManifestPaths, manifestPaths);

        // Test that all manifests were created
        foreach (var manifestPath in manifestPaths)
        {
            Assert.True(File.Exists(manifestPath));
        }

        // Test that all OpenAPI files were created
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName1)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName2)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(manifestPathMerged);
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal(OpenApiFileName1, resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Equal(3, resultingManifest.Document.Functions.Count);
        Assert.Equal("test_get", resultingManifest.Document.Functions[0].Name);
        Assert.Equal("test_WithId", resultingManifest.Document.Functions[1].Name);
        Assert.Equal("test_WithId_2", resultingManifest.Document.Functions[2].Name);
        Assert.Equal(2, resultingManifest.Document.Runtimes.Count);
        Assert.Null(resultingManifest.Document.Capabilities?.ConversationStarters);// conversation starters should not be generated for API plugins

        // Check that every runtime has at least one function
        foreach (var runtime in resultingManifest.Document.Runtimes)
        {
            Assert.NotEmpty(runtime.RunForFunctions);
        }
    }

    [Fact]
    public async Task GenerateAndMergePluginManifestsAsync_ThreeFilesTest()
    {
        // Arrange
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);

        var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description-partial-1-3.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath1, ManifestContent1);

        var simpleDescriptionPath2 = Path.Combine(workingDirectory, "description-partial-2-3.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath2, ManifestContent2);

        var simpleDescriptionPath3 = Path.Combine(workingDirectory, "description-partial-3-3.yaml");
        var manifestContent3 = """
            openapi: 3.0.0
            info:
              title: test
              version: 1.0
            servers:
              - url: http://localhost/
                description: There's no place like home
            paths:
              /test/{id}/details:
                get:
                  summary: description for test path with details
                  operationId: test.WithDetails
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
                      description: success
            """;
        await File.WriteAllTextAsync(simpleDescriptionPath3, manifestContent3);

        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath1,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", // Kiota builder would set this for us
        };

        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath1, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        pluginsGenerationService.DownloadService = openAPIDocumentDS;

        // Act
        var manifestPaths = await pluginsGenerationService.GenerateAndMergeMultipleManifestsAsync();

        // Assert
        Assert.NotNull(manifestPaths);
        string ManifestFileName1 = "client-apiplugin-partial-1-3.json";
        string ManifestFileName2 = "client-apiplugin-partial-2-3.json";
        string ManifestFileName3 = "client-apiplugin-partial-3-3.json";
        string ManifestFileNameMerged = "client-apiplugin.json";
        string manifestPath1 = Path.Combine(outputDirectory, ManifestFileName1);
        string manifestPath2 = Path.Combine(outputDirectory, ManifestFileName2);
        string manifestPath3 = Path.Combine(outputDirectory, ManifestFileName3);
        string manifestPathMerged = Path.Combine(outputDirectory, ManifestFileNameMerged);
        var expectedManifestPaths = new List<string>
        {
            manifestPath1,
            manifestPath2,
            manifestPath3,
            manifestPathMerged
        };
        Assert.Equal(expectedManifestPaths, manifestPaths);
        foreach (var manifestPath in manifestPaths)
        {
            Assert.True(File.Exists(manifestPath));
        }
    }


    [Fact]
    public async Task GenerateAndMergePluginManifestsAsync_ThreeFiles_StartingFromSecondTest()
    {
        // Arrange
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);

        var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description-partial-1-3.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath1, ManifestContent1);

        var simpleDescriptionPath2 = Path.Combine(workingDirectory, "description-partial-2-3.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath2, ManifestContent2);

        var simpleDescriptionPath3 = Path.Combine(workingDirectory, "description-partial-3-3.yaml");
        var manifestContent3 = """
            openapi: 3.0.0
            info:
              title: test
              version: 1.0
            servers:
              - url: http://localhost/
                description: There's no place like home
            paths:
              /test/{id}/details:
                get:
                  summary: description for test path with details
                  operationId: test.WithDetails
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
                      description: success
            """;
        await File.WriteAllTextAsync(simpleDescriptionPath3, manifestContent3);

        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath2,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", // Kiota builder would set this for us
        };

        // Start with the second manifest
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath2, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        pluginsGenerationService.DownloadService = openAPIDocumentDS;

        // Act
        var manifestPaths = await pluginsGenerationService.GenerateAndMergeMultipleManifestsAsync();

        // Assert
        Assert.NotNull(manifestPaths);
        string ManifestFileName1 = "client-apiplugin-partial-1-3.json";
        string ManifestFileName2 = "client-apiplugin-partial-2-3.json";
        string ManifestFileName3 = "client-apiplugin-partial-3-3.json";
        string ManifestFileNameMerged = "client-apiplugin.json";
        string manifestPath1 = Path.Combine(outputDirectory, ManifestFileName1);
        string manifestPath2 = Path.Combine(outputDirectory, ManifestFileName2);
        string manifestPath3 = Path.Combine(outputDirectory, ManifestFileName3);
        string manifestPathMerged = Path.Combine(outputDirectory, ManifestFileNameMerged);
        var expectedManifestPaths = new List<string>
        {
            manifestPath2,
            manifestPath3,
            manifestPathMerged
        };
        Assert.Equal(expectedManifestPaths, manifestPaths);
        Assert.False(File.Exists(manifestPath1));
        foreach (var manifestPath in manifestPaths)
        {
            Assert.True(File.Exists(manifestPath));
        }
    }

    [Fact]
    public async Task GenerateAndMergePluginManifestsAsync_ThrowsException_WhenNoManifestsGenerated()
    {
        // Arrange
        var mockDownloadService = new Mock<OpenApiDocumentDownloadService>(Mock.Of<HttpClient>(), Mock.Of<ILogger>());
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var generationConfiguration = new GenerationConfiguration
        {
            PluginTypes = new HashSet<PluginType> { PluginType.APIPlugin },
            OutputPath = Path.GetTempPath(),
            OpenAPIFilePath = "description-partial-1-1.yaml"
        };
        var service = new PluginsGenerationService(
            new OpenApiDocument(),
            OpenApiUrlTreeNode.Create(),
            generationConfiguration,
            generationConfiguration.OutputPath,

            mockLogger.Object
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAndMergeMultipleManifestsAsync(CancellationToken.None));
    }




    #region Security

    public static TheoryData<string, string, string, PluginAuthConfiguration, Action<DocumentValidationResults<PluginManifestDocument>>>
        SecurityInformationSuccess()
    {
        return new TheoryData<string, string, string, PluginAuthConfiguration, Action<DocumentValidationResults<PluginManifestDocument>>>
        {
            // security requirement in operation object
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key, in: header }}}",
                string.Empty, "security: [apiKey0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<ApiKeyPluginVault>(auth0);
                    Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
                    Assert.Equal("{apiKey0_REGISTRATION_ID}", ((ApiKeyPluginVault)auth0!).ReferenceId);
                }
            },
            // multiple security schemes
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key0, in: header, x-ai-auth-reference-id: auth1234 }, apiKey1: {type: apiKey, name: x-api-key1, in: header }}}",
                string.Empty, "security: [apiKey0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<ApiKeyPluginVault>(auth0);
                    Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
                    Assert.Equal("auth1234", ((ApiKeyPluginVault)auth0!).ReferenceId);
                }
            },
            // security requirement in root object
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key, in: header }}}",
                "security: [apiKey0: []]", string.Empty, null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<ApiKeyPluginVault>(auth0);
                    Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
                    Assert.Equal("{apiKey0_REGISTRATION_ID}", ((ApiKeyPluginVault)auth0!).ReferenceId);
                }
            },
            // auth provided in config overrides openapi file auth
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key, in: header, x-ai-auth-reference-id: auth1234 }}}",
                string.Empty, "security: [apiKey0: []]", new PluginAuthConfiguration("different_ref_id") {AuthType = PluginAuthType.OAuthPluginVault}, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<OAuthPluginVault>(auth0);
                    Assert.Equal(AuthType.OAuthPluginVault, auth0?.Type);
                    Assert.Equal("different_ref_id", ((OAuthPluginVault)auth0!).ReferenceId);
                }
            },
            // auth provided in config applies when no openapi file auth
            {
                "{}",
                string.Empty, string.Empty,
                new PluginAuthConfiguration("different_ref_id") {AuthType = PluginAuthType.OAuthPluginVault},
                resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<OAuthPluginVault>(auth0);
                    Assert.Equal(AuthType.OAuthPluginVault, auth0?.Type);
                    Assert.Equal("different_ref_id", ((OAuthPluginVault)auth0!).ReferenceId);
                }
            },
            // http bearer auth
            {
                "{securitySchemes: {httpBearer0: {type: http, scheme: bearer, x-ai-auth-reference-id: bearer-1234}}}",
                string.Empty, "security: [httpBearer0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<ApiKeyPluginVault>(auth0);
                    Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
                    Assert.Equal("bearer-1234", ((ApiKeyPluginVault)auth0!).ReferenceId);
                }
            },
            // openid connect auth
            {
                "{securitySchemes: {openIdConnect0: {type: openIdConnect, openIdConnectUrl: 'http://auth.com'}}}",
                string.Empty, "security: [openIdConnect0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<ApiKeyPluginVault>(auth0);
                    Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
                    Assert.Equal("{openIdConnect0_REGISTRATION_ID}", ((ApiKeyPluginVault)auth0!).ReferenceId);
                }
            },
            // oauth2
            {
                "{securitySchemes: {oauth2_0: {type: oauth2, flows: {}, x-ai-auth-reference-id: auth1234}}}",
                string.Empty, "security: [oauth2_0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<OAuthPluginVault>(auth0);
                    Assert.Equal(AuthType.OAuthPluginVault, auth0?.Type);
                    Assert.Equal("auth1234", ((OAuthPluginVault)auth0!).ReferenceId);
                }
            },
            // oauth2 without reference id
            {
                "{securitySchemes: {oauth2_0: {type: oauth2, flows: {}}}}",
                string.Empty, "security: [oauth2_0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<OAuthPluginVault>(auth0);
                    Assert.Equal(AuthType.OAuthPluginVault, auth0?.Type);
                    Assert.Equal("{oauth2_0_REGISTRATION_ID}", ((OAuthPluginVault)auth0!).ReferenceId);
                }
            },
            // OAuth2 with implicit flow should return (None)
            {
                "{securitySchemes: {oauth2_implicit: {type: oauth2, flows: {implicit: {authorizationUrl: 'https://example.com/auth'}}}}}",
                string.Empty, "security: [oauth2_implicit: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<AnonymousAuth>(auth);
                }
            },
            // should be anonymous
            {
                "{}", string.Empty, "security: [invalid: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<AnonymousAuth>(auth0);
                }
            },
            // multiple security schemes in operation object
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key0, in: header}, apiKey1: {type: apiKey, name: x-api-key1, in: header}}}",
                string.Empty, "security: [apiKey0: [], apiKey1: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<AnonymousAuth>(auth0);
                }
            },
            // Unsupported security scheme (http basic)
            {
                "{securitySchemes: {httpBasic0: {type: http, scheme: basic}}}",
                string.Empty, "security: [httpBasic0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<AnonymousAuth>(auth0);
                }
            }


        };
    }

    [Theory]
    [MemberData(nameof(SecurityInformationSuccess))]
    public async Task GeneratesManifestWithAuthAsync(string securitySchemesComponent, string rootSecurity,
        string operationSecurity, PluginAuthConfiguration pluginAuthConfiguration, Action<DocumentValidationResults<PluginManifestDocument>> assertions)
    {
        var apiDescription = $"""
                              openapi: 3.0.0
                              info:
                                title: test
                                version: "1.0"
                              paths:
                                /test:
                                  get:
                                    description: description for test path
                                    responses:
                                      '200':
                                        description: test
                                    {operationSecurity}
                              {rootSecurity}
                              components: {securitySchemesComponent}
                              """;
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, apiDescription);
        var openApiDocumentDs = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
            PluginAuthInformation = pluginAuthConfiguration,
        };
        var (openApiDocumentStream, _) =
            await openApiDocumentDs.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument =
            await openApiDocumentDs.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfiguration);
        Assert.NotNull(openApiDocument);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService =
            new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);

        assertions(resultingManifest);
        // Cleanup
        try
        {
            Directory.Delete(outputDirectory, true);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    [Fact]
    public async Task GeneratesManifestWithMultipleSecuritySchemesAsync()
    {
        var apiDescription = """
                              openapi: 3.0.0
                              info:
                                title: test
                                version: "1.0"
                              servers:
                                - url: https://localhost:8080
                              paths:
                                /test:
                                  get:
                                    description: description for test path
                                    responses:
                                      "200":
                                        description: test
                                    security: [{apiKey0: []}]
                                  patch:
                                    description: description for test path
                                    responses:
                                      "200":
                                        description: test
                                    security: [{apiKey1: []}]
                              components:
                                {
                                  securitySchemes: {
                                    apiKey0: { type: apiKey, name: x-api-key0, in: header, x-ai-auth-reference-id: auth1234 },
                                    apiKey1: { type: apiKey, name: x-api-key1, in: header, x-ai-auth-reference-id: auth5678 },
                                  },
                                }
                              """;
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, apiDescription);
        var openApiDocumentDs = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openApiDocumentStream, _) =
            await openApiDocumentDs.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument =
            await openApiDocumentDs.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfiguration);
        Assert.NotNull(openApiDocument);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService =
            new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);

        Assert.NotNull(resultingManifest.Document);
        Assert.Empty(resultingManifest.Problems);
        Assert.NotEmpty(resultingManifest.Document.Runtimes);
        var auth0 = resultingManifest.Document.Runtimes[0].Auth;
        Assert.IsType<ApiKeyPluginVault>(auth0);
        Assert.Equal(AuthType.ApiKeyPluginVault, auth0.Type);
        Assert.Equal("auth1234", ((ApiKeyPluginVault)auth0!).ReferenceId);
        var auth1 = resultingManifest.Document.Runtimes[1].Auth;
        Assert.IsType<ApiKeyPluginVault>(auth1);
        Assert.Equal(AuthType.ApiKeyPluginVault, auth1.Type);
        Assert.Equal("auth5678", ((ApiKeyPluginVault)auth1!).ReferenceId);
        // Cleanup
        try
        {
            Directory.Delete(outputDirectory);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    #endregion

    #region Function capabilities

    [Fact]
    public async Task GeneratesManifestWithAiCapabilitiesExtensionAsync()
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
      x-ai-capabilities:
        response_semantics:
          data_path: $.test
          static_template:
            type: AdaptiveCard
            version: 1.5
            body:
              - type: TextBlock
                text: Hello World
          properties:
            title: Card Title
            subtitle: Card Subtitle
            thumbnail_url: https://example.com/image.png
            url: https://example.com
            information_protection_label: general
            template_selector: defaultTemplate
        confirmation:
          type: text
          title: Confirmation Title
          body: Are you sure you want to proceed?
        security_info:
          data_handling:
            - sensitiveData
            - personalData
      responses:
        '200':
          description: test
  /test/{id}:
    get:
      summary: description for test path with id
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
          description: success
";

        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal(OpenApiFileName, resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Equal(2, resultingManifest.Document.Functions.Count);

        // Validate x-ai-capabilities data is correctly parsed
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities);

        // Validate ResponseSemantics
        Assert.Equal("$.test", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.DataPath);
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.StaticTemplate);
        Assert.Contains("AdaptiveCard", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.StaticTemplate.ToString());

        // Validate ResponseSemantics.Properties
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties);
        Assert.Equal("Card Title", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties.Title);
        Assert.Equal("Card Subtitle", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties.Subtitle);
        Assert.Equal("https://example.com/image.png", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties.ThumbnailUrl);
        Assert.Equal("https://example.com", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties.Url);
        Assert.Equal("general", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties.InformationProtectionLabel);
        Assert.Equal("defaultTemplate", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.Properties.TemplateSelector);

        // Validate Confirmation
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities.Confirmation);
        Assert.Equal("text", resultingManifest.Document.Functions[0].Capabilities.Confirmation.Type);
        Assert.Equal("Confirmation Title", resultingManifest.Document.Functions[0].Capabilities.Confirmation.Title);
        Assert.Equal("Are you sure you want to proceed?", resultingManifest.Document.Functions[0].Capabilities.Confirmation.Body);

        // Validate SecurityInfo
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities.SecurityInfo);
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities.SecurityInfo.DataHandling);
        Assert.Equal(2, resultingManifest.Document.Functions[0].Capabilities.SecurityInfo.DataHandling.Count);
        Assert.Contains("sensitiveData", resultingManifest.Document.Functions[0].Capabilities.SecurityInfo.DataHandling);
        Assert.Contains("personalData", resultingManifest.Document.Functions[0].Capabilities.SecurityInfo.DataHandling);

        // Second function has no response semantics
        Assert.Null(resultingManifest.Document.Functions[1].Capabilities);
    }

    [Fact]
    public async Task PrefersCapabilitiesOverAdaptiveCardExtensionAsync()
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
      x-ai-capabilities:
        response_semantics:
          data_path: $.capabilities
          static_template:
            type: AdaptiveCard
            version: 1.5
      x-ai-adaptive-card:
        data_path: $.adaptiveCard
        file: path_to_file
        title: title
      responses:
        '200':
          description: test
";

        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, simpleDescriptionContent);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, ManifestFileName));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);

        // Should use x-ai-capabilities over x-ai-adaptive-card
        Assert.NotNull(resultingManifest.Document.Functions[0].Capabilities);
        Assert.Equal("$.capabilities", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.DataPath);
        Assert.NotEqual("$.adaptiveCard", resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.DataPath);
    }

    #endregion

    #region Validation

    public static TheoryData<string, Action<OpenApiDocument, OpenApiDiagnostic>>
        ValidationSchemaTestInput()
    {
        return new TheoryData<string, Action<OpenApiDocument, OpenApiDiagnostic>>
        {
            // AllOf
            // simple disjoint
            {
                """
                content:
                            application/json:
                                schema:
                                    allOf: [
                                        {type: string},
                                        {maxLength: 5}
                                    ]
                """, (slicedDocument, _) =>
                {
                    Assert.NotNull(slicedDocument);
                    Assert.NotEmpty(slicedDocument.Paths);
                    var schema = slicedDocument.Paths["/test"].Operations[HttpMethod.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal(JsonSchemaType.String, schema.Type.Value);
                    Assert.Equal(5, schema.MaxLength);
                }
            },
            // objects
            {
                """
                content:
                            application/json:
                                schema:
                                    allOf: [
                                        {type: object, properties: {a: {type: string}, b: {type: number}}},
                                        {type: object, properties: {c: {type: number}}}
                                    ]
                """, (slicedDocument, _) =>
                {
                    Assert.NotNull(slicedDocument);
                    Assert.NotEmpty(slicedDocument.Paths);
                    var schema = slicedDocument.Paths["/test"].Operations[HttpMethod.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal(JsonSchemaType.Object, schema.Type.Value);
                    Assert.Equal(3, schema.Properties.Count);
                }
            },
            // objects with repeated properties
            {
                """
                content:
                            application/json:
                                schema:
                                    allOf: [
                                        {type: object, properties: {a: {type: string}, b: {type: number}}},
                                        {type: object, properties: {b: {type: number}}}
                                    ]
                """, (slicedDocument, _) =>
                {
                    Assert.NotNull(slicedDocument);
                    Assert.NotEmpty(slicedDocument.Paths);
                    var schema = slicedDocument.Paths["/test"].Operations[HttpMethod.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal(JsonSchemaType.Object, schema.Type.Value);
                    Assert.Equal(2, schema.Properties.Count);
                }
            },
            // AnyOf
            {
                """
                content:
                            application/json:
                                schema:
                                    anyOf: [
                                        {type: object, properties: {a: {type: string}, b: {type: number}}},
                                        {type: object, properties: {c: {type: number}}}
                                    ]
                """, (slicedDocument, _) =>
                {
                    Assert.NotNull(slicedDocument);
                    Assert.NotEmpty(slicedDocument.Paths);
                    var schema = slicedDocument.Paths["/test"].Operations[HttpMethod.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal(JsonSchemaType.Object, schema.Type.Value);
                    Assert.Equal(2, schema.Properties.Count);
                }
            },
            // OneOf
            {
                """
                content:
                            application/json:
                                schema:
                                    oneOf: [
                                        {type: object, properties: {c: {type: number}}},
                                        {type: object, properties: {a: {type: string}, b: {type: number}}}
                                    ]
                """, (slicedDocument, _) =>
                {
                    Assert.NotNull(slicedDocument);
                    Assert.NotEmpty(slicedDocument.Paths);
                    var schema = slicedDocument.Paths["/test"].Operations[HttpMethod.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal(JsonSchemaType.Object, schema.Type.Value);
                    Assert.Single(schema.Properties);
                }
            },
            // normal schema
            {
                """
                content:
                            application/json:
                                schema: {type: object, properties: {c: {type: number}}}
                """, (slicedDocument, _) =>
                {
                    Assert.NotNull(slicedDocument);
                    Assert.NotEmpty(slicedDocument.Paths);
                    var schema = slicedDocument.Paths["/test"].Operations[HttpMethod.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal(JsonSchemaType.Object, schema.Type.Value);
                    Assert.Single(schema.Properties);
                }
            },
        };
    }

    [Theory]
    [MemberData(nameof(ValidationSchemaTestInput))]
    public async Task MergesAllOfRequestBodyAsync(string content, Action<OpenApiDocument, OpenApiDiagnostic> assertions)
    {
        var apiDescription = $"""
        openapi: 3.0.0
        info:
          title: test
          version: "1.0"
        paths:
          /test:
            post:
              description: description for test path
              requestBody:
                required: true
                {content}
              responses:
                '200':
                  description: "success"
        """;
        // creates a new schema with both type:string & maxLength:5
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, apiDescription);
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, _logger);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = simpleDescriptionPath,
            PluginTypes = [PluginType.APIPlugin],
            ClientClassName = "client",
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));

        try
        {
            // Validate the sliced openapi
            using var stream = File.Open(Path.Combine(outputDirectory, OpenApiFileName), FileMode.Open);
            var settings = new OpenApiReaderSettings();
            settings.AddYamlReader();
            var readResult = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
            assertions(readResult.Document, readResult.Diagnostic);
        }
        finally
        {
            try
            {
                Directory.Delete(outputDirectory);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    #endregion

    #region Tests for helper methods

    /// <summary>
    /// Creates an instance of <see cref="PluginsGenerationService"/> with an empty OpenAPI document and URL tree node.
    /// This is useful for testing scenarios where no OpenAPI document is provided.
    /// </summary>
    /// <param name="generationConfiguration">The configuration to use for the generation process.</param>
    /// <returns>An instance of <see cref="PluginsGenerationService"/> initialized with empty data.</returns>
    private PluginsGenerationService CreateEmptyPluginsGenerationService(GenerationConfiguration generationConfiguration)
    {
        var openApiDocument = new OpenApiDocument();
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        return new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory, _logger);
    }

    [Fact]
    public void SanitizeClientClassName_RemovesSpecialCharacters()
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = "My@Client#Name!"
        });

        // Act
        var result = pluginsGenerationService.SanitizeClientClassName();

        // Assert
        Assert.Equal("MyClientName", result);
    }

    [Theory]
    [InlineData("My@Client#Name!", "MyClientName")]
    [InlineData("Client123Name", "Client123Name")]
    [InlineData("", "")]
    public void SanitizeClientClassName_ValidatesVariousInputs(string inputClientClassName, string expectedSanitizedName)
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = inputClientClassName
        });

        // Act
        var result = pluginsGenerationService.SanitizeClientClassName();

        // Assert
        Assert.Equal(expectedSanitizedName, result);
    }

    [Fact]
    public void SanitizeClientClassName_ThrowsArgumentNullException_WhenInputIsNull()
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = null
        });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pluginsGenerationService.SanitizeClientClassName());
    }

    [Fact]
    public void EnsureOutputDirectoryExists_CreatesDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TestDirectory");
        var tempFilePath = Path.Combine(tempDirectory, "testfile.txt");
        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, true);

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = "Client123Name"
        });

        // Act
        pluginsGenerationService.EnsureOutputDirectoryExists(tempFilePath);

        // Assert
        Assert.True(Directory.Exists(tempDirectory));

        // Cleanup
        Directory.Delete(tempDirectory, true);
    }

    [Fact]
    public void EnsureOutputDirectoryExists_DoesNothing_WhenDirectoryExists()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TestDirectory");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, "testfile.txt");

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = "Client123Name"
        });

        // Act
        pluginsGenerationService.EnsureOutputDirectoryExists(tempFilePath);

        // Assert
        Assert.True(Directory.Exists(tempDirectory));

        // Cleanup
        Directory.Delete(tempDirectory, true);
    }

    [Theory]
    [InlineData("description-partial-1-1.yaml", 1, "description-partial-1-1.yaml")]
    [InlineData("description-partial-1-2.yaml", 2, "description-partial-2-2.yaml")]
    [InlineData("description-partial-1-3.yaml", 2, "description-partial-2-3.yaml")]
    [InlineData("description-partial-1-3.yaml", 3, "description-partial-3-3.yaml")]
    [InlineData("description-partial-1-9.yaml", 5, "description-partial-5-9.yaml")]
    [InlineData("description-partial-1-8.yaml", 3, "description-partial-3-8.yaml")]
    [InlineData("description-partial-1-8.yaml", 8, "description-partial-8-8.yaml")]
    public void GetNextFileInfo_ValidInputs_ReturnsExpectedResults(String originalFilePath, uint fileNumber, string expectedFilePath)
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            OpenAPIFilePath = originalFilePath
        });

        // Act
        string updatedOpenAPIFilePath = pluginsGenerationService.GetNextFilePath(originalFilePath, fileNumber);

        // Assert
        Assert.Equal(expectedFilePath, updatedOpenAPIFilePath);
    }

    [Fact]
    public void GetNextFileInfo_InvalidRegex_ThrowsException()
    {
        // Arrange
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            OpenAPIFilePath = "invalid_file_name.yaml"
        };
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(generationConfiguration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pluginsGenerationService.GetNextFilePath(generationConfiguration.OpenAPIFilePath, 2));
    }

    [Theory]
    [InlineData("openapi.agentname.actionname-partial-1-3.yaml", true, 1, 3)] // Valid pattern with 3 files
    [InlineData("openapi.agentname.actionname-partial-1-2.yaml", true, 1, 2)] // Valid pattern with 2 files
    [InlineData("openapi.agentname.actionname-partial-1-10.yaml", true, 1, 10)] // Valid pattern with 10 files
    [InlineData("openapi.agentname.actionname.yaml", false, 0, 0)] // Invalid pattern, no partial prefix
    [InlineData("openapi.agentname.actionname-partial-2.yaml", false, 0, 0)] // Invalid pattern, not having the second number
    [InlineData("openapi.agentname.actionname-partial-2-2.yaml", true, 2, 2)] // Valid pattern with 2 file
    [InlineData("openapi.agentname.actionname-partial-2-5.yaml", true, 2, 5)] // Valid pattern with 5 files
    [InlineData("openapi.agentname.actionname-partial-2-20.yaml", true, 2, 20)] // Valid pattern with 20 files
    [InlineData("openapi.agentname.actionname-partial-1-0.yaml", true, 1, 0)] // Invalid pattern, zero files
    [InlineData("openapi.agentname.actionname-partial-a-2.yaml", false, 0, 0)] // Invalid pattern, wrong first number
    [InlineData("openapi.agentname.actionname-partial-1-b.yaml", false, 0, 0)] // Invalid pattern, wrong second number
    [InlineData("randomfile.yaml", false, 0, 0)] // Completely invalid file name
    [InlineData("", false, 0, 0)] // Empty file name
    public void TryMatchMultipleFilesRequest_ValidatesFilePatterns(string filePath, bool expectedResult, uint expectedFileNumber, uint expectedFilesCount)
    {
        // Arrange
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            OpenAPIFilePath = filePath
        };
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(generationConfiguration);

        // Act
        var result = pluginsGenerationService.TryMatchMultipleFilesRequest(filePath, out var fileNumber, out var filesCount);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedFileNumber, fileNumber);
        Assert.Equal(expectedFilesCount, filesCount);
    }

    [Fact]
    public void TryMatchMultipleFilesRequest_ThrowsArgumentNullException_WhenFilePathIsNull()
    {
        // Arrange  
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act & Assert  
        Assert.Throws<ArgumentNullException>(() => pluginsGenerationService.TryMatchMultipleFilesRequest(null, out _, out _));
    }

    [Fact]
    public async Task SavePluginManifestAsync_SavesManifestSuccessfully()
    {
        // Arrange
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);
        var manifestPath = Path.Combine(workingDirectory, "test-manifest.json");
        var pluginManifestDocument = new PluginManifestDocument
        {
            Namespace = "TestNamespace",
            DescriptionForHuman = "Test Description"
        };

        // Act
        await PluginsGenerationService.SavePluginManifestAsync(manifestPath, pluginManifestDocument);

        // Assert
        Assert.True(File.Exists(manifestPath), "The manifest file was not created.");
        var savedContent = await File.ReadAllTextAsync(manifestPath);
        Assert.Contains("\"namespace\": \"TestNamespace\"", savedContent);
        Assert.Contains("\"description_for_human\": \"Test Description\"", savedContent);

        // Cleanup
        Directory.Delete(workingDirectory, true);
    }

    [Fact]
    public async Task SavePluginManifestAsync_ThrowsException_WhenPathIsInvalid()
    {
        // Arrange
        var invalidPath = Path.Combine("Invalid", "Path", "test-manifest.json");
        var pluginManifestDocument = new PluginManifestDocument
        {
            Namespace = "TestNamespace",
            DescriptionForHuman = "Test Description"
        };

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await PluginsGenerationService.SavePluginManifestAsync(invalidPath, pluginManifestDocument));
    }

    [Fact]
    public async Task SavePluginManifestAsync_OverwritesExistingFile()
    {
        // Arrange
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);
        var manifestPath = Path.Combine(workingDirectory, "test-manifest.json");
        await File.WriteAllTextAsync(manifestPath, "{\"Namespace\":\"OldNamespace\"}");
        var pluginManifestDocument = new PluginManifestDocument
        {
            Namespace = "NewNamespace",
            DescriptionForHuman = "Updated Description"
        };

        // Act
        await PluginsGenerationService.SavePluginManifestAsync(manifestPath, pluginManifestDocument);

        // Assert
        var savedContent = await File.ReadAllTextAsync(manifestPath);
        Assert.Contains("\"namespace\": \"NewNamespace\"", savedContent);
        Assert.Contains("\"description_for_human\": \"Updated Description\"", savedContent);

        // Cleanup
        Directory.Delete(workingDirectory, true);
    }
    [Fact]
    public void MergeConversationStarters_MergesUniqueStarters()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = new List<ConversationStarter>
                {
                    new ConversationStarter { Text = "Starter1" },
                    new ConversationStarter { Text = "Starter2" }
                }
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = new List<ConversationStarter>
                {
                    new ConversationStarter { Text = "Starter2" }, // Duplicate
                    new ConversationStarter { Text = "Starter3" }
                }
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeConversationStarters(mainManifest, additionalManifest);

        // Assert
        Assert.NotNull(mainManifest.Capabilities.ConversationStarters);
        Assert.Equal(3, mainManifest.Capabilities.ConversationStarters.Count);
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter1");
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter2");
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter3");
    }

    [Fact]
    public void MergeConversationStarters_HandlesNullStartersInMainManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = null
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = new List<ConversationStarter>
                {
                    new ConversationStarter { Text = "Starter1" }
                }
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeConversationStarters(mainManifest, additionalManifest);

        // Assert
        Assert.NotNull(mainManifest.Capabilities.ConversationStarters);
        Assert.Single(mainManifest.Capabilities.ConversationStarters);
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter1");
    }

    [Fact]
    public void MergeConversationStarters_HandlesNullStartersInAdditionalManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = new List<ConversationStarter>
                {
                    new ConversationStarter { Text = "Starter1" }
                }
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = null
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeConversationStarters(mainManifest, additionalManifest);

        // Assert
        Assert.NotNull(mainManifest.Capabilities.ConversationStarters);
        Assert.Single(mainManifest.Capabilities.ConversationStarters);
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter1");
    }

    [Fact]
    public void MergeConversationStarters_HandlesNullCapabilitiesInAdditionalManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = new List<ConversationStarter>
                {
                    new ConversationStarter { Text = "Starter1" }
                }
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Capabilities = null
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeConversationStarters(mainManifest, additionalManifest);

        // Assert
        Assert.NotNull(mainManifest.Capabilities.ConversationStarters);
        Assert.Single(mainManifest.Capabilities.ConversationStarters);
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter1");
    }

    [Fact]
    public void MergeConversationStarters_HandlesNullCapabilitiesInMainManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Capabilities = null
        };

        var additionalManifest = new PluginManifestDocument
        {
            Capabilities = new Capabilities
            {
                ConversationStarters = new List<ConversationStarter>
                {
                    new ConversationStarter { Text = "Starter1" }
                }
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeConversationStarters(mainManifest, additionalManifest);

        // Assert
        Assert.NotNull(mainManifest.Capabilities);
        Assert.NotNull(mainManifest.Capabilities.ConversationStarters);
        Assert.Single(mainManifest.Capabilities.ConversationStarters);
        Assert.Contains(mainManifest.Capabilities.ConversationStarters, cs => cs.Text == "Starter1");
    }


    [Fact]
    public void MergeFunctions_MergesUniqueFunctions()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function1" },
                new Function { Name = "Function2" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function3" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeFunctions(mainManifest, additionalManifest, 1);

        // Assert
        Assert.NotNull(mainManifest.Functions);
        Assert.Equal(3, mainManifest.Functions.Count);
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function1");
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function2");
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function3");
    }

    [Fact]
    public void MergeFunctions_AppendsManifestIndexToFunctionNames()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function1" },
                new Function { Name = "Function2" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function2" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeFunctions(mainManifest, additionalManifest, 1);

        // Assert
        Assert.NotNull(mainManifest.Functions);
        Assert.Equal(3, mainManifest.Functions.Count);
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function1");
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function2");
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function2_2");
    }

    [Fact]
    public void MergeFunctions_HandlesNullFunctionsInMainManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Functions = null,
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function1" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeFunctions(mainManifest, additionalManifest, 1);

        // Assert
        Assert.NotNull(mainManifest.Functions);
        Assert.Single(mainManifest.Functions);
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function1");
    }

    [Fact]
    public void MergeFunctions_HandlesNullFunctionsInAdditionalManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function1" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Functions = null,
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeFunctions(mainManifest, additionalManifest, 1);

        // Assert
        Assert.NotNull(mainManifest.Functions);
        Assert.Single(mainManifest.Functions);
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function1");
    }

    [Fact]
    public void MergeFunctions_HandlesEmptyFunctionsInAdditionalManifest()
    {
        // Arrange
        var mainManifest = new PluginManifestDocument
        {
            Functions = new List<Function>
            {
                new Function { Name = "Function1" }
            },
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var additionalManifest = new PluginManifestDocument
        {
            Functions = new List<Function>(),
            Runtimes = new List<Runtime>
            {
                new OpenApiRuntime()
            }
        };

        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        pluginsGenerationService.MergeFunctions(mainManifest, additionalManifest, 1);

        // Assert
        Assert.NotNull(mainManifest.Functions);
        Assert.Single(mainManifest.Functions);
        Assert.Contains(mainManifest.Functions, f => f.Name == "Function1");
    }

    [Theory]
    [InlineData(1, 3, "-partial-1-3")]
    [InlineData(2, 5, "-partial-2-5")]
    [InlineData(10, 10, "-partial-10-10")]
    [InlineData(1, 1, "-partial-1-1")]
    public void GetFileNameSuffixForMultipleFiles_ValidInputs_ReturnsExpectedSuffix(uint fileNumber, uint filesCount, string expectedSuffix)
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act
        var result = pluginsGenerationService.GetFileNameSuffixForMultipleFiles(fileNumber, filesCount);

        // Assert
        Assert.Equal(expectedSuffix, result);
    }

    [Fact]
    public void GetFileNameSuffixForMultipleFiles_ThrowsArgumentException_WhenFileNumberIsZero()
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pluginsGenerationService.GetFileNameSuffixForMultipleFiles(0, 3));
    }

    [Fact]
    public void GetFileNameSuffixForMultipleFiles_ThrowsArgumentException_WhenFilesCountIsZero()
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pluginsGenerationService.GetFileNameSuffixForMultipleFiles(1, 0));
    }

    [Theory]
    [InlineData("description-partial-1-3.yaml", "description-partial-1-3.yaml")]
    [InlineData("description-partial-2-3.yaml", "description-partial-1-3.yaml")]
    [InlineData("description-partial-5-10.yaml", "description-partial-1-10.yaml")]
    [InlineData("description-partial-10-10.yaml", "description-partial-1-10.yaml")]
    [InlineData("description-partial-1-1.yaml", "description-partial-1-1.yaml")]
    [InlineData("description-partial-a-b.yaml", "description-partial-a-b.yaml")]
    [InlineData("description.yaml", "description.yaml")]
    public void GetFirstPartialFileName_ValidInputs_ReturnsExpectedResults(string inputFilePath, string expectedFilePath)
    {
        // Act
        var result = PluginsGenerationService.GetFirstPartialFileName(inputFilePath);

        // Assert
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void GetFirstPartialFileName_ThrowsArgumentException_WhenInputIsNullOrEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PluginsGenerationService.GetFirstPartialFileName(null));
        Assert.Throws<ArgumentException>(() => PluginsGenerationService.GetFirstPartialFileName(string.Empty));
    }

    #endregion
}

