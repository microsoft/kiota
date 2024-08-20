using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Export;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Export;

public class PublicApiExportServiceTests
{
    private readonly HttpClient _httpClient = new();
    private static Task<Stream> GetTestDocumentStream()
    {
        return KiotaBuilderTests.GetDocumentStream(@"openapi: 3.0.0
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
          type: string
        importance:
          $ref: '#/components/schemas/microsoft.graph.importance'
    microsoft.graph.importance:
      title: importance
      enum:
        - low
        - normal
        - high
      type: string");
    }

    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new PublicApiExportService(null));
        Assert.Throws<ArgumentException>(() => new PublicApiExportService(string.Empty));
    }

    private static readonly Dictionary<GenerationLanguage, Action<string[]>> Validators = new()
    {
        { GenerationLanguage.CSharp, ValidateExportCSharp },
        { GenerationLanguage.Go, ValidateExportGo },
        { GenerationLanguage.Python, ValidateExportPython },
        { GenerationLanguage.TypeScript, ValidateExportTypeScript },
        { GenerationLanguage.Java, ValidateExportJava },
        { GenerationLanguage.PHP, ValidateExportPhp },
    };

    [Theory]
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.PHP)]
    public async Task GeneratesExportsAndFileHasExpectedAssertions(GenerationLanguage generationLanguage)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await using var testDocumentStream = await GetTestDocumentStream();
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var generationConfig = new GenerationConfiguration
        {
            ClientClassName = "Graph",
            OpenAPIFilePath = tempFilePath,
            Language = generationLanguage,
            ClientNamespaceName = "exportNamespace"
        };
        var builder = new KiotaBuilder(mockLogger.Object, generationConfig, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(testDocumentStream);

        Assert.NotNull(document);

        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinement(generationConfig, codeModel, default);

        // serialize the dom model
        var exportService = new PublicApiExportService(Path.GetTempPath());
        await exportService.SerializeDomAsync(codeModel);

        // validate the export exists
        var exportPath = Path.Join(Path.GetTempPath(), "kiota-dom-export.txt");
        Assert.True(File.Exists(exportPath));
        if (!Validators.TryGetValue(generationLanguage, out var validator))
        {
            Assert.Fail($"No Validator present for language {generationLanguage}");
        }
        // run the language validator
        var contents = File.ReadLines(exportPath).ToArray();
        validator.Invoke(contents);
    }

    private static void ValidateExportCSharp(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("ExportNamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user~~>IAdditionalDataHolder; IParsable", exportContents);// captures implemented interfaces
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|public|Id:string", exportContents);// captures property location,type and access
        Assert.Contains("ExportNamespace.Me.Get.getRequestBuilder::|public|constructor(rawUrl:string; requestAdapter:IRequestAdapter):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("ExportNamespace.Me.Get.getRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:RequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:IParseNode):ExportNamespace.Models.Microsoft.Graph.user", exportContents);// captures static methods too :)
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.importance::0000-low", exportContents);// captures enum members
    }

    private static void ValidateExportJava(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportnamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportnamespace.models.microsoft.graph.User~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|setId(value?:String):void", exportContents);// captures property setter location,type and access
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|getId():String", exportContents);// captures property getter location,type and access
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|constructor():void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportnamespace.me.MeRequestBuilder::|public|toGetRequestInformation(requestConfiguration?:exportnamespace.me.MeRequestBuilder.GetRequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|static|public|createFromDiscriminatorValue(parseNode:ParseNode):exportnamespace.models.microsoft.graph.User", exportContents);// captures static methods too :)
        Assert.Contains("exportnamespace.models.microsoft.graph.Importance::0000-Low", exportContents);// captures enum members
    }

    private static void ValidateExportGo(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.userable~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.user~~>exportNamespace.models.microsoft.graph.userable", exportContents);// captures implemented MODEL interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.userable::|public|GetId():string", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.userable::|public|SetId(value:string):void", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.me.GetRequestBuilder::|public|constructor(rawUrl:string; requestAdapter:RequestAdapter):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.GetRequestBuilder::|public|ToGetRequestInformation(ctx:context.Context; requestConfiguration?:exportNamespace.me.GetRequestBuilder.GetRequestBuilderGetRequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:ParseNode):Parsable", exportContents);// captures static methods too :)
        Assert.Contains("exportNamespace.models.microsoft.graph.importance::0000-low", exportContents);// captures enum members
    }

    private static void ValidateExportPython(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.User~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|id():String", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|id(value:String):void", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.me.MeRequestBuilder::|public|constructor(path_parameters:Union[str, Dict[str, Any]]; request_adapter:RequestAdapter):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.get.GetRequestBuilder::|public|to_get_request_information(request_configuration?:RequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|static|public|create_from_discriminator_value(parse_node:ParseNode):exportNamespace.models.microsoft.graph.User", exportContents);// captures static methods too :)
        Assert.Contains("exportNamespace.models.microsoft.graph.Importance::0000-Low", exportContents);// captures enum members
    }

    private static void ValidateExportTypeScript(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph~~>BaseRequestBuilder", exportContents); // captures class inheritance. TS does not do inheritance due to interfaces.
        Assert.Contains("exportNamespace.models.microsoft.graph.User~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|id:string", exportContents);// captures property location,type and access inheritance. No getter/setter in TS
        // NOTE: No constructors in TS
        Assert.Contains("exportNamespace.me.meRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:RequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph::createUserFromDiscriminatorValue(parseNode:ParseNode):exportNamespace.models.microsoft.graph.user", exportContents);// captures code functions
        Assert.Contains("exportNamespace.models.microsoft.graph::deserializeIntoUser(User:exportNamespace.models.microsoft.graph.User={}):Record<string, (node: ParseNode) => void>", exportContents);// captures code functions and default params
        Assert.Contains("exportNamespace.models.microsoft.graph.importance::0000-low", exportContents);// captures enum members
        Assert.Contains("exportNamespace.models.microsoft.graph.importanceObject", exportContents);// captures enum code constant object
        Assert.Contains("exportNamespace.graphNavigationMetadata", exportContents);// captures navigation metadata code constant object
        Assert.Contains("exportNamespace.me.get.getRequestBuilderRequestsMetadata", exportContents);// captures request builder metadata code constant object
    }

    private static void ValidateExportPhp(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.user~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|getId():string", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|setId(value?:string):void", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.me.get.getRequestBuilderGetRequestConfiguration::|public|constructor(headers?:[array]; options?:[array]):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.MeRequestBuilder::|public|constructor(pathParameters:[array]; requestAdapter:RequestAdapter):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.get.GetRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:exportNamespace.me.get.getRequestBuilderGetRequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:ParseNode):exportNamespace.models.microsoft.graph.user", exportContents);// captures static methods too :)
        Assert.Contains("exportNamespace.models.microsoft.graph.importance::0000-low", exportContents);// captures enum members
    }
}
