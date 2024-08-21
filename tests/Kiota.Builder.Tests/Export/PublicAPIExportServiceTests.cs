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
        otherNames:
          type: array
          items:
            type: string
            nullable: true
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
            ClientNamespaceName = "exportNamespace",
            OutputPath = Path.GetTempPath()
        };
        var builder = new KiotaBuilder(mockLogger.Object, generationConfig, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(testDocumentStream);

        Assert.NotNull(document);

        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinement(generationConfig, codeModel, default);

        // serialize the dom model
        var exportService = new PublicApiExportService(generationConfig);
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
        Assert.Contains("ExportNamespace.Me.Get.getRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:Action<RequestConfiguration<DefaultQueryParameters>>):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:IParseNode):global.ExportNamespace.Models.Microsoft.Graph.User", exportContents);// captures static methods too :)
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.importance::0000-low", exportContents);// captures enum members
        Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|public|OtherNames:List<string>", exportContents);// captures collection info in language specific format    
    }

    private static void ValidateExportJava(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportnamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportnamespace.models.microsoft.graph.User~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|setId(value?:String):void", exportContents);// captures property setter location,type and access
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|getId():String", exportContents);// captures property getter location,type and access
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|constructor():void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportnamespace.me.MeRequestBuilder::|public|toGetRequestInformation(requestConfiguration?:java.util.function.Consumer<GetRequestConfiguration>):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|static|public|createFromDiscriminatorValue(parseNode:ParseNode):User", exportContents);// captures static methods too :)
        Assert.Contains("exportnamespace.models.microsoft.graph.Importance::0000-Low", exportContents);// captures enum members
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|getOtherNames():java.util.List<String>", exportContents);// captures collection info in language specific format
        Assert.Contains("exportnamespace.models.microsoft.graph.User::|public|setOtherNames(value?:java.util.List<String>):void", exportContents);// captures collection info in language specific format
    }

    private static void ValidateExportGo(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph-->*i2ae4187f7daee263371cb1c977df639813ab50ffa529013b7437480d1ec0158f.BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.userable~~>*i878a80d2330e89d26896388a3f487eef27b0a0e6c010c493bf80be1452208f91.AdditionalDataHolder; *i878a80d2330e89d26896388a3f487eef27b0a0e6c010c493bf80be1452208f91.Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.user~~>Userable", exportContents);// captures implemented MODEL interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.userable::|public|GetId():*string", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.userable::|public|SetId(value:*string):void", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|GetId():*string", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|SetId(value:*string):void", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.me.GetRequestBuilder::|public|constructor(rawUrl:string; requestAdapter:RequestAdapter):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.GetRequestBuilder::|public|ToGetRequestInformation(ctx:context.Context; requestConfiguration?:*GetRequestBuilderGetRequestConfiguration):*RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:ParseNode):Parsable", exportContents);// captures static methods too :)
        Assert.Contains("exportNamespace.models.microsoft.graph.importance::0000-low", exportContents);// captures enum members
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|GetOtherNames():[]string", exportContents);// captures collection info in language specific format
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|SetOtherNames(value:[]string):void", exportContents);// captures collection info in language specific format
    }

    private static void ValidateExportPython(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.User~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|id():str", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|id(value:str):None", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.me.MeRequestBuilder::|public|constructor(path_parameters:Union[str, Dict[str, Any]]; request_adapter:RequestAdapter):None", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.get.GetRequestBuilder::|public|to_get_request_information(request_configuration?:RequestConfiguration[QueryParameters]):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|static|public|create_from_discriminator_value(parse_node:ParseNode):User", exportContents);// captures static methods too :)
        Assert.Contains("exportNamespace.models.microsoft.graph.Importance::0000-Low", exportContents);// captures enum members
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|other_names():List[str]", exportContents);// captures collection info in language specific format
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|other_names(value:List[str]):None", exportContents);// captures collection info in language specific format
    }

    private static void ValidateExportTypeScript(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph~~>BaseRequestBuilder<Graph>", exportContents); // captures class inheritance. TS does not do inheritance due to interfaces.
        Assert.Contains("exportNamespace.models.microsoft.graph.User~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|id:string", exportContents);// captures property location,type and access inheritance. No getter/setter in TS
        // NOTE: No constructors in TS
        Assert.Contains("exportNamespace.me.meRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:RequestConfiguration<object>):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph::createUserFromDiscriminatorValue(parseNode:ParseNode):User", exportContents);// captures code functions
        Assert.Contains("exportNamespace.models.microsoft.graph::deserializeIntoUser(User:User={}):Record<string, (node: ParseNode) => void>", exportContents);// captures code functions and default params
        Assert.Contains("exportNamespace.models.microsoft.graph.importance::0000-low", exportContents);// captures enum members
        Assert.Contains("exportNamespace.models.microsoft.graph.importanceObject", exportContents);// captures enum code constant object
        Assert.Contains("exportNamespace.graphNavigationMetadata", exportContents);// captures navigation metadata code constant object
        Assert.Contains("exportNamespace.me.get.getRequestBuilderRequestsMetadata", exportContents);// captures request builder metadata code constant object
        Assert.Contains("exportNamespace.models.microsoft.graph.User::|public|otherNames:string[]", exportContents);// captures collection info in language specific format
    }

    private static void ValidateExportPhp(string[] exportContents)
    {
        Assert.NotEmpty(exportContents);
        Assert.Contains("exportNamespace.Graph-->BaseRequestBuilder", exportContents); // captures class inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.user~~>AdditionalDataHolder; Parsable", exportContents);// captures implemented interfaces
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|getId():string", exportContents);// captures property getter location,type and access inheritance
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|setId(value?:string):void", exportContents);// captures property setter location,type and access inheritance
        Assert.Contains("exportNamespace.me.get.getRequestBuilderGetRequestConfiguration::|public|constructor(headers?:array; options?:array):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.MeRequestBuilder::|public|constructor(pathParameters:array; requestAdapter:RequestAdapter):void", exportContents); // captures constructors, their parameters(name and types), return and access 
        Assert.Contains("exportNamespace.me.get.GetRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:GetRequestBuilderGetRequestConfiguration):RequestInformation", exportContents);// captures methods, their parameters(name and types), return and access
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:ParseNode):User", exportContents);// captures static methods too :)
        Assert.Contains("exportNamespace.models.microsoft.graph.importance::0000-low", exportContents);// captures enum members
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|getOtherNames():array", exportContents);// captures collection info in language specific format
        Assert.Contains("exportNamespace.models.microsoft.graph.user::|public|setOtherNames(value?:array):void", exportContents);// captures collection info in language specific format
    }
}
