using Microsoft.OpenApi.Services;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Moq;
using Xunit;
using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using System.Threading.Tasks;
using System.IO;
using System;

namespace Kiota.Builder.Tests;
public class KiotaBuilderTests
{
    [Fact]
    public async Task ThrowsOnMissingServer() {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllLinesAsync(tempFilePath, new string[] {"openapi: 3.0.0", "info:", "  title: \"Todo API\"", "  version: \"1.0.0\""});
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath });
        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.GenerateSDK());
        File.Delete(tempFilePath);
    }
    [Fact]
    public async Task DoesntThrowOnMissingServerForV2() {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllLinesAsync(tempFilePath, new string[] {"swagger: 2.0", "title: \"Todo API\"", "version: \"1.0.0\"", "host: mytodos.doesntexit", "basePath: v2", "schemes:", " - https"," - http"});
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath });
        await builder.GenerateSDK();
        File.Delete(tempFilePath);
    }
    [Fact]
    public void Single_root_node_creates_single_request_builder_class()
    {
        var node = OpenApiUrlTreeNode.Create();
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);

        Assert.Single(codeModel.GetChildElements(true));
    }
    [Fact]
    public void Single_path_with_get_collection()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem() {
            Operations = {
                [OperationType.Get] = new OpenApiOperation() { 
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);

        var rootNamespace = codeModel.GetChildElements(true).Single();
        var rootBuilder = rootNamespace.GetChildElements(true).OfType<CodeClass>().Single(e => e.Name == "Graph");
        var tasksProperty = rootBuilder.Properties.Single(e => e.Name.Equals("Tasks", StringComparison.OrdinalIgnoreCase));
        var tasksRequestBuilder = tasksProperty.Type as CodeType;
        Assert.NotNull(tasksRequestBuilder);
        var getMethod = (tasksRequestBuilder.TypeDefinition as CodeClass).Methods.Single(e => e.Name == "Get");
        var returnType = getMethod.ReturnType;
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Array, returnType.CollectionKind);
    }
    [Fact]
    public void OData_doubles_as_any_of(){
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem() {
            Operations = {
                [OperationType.Get] = new OpenApiOperation() { 
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress", true);
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void Object_Arrays_are_supported() {
        var userSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference() {
                Id = "#/components/schemas/microsoft.graph.user"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["users/{id}"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse() {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType() {
                                            Schema = new OpenApiSchema {
                                                Type = "object",
                                                Properties = new Dictionary<string, OpenApiSchema> {
                                                    {
                                                        "value", new OpenApiSchema {
                                                            Type = "array",
                                                            Items = userSchema
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
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.user", userSchema
                    }
                }
            }
        };
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        builder.CreateUriSpace(document);//needed so the component index exists
        var codeModel = builder.CreateSourceModel(node);
        var userClass = codeModel.FindNamespaceByName("ApiSdk.models").FindChildByName<CodeClass>("user");
        Assert.NotNull(userClass);
    }
    [Fact]
    public void Supports_Path_Parameters() {
        var resourceActionSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference() {
                Id = "#/components/schemas/microsoft.graph.resourceAction"
            },
            UnresolvedReference = false
        };
        var permissionSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference() {
                Id = "#/components/schemas/microsoft.graph.rolePermission"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["/deviceManagement/microsoft.graph.getEffectivePermissions(scope='{scope}')"] = new OpenApiPathItem() {
                    Parameters = {
                        new OpenApiParameter() {
                            Name = "scope",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    },
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse() {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType() {
                                            Schema = new OpenApiSchema {
                                                Type = "array",
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
                },
            },
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    { "microsoft.graph.rolePermission", permissionSchema },
                    { "microsoft.graph.resourceAction", resourceActionSchema },
                }
            }
        };
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        builder.CreateUriSpace(document);//needed so the component index exists
        var codeModel = builder.CreateSourceModel(node);
        var deviceManagementNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement");
        Assert.NotNull(deviceManagementNS);
        var deviceManagementRequestBuilder = deviceManagementNS.FindChildByName<CodeClass>("DeviceManagementRequestBuilder");
        Assert.NotNull(deviceManagementRequestBuilder);
        var getEffectivePermissionsMethod = deviceManagementRequestBuilder.FindChildByName<CodeMethod>("getEffectivePermissionsWithScope");
        Assert.NotNull(getEffectivePermissionsMethod);
        Assert.Single(getEffectivePermissionsMethod.Parameters);
        var getEffectivePermissionsNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement.getEffectivePermissionsWithScope");
        Assert.NotNull(getEffectivePermissionsNS);
        var getEffectivePermissionsRequestBuilder = getEffectivePermissionsNS.FindChildByName<CodeClass>("GetEffectivePermissionsWithScopeRequestBuilder");
        Assert.NotNull(getEffectivePermissionsRequestBuilder);
        var constructorMethod = getEffectivePermissionsRequestBuilder.FindChildByName<CodeMethod>("constructor");
        Assert.NotNull(constructorMethod);
        Assert.Single(constructorMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
    }
    [Fact]
    public void Inline_Property_Inheritance_Is_Supported() {
        var resourceSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "info", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference() {
                Id = "resource"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["resource/{id}"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse() {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType() {
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
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "#/components/resource", resourceSchema
                    }
                }
            }
        };
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        builder.CreateUriSpace(document);//needed so the component index exists
        var codeModel = builder.CreateSourceModel(node);
        var resourceClass = codeModel.FindNamespaceByName("ApiSdk.models").FindChildByName<CodeClass>("resource");
        var responseClass = codeModel.FindNamespaceByName("ApiSdk.resource.item").FindChildByName<CodeClass>("WithResponse");
        var derivedResourceClass = codeModel.FindNamespaceByName("ApiSdk.resource.item").FindChildByName<CodeClass>("WithResponse_derivedResource");
        var derivedResourceInfoClass = codeModel.FindNamespaceByName("ApiSdk.resource.item").FindChildByName<CodeClass>("WithResponse_derivedResource_info");

        
        Assert.NotNull(resourceClass);
        Assert.NotNull(derivedResourceClass);
        Assert.NotNull(derivedResourceClass.StartBlock);
        Assert.Equal((derivedResourceClass.StartBlock as CodeClass.Declaration).Inherits.TypeDefinition, resourceClass);
        Assert.NotNull(derivedResourceInfoClass);
        Assert.NotNull(responseClass);
    }
    [Fact]
    public void MapsTime(){
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem() {
            Operations = {
                [OperationType.Get] = new OpenApiOperation() { 
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress", true);
        Assert.Equal("TimeOnly", progressProp.Type.Name);
    }
    [Fact]
    public void MapsDate(){
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem() {
            Operations = {
                [OperationType.Get] = new OpenApiOperation() { 
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress", true);
        Assert.Equal("DateOnly", progressProp.Type.Name);
    }
    [Fact]
    public void MapsDuration(){
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem() {
            Operations = {
                [OperationType.Get] = new OpenApiOperation() { 
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var progressProp = codeModel.FindChildByName<CodeProperty>("progress", true);
        Assert.Equal("TimeSpan", progressProp.Type.Name);
    }
    [Fact]
    public void AddsErrorMapping(){
        var node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem() {
            Operations = {
                [OperationType.Get] = new OpenApiOperation() { 
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
                        ["4XX"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
                        ["5XX"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
                        ["401"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var executorMethod = codeModel.FindChildByName<CodeMethod>("get", true);
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        Assert.Contains("4XX", executorMethod.ErrorMappings.Keys);
        Assert.Contains("401", executorMethod.ErrorMappings.Keys);
        Assert.Contains("5XX", executorMethod.ErrorMappings.Keys);
        var errorType401 = codeModel.FindChildByName<CodeClass>("tasks401Error", true);
        Assert.NotNull(errorType401);
        Assert.True(errorType401.IsErrorDefinition);
        Assert.NotNull(errorType401.FindChildByName<CodeProperty>("authenticationRealm", true));
        var errorType4XX = codeModel.FindChildByName<CodeClass>("tasks4XXError", true);
        Assert.NotNull(errorType4XX);
        Assert.True(errorType4XX.IsErrorDefinition);
        Assert.NotNull(errorType4XX.FindChildByName<CodeProperty>("errorId", true));
        var errorType5XX = codeModel.FindChildByName<CodeClass>("tasks5XXError", true);
        Assert.NotNull(errorType5XX);
        Assert.True(errorType5XX.IsErrorDefinition);
        Assert.NotNull(errorType5XX.FindChildByName<CodeProperty>("serviceErrorId", true));

    }
    [Fact]
    public void DoesntAddSuffixesToErrorTypesWhenComponents(){
        var errorSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "errorId", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "microsoft.graph.error",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var errorResponse = new OpenApiResponse()
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = errorSchema
                }
            },
            Reference = new OpenApiReference {
                Id = "microsoft.graph.error",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["tasks"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse()
                                {
                                    Content =
                                    {
                                        ["application/json"] = new OpenApiMediaType()
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
            Components = new OpenApiComponents() {
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
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var executorMethod = codeModel.FindChildByName<CodeMethod>("get", true);
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        Assert.Contains("4XX", executorMethod.ErrorMappings.Keys);
        Assert.Contains("401", executorMethod.ErrorMappings.Keys);
        Assert.Contains("5XX", executorMethod.ErrorMappings.Keys);
        var errorType = codeModel.FindChildByName<CodeClass>("Error", true);
        Assert.NotNull(errorType);
        Assert.True(errorType.IsErrorDefinition);
        Assert.NotNull(errorType.FindChildByName<CodeProperty>("errorId", true));
        
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks401Error", true));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks4XXError", true));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks5XXError", true));
    }

    [Fact]
    public void AddsDiscriminatorMappings(){
        var entitySchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Discriminator = new() {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, string> {
                    {
                        "#microsoft.graph.directoryObject", "#/components/schemas/microsoft.graph.directoryObject"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "microsoft.graph.entity",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var directoryObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "microsoft.graph.directoryObject",
                Type = ReferenceType.Schema
            },
            AllOf = new List<OpenApiSchema> {
                entitySchema
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
            Reference = new OpenApiReference {
                Id = "microsoft.graph.directoryObjects",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["objects"] = new OpenApiPathItem() {
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
            Components = new OpenApiComponents() {
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var entityClass = codeModel.FindChildByName<CodeClass>("entity", true);
        var directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject", true);
        Assert.NotNull(entityClass);
        Assert.Equal("@odata.type", entityClass.DiscriminatorPropertyName);
        Assert.NotEmpty(entityClass.DiscriminatorMappings);
        Assert.True(entityClass.DiscriminatorMappings.TryGetValue("#microsoft.graph.directoryObject", out var directoryObjectMappingType));
        var castType = directoryObjectMappingType as CodeType;
        Assert.NotNull(castType);
        Assert.NotNull(castType.TypeDefinition);
        Assert.Equal(directoryObjectClass, castType.TypeDefinition);
    }

}
