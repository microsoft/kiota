using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Plugins;
using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Reader;
using Microsoft.OpenApi.Services;
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
        var simpleDescriptionContent = @"openapi: 3.0.0
info:
  title: test
  version: 1.0
  description: test description we've created
servers:
  - url: http://localhost/
    description: There's no place like home
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
          description: test";
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
        Assert.Equal(2, resultingManifest.Document.Capabilities.ConversationStarters.Count);// conversation starters are generated for each function
        Assert.Contains("Summary for test path with id", resultingManifest.Document.Capabilities.ConversationStarters[1].Text);// Uses the operation summary
        Assert.True(resultingManifest.Document.Capabilities.ConversationStarters[1].Text.Length <= 50);// Conversation starters are limited to 50 characters
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
        Assert.Equal(2, resultingManifest.Document.Capabilities.ConversationStarters.Count);// conversation starters are generated for each function
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
        Assert.Empty(resultDocument.Components.Schemas);// no schema is referenced. so ensure they are all removed
        Assert.Empty(resultDocument.Extensions); // no extension at root (unsupported extension is removed)
        Assert.Equal(2, resultDocument.Paths.Count); // document has only two paths
        Assert.Equal(originalDocument.Paths["/test"].Operations[HttpMethod.Get].Responses.Count - 1, resultDocument.Paths["/test"].Operations[HttpMethod.Get].Responses.Count); // We removed the error response
        Assert.NotEmpty(resultDocument.Paths["/test"].Operations[HttpMethod.Get].Responses["200"].Description); // response description string is not empty
        Assert.Null(resultDocument.Paths["/test"].Operations[HttpMethod.Get].ExternalDocs); // external docs are removed
        Assert.Empty(resultDocument.Paths["/test"].Operations[HttpMethod.Get].Extensions); // NO UNsupported extension
        Assert.Equal(originalDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses.Count - 1, resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses.Count); // Responses are still intact.
        Assert.NotEmpty(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Description);// response description string is not empty
        Assert.Single(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Extensions); // 1 supported extension still present in operation
        Assert.Empty(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema.AllOf); // allOf were merged
        Assert.Empty(resultDocument.Paths["/test/{id}"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema.Properties["id"].AnyOf); // anyOf we selected
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
        Assert.Equal(staticTemplate.ToString(), resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics.StaticTemplate.ToString());
        Assert.Null(resultingManifest.Document.Functions[1].Capabilities.ResponseSemantics);// no response semantics is added if no adaptive card
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
        Assert.Null(resultingManifest.Document.Functions[0].Capabilities.ResponseSemantics); // no response semantics is added if no schema
        Assert.NotNull(resultingManifest.Document.Functions[1].Capabilities.ResponseSemantics); // response semantics is added if response has schema
        string jsonString = $"{{\"file\": \"./adaptiveCards/{resultingManifest.Document.Functions[1].Name}.json\"}}";
        using JsonDocument doc = JsonDocument.Parse(jsonString);
        JsonElement staticTemplate = doc.RootElement.Clone();
        Assert.Equal(staticTemplate.ToString(), resultingManifest.Document.Functions[1].Capabilities.ResponseSemantics.StaticTemplate.ToString()); // adaptive card present

        // validate presence of adaptive card
        var path = Path.Combine(outputDirectory, "adaptiveCards", $"{resultingManifest.Document.Functions[1].Name}.json");
        Assert.True(File.Exists(path));

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

            var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description_m1-1.yaml");
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

            // Act
            var manifestPaths = await pluginsGenerationService.GenerateAndMergePluginManifestsAsync(openAPIDocumentDS);

            // Assert
            Assert.NotNull(manifestPaths);
            string ManifestFileName1 = "client-apiplugin.json";
            string ManifestFileNameMerged = "client-apiplugin-merged.json";
            string manifestPath1 = Path.Combine(outputDirectory, ManifestFileName1);
            string manifestPathMerged = Path.Combine(outputDirectory, ManifestFileNameMerged);
            var expectedManifestPaths = new List<string>
            {
                manifestPath1,
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
        var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description_m1-2.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath1, ManifestContent1);
        // Write the second description
        var simpleDescriptionPath2 = Path.Combine(workingDirectory, "description_m2-2.yaml");
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
        List<string> manifestPaths = await pluginsGenerationService.GenerateAndMergePluginManifestsAsync(openAPIDocumentDS);

        string ManifestFileName1 = "client-apiplugin.json";
        string OpenApiFileName1 = "client-openapi.yml";
        string ManifestFileName2 = "client2-apiplugin.json";
        string OpenApiFileName2 = "client2-openapi.yml";
        string ManifestFileNameMerged = "client-apiplugin-merged.json";

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
        Assert.Equal("test_WithId_m2", resultingManifest.Document.Functions[2].Name);
        Assert.Equal(2, resultingManifest.Document.Runtimes.Count);
        Assert.Equal(3, resultingManifest.Document.Capabilities.ConversationStarters.Count);

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

        var simpleDescriptionPath1 = Path.Combine(workingDirectory, "description_m1-3.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath1, ManifestContent1);

        var simpleDescriptionPath2 = Path.Combine(workingDirectory, "description_m2-3.yaml");
        await File.WriteAllTextAsync(simpleDescriptionPath2, ManifestContent2);

        var simpleDescriptionPath3 = Path.Combine(workingDirectory, "description_m3-3.yaml");
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

        // Act
        var manifestPaths = await pluginsGenerationService.GenerateAndMergePluginManifestsAsync(openAPIDocumentDS);

        // Assert
        Assert.NotNull(manifestPaths);
        string ManifestFileName1 = "client-apiplugin.json";
        string ManifestFileName2 = "client2-apiplugin.json";
        string ManifestFileName3 = "client3-apiplugin.json";
        string ManifestFileNameMerged = "client-apiplugin-merged.json";
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
    public async Task GenerateAndMergePluginManifestsAsync_ThrowsException_WhenNoManifestsGenerated()
    {
        // Arrange
        var mockDownloadService = new Mock<OpenApiDocumentDownloadService>(Mock.Of<HttpClient>(), Mock.Of<ILogger>());
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var generationConfiguration = new GenerationConfiguration
        {
            PluginTypes = new HashSet<PluginType> { PluginType.APIPlugin },
            OutputPath = Path.GetTempPath(),
            OpenAPIFilePath = "description_m1-1.yaml"
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
            service.GenerateAndMergePluginManifestsAsync(mockDownloadService.Object, CancellationToken.None));
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
        Assert.Null(resultingManifest.Document.Functions[1].Capabilities.ResponseSemantics);
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

    [Fact]
    public void SanitizeClientClassName_LeavesAlphanumericCharacters()
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = "Client123Name"
        });

        // Act
        var result = pluginsGenerationService.SanitizeClientClassName();

        // Assert
        Assert.Equal("Client123Name", result);
    }

    [Fact]
    public void SanitizeClientClassName_ReturnsEmptyString_WhenInputIsEmpty()
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = string.Empty
        });

        // Act
        var result = pluginsGenerationService.SanitizeClientClassName();

        // Assert
        Assert.Equal(string.Empty, result);
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
    [InlineData("ClientName", "description_m1-2.yaml", 2, "ClientName-2", "description_m2-2.yaml")]
    [InlineData("ClientName", "description_m1-3.yaml", 2, "ClientName-2", "description_m2-3.yaml")]
    [InlineData("ClientName", "description_m1-3.yaml", 3, "ClientName-3", "description_m3-3.yaml")]
    [InlineData("ClientName", "description_m1-9.yaml", 5, "ClientName-5", "description_m5-9.yaml")]
    [InlineData("MyClient", "description_m1-8.yaml", 3, "MyClient-3", "description_m3-8.yaml")]
    [InlineData("MyClient", "description_m1-8.yaml", 8, "MyClient-8", "description_m8-8.yaml")]
    public void GetNextFileName_ValidInputs_ReturnsExpectedResults(string originalClientClassName, String originalFilePath, int fileNumber, string expectedClientClassName, string expectedFilePath)
    {
        // Arrange
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(new GenerationConfiguration
        {
            ClientClassName = originalClientClassName,
            OpenAPIFilePath = originalFilePath
        });

        // Act
        var (updatedClientClassName, updatedOpenAPIFilePath) = pluginsGenerationService.GetNextFileInfo(originalClientClassName, originalFilePath, fileNumber);

        // Assert
        Assert.Equal(expectedClientClassName, updatedClientClassName);
        Assert.Equal(expectedFilePath, updatedOpenAPIFilePath);
    }

    [Fact]
    public void GetNextFileName_InvalidRegex_ThrowsException()
    {
        // Arrange
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            OpenAPIFilePath = "invalid_file_name.yaml"
        };
        var pluginsGenerationService = CreateEmptyPluginsGenerationService(generationConfiguration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pluginsGenerationService.GetNextFileInfo(generationConfiguration.ClientNamespaceName, generationConfiguration.OpenAPIFilePath, 2));
    }

    #endregion
}

