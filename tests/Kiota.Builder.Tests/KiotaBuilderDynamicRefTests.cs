using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("#category", "category")]
    [InlineData("https://example.com/schema#itemType", "itemType")]
    [InlineData("itemType", "itemType")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ExtractAnchorNameReturnsExpectedValue(string dynamicRef, string expected)
    {
        Assert.Equal(expected, KiotaBuilder.ExtractAnchorName(dynamicRef));
    }

    [Fact]
    public async Task DynamicBindingPopulatesTypeParametersOnTemplateAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        _tempFiles.Add(tempFilePath);
        await File.WriteAllTextAsync(tempFilePath, """
openapi: 3.1.0
info:
  title: T
  version: 0.1.0
servers:
  - url: https://localhost
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/User'
                $ref: '#/components/schemas/PaginatedTemplate'
        default:
          description: err
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
    PaginatedTemplate:
      $id: https://example.com/schemas/PaginatedTemplate
      $dynamicAnchor: itemType
      type: object
      properties:
        items:
          type: array
          items:
            $dynamicRef: '#itemType'
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var node = builder.CreateUriSpace(document!);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);

        var modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        var templateClass = modelsNS!.FindChildByName<CodeClass>("PaginatedTemplateUser", true);
        Assert.NotNull(templateClass);
        Assert.True(templateClass!.IsGeneric);
        var parameter = Assert.Single(templateClass.TypeParameters);
        Assert.Equal("TItemType", parameter.Name);
    }

    [Fact]
    public async Task UsesDistinctNamesForUnboundDynamicRefUnionsAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        _tempFiles.Add(tempFilePath);
        await File.WriteAllTextAsync(tempFilePath, """
openapi: 3.1.0
info:
  title: T
  version: 0.1.0
paths:
  /envelope:
    get:
      operationId: getEnvelope
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Envelope'
components:
  schemas:
    Envelope:
      type: object
      properties:
        data:
          $dynamicRef: '#dataType'
        error:
          $dynamicRef: '#errorType'
    v1.StringModel:
      $dynamicAnchor: dataType
      type: object
    v1.NumberModel:
      $dynamicAnchor: dataType
      type: object
    ErrorA:
      $dynamicAnchor: errorType
      type: object
    ErrorB:
      $dynamicAnchor: errorType
      type: object
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        var envelope = modelsNamespace.FindChildByName<CodeClass>("Envelope", true);
        Assert.NotNull(envelope);
        var dataType = Assert.IsType<CodeUnionType>(envelope.Properties.Single(x => x.Name == "data").Type);
        var errorType = Assert.IsType<CodeUnionType>(envelope.Properties.Single(x => x.Name == "error").Type);

        Assert.Equal("Envelope_data", dataType.Name);
        Assert.Equal("Envelope_error", errorType.Name);
        Assert.Equal(["NumberModel", "StringModel"], dataType.Types.Select(x => x.TypeDefinition!.Name).OrderBy(x => x));
    }

    [Fact]
    public async Task InheritsBindingSuffixForDynamicRefsInAdditionalPropertiesAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        _tempFiles.Add(tempFilePath);
        await File.WriteAllTextAsync(tempFilePath, """
openapi: 3.1.0
info:
  title: T
  version: 0.1.0
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/User'
                $ref: '#/components/schemas/PaginatedTemplate'
components:
  schemas:
    User:
      type: object
    PaginatedTemplate:
      $dynamicAnchor: itemType
      type: object
      properties:
        entries:
          type: object
          additionalProperties:
            $dynamicRef: '#itemType'
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        var template = modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplateUser", true);
        Assert.NotNull(template);
        var entries = Assert.IsType<CodeType>(template.Properties.Single(x => x.Name == "entries").Type);

        Assert.Equal("PaginatedTemplateUser_entriesUser", entries.TypeDefinition!.Name);
    }

    [Fact]
    public async Task MultiCandidateDottedKeysLandInDistinctNamespacesAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        _tempFiles.Add(tempFilePath);
        await File.WriteAllTextAsync(tempFilePath, """
openapi: 3.1.0
info:
  title: T
  version: 0.1.0
paths:
  /container:
    get:
      operationId: getContainer
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Container'
components:
  schemas:
    Container:
      type: object
      properties:
        item:
          $dynamicRef: '#node'
    v1.UserModel:
      $dynamicAnchor: node
      type: object
      properties:
        version: { type: string }
    v2.AdminModel:
      $dynamicAnchor: node
      type: object
      properties:
        version: { type: integer }
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var v1Ns = codeModel.FindNamespaceByName("ApiSdk.models.v1");
        var v2Ns = codeModel.FindNamespaceByName("ApiSdk.models.v2");
        Assert.NotNull(v1Ns);
        Assert.NotNull(v2Ns);
        Assert.NotNull(v1Ns.FindChildByName<CodeClass>("UserModel", true));
        Assert.NotNull(v2Ns.FindChildByName<CodeClass>("AdminModel", true));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        var container = modelsNamespace.FindChildByName<CodeClass>("Container", true);
        Assert.NotNull(container);
        var itemType = Assert.IsType<CodeUnionType>(container.Properties.Single(x => x.Name == "item").Type);
        Assert.Equal(["ApiSdk.models.v1.UserModel", "ApiSdk.models.v2.AdminModel"], itemType.Types.Select(x => $"{((CodeNamespace)x.TypeDefinition!.Parent!).Name}.{x.TypeDefinition.Name}").OrderBy(x => x));
    }

    [Fact]
    public async Task ArrayRootBindingSuffixAppliesToTemplateItemsAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        _tempFiles.Add(tempFilePath);
        await File.WriteAllTextAsync(tempFilePath, """
openapi: 3.1.0
info:
  title: T
  version: 0.1.0
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/User'
                type: array
                items:
                  $ref: '#/components/schemas/PaginatedTemplate'
components:
  schemas:
    User:
      type: object
    PaginatedTemplate:
      type: object
      properties:
        items:
          type: array
          items:
            $dynamicRef: '#itemType'
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        Assert.NotNull(modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplateUser", true));
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplate", true));
    }
}
