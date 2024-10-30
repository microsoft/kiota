using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Plugins;
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
  - url: https://localhost/
    description: There's no place like home
paths:
  /test:
    get:
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
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openAPIDocumentDS = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
            PluginTypes = [PluginType.APIPlugin, PluginType.APIManifest, PluginType.OpenAI],
            ClientClassName = inputPluginName,
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
        await pluginsGenerationService.GenerateManifestAsync();

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

    [Theory]
    [InlineData("client", "client")]
    [InlineData("Budget Tracker", "BudgetTracker")]//drop the space
    [InlineData("My-Super complex() %@#$& Name", "MySupercomplexName")]//drop the space and special characters
    public async Task GenerateManifestAsyncFailsOnInvalidOpenApiFile(string inputPluginName, string expectedPluginName)
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
      summary: summary for test path
      description: description for test path
      security:
        - OAuth2: [read]
          OpenID: []
      responses:
        '200':
          description: test
  /test/{id}:
    get:
      summary: Summary for test path with id that is longer than 50 characters 
      description: description for test path with id
      operationId: test.WithId
      security:
        - BasicAuth: []
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
components:
  securitySchemes:
    BasicAuth:
      type: http
      scheme: basic

    BearerAuth:
      type: http
      scheme: bearer

    ApiKeyAuth:
      type: apiKey
      in: header
      name: X-API-Key

    OpenID:
      type: openIdConnect
      openIdConnectUrl: https://example.com/.well-known/openid-configuration

    OAuth2:
      type: oauth2
      flows:
        implicit:
          authorizationUrl: https://example.com/api/oauth/dialog
          scopes:
            write: modify pets in your account
            read: read your pets";
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
            ClientClassName = inputPluginName,
            ApiRootUrl = "http://localhost/", //Kiota builder would set this for us
        };
        var (openAPIDocumentStream, _) = await openAPIDocumentDS.LoadStreamAsync(simpleDescriptionPath, generationConfiguration, null, false);
        var openApiDocument = await openAPIDocumentDS.GetDocumentFromStreamAsync(openAPIDocumentStream, generationConfiguration);
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await pluginsGenerationService.GenerateManifestAsync());
        Assert.Contains("OpenApi document validation failed", exception.Message);
        Assert.Contains("Server URL must use HTTPS protocol", exception.Message);
        Assert.Contains("Only Authorization Code flow is allowed for OAuth2", exception.Message);
        Assert.Contains("Operation cannot have more than one security scheme", exception.Message);
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
  - url: https://localhost/
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
  - url: https://localhost/
    description: There's no place like home
paths:
  /test:
    get:
      description: description for test path
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
        Assert.Equal(2, resultingManifest.Document.Capabilities.ConversationStarters.Count);// conversation starters are generated for each function
        Assert.Empty(resultingManifest.Problems);// no problems are expected with names

        var openApiReader = new OpenApiStreamReader();

        // Validate the original file.
        var originalOpenApiFile = File.OpenRead(simpleDescriptionPath);
        var originalDocument = openApiReader.Read(originalOpenApiFile, out var originalDiagnostic);
        Assert.Empty(originalDiagnostic.Errors);

        Assert.Equal(originalDocument.Paths["/test"].Operations[OperationType.Get].Description, resultingManifest.Document.Functions[0].Description);// pulls from description
        Assert.Equal(originalDocument.Paths["/test/{id}"].Operations[OperationType.Get].Summary, resultingManifest.Document.Functions[1].Description);// pulls from summary
        Assert.Single(originalDocument.Components.Schemas);// one schema originally
        Assert.Single(originalDocument.Extensions); // single unsupported extension at root
        Assert.Equal(2, originalDocument.Paths.Count); // document has only two paths
        Assert.Equal(2, originalDocument.Paths["/test"].Operations[OperationType.Get].Responses.Count); // 2 responses originally
        Assert.Single(originalDocument.Paths["/test"].Operations[OperationType.Get].Extensions); // 1 UNsupported extension
        Assert.Equal(2, originalDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses.Count); // 2 responses originally
        Assert.Single(originalDocument.Paths["/test/{id}"].Operations[OperationType.Get].Extensions); // 1 supported extension

        // Validate the output open api file
        var resultOpenApiFile = File.OpenRead(Path.Combine(outputDirectory, OpenApiFileName));
        var resultDocument = openApiReader.Read(resultOpenApiFile, out var diagnostic);
        Assert.Empty(diagnostic.Errors);

        // Assertions / validations
        Assert.Empty(resultDocument.Components.Schemas);// no schema is referenced. so ensure they are all removed
        Assert.Empty(resultDocument.Extensions); // no extension at root (unsupported extension is removed)
        Assert.Equal(2, resultDocument.Paths.Count); // document has only two paths
        Assert.Equal(originalDocument.Paths["/test"].Operations[OperationType.Get].Responses.Count, resultDocument.Paths["/test"].Operations[OperationType.Get].Responses.Count); // Responses are still intact.
        Assert.NotEmpty(resultDocument.Paths["/test"].Operations[OperationType.Get].Responses["200"].Description); // response description string is not empty
        Assert.Empty(resultDocument.Paths["/test"].Operations[OperationType.Get].Extensions); // NO UNsupported extension
        Assert.Equal(originalDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses.Count, resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses.Count); // Responses are still intact.
        Assert.NotEmpty(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses["200"].Description);// response description string is not empty
        Assert.Single(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Extensions); // 1 supported extension still present in operation
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
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key0, in: header }, apiKey1: {type: apiKey, name: x-api-key1, in: header }}}",
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
            // security requirement in root object
            // TODO: Revisit when https://github.com/microsoft/OpenAPI.NET/issues/1797 is fixed
            // {
            //     "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key, in: header }}}",
            //     "security: [apiKey0: []]", string.Empty, null, resultingManifest =>
            //     {
            //         Assert.NotNull(resultingManifest.Document);
            //         Assert.Empty(resultingManifest.Problems);
            //         Assert.NotEmpty(resultingManifest.Document.Runtimes);
            //         var auth0 = resultingManifest.Document.Runtimes[0].Auth;
            //         Assert.IsType<ApiKeyPluginVault>(auth0);
            //         Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
            //         Assert.Equal("{apiKey0_REGISTRATION_ID}", ((ApiKeyPluginVault)auth0!).ReferenceId);
            //     }
            // },
            // auth provided in config overrides openapi file auth
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key, in: header }}}",
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
                "{securitySchemes: {httpBearer0: {type: http, scheme: bearer}}}",
                string.Empty, "security: [httpBearer0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<ApiKeyPluginVault>(auth0);
                    Assert.Equal(AuthType.ApiKeyPluginVault, auth0?.Type);
                    Assert.Equal("{httpBearer0_REGISTRATION_ID}", ((ApiKeyPluginVault)auth0!).ReferenceId);
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
                "{securitySchemes: {oauth2_0: {type: oauth2, flows: {}}}}",
                string.Empty, "security: [oauth2_0: []]", null, resultingManifest =>
                {
                    Assert.NotNull(resultingManifest.Document);
                    Assert.Empty(resultingManifest.Problems);
                    Assert.NotEmpty(resultingManifest.Document.Runtimes);
                    var auth0 = resultingManifest.Document.Runtimes[0].Auth;
                    Assert.IsType<OAuthPluginVault>(auth0);
                    Assert.Equal(AuthType.OAuthPluginVault, auth0?.Type);
                    Assert.Equal("{oauth2_0_CONFIGURATION_ID}", ((OAuthPluginVault)auth0!).ReferenceId);
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
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openApiDocumentDs = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
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
            new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
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
            Directory.Delete(outputDirectory);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static TheoryData<string, string, string, PluginAuthConfiguration, Func<Func<Task>, Task>>
        SecurityInformationFail()
    {
        return new TheoryData<string, string, string, PluginAuthConfiguration, Func<Func<Task>, Task>>
        {
            // multiple security schemes in operation object
            {
                "{securitySchemes: {apiKey0: {type: apiKey, name: x-api-key0, in: header}, apiKey1: {type: apiKey, name: x-api-key1, in: header}}}",
                string.Empty, "security: [apiKey0: [], apiKey1: []]", null, async (action) =>
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    {
                        await action();
                    });
                }
            },
            // Unsupported security scheme (http basic)
            {
                "{securitySchemes: {httpBasic0: {type: http, scheme: basic}}}",
                string.Empty, "security: [httpBasic0: []]", null, async (action) =>
                {
                    await Assert.ThrowsAsync<UnsupportedSecuritySchemeException>(async () =>
                    {
                        await action();
                    });
                }
            },
        };
    }

    [Theory]
    [MemberData(nameof(SecurityInformationFail))]
    public async Task FailsToGeneratesManifestWithInvalidAuthAsync(string securitySchemesComponent, string rootSecurity,
        string operationSecurity, PluginAuthConfiguration pluginAuthConfiguration, Func<Func<Task>, Task> assertions)
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
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openApiDocumentDs = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
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
            new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);

        await assertions(async () =>
        {
            await pluginsGenerationService.GenerateManifestAsync();
        });
        // cleanup
        try
        {
            Directory.Delete(outputDirectory);
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
                                    apiKey0: { type: apiKey, name: x-api-key0, in: header },
                                    apiKey1: { type: apiKey, name: x-api-key1, in: header },
                                  },
                                }
                              """;
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var simpleDescriptionPath = Path.Combine(workingDirectory) + "description.yaml";
        await File.WriteAllTextAsync(simpleDescriptionPath, apiDescription);
        var mockLogger = new Mock<ILogger<PluginsGenerationService>>();
        var openApiDocumentDs = new OpenApiDocumentDownloadService(_httpClient, mockLogger.Object);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var generationConfiguration = new GenerationConfiguration
        {
            OutputPath = outputDirectory,
            OpenAPIFilePath = "openapiPath",
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
            new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
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
        Assert.Equal("{apiKey0_REGISTRATION_ID}", ((ApiKeyPluginVault)auth0!).ReferenceId);
        var auth1 = resultingManifest.Document.Runtimes[1].Auth;
        Assert.IsType<ApiKeyPluginVault>(auth1);
        Assert.Equal(AuthType.ApiKeyPluginVault, auth1.Type);
        Assert.Equal("{apiKey1_REGISTRATION_ID}", ((ApiKeyPluginVault)auth1!).ReferenceId);
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
                    var schema = slicedDocument.Paths["/test"].Operations[OperationType.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal("string", schema.Type);
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
                    var schema = slicedDocument.Paths["/test"].Operations[OperationType.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal("object", schema.Type);
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
                    var schema = slicedDocument.Paths["/test"].Operations[OperationType.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal("object", schema.Type);
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
                    var schema = slicedDocument.Paths["/test"].Operations[OperationType.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal("object", schema.Type);
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
                    var schema = slicedDocument.Paths["/test"].Operations[OperationType.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal("object", schema.Type);
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
                    var schema = slicedDocument.Paths["/test"].Operations[OperationType.Post].RequestBody
                        .Content["application/json"].Schema;
                    Assert.Equal("object", schema.Type);
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

        try
        {
            // Validate the sliced openapi
            var slicedApiContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, OpenApiFileName));
            var r = new OpenApiStringReader();
            var slicedDocument = r.Read(slicedApiContent, out var diagnostic);
            assertions(slicedDocument, diagnostic);
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
}
