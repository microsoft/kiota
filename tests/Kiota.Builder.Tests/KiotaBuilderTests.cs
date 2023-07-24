using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;

using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests;
public class KiotaBuilderTests : IDisposable
{
    private readonly List<string> _tempFiles = new();
    public void Dispose()
    {
        foreach (var file in _tempFiles)
            File.Delete(file);
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    [InlineData("https://graph.microsoft.com/description.yaml", "/v1.0", "https://graph.microsoft.com/v1.0")]
    [InlineData("/home/vsts/a/s/1", "/v1.0", "/v1.0")]
    [InlineData("https://graph.microsoft.com/docs/description.yaml", "../v1.0", "https://graph.microsoft.com/v1.0")]
    [InlineData("https://graph.microsoft.com/description.yaml", "https://graph.microsoft.com/v1.0", "https://graph.microsoft.com/v1.0")]
    [Theory]
    public async Task SupportsRelativeServerUrl(string descriptionUrl, string serverRelativeUrl, string expected)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: {serverRelativeUrl}
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = descriptionUrl }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Equal(expected, constructor.BaseUrl);
    }
    [Fact]
    public async Task DeduplicatesHostNames()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: http://api.funtranslations.com
  - url: https://api.funtranslations.com
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json" }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Equal("https://api.funtranslations.com", constructor.BaseUrl);
    }
    [Fact]
    public async Task DeduplicatesHostNamesWithOpenAPI2()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, @$"swagger: 2.0
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
schemes:
  - https
  - http
host: api.funtranslations.com
basePath: /
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json" }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Equal("https://api.funtranslations.com", constructor.BaseUrl);
    }
    private readonly HttpClient _httpClient = new();
    [Fact]
    public async Task ParsesEnumDescriptions()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/StorageAccount'
components:
  schemas:
    StorageAccount:
      type: object
      properties:
        accountType:
          $ref: '#/components/schemas/StorageAccountType'
    StorageAccountType:
      type: string
      enum:
        - +1
        - -1
        - Standard_LRS
        - Standard_ZRS
        - Standard_GRS
        - Standard_RAGRS
        - Premium_LRS
        - Premium_LRS
      x-ms-enum:
        name: AccountType
        modelAsString: false
        values:
          - value: +1
          - value: -1
          - value: Standard_LRS
            description: Locally redundant storage.
            name: StandardLocalRedundancy
          - value: Standard_ZRS
            description: Zone-redundant storage.
          - value: Standard_GRS
            name: StandardGeoRedundancy
          - value: Standard_RAGRS
          - value: Premium_LRS");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        var enumDef = modelsNS.FindChildByName<CodeEnum>("StorageAccountType", false);
        Assert.NotNull(enumDef);
        var firstOption = enumDef.Options.First();
        Assert.Equal("+1", firstOption.SerializationName);
        Assert.Equal("plus_1", firstOption.Name);
        Assert.Empty(firstOption.Documentation.Description);
        var secondOption = enumDef.Options.ElementAt(1);
        Assert.Equal("-1", secondOption.SerializationName);
        Assert.Equal("minus_1", secondOption.Name);
        Assert.Empty(secondOption.Documentation.Description);
        var thirdOption = enumDef.Options.ElementAt(2);
        Assert.Equal("Standard_LRS", thirdOption.SerializationName);
        Assert.Equal("StandardLocalRedundancy", thirdOption.Name);
        Assert.NotEmpty(thirdOption.Documentation.Description);
        Assert.Single(enumDef.Options.Where(static x => x.Name.Equals("Premium_LRS", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task TrimsInheritanceUnusedModels()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
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
          type: string
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.auditEvent': '#/components/schemas/microsoft.graph.auditEvent'
          '#microsoft.graph.directoryObject': '#/components/schemas/microsoft.graph.directoryObject'
      x-ms-discriminator-value: '#microsoft.graph.entity'
    microsoft.graph.auditEvent:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: auditEvent
          required:
            - '@odata.type'
          type: object
          properties:
            eventDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.auditEvent'
    microsoft.graph.directoryObject:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: directoryObject
          required:
            - '@odata.type'
          type: object
          properties:
            deletedDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.directoryObject'
    microsoft.graph.user:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            accountEnabled:
              type: boolean
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.user'
            mailboxSettings:
              $ref: '#/components/schemas/microsoft.graph.mailboxSettings'
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.educationUser': '#/components/schemas/microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.user'
    microsoft.graph.mailboxSettingsBase:
      title: mailboxSettingsBase
      type: object
      properties:
        premium:
          type: boolean
          nullable: true
    microsoft.graph.mailboxSettings:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.mailboxSettingsBase'
        - title: mailboxSettings
          type: object
          properties:
            antiSpamEnabled:
              type: boolean
              nullable: true
    microsoft.graph.mailboxSecuritySettings:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.mailboxSettings'
        - title: mailboxSettings
          type: object
          properties:
            encryptionAtRestEnabled:
              type: boolean
              nullable: true
    microsoft.graph.educationUser:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.user'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            pupilEnrolled:
              type: boolean
              description: 'true if the account is enabled; otherwise, false. This property is required when a user is created. Returned only on $select. Supports $filter (eq, ne, not, and in).'
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.educationUser'");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("Entity", false)); //parent type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("DirectoryObject", false)); //type in use
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("User", false)); //derived type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MailboxSettingsBase", false)); //base of a property of a derived type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MailboxSecuritySettings", false)); //derived type of a property
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MailboxSettings", false)); //property of a derived type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("EducationUser", false)); // recursive downcast
        Assert.Null(modelsNS.FindChildByName<CodeClass>("AuditEvent", false)); //unused type
    }
    [Fact]
    public async Task TrimsInheritanceUnusedModelsWithUnion()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                anyOf:
                  - $ref: '#/components/schemas/microsoft.graph.user'
                  - $ref: '#/components/schemas/microsoft.graph.educationUser'
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
          description: The unique idenfier for an entity. Read-only.
        '@odata.type':
          type: string
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.auditEvent': '#/components/schemas/microsoft.graph.auditEvent'
          '#microsoft.graph.directoryObject': '#/components/schemas/microsoft.graph.directoryObject'
      x-ms-discriminator-value: '#microsoft.graph.entity'
    microsoft.graph.auditEvent:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: auditEvent
          required:
            - '@odata.type'
          type: object
          properties:
            eventDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.auditEvent'
    microsoft.graph.directoryObject:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: directoryObject
          required:
            - '@odata.type'
          type: object
          properties:
            deletedDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              description: Date and time when this object was deleted. Always null when the object hasn't been deleted.
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.directoryObject'
    microsoft.graph.user:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            accountEnabled:
              type: boolean
              description: 'true if the account is enabled; otherwise, false. This property is required when a user is created. Returned only on $select. Supports $filter (eq, ne, not, and in).'
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.user'
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.educationUser': '#/components/schemas/microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.user'
    microsoft.graph.educationUser:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.user'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            pupilEnrolled:
              type: boolean
              description: 'true if the account is enabled; otherwise, false. This property is required when a user is created. Returned only on $select. Supports $filter (eq, ne, not, and in).'
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.educationUser'");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("Entity", false));
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("DirectoryObject", false));
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("User", false));
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("EducationUser", false));
        Assert.Null(modelsNS.FindChildByName<CodeClass>("AuditEvent", false));
    }
    private static async Task<Stream> GetDocumentStream(string document)
    {
        var ms = new MemoryStream();
        await using var tw = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);
        tw.Write(document);
        await tw.FlushAsync();
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
    [Fact]
    public async Task ParsesKiotaExtension()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
x-ms-kiota-info:
  languagesInformation:
    CSharp:
      maturityLevel: Experimental
      dependencyInstallCommand: dotnet add {0} {1}
      dependencies:
        - name: Microsoft.Graph.Core
          version: 3.0.0
servers:
  - url: https://graph.microsoft.com/v1.0");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var extensionResult = await builder.GetLanguagesInformationAsync(new CancellationToken());
        Assert.NotNull(extensionResult);
        Assert.True(extensionResult.TryGetValue("CSharp", out var csharpInfo));
        Assert.Equal("Experimental", csharpInfo.MaturityLevel.ToString());
        Assert.Equal("dotnet add {0} {1}", csharpInfo.DependencyInstallCommand);
        Assert.Single(csharpInfo.Dependencies);
        Assert.Equal("Microsoft.Graph.Core", csharpInfo.Dependencies.First().Name);
        Assert.Equal("3.0.0", csharpInfo.Dependencies.First().Version);
    }
    [Fact]
    public async Task UpdatesGenerationConfigurationFromInformation()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
x-ms-kiota-info:
  languagesInformation:
    CSharp:
      maturityLevel: Experimental
      dependencyInstallCommand: dotnet add {0} {1}
      clientClassName: GraphClient
      clientNamespaceName: Microsoft.Graph
      structuredMimeTypes:
        - application/json
        - application/xml
      dependencies:
        - name: Microsoft.Graph.Core
          version: 3.0.0
servers:
  - url: https://graph.microsoft.com/v1.0");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var configuration = new GenerationConfiguration { OpenAPIFilePath = tempFilePath, Language = GenerationLanguage.CSharp };
        var builder = new KiotaBuilder(mockLogger.Object, configuration, _httpClient);
        var treeNode = await builder.GetUrlTreeNodeAsync(new CancellationToken());
        Assert.NotNull(treeNode);
        Assert.Equal("GraphClient", configuration.ClientClassName);
        Assert.Equal("Microsoft.Graph", configuration.ClientNamespaceName);
        Assert.Contains("application/json", configuration.StructuredMimeTypes);
        Assert.Contains("application/xml", configuration.StructuredMimeTypes);
        _tempFiles.Add(tempFilePath);
    }
    [Fact]
    public async Task DoesntFailOnEmptyKiotaExtension()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var extensionResult = await builder.GetLanguagesInformationAsync(new CancellationToken());
        Assert.Null(extensionResult);
    }
    [Fact]
    public async Task GetsUrlTreeNode()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var treeNode = await builder.GetUrlTreeNodeAsync(new CancellationToken());
        Assert.NotNull(treeNode);
        Assert.Equal("/", treeNode.Segment);
        Assert.Equal("enumeration", treeNode.Children.First().Value.Segment);

        _tempFiles.Add(tempFilePath);
    }
    [Fact]
    public async Task DoesntThrowOnMissingServerForV2()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllLinesAsync(tempFilePath, new[] { "swagger: 2.0", "title: \"Todo API\"", "version: \"1.0.0\"", "host: mytodos.doesntexit", "basePath: v2", "schemes:", " - https", " - http" });
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        await builder.GenerateClientAsync(new());
        _tempFiles.Add(tempFilePath);
    }
    [Fact]
    public void Single_root_node_creates_single_request_builder_class()
    {
        var node = OpenApiUrlTreeNode.Create();
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);

        Assert.Single(codeModel.GetChildElements(true));
    }
    [Fact]
    public void Single_path_with_get_collection()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Type = "int"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);

        var rootNamespace = codeModel.GetChildElements(true).Single();
        var rootBuilder = rootNamespace.GetChildElements(true).OfType<CodeClass>().Single(e => e.Name == "Graph");
        var tasksProperty = rootBuilder.Properties.Single(e => e.Name.Equals("Tasks", StringComparison.OrdinalIgnoreCase));
        var tasksRequestBuilder = tasksProperty.Type as CodeType;
        Assert.NotNull(tasksRequestBuilder);
        var getMethod = (tasksRequestBuilder.TypeDefinition as CodeClass).Methods.Single(e => e.Name == "Get");
        var returnType = getMethod.ReturnType;
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, returnType.CollectionKind);
    }
    [Fact]
    public void OData_doubles_as_one_of()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    OneOf = new List<OpenApiSchema>{
                                                        new OpenApiSchema{
                                                            Type = "number"
                                                        },
                                                        new OpenApiSchema{
                                                            Type = "string"
                                                        },
                                                        new OpenApiSchema {
                                                            Enum = new List<IOpenApiAny> { new OpenApiString("-INF"), new OpenApiString("INF"), new OpenApiString("NaN") }
                                                        }
                                                    },
                                                    Format = "double"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void OData_doubles_as_one_of_format_inside()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    OneOf = new List<OpenApiSchema>{
                                                        new OpenApiSchema{
                                                            Type = "number",
                                                            Format = "double"
                                                        },
                                                        new OpenApiSchema{
                                                            Type = "string"
                                                        },
                                                        new OpenApiSchema {
                                                            Enum = new List<IOpenApiAny> { new OpenApiString("-INF"), new OpenApiString("INF"), new OpenApiString("NaN") }
                                                        }
                                                    },
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void OData_doubles_as_any_of()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    AnyOf = new List<OpenApiSchema>{
                                                        new OpenApiSchema{
                                                            Type = "number"
                                                        },
                                                        new OpenApiSchema{
                                                            Type = "string"
                                                        },
                                                        new OpenApiSchema {
                                                            Enum = new List<IOpenApiAny> { new OpenApiString("-INF"), new OpenApiString("INF"), new OpenApiString("NaN") }
                                                        }
                                                    },
                                                    Format = "double"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void Object_Arrays_are_supported()
    {
        var userSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "displayName", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "#/components/schemas/microsoft.graph.user"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                Type = "object",
                                                Properties = new Dictionary<string, OpenApiSchema> {
                                                    {
                                                        "value", new OpenApiSchema {
                                                            Type = "array",
                                                            Items = userSchema
                                                        }
                                                    },
                                                    {
                                                        "unknown", new OpenApiSchema {
                                                            Type = "array",
                                                            Items = new OpenApiSchema {
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.user", userSchema
                    }
                }
            }
        };
        var mockLogger = new CountLogger<KiotaBuilder>();
        var builder = new KiotaBuilder(mockLogger, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var userClass = codeModel.FindNamespaceByName("ApiSdk.models").FindChildByName<CodeClass>("user");
        Assert.NotNull(userClass);
        var userResponseClass = codeModel.FindNamespaceByName("ApiSdk.users.item").FindChildByName<CodeClass>("UsersResponse", false);
        Assert.NotNull(userResponseClass);
        var valueProp = userResponseClass.FindChildByName<CodeProperty>("value", false);
        Assert.NotNull(valueProp);
        var unknownProp = userResponseClass.FindChildByName<CodeProperty>("unknown", false);
        Assert.Null(unknownProp);
        Assert.Equal(1, mockLogger.Count.First(static x => x.Key == LogLevel.Warning).Value);
    }
    [Fact]
    public void TextPlainEndpointsAreSupported()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users/$count"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["text/plain"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                Type = "number",
                                                Format = "int32",
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderClass = codeModel.FindChildByName<CodeClass>("CountRequestBuilder");
        Assert.NotNull(requestBuilderClass);
        var executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        var methodReturnType = executorMethod.ReturnType as CodeType;
        Assert.NotNull(methodReturnType);
        Assert.Equal("integer", methodReturnType.Name);
    }
    [Fact]
    public void Supports_Path_Parameters()
    {
        var resourceActionSchema = new OpenApiSchema
        {
            Type = "object",
            Title = "resourceAction",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "allowedResourceActions", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema {
                            Type = "string"
                        }
                    }
                },
                {
                    "notAllowedResourceActions", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema {
                            Type = "string"
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "#/components/schemas/microsoft.graph.resourceAction"
            },
            UnresolvedReference = false
        };
        var permissionSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "resourceActions", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema {
                            AnyOf = new List<OpenApiSchema> {
                                resourceActionSchema,
                            }
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "#/components/schemas/microsoft.graph.rolePermission"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/deviceManagement/microsoft.graph.getEffectivePermissions(scope='{scope}')"] = new OpenApiPathItem
                {
                    Parameters = {
                        new OpenApiParameter
                        {
                            Name = "scope",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    },
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                Type = "array",
                                                Items = new OpenApiSchema {
                                                    AnyOf = new List<OpenApiSchema> {
                                                        permissionSchema,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    { "microsoft.graph.rolePermission", permissionSchema },
                    { "microsoft.graph.resourceAction", resourceActionSchema },
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var deviceManagementNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement");
        Assert.NotNull(deviceManagementNS);
        var deviceManagementRequestBuilder = deviceManagementNS.FindChildByName<CodeClass>("DeviceManagementRequestBuilder", false);
        Assert.NotNull(deviceManagementRequestBuilder);
        var getEffectivePermissionsMethod = deviceManagementRequestBuilder.FindChildByName<CodeMethod>("microsoftGraphGetEffectivePermissionsWithScope", false);
        Assert.NotNull(getEffectivePermissionsMethod);
        Assert.Single(getEffectivePermissionsMethod.Parameters);
        var getEffectivePermissionsNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement.microsoftGraphGetEffectivePermissionsWithScope");
        Assert.NotNull(getEffectivePermissionsNS);
        var getEffectivePermissionsRequestBuilder = getEffectivePermissionsNS.FindChildByName<CodeClass>("microsoftGraphGetEffectivePermissionsWithScopeRequestBuilder", false);
        Assert.NotNull(getEffectivePermissionsRequestBuilder);
        var constructorMethod = getEffectivePermissionsRequestBuilder.FindChildByName<CodeMethod>("constructor", false);
        Assert.NotNull(constructorMethod);
        Assert.Single(constructorMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
    }
    [Fact]
    public void Supports_Path_Query_And_Header_Parameters()
    {
        var resourceActionSchema = new OpenApiSchema
        {
            Type = "object",
            Title = "resourceAction",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "allowedResourceActions", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema {
                            Type = "string"
                        }
                    }
                },
                {
                    "notAllowedResourceActions", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema {
                            Type = "string"
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "#/components/schemas/microsoft.graph.resourceAction"
            },
            UnresolvedReference = false
        };
        var permissionSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "resourceActions", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema {
                            AnyOf = new List<OpenApiSchema> {
                                resourceActionSchema,
                            }
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "#/components/schemas/microsoft.graph.rolePermission"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/deviceManagement/microsoft.graph.getEffectivePermissions(scope='{scope}')"] = new OpenApiPathItem
                {
                    Parameters = {
                        new OpenApiParameter
                        {
                            Name = "scope",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter
                        {
                            Name = "select",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        },
                        new OpenApiParameter
                        {
                            Name = "If-Match",
                            In = ParameterLocation.Header,
                            Description = "ETag",
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        },
                        new OpenApiParameter
                        {
                            Name = "ConsistencyLevel",
                            In = ParameterLocation.Header,
                            Description = "Consistency level",
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        }
                    },
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                Type = "array",
                                                Items = new OpenApiSchema {
                                                    AnyOf = new List<OpenApiSchema> {
                                                        permissionSchema,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    { "microsoft.graph.rolePermission", permissionSchema },
                    { "microsoft.graph.resourceAction", resourceActionSchema },
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost", Language = GenerationLanguage.Shell }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var deviceManagementNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement");
        Assert.NotNull(deviceManagementNS);
        var deviceManagementRequestBuilder = deviceManagementNS.FindChildByName<CodeClass>("DeviceManagementRequestBuilder", false);
        Assert.NotNull(deviceManagementRequestBuilder);
        var getEffectivePermissionsMethod = deviceManagementRequestBuilder.FindChildByName<CodeMethod>("microsoftGraphGetEffectivePermissionsWithScope", false);
        Assert.NotNull(getEffectivePermissionsMethod);
        Assert.Single(getEffectivePermissionsMethod.Parameters);
        var getEffectivePermissionsNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement.microsoftGraphGetEffectivePermissionsWithScope");
        Assert.NotNull(getEffectivePermissionsNS);
        var getEffectivePermissionsRequestBuilder = getEffectivePermissionsNS.FindChildByName<CodeClass>("microsoftGraphGetEffectivePermissionsWithScopeRequestBuilder", false);
        Assert.NotNull(getEffectivePermissionsRequestBuilder);
        var constructorMethod = getEffectivePermissionsRequestBuilder.FindChildByName<CodeMethod>("constructor", false);
        Assert.NotNull(constructorMethod);
        Assert.Single(constructorMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
        var parameters = getEffectivePermissionsRequestBuilder
            .Methods
            .SingleOrDefault(static cm => cm.IsOfKind(CodeMethodKind.RequestGenerator) && cm.HttpMethod == Builder.CodeDOM.HttpMethod.Get)?
            .PathQueryAndHeaderParameters;
        Assert.Equal(4, parameters.Count());
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "IfMatch" && p.Kind == CodeParameterKind.Headers));
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "ConsistencyLevel" && p.Kind == CodeParameterKind.Headers));
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "select" && p.Kind == CodeParameterKind.QueryParameter));
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "scope" && p.Kind == CodeParameterKind.Path));
    }
    [Fact]
    public void DeduplicatesConflictingParameterNamesForCLI()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/test/{id}/results"] = new OpenApiPathItem
                {
                    Parameters = {
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        },
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Header,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        },
                    },
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                Type = "object",
                                                Properties = new Dictionary<string, OpenApiSchema>() {
                                                    { "foo", new() {
                                                            Type = "string"
                                                        }
                                                    }
                                                },
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost", Language = GenerationLanguage.Shell }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var resultsNS = codeModel.FindNamespaceByName("ApiSdk.test.item.results");
        Assert.NotNull(resultsNS);
        var resultsRequestBuilder = resultsNS.FindChildByName<CodeClass>("ResultsRequestBuilder");
        Assert.NotNull(resultsRequestBuilder);
        var parameters = resultsRequestBuilder
            .Methods
            .SingleOrDefault(cm => cm.IsOfKind(CodeMethodKind.RequestGenerator) && cm.HttpMethod == Builder.CodeDOM.HttpMethod.Get)?
            .PathQueryAndHeaderParameters;
        Assert.Equal(3, parameters.Count());
        Assert.NotNull(parameters.SingleOrDefault(p => "id-query".Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p.Kind == CodeParameterKind.QueryParameter));
        Assert.Null(parameters.SingleOrDefault(p => "id".Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p.Kind == CodeParameterKind.QueryParameter));
        Assert.NotNull(parameters.SingleOrDefault(p => "id".Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p.Kind == CodeParameterKind.Path));
        Assert.Null(parameters.SingleOrDefault(p => "id-query".Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p.Kind == CodeParameterKind.Path));
        Assert.NotNull(parameters.SingleOrDefault(p => "id-header".Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p.Kind == CodeParameterKind.Headers));
        Assert.Null(parameters.SingleOrDefault(p => "id".Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p.Kind == CodeParameterKind.Headers));
    }
    [Fact]
    public void Inline_Property_Inheritance_Is_Supported()
    {
        var resourceSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "info", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "resource"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["resource/{id}"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                Type = "object",
                                                Properties = new Dictionary<string, OpenApiSchema> {
                                                    {
                                                        "derivedResource", new OpenApiSchema {
                                                            Type = "object",
                                                            Properties = new Dictionary<string, OpenApiSchema> {
                                                                {
                                                                    "info", new OpenApiSchema {
                                                                        Type = "object",
                                                                        Properties = new Dictionary<string, OpenApiSchema> {
                                                                            {
                                                                                "title", new OpenApiSchema {
                                                                                    Type = "string",
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            },
                                                            AllOf = new List<OpenApiSchema> {
                                                                resourceSchema,
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "#/components/resource", resourceSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        builder.SetOpenApiDocument(document);
        var codeModel = builder.CreateSourceModel(node);
        var resourceClass = codeModel.FindNamespaceByName("ApiSdk.models").FindChildByName<CodeClass>("resource");
        var itemsNS = codeModel.FindNamespaceByName("ApiSdk.resource.item");
        var responseClass = itemsNS.FindChildByName<CodeClass>("ResourceResponse");
        var derivedResourceClass = itemsNS.FindChildByName<CodeClass>("ResourceResponse_derivedResource");
        var derivedResourceInfoClass = itemsNS.FindChildByName<CodeClass>("ResourceResponse_derivedResource_info");


        Assert.NotNull(resourceClass);
        Assert.NotNull(derivedResourceClass);
        Assert.NotNull(derivedResourceClass.StartBlock);
        Assert.Equal(derivedResourceClass.StartBlock.Inherits.TypeDefinition, resourceClass);
        Assert.NotNull(derivedResourceInfoClass);
        Assert.NotNull(responseClass);
    }

    [Fact]
    public void Inline_Property_Inheritance_Is_Supported2()
    {
        var resourceSchema = new OpenApiSchema
        {
            Type = "object",
            Reference = new OpenApiReference
            {
                Id = "resource"
            },
            UnresolvedReference = false
        };

        var properties = new Dictionary<string, OpenApiSchema>
        {
            { "info", new OpenApiSchema { Type = "string", } },
            { "derivedResource", new OpenApiSchema { AllOf = new List<OpenApiSchema> { resourceSchema, } } },
        };

        resourceSchema.Properties = properties;

        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["resource/{id}"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema {
                                                AllOf = new List<OpenApiSchema>()
                                                {
                                                    resourceSchema
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "#/components/resource", resourceSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        builder.SetOpenApiDocument(document);
        var codeModel = builder.CreateSourceModel(node);
        var resourceClass = codeModel.FindNamespaceByName("ApiSdk.models").FindChildByName<CodeClass>("resource");
        var itemsNS = codeModel.FindNamespaceByName("ApiSdk.resource.item");
        var responseClass = itemsNS.FindChildByName<CodeClass>("ResourceResponse");


        Assert.NotNull(resourceClass);
        Assert.Null(responseClass);
    }
    [Fact]
    public void MapsTime()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = "string",
                                                    Format = "time"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.Equal("TimeOnly", progressProp.Type.Name);
    }
    [Fact]
    public void MapsDate()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = "string",
                                                    Format = "date"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.Equal("DateOnly", progressProp.Type.Name);
    }
    [Fact]
    public void MapsDuration()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = "string",
                                                    Format = "duration"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.Equal("TimeSpan", progressProp.Type.Name);
    }
    [Fact]
    public void AddsErrorMapping()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = "string",
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["4XX"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "errorId", new OpenApiSchema{
                                                    Type = "string",
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["5XX"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "serviceErrorId", new OpenApiSchema{
                                                    Type = "string",
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "authenticationRealm", new OpenApiSchema{
                                                    Type = "string",
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        var keys = executorMethod.ErrorMappings.Select(x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.Contains("5XX", keys);
        var errorType401 = codeModel.FindChildByName<CodeClass>("tasks401Error");
        Assert.NotNull(errorType401);
        Assert.True(errorType401.IsErrorDefinition);
        Assert.NotNull(errorType401.FindChildByName<CodeProperty>("authenticationRealm"));
        var errorType4XX = codeModel.FindChildByName<CodeClass>("tasks4XXError");
        Assert.NotNull(errorType4XX);
        Assert.True(errorType4XX.IsErrorDefinition);
        Assert.NotNull(errorType4XX.FindChildByName<CodeProperty>("errorId"));
        var errorType5XX = codeModel.FindChildByName<CodeClass>("tasks5XXError");
        Assert.NotNull(errorType5XX);
        Assert.True(errorType5XX.IsErrorDefinition);
        Assert.NotNull(errorType5XX.FindChildByName<CodeProperty>("serviceErrorId"));
    }
    [Fact]
    public void IgnoresErrorCodesWithNoSchema()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = {
                [OperationType.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = "string",
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["4XX"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        },
                        ["5XX"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        },
                        ["401"] = new OpenApiResponse
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        }
                    }
                }
            }
        }, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var codeModel = builder.CreateSourceModel(node);
        var executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.Empty(executorMethod.ErrorMappings);
    }
    [Fact]
    public void DoesntAddSuffixesToErrorTypesWhenComponents()
    {
        var errorSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "errorId", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.error",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var errorResponse = new OpenApiResponse
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = errorSchema
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.error",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["tasks"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content =
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = "object",
                                                Properties = new Dictionary<string, OpenApiSchema> {
                                                    {
                                                        "progress", new OpenApiSchema{
                                                            Type = "string",
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                ["4XX"] = errorResponse,
                                ["5XX"] = errorResponse,
                                ["401"] = errorResponse
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.error", errorSchema
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "microsoft.graph.error", errorResponse
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        var keys = executorMethod.ErrorMappings.Select(x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.Contains("5XX", keys);
        var errorType = codeModel.FindChildByName<CodeClass>("Error");
        Assert.NotNull(errorType);
        Assert.True(errorType.IsErrorDefinition);
        Assert.NotNull(errorType.FindChildByName<CodeProperty>("errorId"));

        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks401Error"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks4XXError"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks5XXError"));
    }
    [Fact]
    public void DoesntAddPropertyHolderOnNonAdditionalModels()
    {
        var weatherForecastSchema = new OpenApiSchema
        {
            Type = "object",
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = "string",
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = "integer",
                        Format = "int32"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "weatherForecast",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var forecastResponse = new OpenApiResponse
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = weatherForecastSchema
                }
            },
            Reference = new OpenApiReference
            {
                Id = "weatherForecast",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["weatherforecast"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = forecastResponse
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "weatherForecast", weatherForecastSchema
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "weatherForecast", forecastResponse
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var weatherType = codeModel.FindChildByName<CodeClass>("WeatherForecast");
        Assert.NotNull(weatherType);
        Assert.Empty(weatherType.StartBlock.Implements.Where(x => x.Name.Equals("IAdditionalDataHolder", StringComparison.OrdinalIgnoreCase)));
        Assert.Empty(weatherType.Properties.Where(x => x.IsOfKind(CodePropertyKind.AdditionalData)));
    }
    [Fact]
    public void SquishesLonelyNullables()
    {
        var uploadSessionSchema = new OpenApiSchema
        {
            Type = "object",
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = "string",
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = "integer",
                        Format = "int32"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.uploadSession",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["createUploadSession"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, OpenApiMediaType> {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Nullable = true,
                                                AnyOf = new List<OpenApiSchema> {
                                                    uploadSessionSchema
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.uploadSession", uploadSessionSchema
                    }
                },
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var responseClass = codeModel.FindChildByName<CodeClass>("CreateUploadSessionResponse");
        Assert.Null(responseClass);
        var sessionClass = codeModel.FindChildByName<CodeClass>("UploadSession");
        Assert.NotNull(sessionClass);
        var requestBuilderClass = codeModel.FindChildByName<CodeClass>("createUploadSessionRequestBuilder");
        Assert.NotNull(requestBuilderClass);
        var executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        Assert.True(executorMethod.ReturnType is CodeType); // not union
        Assert.Null(codeModel.FindChildByName<CodeClass>("createUploadSessionResponseMember1"));
    }
    [Fact]
    public void SquishesLonelyNullablesBothAnyOf()
    {
        var uploadSessionSchema = new OpenApiSchema
        {
            Type = "object",
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = "string",
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = "integer",
                        Format = "int32"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.uploadSession",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["createUploadSession"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, OpenApiMediaType> {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                AnyOf = new List<OpenApiSchema> {
                                                    uploadSessionSchema,
                                                    new OpenApiSchema {
                                                        Nullable = true,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.uploadSession", uploadSessionSchema
                    }
                },
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var responseClass = codeModel.FindChildByName<CodeClass>("CreateUploadSessionResponse");
        Assert.Null(responseClass);
        var sessionClass = codeModel.FindChildByName<CodeClass>("UploadSession");
        Assert.NotNull(sessionClass);
        var requestBuilderClass = codeModel.FindChildByName<CodeClass>("createUploadSessionRequestBuilder");
        Assert.NotNull(requestBuilderClass);
        var executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        Assert.True(executorMethod.ReturnType is CodeType); // not union
        Assert.Null(codeModel.FindChildByName<CodeClass>("createUploadSessionResponseMember1"));
    }

    [Fact]
    public void AddsDiscriminatorMappings()
    {
        var entitySchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.entity")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, string> {
                    {
                        "#microsoft.graph.directoryObject", "#/components/schemas/microsoft.graph.directoryObject"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.entity",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.directoryObject")
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObject",
                Type = ReferenceType.Schema
            },
            AllOf = new List<OpenApiSchema> {
                entitySchema
            },
            UnresolvedReference = false
        };
        var directoryObjects = new OpenApiResponse
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = entitySchema
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = directoryObjects,
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.entity", entitySchema
                    },
                    {
                        "microsoft.graph.directoryObject", directoryObjectSchema
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "microsoft.graph.directoryObjects", directoryObjects
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var entityClass = codeModel.FindChildByName<CodeClass>("entity");
        var directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject");
        Assert.NotNull(entityClass);
        var factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Single(entityClass.DiscriminatorInformation.DiscriminatorMappings);
        var doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Empty(directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings);
        if (entityClass.DiscriminatorInformation?.GetDiscriminatorMappingValue("#microsoft.graph.directoryObject") is not CodeType castType)
            throw new InvalidOperationException("Discriminator mapping value is not a CodeType");
        Assert.NotNull(castType.TypeDefinition);
        Assert.Equal(directoryObjectClass, castType.TypeDefinition);
        var doTypeProperty = directoryObjectClass.Properties.First(static x => x.Name.Equals("ODataType", StringComparison.OrdinalIgnoreCase));
        Assert.True(doTypeProperty.ExistsInBaseType);
        Assert.Equal("\"#microsoft.graph.directoryObject\"", doTypeProperty.DefaultValue);
    }
    [Fact]
    public void DoesntAddDiscriminatorMappingsOfNonDerivedTypes()
    {
        var entitySchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.entity")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, string> {
                    {
                        "#microsoft.graph.directoryObject", "#/components/schemas/microsoft.graph.directoryObject"
                    },
                    {
                        "#microsoft.graph.file", "#/components/schemas/microsoft.graph.file"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.entity",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.directoryObject")
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObject",
                Type = ReferenceType.Schema
            },
            AllOf = new List<OpenApiSchema> {
                entitySchema
            },
            UnresolvedReference = false
        };
        var fileSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.file")
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.file",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjects = new OpenApiResponse()
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = entitySchema
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument()
        {
            Paths = new OpenApiPaths()
            {
                ["objects"] = new OpenApiPathItem()
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = directoryObjects,
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.entity", entitySchema
                    },
                    {
                        "microsoft.graph.directoryObject", directoryObjectSchema
                    },
                    {
                        "microsoft.graph.file", fileSchema
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "microsoft.graph.directoryObjects", directoryObjects
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var entityClass = codeModel.FindChildByName<CodeClass>("entity", true);
        var directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject", true);
        Assert.NotNull(entityClass);
        var factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Single(entityClass.DiscriminatorInformation.DiscriminatorMappings);
    }
    [Fact]
    public async Task AddsDiscriminatorMappingsOneOfImplicit()
    {
        var entitySchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("microsoft.graph.entity")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.entity",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("microsoft.graph.directoryObject")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectsResponse = new OpenApiSchema
        {
            Type = "object",
            OneOf = new List<OpenApiSchema> {
                entitySchema,
                directoryObjectSchema
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var directoryObjects = new OpenApiResponse
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = directoryObjectsResponse
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = directoryObjects,
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.entity", entitySchema
                    },
                    {
                        "microsoft.graph.directoryObject", directoryObjectSchema
                    },
                    {
                        "microsoft.graph.directoryObjects", directoryObjectsResponse
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "microsoft.graph.directoryObjects", directoryObjects
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var config = new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" };
        var builder = new KiotaBuilder(mockLogger.Object, config, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinement(config, codeModel, CancellationToken.None);
        var entityClass = codeModel.FindChildByName<CodeClass>("entity");
        var directoryObjectsClass = codeModel.FindChildByName<CodeClass>("directoryObjects");
        Assert.NotNull(entityClass);
        Assert.NotNull(directoryObjectsClass);
        var factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Empty(entityClass.DiscriminatorInformation.DiscriminatorMappings);
        var doFactoryMethod = directoryObjectsClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Equal(2, directoryObjectsClass.DiscriminatorInformation.DiscriminatorMappings.Count());
    }
    [Fact]
    public async Task AddsDiscriminatorMappingsAllOfImplicit()
    {
        var entitySchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.entity")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.entity",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectSchema = new OpenApiSchema
        {
            Type = "object",
            AllOf = new List<OpenApiSchema> {
                entitySchema,
                new OpenApiSchema {
                    Properties = new Dictionary<string, OpenApiSchema> {
                        {
                            "tenant", new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = "string",
                                Default = new OpenApiString("microsoft.graph.directoryObject")
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var userSchema = new OpenApiSchema
        {
            Type = "object",
            AllOf = new List<OpenApiSchema> {
                directoryObjectSchema,
                new OpenApiSchema {
                    Properties = new Dictionary<string, OpenApiSchema> {
                        {
                            "firstName", new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = "string",
                                Default = new OpenApiString("microsoft.graph.firstName")
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.user",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjects = new OpenApiResponse
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = directoryObjectSchema
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = directoryObjects,
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.entity", entitySchema
                    },
                    {
                        "microsoft.graph.directoryObject", directoryObjectSchema
                    },
                    {
                        "microsoft.graph.user", userSchema
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "microsoft.graph.directoryObjects", directoryObjects
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var config = new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" };
        var builder = new KiotaBuilder(mockLogger.Object, config, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinement(config, codeModel, CancellationToken.None);
        var entityClass = codeModel.FindChildByName<CodeClass>("entity");
        var directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject");
        var userClass = codeModel.FindChildByName<CodeClass>("user");
        Assert.NotNull(entityClass);
        Assert.NotNull(directoryObjectClass);
        Assert.NotNull(userClass);
        var factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Equal(2, entityClass.DiscriminatorInformation.DiscriminatorMappings.Count());
        Assert.Contains("microsoft.graph.directoryObject", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Contains("microsoft.graph.user", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        var doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Single(directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings);
        Assert.Contains("microsoft.graph.user", directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Empty(userClass.DiscriminatorInformation.DiscriminatorMappings);
    }

    [Fact]
    public async Task AddsDiscriminatorMappingsAllOfImplicitWithParentHavingMappingsWhileChildDoesNot()
    {
        var entitySchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.entity")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, string>
                {
                    {
                        "microsoft.graph.directoryObject", "#/components/schemas/microsoft.graph.directoryObject"
                    },
                    {
                        "microsoft.graph.user", "#/components/schemas/microsoft.graph.user"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.entity",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectSchema = new OpenApiSchema
        {
            Type = "object",
            AllOf = new List<OpenApiSchema> {
                entitySchema,
                new OpenApiSchema {
                    Properties = new Dictionary<string, OpenApiSchema> {
                        {
                            "tenant", new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = "string",
                                Default = new OpenApiString("microsoft.graph.directoryObject")
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var userSchema = new OpenApiSchema
        {
            Type = "object",
            AllOf = new List<OpenApiSchema> {
                directoryObjectSchema,
                new OpenApiSchema {
                    Properties = new Dictionary<string, OpenApiSchema> {
                        {
                            "firstName", new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = "string",
                                Default = new OpenApiString("microsoft.graph.firstName")
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.user",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjects = new OpenApiResponse
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = directoryObjectSchema
                }
            },
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = directoryObjects,
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.entity", entitySchema
                    },
                    {
                        "microsoft.graph.directoryObject", directoryObjectSchema
                    },
                    {
                        "microsoft.graph.user", userSchema
                    }
                },
                Responses = new Dictionary<string, OpenApiResponse> {
                    {
                        "microsoft.graph.directoryObjects", directoryObjects
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var config = new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" };
        var builder = new KiotaBuilder(mockLogger.Object, config, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinement(config, codeModel, CancellationToken.None);
        var entityClass = codeModel.FindChildByName<CodeClass>("entity");
        var directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject");
        var userClass = codeModel.FindChildByName<CodeClass>("user");
        Assert.NotNull(entityClass);
        Assert.NotNull(directoryObjectClass);
        Assert.NotNull(userClass);
        var factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Equal(2, entityClass.DiscriminatorInformation.DiscriminatorMappings.Count());
        Assert.Contains("microsoft.graph.directoryObject", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Contains("microsoft.graph.user", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        var doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Single(directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings);
        Assert.Contains("microsoft.graph.user", directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Empty(userClass.DiscriminatorInformation.DiscriminatorMappings);
    }
    [Fact]
    public void UnionOfPrimitiveTypesWorks()
    {
        var simpleObjet = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.simpleObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                OneOf = new List<OpenApiSchema> {
                                                    simpleObjet,
                                                    new OpenApiSchema {
                                                        Type = "number"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.simpleObject", simpleObjet
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeUnionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        var typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("double", typeNames);
    }
    [Fact]
    public void UnionOfInlineSchemasWorks()
    {
        var simpleObjet = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.simpleObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                OneOf = new List<OpenApiSchema> {
                                                    simpleObjet,
                                                    new OpenApiSchema {
                                                        Type = "object",
                                                        Properties = new Dictionary<string, OpenApiSchema> {
                                                            {
                                                                "name", new OpenApiSchema {
                                                                    Type = "string"
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.simpleObject", simpleObjet
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeUnionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        var typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("unionTypeResponseMember1", typeNames);
    }
    [Fact]
    public void IntersectionOfPrimitiveTypesWorks()
    {
        var simpleObjet = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.simpleObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                AnyOf = new List<OpenApiSchema> {
                                                    simpleObjet,
                                                    new OpenApiSchema {
                                                        Type = "number"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.simpleObject", simpleObjet
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeIntersectionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        var typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("double", typeNames);
    }
    [Fact]
    public void IntersectionOfInlineSchemasWorks()
    {
        var simpleObjet = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.simpleObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                AnyOf = new List<OpenApiSchema> {
                                                    simpleObjet,
                                                    new OpenApiSchema {
                                                        Type = "object",
                                                        Properties = new Dictionary<string, OpenApiSchema> {
                                                            {
                                                                "name", new OpenApiSchema {
                                                                    Type = "string"
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.simpleObject", simpleObjet
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeIntersectionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        var typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("unionTypeResponseMember1", typeNames);
    }
    [Fact]
    public void InheritedTypeWithInlineSchemaWorks()
    {
        var baseObjet = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "kind", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "kind",
                Mapping = new Dictionary<string, string> {
                    {
                        "derivedObject", "#/components/schemas/subNS.derivedObject"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.baseObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var derivedObjet = new OpenApiSchema
        {
            Type = "object",
            AllOf = new List<OpenApiSchema> {
                baseObjet,
                new OpenApiSchema {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema> {
                        {
                            "special", new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    },
                    Discriminator = new OpenApiDiscriminator {
                        PropertyName = "kind",
                        Mapping = new Dictionary<string, string> {
                            {
                                "secondLevelDerivedObject", "#/components/schemas/subNS.secondLevelDerivedObject"
                            }
                        }
                    },
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.derivedObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var secondLevelDerivedObject = new OpenApiSchema
        {
            Type = "object",
            AllOf = new List<OpenApiSchema> {
                derivedObjet,
                new OpenApiSchema {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema> {
                        {
                            "moreSpecial", new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subNS.secondLevelDerivedObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["derivedType"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = derivedObjet
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.baseObject", baseObjet
                    },
                    {
                        "subNS.derivedObject", derivedObjet
                    },
                    {
                        "subNS.secondLevelDerivedObject", secondLevelDerivedObject
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.derivedType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("derivedTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeType;
        Assert.NotNull(executorReturnType);
        Assert.Contains("derivedObject", requestExecutorMethod.ReturnType.Name);
        var secondLevelDerivedClass = codeModel.FindChildByName<CodeClass>("derivedObject");
        Assert.NotNull(secondLevelDerivedObject);
        var factoryMethod = secondLevelDerivedClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("kind", secondLevelDerivedClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.NotEmpty(secondLevelDerivedClass.DiscriminatorInformation.DiscriminatorMappings);
    }
    [InlineData("string", "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData("string", "commonmark", "string")]
    [InlineData("string", "html", "string")]
    [InlineData("string", "date-time", "DateTimeOffset")]
    [InlineData("string", "duration", "TimeSpan")]
    [InlineData("string", "date", "DateOnly")]
    [InlineData("string", "time", "TimeOnly")]
    [InlineData("string", "base64url", "base64url")]
    [InlineData("string", "uuid", "Guid")]
    // floating points can only be declared as numbers
    [InlineData("number", "double", "double")]
    [InlineData("number", "float", "float")]
    [InlineData("number", "decimal", "decimal")]
    // integers can only be declared as numbers or integers
    [InlineData("number", "int32", "integer")]
    [InlineData("integer", "int32", "integer")]
    [InlineData("number", "int64", "int64")]
    [InlineData("integer", "int64", "int64")]
    [InlineData("number", "int8", "sbyte")]
    [InlineData("integer", "int8", "sbyte")]
    [InlineData("number", "uint8", "byte")]
    [InlineData("integer", "uint8", "byte")]
    [InlineData("number", "", "double")]
    [InlineData("integer", "", "integer")]
    [InlineData("boolean", "", "boolean")]
    [InlineData("", "byte", "base64")]
    [InlineData("", "binary", "binary")]
    [InlineData("file", null, "binary")]
    [Theory]
    public void MapsPrimitiveFormats(string type, string format, string expected)
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                Type = type,
                                                Format = format
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilder = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilder");
        Assert.NotNull(requestBuilder);
        var method = requestBuilder.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(method);
        Assert.Equal(expected, method.ReturnType.Name);
        Assert.True(method.ReturnType.AllTypes.First().IsExternal);
    }
    [InlineData("string", "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData("string", "commonmark", "string")]
    [InlineData("string", "html", "string")]
    [InlineData("string", "date-time", "DateTimeOffset")]
    [InlineData("string", "duration", "TimeSpan")]
    [InlineData("string", "date", "DateOnly")]
    [InlineData("string", "time", "TimeOnly")]
    [InlineData("string", "base64url", "base64url")]
    // floating points can only be declared as numbers
    [InlineData("number", "double", "double")]
    [InlineData("number", "float", "float")]
    [InlineData("number", "decimal", "decimal")]
    // integers can only be declared as numbers or integers
    [InlineData("number", "int32", "integer")]
    [InlineData("integer", "int32", "integer")]
    [InlineData("number", "int64", "int64")]
    [InlineData("integer", "int64", "int64")]
    [InlineData("number", "int8", "sbyte")]
    [InlineData("integer", "int8", "sbyte")]
    [InlineData("number", "uint8", "byte")]
    [InlineData("integer", "uint8", "byte")]
    [InlineData("number", "", "double")]
    [InlineData("integer", "", "integer")]
    [InlineData("boolean", "", "boolean")]
    [InlineData("", "byte", "base64")]
    [InlineData("", "binary", "binary")]
    [InlineData("file", null, "binary")]
    [Theory]
    public void MapsQueryParameterTypes(string type, string format, string expected)
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = type,
                                        Format = format
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        var property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal(expected, property.Type.Name);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void MapsQueryParameterCollectionKinds(bool isArray)
    {
        var baseSchema = new OpenApiSchema
        {
            Type = "number",
            Format = "int64"
        };
        var arraySchema = new OpenApiSchema
        {
            Type = "array",
            Items = baseSchema
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = isArray ? arraySchema : baseSchema
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        var property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("int64", property.Type.Name);
        Assert.Equal(isArray ? CodeTypeBase.CodeTypeCollectionKind.Array : CodeTypeBase.CodeTypeCollectionKind.None, property.Type.CollectionKind);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void DefaultsQueryParametersWithNoSchemaToString()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        var property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("string", property.Type.Name);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void DoesntGenerateNamespacesWhenNotRequired()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var myObjectModel = modelsNS.FindChildByName<CodeClass>("Myobject", false);
        Assert.NotNull(myObjectModel);
        var modelsSubNS = codeModel.FindNamespaceByName("TestSdk.Models.Myobject");
        Assert.Null(modelsSubNS);
    }
    [Fact]
    public void GeneratesNamesapacesWhenRequired()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "subns.myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subns.myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        Assert.Null(codeModel.FindChildByName<CodeClass>("Myobject", false));
        var modelsSubNS = codeModel.FindNamespaceByName("TestSdk.Models.subns");
        Assert.NotNull(modelsSubNS);
        Assert.NotNull(modelsSubNS.FindChildByName<CodeClass>("Myobject", false));
    }
    [Fact]
    public void IdsResultInIndexers()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answers/{id}"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindChildByName<CodeClass>("With"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("WithResponse"));
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answers.Item");
        Assert.NotNull(rbNS);
        var rb = rbNS.Classes.First();
        Assert.Equal("AnswersItemRequestBuilder", rb.Name);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        Assert.Null(modelsNS.FindChildByName<CodeClass>("With", false));
    }
    [Fact]
    public void HandlesCollectionOfEnumSchemasInAnyOfWithNullable()
    {
        var enumSchema = new OpenApiSchema
        {
            Title = "riskLevel",
            Enum = new List<IOpenApiAny>
            {
                new OpenApiString("low"),
                new OpenApiString("medium"),
                new OpenApiString("high"),
                new OpenApiString("hidden"),
                new OpenApiString("none"),
                new OpenApiString("unknownFutureValue")
            },
            Type = "string"
        };
        var myObjectSchema = new OpenApiSchema
        {
            Title = "conditionalAccessConditionSet",
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "signInRiskLevels", new OpenApiSchema {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            AnyOf = new List<OpenApiSchema>
                            {
                                enumSchema,
                                new OpenApiSchema
                                {
                                    Type = "object",
                                    Nullable = true
                                }
                            }
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };

        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    },
                    {
                        "riskLevel", enumSchema
                    }
                },

            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(static x => x.IsOfKind(CodeClassKind.Model) && x.Name.Equals("myobject", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(responseClass);
        var property = responseClass.Properties.FirstOrDefault(static x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("signInRiskLevels", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.NotEmpty(property.Type.Name);
        var codeType = property.Type as CodeType;
        Assert.NotNull(codeType);
        Assert.IsType<CodeEnum>(codeType.TypeDefinition);// Ensure the collection is a codeEnum
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, codeType.CollectionKind);// Ensure the collection is a codeEnum
    }
    [Fact]
    public void HandlesCollectionOfEnumSchemas()
    {
        var enumSchema = new OpenApiSchema
        {
            Title = "riskLevel",
            Enum = new List<IOpenApiAny>
            {
                new OpenApiString("low"),
                new OpenApiString("medium"),
                new OpenApiString("high"),
                new OpenApiString("hidden"),
                new OpenApiString("none"),
                new OpenApiString("unknownFutureValue")
            },
            Type = "string"
        };
        var myObjectSchema = new OpenApiSchema
        {
            Title = "conditionalAccessConditionSet",
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "signInRiskLevels", new OpenApiSchema {
                        Type = "array",
                        Items = enumSchema
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };

        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    },
                    {
                        "riskLevel", enumSchema
                    }
                },

            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model) && x.Name.Equals("myobject", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(responseClass);
        var property = responseClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("signInRiskLevels", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.NotEmpty(property.Type.Name);
        var codeType = property.Type as CodeType;
        Assert.NotNull(codeType);
        Assert.IsType<CodeEnum>(codeType.TypeDefinition);// Ensure the collection is a codeEnum
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, codeType.CollectionKind);// Ensure the collection is a codeEnum
    }
    [Fact]
    public void InlinePropertiesGenerateTypes()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "tilleggsinformasjon", new OpenApiSchema {
                        Type = "object",
                        AdditionalProperties = new OpenApiSchema {
                            Type = "string"
                        }
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model) && x.Name.Equals("myobject", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(responseClass);
        var property = responseClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("Tilleggsinformasjon", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.NotEmpty(property.Type.Name);
    }
    [Fact]
    public void ModelsDoesntUsePathDescriptionWhenAvailable()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Description = "some path item description",
                    Summary = "some path item summary",
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Description = "some operation description",
                            Summary = "some operation summary",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Empty(responseClass.Documentation.Description);
    }
    [Fact]
    public void CleansUpInvalidDescriptionCharacters()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
            Description = @"	some description with invalid characters: 
",
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Equal("some description with invalid characters: ", responseClass.Documentation.Description);
    }
    [InlineData("application/json")]
    [InlineData("application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8")]
    [InlineData("application/vnd.github.mercy-preview+json")]
    [InlineData("application/vnd.github.mercy-preview+json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8")]
    [Theory]
    public void AcceptVendorsTypes(string contentType)
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        [contentType] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.NotNull(executorMethod);
        Assert.Equal("myobject", executorMethod.ReturnType.Name);
    }
    [Fact]
    public void ModelsUseDescriptionWhenAvailable()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Description = "some path item description",
                    Summary = "some path item summary",
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Description = "some operation description",
                            Summary = "some operation summary",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                Description = "some description",
                                                Properties = new Dictionary<string, OpenApiSchema> {
                                                    {
                                                        "name", new OpenApiSchema {
                                                            Type = "string"
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsSubNS = codeModel.FindNamespaceByName("TestSdk.answer");
        Assert.NotNull(modelsSubNS);
        var responseClass = modelsSubNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Equal("some description", responseClass.Documentation.Description);

        responseClass = modelsSubNS.Classes.FirstOrDefault(c => c.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(responseClass);
        Assert.Equal("some path item description", responseClass.Documentation.Description);

        var responseProperty = codeModel.FindNamespaceByName("TestSdk").Classes.SelectMany(c => c.Properties).FirstOrDefault(p => p.Kind == CodePropertyKind.RequestBuilder);
        Assert.NotNull(responseProperty);
        Assert.Equal("some path item description", responseProperty.Documentation.Description);
    }

    [InlineData("application/json", "206", true, "default", "myobject")]
    [InlineData("application/json", "206", false, "default", "binary")]
    [InlineData("application/json", "205", true, "default", "void")]
    [InlineData("application/json", "205", false, "default", "void")]
    [InlineData("application/json", "204", true, "default", "void")]
    [InlineData("application/json", "204", false, "default", "void")]
    [InlineData("application/json", "203", true, "default", "myobject")]
    [InlineData("application/json", "203", false, "default", "binary")]
    [InlineData("application/json", "202", true, "default", "myobject")]
    [InlineData("application/json", "202", false, "default", "void")]
    [InlineData("application/json", "201", true, "default", "myobject")]
    [InlineData("application/json", "201", false, "default", "void")]
    [InlineData("application/json", "200", true, "default", "myobject")]
    [InlineData("application/json", "200", false, "default", "binary")]
    [InlineData("application/json", "2XX", true, "default", "myobject")]
    [InlineData("application/json", "2XX", false, "default", "binary")]
    [InlineData("application/xml", "204", true, "default", "void")]
    [InlineData("application/xml", "204", false, "default", "void")]
    [InlineData("application/xml", "200", true, "default", "binary")] // MyObject when we support xml deserialization
    [InlineData("application/xml", "200", false, "default", "binary")]
    [InlineData("text/xml", "204", true, "default", "void")]
    [InlineData("text/xml", "204", false, "default", "void")]
    [InlineData("text/xml", "200", true, "default", "binary")] // MyObject when we support xml deserialization
    [InlineData("text/xml", "200", false, "default", "binary")]
    [InlineData("text/yaml", "204", true, "default", "void")]
    [InlineData("text/yaml", "204", false, "default", "void")]
    [InlineData("text/yaml", "200", true, "default", "binary")] // MyObject when we support xml deserialization
    [InlineData("text/yaml", "200", false, "default", "binary")]
    [InlineData("application/octet-stream", "204", true, "default", "void")]
    [InlineData("application/octet-stream", "204", false, "default", "void")]
    [InlineData("application/octet-stream", "200", true, "default", "binary")]
    [InlineData("application/octet-stream", "200", false, "default", "binary")]
    [InlineData("text/html", "204", true, "default", "void")]
    [InlineData("text/html", "204", false, "default", "void")]
    [InlineData("text/html", "200", true, "default", "binary")]
    [InlineData("text/html", "200", false, "default", "binary")]
    [InlineData("*/*", "204", true, "default", "void")]
    [InlineData("*/*", "204", false, "default", "void")]
    [InlineData("*/*", "200", true, "default", "binary")]
    [InlineData("*/*", "200", false, "default", "binary")]
    [InlineData("text/plain", "204", true, "default", "void")]
    [InlineData("text/plain", "204", false, "default", "void")]
    [InlineData("text/plain", "200", true, "default", "myobject")]
    [InlineData("text/plain", "200", false, "default", "string")]
    [InlineData("text/plain", "204", true, "application/json", "void")]
    [InlineData("text/plain", "204", false, "application/json", "void")]
    [InlineData("text/plain", "200", true, "application/json", "string")]
    [InlineData("text/plain", "200", false, "application/json", "string")]
    [InlineData("text/yaml", "204", true, "application/json", "void")]
    [InlineData("text/yaml", "204", false, "application/json", "void")]
    [InlineData("text/yaml", "200", true, "application/json", "binary")]
    [InlineData("text/yaml", "200", false, "application/json", "binary")]
    [Theory]
    public void GeneratesTheRightReturnTypeBasedOnContentAndStatus(string contentType, string statusCode, bool addModel, string acceptedContentType, string returnType)
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                [statusCode] = new OpenApiResponse {
                                    Content = {
                                        [contentType] = new OpenApiMediaType {
                                            Schema = addModel ? myObjectSchema : null
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = acceptedContentType.Equals("default", StringComparison.OrdinalIgnoreCase) ?
                                            new GenerationConfiguration().StructuredMimeTypes :
                                            new(StringComparer.OrdinalIgnoreCase) { acceptedContentType }
            }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal(returnType, executor.ReturnType.Name);
    }
    [Fact]
    public void Considers200WithSchemaOver2XXWithSchema()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var myOtherObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myotherobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["2XX"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myOtherObjectSchema
                                        }
                                    }
                                },
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    { "myobject", myObjectSchema },
                    { "myotherobject", myOtherObjectSchema },
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new GenerationConfiguration().StructuredMimeTypes
            }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("myobject", executor.ReturnType.Name);
    }
    [Fact]
    public void Considers2XXWithSchemaOver204WithNoSchema()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["2XX"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new GenerationConfiguration().StructuredMimeTypes
            }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("myobject", executor.ReturnType.Name);
    }
    [Fact]
    public void Considers204WithNoSchemaOver206WithNoSchema()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["206"] = new OpenApiResponse(),
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new GenerationConfiguration().StructuredMimeTypes
            }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("void", executor.ReturnType.Name);
    }
    [InlineData("application/json", true, "default", "myobject")]
    [InlineData("application/json", false, "default", "binary")]
    [InlineData("application/xml", false, "default", "binary")]
    [InlineData("application/xml", true, "default", "binary")] //MyObject when we support it
    [InlineData("text/xml", false, "default", "binary")]
    [InlineData("text/xml", true, "default", "binary")] //MyObject when we support it
    [InlineData("text/yaml", false, "default", "binary")]
    [InlineData("text/yaml", true, "default", "binary")] //MyObject when we support it
    [InlineData("application/octet-stream", true, "default", "binary")]
    [InlineData("application/octet-stream", false, "default", "binary")]
    [InlineData("text/html", true, "default", "binary")]
    [InlineData("text/html", false, "default", "binary")]
    [InlineData("*/*", true, "default", "binary")]
    [InlineData("*/*", false, "default", "binary")]
    [InlineData("text/plain", false, "default", "binary")]
    [InlineData("text/plain", true, "default", "myobject")]
    [InlineData("text/plain", true, "application/json", "binary")]
    [InlineData("text/plain", false, "application/json", "binary")]
    [InlineData("text/yaml", true, "application/json", "binary")]
    [InlineData("text/yaml", false, "application/json", "binary")]
    [Theory]
    public void GeneratesTheRightParameterTypeBasedOnContentAndStatus(string contentType, bool addModel, string acceptedContentType, string parameterType)
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody {
                                Content = {
                                    [contentType] = new OpenApiMediaType {
                                        Schema = addModel ? myObjectSchema : null
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = acceptedContentType.Equals("default", StringComparison.OrdinalIgnoreCase) ?
                                            new GenerationConfiguration().StructuredMimeTypes :
                                            new(StringComparer.OrdinalIgnoreCase) { acceptedContentType }
            }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal(parameterType, executor.Parameters.OfKind(CodeParameterKind.RequestBody).Type.Name);
    }
    [Fact]
    public void DoesntGenerateVoidExecutorOnMixed204()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        Assert.Single(rbClass.Methods.Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor)));
        var executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.NotEqual("void", executor.ReturnType.Name);
    }
    [InlineData(new[] { "microsoft.graph.user", "microsoft.graph.termstore.term" }, "microsoft.graph")]
    [InlineData(new[] { "microsoft.graph.user", "odata.errors.error" }, "")]
    [InlineData(new string[] { }, "")]
    [Theory]
    public void StripsCommonModelsPrefix(string[] componentNames, string stripPrefix)
    {
        var paths = new OpenApiPaths();
        var components = new OpenApiComponents
        {
            Schemas = new Dictionary<string, OpenApiSchema>()
        };
        foreach (var componentName in componentNames)
        {
            var myObjectSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema> {
                    {
                        "id", new OpenApiSchema {
                            Type = "string",
                        }
                    }
                },
                Reference = new OpenApiReference
                {
                    Id = componentName,
                    Type = ReferenceType.Schema
                },
                UnresolvedReference = false
            };
            paths.Add($"answer{componentName}", new OpenApiPathItem
            {
                Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
            });
            components.Schemas.Add(componentName, myObjectSchema);
        }
        var document = new OpenApiDocument
        {
            Paths = paths,
            Components = components,
        };
        var result = KiotaBuilder.GetDeeperMostCommonNamespaceNameForModels(document);
        Assert.Equal(stripPrefix, result);
    }
    [Fact]
    public void HandlesContentParameters()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["answer(ids={ids}"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "ids",
                                    In = ParameterLocation.Path,
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        { "application/json",
                                        new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                                Type = "array",
                                                                Items = new OpenApiSchema {
                                                                    Type = "string",
                                                                }
                                                            }
                                            }
                                        }
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var answersNS = codeModel.FindNamespaceByName("TestSdk.answerWithIds");
        Assert.NotNull(answersNS);
        var rbClass = answersNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var ctorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Constructor));
        Assert.NotNull(ctorMethod);
        var idsParam = ctorMethod.Parameters.FirstOrDefault(x => x.Name.Equals("ids", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(idsParam);
        Assert.Equal("string", idsParam.Type.Name);
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.None, idsParam.Type.CollectionKind);
    }

    [Fact]
    public void HandlesPagingExtension()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Extensions = new Dictionary<string, IOpenApiExtension> {
                                { OpenApiPagingExtension.Name, new OpenApiPagingExtension { NextLinkName = "@odata.nextLink" } }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var answersNS = codeModel.FindNamespaceByName("TestSdk.users");
        Assert.NotNull(answersNS);
        var rbClass = answersNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.NotNull(executorMethod);
        Assert.Equal("@odata.nextLink", executorMethod.PagingInformation?.NextLinkName);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void SetsReadonlyProperties(bool isReadonly)
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                        ReadOnly = isReadonly,
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var objectClass = codeModel.FindChildByName<CodeClass>("myobject");
        Assert.NotNull(objectClass);
        var nameProperty = objectClass.Properties.First(static x => "name".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(isReadonly, nameProperty.ReadOnly);
    }
    [Fact]
    public void SupportsIncludeFilter()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["groups"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = new() {
                "*users*"
            }
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
    }
    [Fact]
    public void SupportsExcludeFilter()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["groups"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            ExcludePatterns = new() {
                "*groups*"
            }
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
    }
    [Fact]
    public void SupportsIncludeFilterWithOperation()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}/messages"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        },
                        [OperationType.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody {
                                Content = {
                                    ["application/json"] = new OpenApiMediaType {
                                        Schema = myObjectSchema
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        },
                        [OperationType.Put] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody {
                                Content = {
                                    ["application/json"] = new OpenApiMediaType {
                                        Schema = myObjectSchema
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["groups"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["students"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = new() {
                "users/*/messages*#get,PATCH", // lowercase is voluntary to test case insensitivity
                "users/**#POST",
                "students"
            }
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
        var messagesNS = codeModel.FindNamespaceByName("TestSdk.users.item.messages");
        Assert.NotNull(messagesNS);
        var messagesRS = messagesNS.FindChildByName<CodeClass>("MessagesRequestBuilder");
        Assert.NotNull(messagesRS);
        Assert.Single(messagesRS.Methods.Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Post));
        Assert.Single(messagesRS.Methods.Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get));
        Assert.Empty(messagesRS.Methods.Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Put));
        var studentsNS = codeModel.FindNamespaceByName("TestSdk.students");
        var studentsRS = studentsNS.FindChildByName<CodeClass>("StudentsRequestBuilder");
        Assert.NotNull(studentsRS);
    }
    [Fact]
    public void SupportsIndexingParametersInSubPaths()
    {
        var myObjectSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users({userId})/manager"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "userId",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            },
            Components = new()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
        }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var NS = codeModel.FindNamespaceByName("TestSdk.usersWithUserId");
        Assert.NotNull(NS);
        var rb = NS.FindChildByName<CodeClass>("usersWithUserIdRequestBuilder");
        Assert.NotNull(rb);
        var method = rb.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Constructor));
        Assert.NotNull(method);
        Assert.Equal("userId", method.Parameters.Last(static x => x.IsOfKind(CodeParameterKind.Path)).Name);
    }
    [Fact]
    public async Task DisambiguatesOperationsConflictingWithPath1()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
  /me/get:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
components:
  schemas:
    microsoft.graph.user:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        var getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        Assert.NotNull(getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
        Assert.NotNull(getRB.Properties.FirstOrDefault(static x => x.IsOfKind(CodePropertyKind.RequestBuilder) && "GetPath".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task DisambiguatesOperationsConflictingWithPath2()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me/get:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
  /me:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
components:
  schemas:
    microsoft.graph.user:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        var getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        Assert.NotNull(getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
        Assert.NotNull(getRB.Properties.FirstOrDefault(static x => x.IsOfKind(CodePropertyKind.RequestBuilder) && "GetPath".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task IndexerAndRequestBuilderNamesMatch()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me/posts/{post-id}:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.post'
components:
  schemas:
    microsoft.graph.post:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document!);
        var codeModel = builder.CreateSourceModel(node);
        var collectionRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.me.posts");
        Assert.NotNull(collectionRequestBuilderNamespace);
        var collectionRequestBuilder = collectionRequestBuilderNamespace.FindChildByName<CodeClass>("postsRequestBuilder");
        var collectionIndexer = collectionRequestBuilder.Indexer;
        Assert.NotNull(collectionIndexer);
        var itemRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.me.posts.item");
        Assert.NotNull(itemRequestBuilderNamespace);
        var itemRequestBuilder = itemRequestBuilderNamespace.FindChildByName<CodeClass>("postItemRequestBuilder");
        Assert.Equal(collectionIndexer.ReturnType.Name, itemRequestBuilder.Name);
    }
    [Fact]
    public async Task MapsBooleanEnumToBooleanType()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.0
info:
    title: Microsoft Graph get user API
    version: 1.0.0
servers:
    - url: https://graph.microsoft.com/v1.0/
paths:
    /me:
        get:
            responses:
                200:
                    description: Success!
                    content:
                        application/json:
                            schema:
                                type: boolean
                                enum:
                                    - true
                                    - false");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document!);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        var getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        var getMethod = getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(getMethod);
        Assert.Equal("boolean", getMethod.ReturnType.Name);
    }
    [Fact]
    public async Task MapsNumberEnumToDoubleType()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.0
info:
    title: Microsoft Graph get user API
    version: 1.0.0
servers:
    - url: https://graph.microsoft.com/v1.0/
paths:
    /me:
        get:
            responses:
                200:
                    description: Success!
                    content:
                        application/json:
                            schema:
                                type: number
                                enum:
                                    - 1
                                    - 2");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document!);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        var getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        var getMethod = getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(getMethod);
        Assert.Equal("double", getMethod.ReturnType.Name);
    }
    [InlineData("MV22X/MV72X", "MV22XMV72X")]
    [Theory]
    public async Task CleansInlineTypeNames(string raw, string expected)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://localhost:443
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  {raw}:
                    type: object
                    properties:
                      foo:
                        type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://localhost:443" }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var inlineType = rootNS.FindChildByName<CodeClass>($"enumerationResponse_{expected}", true);
        Assert.NotNull(inlineType);
    }
    [Fact]
    public void SinglePathParametersAreDeduplicated()
    {
        var userSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "displayName", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "#/components/schemas/microsoft.graph.user"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}/careerAdvisor"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = userSchema
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["users/{user-id}/manager"] = new OpenApiPathItem
                {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = userSchema
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.user", userSchema
                    }
                }
            }
        };
        var mockLogger = new CountLogger<KiotaBuilder>();
        var builder = new KiotaBuilder(mockLogger, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var managerRB = codeModel.FindNamespaceByName("ApiSdk.users.item.manager").FindChildByName<CodeClass>("ManagerRequestBuilder", false);
        Assert.NotNull(managerRB);
        var managerUrlTemplate = managerRB.FindChildByName<CodeProperty>("UrlTemplate", false);
        Assert.NotNull(managerUrlTemplate);
        Assert.Equal("{+baseurl}/users/{id}/manager", managerUrlTemplate.DefaultValue.Trim('"'));
        var careerAdvisorRB = codeModel.FindNamespaceByName("ApiSdk.users.item.careerAdvisor").FindChildByName<CodeClass>("CareerAdvisorRequestBuilder", false);
        Assert.NotNull(careerAdvisorRB);
        var careerAdvisorUrlTemplate = careerAdvisorRB.FindChildByName<CodeProperty>("UrlTemplate", false);
        Assert.NotNull(careerAdvisorUrlTemplate);
        Assert.Equal("{+baseurl}/users/{id}/careerAdvisor", careerAdvisorUrlTemplate.DefaultValue.Trim('"'));
    }
    [Fact]
    public async Task MergesIntersectionTypes()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                allOf:
                  - $ref: '#/components/schemas/microsoft.graph.entity'
                  - $ref: '#/components/schemas/microsoft.graph.directoryObject'
                  - $ref: '#/components/schemas/microsoft.graph.user'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string
    microsoft.graph.directoryObject:
      title: directoryObject
      type: object
      properties:
        deletedDateTime:
          pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
          type: string
          format: date-time
          nullable: true
    microsoft.graph.user:
      title: user
      type: object
      properties:
        accountEnabled:
          type: boolean
          nullable: true");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var resultClass = codeModel.FindChildByName<CodeClass>("DirectoryObjectResponse");
        Assert.NotNull(resultClass);
        Assert.Equal(4, resultClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom)).Count());
    }
    [Fact]
    public async Task SkipsInvalidItemsProperties()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  datasets:
                    type: array
                    items: true
                  datakeys:
                    type: array
                    items: {}
                  datainfo:
                    type: array
                  id:
                    type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var resultClass = codeModel.FindChildByName<CodeClass>("DirectoryObjectResponse");
        Assert.NotNull(resultClass);
        var keysToCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "datasets", "datakeys", "datainfo" };
        Assert.Empty(resultClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.Custom) && keysToCheck.Contains(x.Name)));
        Assert.Single(resultClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task DiscriptionTakenFromAllOf()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      description: 'base entity'
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.directoryObject': '#/components/schemas/microsoft.graph.directoryObject'
          '#microsoft.graph.sub1': '#/components/schemas/microsoft.graph.sub1'
          '#microsoft.graph.sub2': '#/components/schemas/microsoft.graph.sub2'
    microsoft.graph.directoryObject:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: directoryObject
          description: 'directory object'
          type: object
          required: [ '@odata.type' ]
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.sub1': '#/components/schemas/microsoft.graph.sub1'
              '#microsoft.graph.sub2': '#/components/schemas/microsoft.graph.sub2'
    microsoft.graph.sub1:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: sub1
          description: 'sub1'
          type: object
    microsoft.graph.sub2:
      description: 'sub2'
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: sub2
          description: 'ignored'
          type: object");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        Assert.Equal("base entity", codeModel.FindChildByName<CodeClass>("entity").Documentation.Description);
        Assert.Equal("directory object", codeModel.FindChildByName<CodeClass>("directoryObject").Documentation.Description);
        Assert.Equal("sub1", codeModel.FindChildByName<CodeClass>("sub1").Documentation.Description);
        Assert.Equal("sub2", codeModel.FindChildByName<CodeClass>("sub2").Documentation.Description);
    }
    [Fact]
    public async Task CleanupSymbolNameDoesNotCauseNameConflicts()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/entity'
components:
  schemas:
    entity:
      title: entity
      type: object
      required: ['type', '@type']
      properties:
        type:
          type: string
        '@type':
          type: integer");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var resultClass = codeModel.FindChildByName<CodeClass>("Entity");
        Assert.NotNull(resultClass);
        Assert.Equal(2, resultClass.Properties.Select(static x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
    [Fact]
    public async Task CleanupSymbolNameDoesNotCauseNameConflictsWithSuperType()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/subtype'
components:
  schemas:
    entity:
      title: entity
      type: object
      required: ['@type']
      properties:
        '@type':
          type: integer
      discriminator:
        propertyName: '@type'
        mapping:
          'subtype': '#/components/schemas/subtype'
    subtype:
      allOf:
        - $ref: '#/components/schemas/entity'
        - title: subtype
          type: object
          required: ['type', '@type']
          properties:
            'type':
              type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var entityClass = codeModel.FindChildByName<CodeClass>("Entity");
        Assert.NotNull(entityClass);
        var atType = entityClass.FindChildByName<CodeProperty>("Type");
        Assert.Equal("@type", atType.WireName);
        var subtypeClass = codeModel.FindChildByName<CodeClass>("Subtype");
        Assert.NotNull(subtypeClass);
        var type = subtypeClass.FindChildByName<CodeProperty>("SubtypeType");
        Assert.Equal("type", type.WireName);
        Assert.Equal("subtypeType", type.Name);
    }
    [Fact]
    public async Task CleanupSymbolNameDoesNotCauseNameConflictsInQueryParameters()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    get:
      parameters:
        - name: $select
          in: query
          schema:
            type: string
        - name: select
          in: query
          schema:
            type: int64
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var parametersClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilderGetQueryParameters");
        Assert.NotNull(parametersClass);
        var dollarSelect = parametersClass.FindChildByName<CodeProperty>("Select");
        Assert.Equal("%24select", dollarSelect.WireName);
        Assert.Equal("string", dollarSelect.Type.Name);
        var select = parametersClass.FindChildByName<CodeProperty>("select0");
        Assert.Equal("select", select.WireName);
        Assert.Equal("int64", select.Type.Name);
    }
    [Fact]
    public async Task SupportsMultiPartFormAsRequestBody()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var fs = await GetDocumentStream(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
            encoding:
              id:
                contentType: text/plain
              address:
                contentType: application/json
              profileImage:
                contentType: image/png
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        var rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        var postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        var bodyParameter = postMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("MultipartBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        var addressClass = codeModel.FindChildByName<CodeClass>("Address");
        Assert.NotNull(addressClass);
    }
}
