using System;
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
        var templateClass = modelsNS!.FindChildByName<CodeClass>("PaginatedTemplate", true);
        Assert.NotNull(templateClass);
        Assert.True(templateClass!.IsGeneric);
        var parameter = Assert.Single(templateClass.TypeParameters);
        Assert.Equal("TItemType", parameter.Name);
        // the concrete specialization is gone: the $dynamicRef items property is typed as the type parameter
        Assert.Null(modelsNS.FindChildByName<CodeClass>("PaginatedTemplateUser", true));
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

    [Fact]
    public async Task GenericTemplateReplacesConcreteSpecializationsForCSharpAsync()
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
  /groups:
    get:
      operationId: listGroups
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/Group'
                $ref: '#/components/schemas/PaginatedTemplate'
components:
  schemas:
    User:
      type: object
    Group:
      type: object
    PaginatedTemplate:
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
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        // one reusable generic template instead of per-binding concrete classes
        var template = modelsNamespace!.FindChildByName<CodeClass>("PaginatedTemplate", true);
        Assert.NotNull(template);
        Assert.True(template!.IsGeneric);
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplateUser", true));
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplateGroup", true));
        // the dynamicRef property is typed as the type parameter
        var itemsProperty = template.Properties.First(static x => x.Name == "items");
        var itemsType = Assert.IsType<CodeType>(itemsProperty.Type);
        Assert.IsType<CodeTypeParameter>(itemsType.TypeDefinition);
        // executor return types carry the bound generic arguments
        var usersNamespace = codeModel.FindNamespaceByName("ApiSdk.users");
        Assert.NotNull(usersNamespace);
        var usersRequestBuilder = usersNamespace!.FindChildByName<CodeClass>("UsersRequestBuilder", true);
        Assert.NotNull(usersRequestBuilder);
        var usersExecutor = usersRequestBuilder!.Methods.First(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get);
        var returnType = Assert.IsType<CodeType>(usersExecutor.ReturnType);
        Assert.Equal("PaginatedTemplate", returnType.Name);
        var genericArgument = Assert.Single(returnType.GenericTypeParameterValues);
        Assert.Equal("User", genericArgument.Name);
    }

    [Fact]
    public async Task GenericInheritedTemplateKeepsConcreteBaseAsync()
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
  /inherited/users:
    get:
      operationId: listInheritedUsers
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
                $ref: '#/components/schemas/InheritedTemplate'
  /inherited/groups:
    get:
      operationId: listInheritedGroups
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/Group'
                $ref: '#/components/schemas/InheritedTemplate'
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
    Group:
      type: object
      properties:
        id:
          type: string
    PageBase:
      type: object
      properties:
        nextLink:
          type: string
    InheritedTemplate:
      $id: https://example.com/schemas/InheritedTemplate
      $dynamicAnchor: itemType
      allOf:
        - $ref: '#/components/schemas/PageBase'
        - type: object
          required: [items]
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
        // the inherited template goes generic, the dynamic-ref items property is typed as the type parameter
        var template = modelsNamespace!.FindChildByName<CodeClass>("InheritedTemplate", true);
        Assert.NotNull(template);
        Assert.True(template!.IsGeneric);
        Assert.Equal("TItemType", Assert.Single(template.TypeParameters).Name);
        var itemsType = Assert.IsType<CodeType>(template.Properties.First(static x => x.Name == "items").Type);
        Assert.IsType<CodeTypeParameter>(itemsType.TypeDefinition);
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("InheritedTemplateUser", true));
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("InheritedTemplateGroup", true));
        // the base has no dynamic ref and stays concrete
        var pageBase = modelsNamespace.FindChildByName<CodeClass>("PageBase", true);
        Assert.NotNull(pageBase);
        Assert.False(pageBase!.IsGeneric);
        Assert.Same(pageBase, template.StartBlock.Inherits?.TypeDefinition);
        // executor return types carry the bound generic arguments
        Assert.Equal(["Group", "User"], codeModel.FindNamespaceByName("ApiSdk.inherited.users")!.FindChildByName<CodeClass>("UsersRequestBuilder", true)!.Methods
            .Union(codeModel.FindNamespaceByName("ApiSdk.inherited.groups")!.FindChildByName<CodeClass>("GroupsRequestBuilder", true)!.Methods)
            .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get)
            .Select(static x => Assert.IsType<CodeType>(x.ReturnType).GenericTypeParameterValues.Single().Name)
            .OrderBy(static x => x, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GenericInheritedComponentPromotesBaseAndClosesDerivedAsync()
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
  /pages/users:
    get:
      operationId: listUserPages
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
                $ref: '#/components/schemas/DerivedPage'
  /pages/groups:
    get:
      operationId: listGroupPages
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/Group'
                $ref: '#/components/schemas/DerivedPage'
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
    Group:
      type: object
      properties:
        id:
          type: string
    BasePage:
      $dynamicAnchor: itemType
      type: object
      properties:
        items:
          type: array
          items:
            $dynamicRef: '#itemType'
    DerivedPage:
      allOf:
        - $ref: '#/components/schemas/BasePage'
        - type: object
          properties:
            page:
              type: integer
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        // the base resolves items through the active binding so it becomes generic
        var basePage = modelsNamespace!.FindChildByName<CodeClass>("BasePage", true);
        Assert.NotNull(basePage);
        Assert.True(basePage!.IsGeneric);
        var itemsType = Assert.IsType<CodeType>(basePage.Properties.First(static x => x.Name == "items").Type);
        Assert.IsType<CodeTypeParameter>(itemsType.TypeDefinition);
        // the derived class is generic because its base is, and closes over the base argument with its parameter
        var derivedPage = modelsNamespace.FindChildByName<CodeClass>("DerivedPage", true);
        Assert.NotNull(derivedPage);
        Assert.True(derivedPage!.IsGeneric);
        var inherits = derivedPage.StartBlock.Inherits;
        Assert.NotNull(inherits);
        Assert.Same(basePage, inherits!.TypeDefinition);
        var baseArgument = Assert.Single(inherits.GenericTypeParameterValues);
        var derivedParameter = Assert.IsType<CodeTypeParameter>(baseArgument.TypeDefinition);
        Assert.Equal("TItemType", derivedParameter.Name);
        // the derived argument closes over the derived's own parameter, the base keeps owning its instance
        Assert.Same(derivedPage.TypeParameters.Single(), derivedParameter);
        Assert.Same(derivedPage.StartBlock, derivedParameter.Parent);
        Assert.Same(basePage.StartBlock, basePage.TypeParameters.Single().Parent);
        Assert.Equal("TItemType", Assert.Single(derivedPage.TypeParameters).Name);
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("BasePageUser", true));
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("DerivedPageGroup", true));
        // executor return types carry the bound generic arguments
        Assert.Equal(["Group", "User"], codeModel.FindNamespaceByName("ApiSdk.pages.users")!.FindChildByName<CodeClass>("UsersRequestBuilder", true)!.Methods
            .Union(codeModel.FindNamespaceByName("ApiSdk.pages.groups")!.FindChildByName<CodeClass>("GroupsRequestBuilder", true)!.Methods)
            .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get)
            .Select(static x => Assert.IsType<CodeType>(x.ReturnType).GenericTypeParameterValues.Single().Name)
            .OrderBy(static x => x, StringComparer.Ordinal));
    }

    [Fact]
    public async Task MixedBoundAndBareTemplateReferencesNeverEmitOpenGenericsAsync()
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
        '400':
          description: bad request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PaginatedTemplate'
  /wrapped:
    get:
      operationId: getWrapped
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  helper:
                    type: string
                type: object
                properties:
                  page:
                    $ref: '#/components/schemas/PaginatedTemplate'
components:
  schemas:
    User:
      type: object
    PaginatedTemplate:
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
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        // A bare reference makes both usages use the existing concrete-specialization path, independent of
        // traversal order. Neither response may degrade to UntypedNode.
        var executorReturnTypes = codeModel.FindNamespaceByName("ApiSdk.users")!.FindChildByName<CodeClass>("UsersRequestBuilder", true)!.Methods
            .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get)
            .Select(static x => x.ReturnType)
            .OfType<CodeType>();
        foreach (var returnType in executorReturnTypes)
            Assert.IsType<CodeClass>(returnType.TypeDefinition);
        Assert.DoesNotContain(executorReturnTypes, static x => x.Name == "UntypedNode");
        Assert.False(modelsNamespace!.FindChildByName<CodeClass>("PaginatedTemplate", true)!.IsGeneric);
        Assert.False(modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplateUser", true)!.IsGeneric);
    }

    [Fact]
    public async Task InlineTemplateWithNestedInlinePropertiesIsNotHijackedAsSelfReferenceAsync()
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
                type: object
                required: [items, metadata]
                properties:
                  items:
                    type: array
                    items:
                      $dynamicRef: '#itemType'
                  metadata:
                    type: object
                    properties:
                      count:
                        type: integer
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        var usersNamespace = codeModel.FindNamespaceByName("ApiSdk.users");
        Assert.NotNull(usersNamespace);
        // the inline template (no $ref, no reference id) goes generic
        var template = usersNamespace!.FindChildByName<CodeClass>("UsersGetResponse", true);
        Assert.NotNull(template);
        Assert.True(template!.IsGeneric);
        // the nested inline metadata property must materialize its own model, not forward to the template:
        // null reference ids must not compare equal as a self-reference
        var metadataType = Assert.IsType<CodeType>(template.Properties.First(static x => x.Name == "metadata").Type);
        var metadataClass = Assert.IsType<CodeClass>(metadataType.TypeDefinition);
        Assert.NotEqual(template, metadataClass);
        Assert.NotNull(metadataClass.Properties.FirstOrDefault(static x => x.Name == "count"));
        Assert.False(metadataClass.IsGeneric);
        // executor return closes over User through the back-compat shim inheriting the closed template
        var usersRequestBuilder = usersNamespace.FindChildByName<CodeClass>("UsersRequestBuilder", true);
        Assert.NotNull(usersRequestBuilder);
        var executor = usersRequestBuilder!.Methods.First(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get);
        var returnType = Assert.IsType<CodeType>(executor.ReturnType);
        Assert.Equal("UsersResponse", returnType.Name, StringComparer.OrdinalIgnoreCase);
        var shimBase = Assert.IsType<CodeType>(Assert.IsType<CodeClass>(returnType.TypeDefinition).StartBlock.Inherits);
        Assert.Equal("UsersGetResponse", shimBase.Name, StringComparer.OrdinalIgnoreCase);
        var boundArgument = Assert.Single(shimBase.GenericTypeParameterValues);
        Assert.Equal("User", boundArgument.Name);
    }

    [Fact]
    public async Task PromotesNestedInlineModelsInsideGenericTemplatesAsync()
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
  /pages/users:
    get:
      operationId: listUserPages
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
                $ref: '#/components/schemas/PageTemplate'
  /pages/groups:
    get:
      operationId: listGroupPages
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $defs:
                  itemType:
                    $dynamicAnchor: itemType
                    $ref: '#/components/schemas/Group'
                $ref: '#/components/schemas/PageTemplate'
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
    Group:
      type: object
      properties:
        id:
          type: string
    PageTemplate:
      $dynamicAnchor: itemType
      type: object
      properties:
        items:
          type: array
          items:
            $dynamicRef: '#itemType'
        metadata:
          type: object
          properties:
            first:
              $dynamicRef: '#itemType'
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        // the nested inline model is shared (no per-binding suffix) and generic over the template's parameter
        var inlineModel = modelsNamespace!.FindChildByName<CodeClass>("PageTemplate_metadata", true);
        Assert.NotNull(inlineModel);
        Assert.True(inlineModel!.IsGeneric);
        var firstType = Assert.IsType<CodeType>(inlineModel.Properties.First(static x => x.Name == "first").Type);
        var inlineParameter = Assert.IsType<CodeTypeParameter>(firstType.TypeDefinition);
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("PageTemplate_metadataUser", true));
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("PageTemplate_metadataGroup", true));
        // the template's metadata property stays open over the type parameter, closing only at the usage sites
        var template = modelsNamespace.FindChildByName<CodeClass>("PageTemplate", true);
        Assert.NotNull(template);
        Assert.True(template!.IsGeneric);
        var metadataType = Assert.IsType<CodeType>(template.Properties.First(static x => x.Name == "metadata").Type);
        Assert.Same(inlineModel, metadataType.TypeDefinition);
        var templateParameter = Assert.IsType<CodeTypeParameter>(Assert.Single(metadataType.GenericTypeParameterValues).TypeDefinition);
        Assert.NotSame(templateParameter, inlineParameter);
        Assert.Same(template.StartBlock, templateParameter.Parent);
        Assert.Same(inlineModel.StartBlock, inlineParameter.Parent);
        var itemType = Assert.IsType<CodeType>(template.Properties.First(static x => x.Name == "items").Type);
        Assert.Same(templateParameter, itemType.TypeDefinition);
        // executor return types still close over the bound types
        Assert.Equal(["Group", "User"], codeModel.FindNamespaceByName("ApiSdk.pages.users")!.FindChildByName<CodeClass>("UsersRequestBuilder", true)!.Methods
            .Union(codeModel.FindNamespaceByName("ApiSdk.pages.groups")!.FindChildByName<CodeClass>("GroupsRequestBuilder", true)!.Methods)
            .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get)
            .Select(static x => Assert.IsType<CodeType>(x.ReturnType).GenericTypeParameterValues.Single().Name)
            .OrderBy(static x => x, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(GenerationLanguage.PHP)]
    [InlineData(GenerationLanguage.Ruby)]
    public async Task KeepsConcreteSpecializationsForNonGenericLanguagesAsync(GenerationLanguage language)
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
components:
  schemas:
    User:
      type: object
    PaginatedTemplate:
      $dynamicAnchor: itemType
      type: object
      properties:
        items:
          type: array
          items:
            $dynamicRef: '#itemType'
""", cancellationToken: TestContext.Current.CancellationToken);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { Language = language, ClientClassName = "ApiSdk", OpenAPIFilePath = tempFilePath }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs, cancellationToken: TestContext.Current.CancellationToken);
        var codeModel = builder.CreateSourceModel(builder.CreateUriSpace(document!));

        // PHP and Ruby have no generics: bindings keep producing concrete per-binding specializations permanently
        var modelsNamespace = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNamespace);
        var concrete = modelsNamespace!.FindChildByName<CodeClass>("PaginatedTemplateUser", true);
        Assert.NotNull(concrete);
        Assert.False(concrete!.IsGeneric);
        Assert.Null(modelsNamespace.FindChildByName<CodeClass>("PaginatedTemplate", true));
    }
}
