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

    [Theory]
    [InlineData("client", "client")]
    [InlineData("Budget Tracker", "BudgetTracker")]//drop the space
    [InlineData("My-Super complex() %@#$& Name", "MySupercomplexName")]//drop the space and special characters
    public async Task GeneratesManifest(string inputPluginName, string expectedPluginName)
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
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenAIPluginFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-openapi.yml")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, AppManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "color.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "outline.png")));

        // Validate the v2 plugin
        var manifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, $"{expectedPluginName.ToLower()}-apiplugin.json"));
        using var jsonDocument = JsonDocument.Parse(manifestContent);
        var resultingManifest = PluginManifestDocument.Load(jsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal($"{expectedPluginName.ToLower()}-openapi.yml", resultingManifest.Document.Runtimes.OfType<OpenApiRuntime>().First().Spec.Url);
        Assert.Equal(2, resultingManifest.Document.Functions.Count);// all functions are generated despite missing operationIds
        Assert.Equal(expectedPluginName, resultingManifest.Document.Namespace);// namespace is cleaned up.
        Assert.Empty(resultingManifest.Problems);// no problems are expected with names

        // Validate the v1 plugin
        var v1ManifestContent = await File.ReadAllTextAsync(Path.Combine(outputDirectory, OpenAIPluginFileName));
        using var v1JsonDocument = JsonDocument.Parse(v1ManifestContent);
        var v1Manifest = PluginManifestDocument.Load(v1JsonDocument.RootElement);
        Assert.NotNull(resultingManifest.Document);
        Assert.Equal($"{expectedPluginName.ToLower()}-openapi.yml", v1Manifest.Document.Api.URL);
        Assert.Empty(v1Manifest.Problems);

        // Validate the manifest file
        var appManifestFile = await File.ReadAllTextAsync(Path.Combine(outputDirectory, AppManifestFileName));
        var appManifestModelObject = JsonSerializer.Deserialize(appManifestFile, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        Assert.Equal($"com.microsoft.kiota.plugin.{expectedPluginName}", appManifestModelObject.PackageName);
        Assert.Equal(expectedPluginName, appManifestModelObject.Name.ShortName);
        Assert.Equal("Microsoft Kiota.", appManifestModelObject.Developer.Name);
        Assert.Equal("color.png", appManifestModelObject.Icons.Color);
        Assert.NotNull(appManifestModelObject.CopilotExtensions.Plugins);
        Assert.Single(appManifestModelObject.CopilotExtensions.Plugins);
        Assert.Equal(expectedPluginName, appManifestModelObject.CopilotExtensions.Plugins[0].Id);
        Assert.Equal($"{expectedPluginName.ToLower()}-apiplugin.json", appManifestModelObject.CopilotExtensions.Plugins[0].File);
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
  description: A sample test api
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
        Directory.CreateDirectory(outputDirectory);
        var preExistingManifestContents = @"{
    ""$schema"": ""https://developer.microsoft.com/json-schemas/teams/v1.17/MicrosoftTeams.schema.json"",
    ""manifestVersion"": ""1.17"",
    ""version"": ""1.0.0"",
    ""id"": ""%MICROSOFT-APP-ID%"",
    ""localizationInfo"": {
        ""defaultLanguageTag"": ""en-us"",
        ""additionalLanguages"": [
            {
                ""languageTag"": ""es-es"",
                ""file"": ""en-us.json""
            }
        ]
    },
    ""developer"": {
        ""name"": ""Publisher Name"",
        ""websiteUrl"": ""https://example.com/"",
        ""privacyUrl"": ""https://example.com/privacy"",
        ""termsOfUseUrl"": ""https://example.com/app-tos"",
        ""mpnId"": ""1234567890""
    },
    ""name"": {
        ""short"": ""Name of your app"",
        ""full"": ""Full name of app, if longer than 30 characters""
    },
    ""description"": {
        ""short"": ""Short description of your app (<= 80 chars)"",
        ""full"": ""Full description of your app (<= 4000 chars)""
    },
    ""icons"": {
        ""outline"": ""A relative path to a transparent .png icon — 32px X 32px"",
        ""color"": ""A relative path to a full color .png icon — 192px X 192px""
    },
    ""accentColor"": ""A valid HTML color code."",
    ""configurableTabs"": [
        {
            ""configurationUrl"": ""https://contoso.com/teamstab/configure"",
            ""scopes"": [
                ""team"",
                ""groupChat""
            ],
            ""canUpdateConfiguration"": true,
            ""context"": [
                ""channelTab"",
                ""privateChatTab"",
                ""meetingChatTab"",
                ""meetingDetailsTab"",
                ""meetingSidePanel"",
                ""meetingStage""
            ],
            ""sharePointPreviewImage"": ""Relative path to a tab preview image for use in SharePoint — 1024px X 768"",
            ""supportedSharePointHosts"": [
                ""sharePointFullPage"",
                ""sharePointWebPart""
            ]
        }
    ],
    ""staticTabs"": [
        {
            ""entityId"": ""unique Id for the page entity"",
            ""scopes"": [
                ""personal""
            ],
            ""context"": [
                ""personalTab"",
                ""channelTab""
            ],
            ""name"": ""Display name of tab"",
            ""contentUrl"": ""https://contoso.com/content (displayed in Teams canvas)"",
            ""websiteUrl"": ""https://contoso.com/content (displayed in web browser)"",
            ""searchUrl"": ""https://contoso.com/content (displayed in web browser)""
        }
    ],
    ""supportedChannelTypes"": [
        ""sharedChannels"",
        ""privateChannels""
    ],
    ""bots"": [
        {
            ""botId"": ""%MICROSOFT-APP-ID-REGISTERED-WITH-BOT-FRAMEWORK%"",
            ""scopes"": [
                ""team"",
                ""personal"",
                ""groupChat""
            ],
            ""needsChannelSelector"": false,
            ""isNotificationOnly"": false,
            ""supportsFiles"": true,
            ""supportsCalling"": false,
            ""supportsVideo"": true,
            ""commandLists"": [
                {
                    ""scopes"": [
                        ""team"",
                        ""groupChat""
                    ],
                    ""commands"": [
                        {
                            ""title"": ""Command 1"",
                            ""description"": ""Description of Command 1""
                        },
                        {
                            ""title"": ""Command 2"",
                            ""description"": ""Description of Command 2""
                        }
                    ]
                },
                {
                    ""scopes"": [
                        ""personal"",
                        ""groupChat""
                    ],
                    ""commands"": [
                        {
                            ""title"": ""Personal command 1"",
                            ""description"": ""Description of Personal command 1""
                        },
                        {
                            ""title"": ""Personal command N"",
                            ""description"": ""Description of Personal command N""
                        }
                    ]
                }
            ]
        }
    ],
    ""connectors"": [
        {
            ""connectorId"": ""GUID-FROM-CONNECTOR-DEV-PORTAL%"",
            ""scopes"": [
                ""team""
            ],
            ""configurationUrl"": ""https://contoso.com/teamsconnector/configure""
        }
    ],
    ""composeExtensions"": [
        {
            ""canUpdateConfiguration"": true,
            ""botId"": ""%MICROSOFT-APP-ID-REGISTERED-WITH-BOT-FRAMEWORK%"",
            ""commands"": [
                {
                    ""id"": ""exampleCmd1"",
                    ""title"": ""Example Command"",
                    ""type"": ""query"",
                    ""context"": [
                        ""compose"",
                        ""commandBox""
                    ],
                    ""description"": ""Command Description; e.g., Search on the web"",
                    ""initialRun"": true,
                    ""fetchTask"": false,
                    ""parameters"": [
                        {
                            ""name"": ""keyword"",
                            ""title"": ""Search keywords"",
                            ""inputType"": ""choiceset"",
                            ""description"": ""Enter the keywords to search for"",
                            ""value"": ""Initial value for the parameter"",
                            ""choices"": [
                                {
                                    ""title"": ""Title of the choice"",
                                    ""value"": ""Value of the choice""
                                }
                            ]
                        }
                    ]
                },
                {
                    ""id"": ""exampleCmd2"",
                    ""title"": ""Example Command 2"",
                    ""type"": ""action"",
                    ""context"": [
                        ""message""
                    ],
                    ""description"": ""Command Description; e.g., Add a customer"",
                    ""initialRun"": true,
                    ""fetchTask"": false ,
                    ""parameters"": [
                        {
                            ""name"": ""custinfo"",
                            ""title"": ""Customer name"",
                            ""description"": ""Enter a customer name"",
                            ""inputType"": ""text""
                        }
                    ]
                },
                {
                    ""id"": ""exampleCmd3"",
                    ""title"": ""Example Command 3"",
                    ""type"": ""action"",
                    ""context"": [
                        ""compose"",
                        ""commandBox"",
                        ""message""
                    ],
                    ""description"": ""Command Description; e.g., Add a customer"",
                    ""fetchTask"": false,
                    ""taskInfo"": {
                        ""title"": ""Initial dialog title"",
                        ""width"": ""Dialog width"",
                        ""height"": ""Dialog height"",
                        ""url"": ""Initial webview URL""
                    }
                }
            ],
            ""messageHandlers"": [
                {
                    ""type"": ""link"",
                    ""value"": {
                        ""domains"": [
                            ""mysite.someplace.com"",
                            ""othersite.someplace.com""
                        ],
                        ""supportsAnonymizedPayloads"": false
                    }
                }
            ]
        }
    ],
    ""permissions"": [
        ""identity"",
        ""messageTeamMembers""
    ],
    ""devicePermissions"": [
        ""geolocation"",
        ""media"",
        ""notifications"",
        ""midi"",
        ""openExternal""
    ],
    ""validDomains"": [
        ""contoso.com"",
        ""mysite.someplace.com"",
        ""othersite.someplace.com""
    ],
    ""webApplicationInfo"": {
        ""id"": ""AAD App ID"",
        ""resource"": ""Resource URL for acquiring auth token for SSO""
    },
    ""authorization"": {
        ""permissions"": {
            ""resourceSpecific"": [
                {
                    ""type"": ""Application"",
                    ""name"": ""ChannelSettings.Read.Group""
                },
                {
                    ""type"": ""Delegated"",
                    ""name"": ""ChannelMeetingParticipant.Read.Group""
                }
            ]
        }
    },
    ""showLoadingIndicator"": false,
    ""isFullScreen"": false,
    ""activities"": {
        ""activityTypes"": [
            {
                ""type"": ""taskCreated"",
                ""description"": ""Task created activity"",
                ""templateText"": ""<team member> created task <taskId> for you""
            },
            {
                ""type"": ""userMention"",
                ""description"": ""Personal mention activity"",
                ""templateText"": ""<team member> mentioned you""
            }
        ]
    },
    ""defaultBlockUntilAdminAction"": true,
    ""publisherDocsUrl"": ""https://example.com/app-info"",
    ""defaultInstallScope"": ""meetings"",
    ""defaultGroupCapability"": {
        ""meetings"": ""tab"",
        ""team"": ""bot"",
        ""groupChat"": ""bot""
    },
    ""configurableProperties"": [
        ""name"",
        ""shortDescription"",
        ""longDescription"",
        ""smallImageUrl"",
        ""largeImageUrl"",
        ""accentColor"",
        ""developerUrl"",
        ""privacyUrl"",
        ""termsOfUseUrl""
    ],
    ""subscriptionOffer"": {
        ""offerId"": ""publisherId.offerId""
    },
    ""meetingExtensionDefinition"": {
        ""scenes"": [
            {
                ""id"": ""9082c811-7e6a-4174-8173-6ccd57d377e6"",
                ""name"": ""Getting started sample"",
                ""file"": ""scenes/sceneMetadata.json"",
                ""preview"": ""scenes/scenePreview.png"",
                ""maxAudience"": 15,
                ""seatsReservedForOrganizersOrPresenters"": 0
            },
            {
                ""id"": ""afeaed22-f89b-48e1-98b4-46a514344e4a"",
                ""name"": ""Sample-1"",
                ""file"": ""scenes/sceneMetadata.json"",
                ""preview"": ""scenes/scenePreview.png"",
                ""maxAudience"": 15,
                ""seatsReservedForOrganizersOrPresenters"": 3
            }
        ]
    }
}";
        var preExistingManifestPath = Path.Combine(outputDirectory, "manifest.json");
        await File.WriteAllTextAsync(preExistingManifestPath, preExistingManifestContents);
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

        // Assert manifest exists before generation and is parsable
        Assert.True(File.Exists(Path.Combine(outputDirectory, "manifest.json")));
        var originalManifestFile = await File.ReadAllTextAsync(Path.Combine(outputDirectory, AppManifestFileName));
        var originalAppManifestModelObject = JsonSerializer.Deserialize<AppManifestModel>(originalManifestFile, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        Assert.Null(originalAppManifestModelObject.PackageName);// package wasn't present
        Assert.Equal("Name of your app", originalAppManifestModelObject.Name.ShortName); // app name is same
        Assert.Equal("Publisher Name", originalAppManifestModelObject.Developer.Name); // app name is same
        Assert.Null(originalAppManifestModelObject.CopilotExtensions?.Plugins); // no plugins present

        // Run the plugin generation
        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "color.png"))); // manifest already existed and specifed the path to a file, so we did not add it.
        Assert.False(File.Exists(Path.Combine(outputDirectory, "outline.png")));// manifest already existed and specifed the path to a file, so we did not add it.
        Assert.True(File.Exists(Path.Combine(outputDirectory, "manifest.json")));// Assert manifest exists after generation

        // Validate the manifest file
        var appManifestFile = await File.ReadAllTextAsync(Path.Combine(outputDirectory, AppManifestFileName));
        var appManifestModelObject = JsonSerializer.Deserialize(appManifestFile, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        Assert.Null(appManifestModelObject.PackageName);// package wasn't present
        Assert.Equal("Name of your app", appManifestModelObject.Name.ShortName); // app name is same
        Assert.Equal("Publisher Name", originalAppManifestModelObject.Developer.Name); // developer name is same
        Assert.NotNull(appManifestModelObject.CopilotExtensions);
        Assert.NotNull(appManifestModelObject.CopilotExtensions.Plugins);
        Assert.Single(appManifestModelObject.CopilotExtensions.Plugins);//one plugin present
        Assert.Equal("client", appManifestModelObject.CopilotExtensions.Plugins[0].Id);
        Assert.Equal(ManifestFileName, appManifestModelObject.CopilotExtensions.Plugins[0].File);
        var rootJsonElement = JsonDocument.Parse(appManifestFile).RootElement;
        Assert.True(rootJsonElement.TryGetProperty("subscriptionOffer", out _));// no loss of information
        Assert.True(rootJsonElement.TryGetProperty("meetingExtensionDefinition", out _));// no loss of information
        Assert.True(rootJsonElement.TryGetProperty("activities", out _));// no loss of information
        Assert.True(rootJsonElement.TryGetProperty("devicePermissions", out _));// no loss of information
        Assert.True(rootJsonElement.TryGetProperty("composeExtensions", out _));// no loss of information
    }

    [Fact]
    public async Task GeneratesManifestAndUpdatesExistingAppManifestWithExistingPlugins()
    {
        var simpleDescriptionContent = @"openapi: 3.0.0
info:
  termsOfService: http://example.com/terms/
  contact:
    name: API Support
    email: support@example.com
    url: http://example.com/support
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
        Directory.CreateDirectory(outputDirectory);
        var preExistingManifestContents = @"{
    ""$schema"": ""https://developer.microsoft.com/json-schemas/teams/vDevPreview/MicrosoftTeams.schema.json"",
    ""manifestVersion"": ""devPreview"",
    ""version"": ""1.0.0"",
    ""id"": ""<generated_GUID>"",
    ""developer"": {
        ""name"": ""Test Name"",
        ""websiteUrl"": ""<Defaults to `contact.url` from the OpenAPI document. If the `contact.url` is not available, it defaults to `https://www.example.com/contact/`>"",
        ""privacyUrl"": ""<Defaults to `x-privacy-policy-url` extension from the OpenAPI document. If the `x-privacy-policy-url` is not available, it defaults to `https://www.example.com/privacy/`>"",
        ""termsOfUseUrl"": ""<Defaults to `termsOfService` from the OpenAPI document. If the `termsOfService` is not available, it defaults to `https://www.example.com/terms/`>""
    },
    ""packageName"": ""com.microsoft.kiota.plugin.client"",
    ""name"": {
        ""short"": ""client"",
        ""full"": ""API Plugin <plugin_name> for <OpenAPI document title>""
    },
   ""description"": {
        ""short"": ""API Plugin for <description from the OpenAPI document>. If the description is not available, it defaults to `API Plugin for <OpenAPI document title>`"",
        ""full"": ""API Plugin for <description from the OpenAPI document>. If the description is not available, it defaults to `API Plugin for <OpenAPI document title>`""
    },
    ""icons"": {
        ""color"": ""color.png"", 
        ""outline"": ""outline.png""
    },
   ""accentColor"": ""#FFFFFF"",
   ""copilotExtensions"": {
        ""plugins"": [
            {
                ""id"": ""client"",
                ""file"": ""dummyFile.json""
            }
        ],
        ""declarativeCopilots"": [
            {
                ""id"": ""client"",
                ""file"": ""dummyFile.json""
            }
        ]
    }
}";
        var preExistingManifestPath = Path.Combine(outputDirectory, "manifest.json");
        await File.WriteAllTextAsync(preExistingManifestPath, preExistingManifestContents);
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

        // Assert manifest exists before generation and is parsable
        Assert.True(File.Exists(Path.Combine(outputDirectory, "manifest.json")));
        var originalManifestFile = await File.ReadAllTextAsync(Path.Combine(outputDirectory, AppManifestFileName));
        var originalAppManifestModelObject = JsonSerializer.Deserialize(originalManifestFile, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        Assert.Equal("com.microsoft.kiota.plugin.client", originalAppManifestModelObject.PackageName);// package was present
        Assert.NotNull(originalAppManifestModelObject.CopilotExtensions);
        Assert.NotNull(originalAppManifestModelObject.CopilotExtensions.Plugins);
        Assert.Single(originalAppManifestModelObject.CopilotExtensions.Plugins);//one plugin present
        Assert.Equal("dummyFile.json", originalAppManifestModelObject.CopilotExtensions.Plugins[0].File); // no plugins present
        Assert.NotNull(originalAppManifestModelObject.CopilotExtensions.DeclarativeCopilots);
        Assert.Single(originalAppManifestModelObject.CopilotExtensions.DeclarativeCopilots);// one declarative copilot present
        Assert.Equal("dummyFile.json", originalAppManifestModelObject.CopilotExtensions.DeclarativeCopilots[0].File); // no plugins present


        // Run the plugin generation
        var pluginsGenerationService = new PluginsGenerationService(openApiDocument, urlTreeNode, generationConfiguration, workingDirectory);
        await pluginsGenerationService.GenerateManifestAsync();

        Assert.True(File.Exists(Path.Combine(outputDirectory, ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, OpenApiFileName)));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "manifest.json")));// Assert manifest exists after generation
        Assert.False(File.Exists(Path.Combine(outputDirectory, "color.png"))); // manifest already existed and specifed the path to a file, so we did not add it.
        Assert.False(File.Exists(Path.Combine(outputDirectory, "outline.png")));// manifest already existed and specifed the path to a file, so we did not add it.

        // Validate the manifest file
        var appManifestFile = await File.ReadAllTextAsync(Path.Combine(outputDirectory, AppManifestFileName));
        var appManifestModelObject = JsonSerializer.Deserialize(appManifestFile, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        Assert.Equal("com.microsoft.kiota.plugin.client", originalAppManifestModelObject.PackageName);// package was present
        Assert.Equal("client", appManifestModelObject.Name.ShortName); // app name is same
        Assert.Equal("Test Name", originalAppManifestModelObject.Developer.Name); // developer name is same
        Assert.Equal("client", appManifestModelObject.CopilotExtensions.Plugins[0].Id);
        Assert.Equal(ManifestFileName, appManifestModelObject.CopilotExtensions.Plugins[0].File);// file name is updated
        Assert.Single(appManifestModelObject.CopilotExtensions.DeclarativeCopilots);// we didn't erase the existing declarative copilots
        Assert.Equal("dummyFile.json", appManifestModelObject.CopilotExtensions.DeclarativeCopilots[0].File); // no plugins present
    }
    [Fact]
    public async Task DoesNotGenerateEmptyPluginOrDeclarativeCopilots()
    {
        var manifestModel = new AppManifestModel
        {
            CopilotExtensions = new CopilotExtensions
            {
            }
        };

        using var appManifestStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(appManifestStream, manifestModel, PluginsGenerationService.AppManifestModelGenerationContext.AppManifestModel);
        appManifestStream.Seek(0, SeekOrigin.Begin);
        var stringRepresentation = await new StreamReader(appManifestStream).ReadToEndAsync();

        Assert.DoesNotContain("\"plugins\":", stringRepresentation);
        Assert.DoesNotContain("\"declarativeCopilots\":", stringRepresentation);
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
      x-random-extension: true
      responses:
        '200':
          description: test
        '400':
          description: client error response
  /test/{id}:
    get:
      description: description for test path with id
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
        Assert.True(File.Exists(Path.Combine(outputDirectory, "color.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "outline.png")));

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
        Assert.Single(resultDocument.Paths["/test"].Operations[OperationType.Get].Responses); // other responses are removed from the document
        Assert.NotEmpty(resultDocument.Paths["/test"].Operations[OperationType.Get].Responses["2XX"].Description); // response description string is not empty
        Assert.Empty(resultDocument.Paths["/test"].Operations[OperationType.Get].Extensions); // NO UNsupported extension
        Assert.Single(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses); // 2 responses originally
        Assert.NotEmpty(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Responses["2XX"].Description);// response description string is not empty
        Assert.Single(resultDocument.Paths["/test/{id}"].Operations[OperationType.Get].Extensions); // 1 supported extension still present in operation
    }
}
