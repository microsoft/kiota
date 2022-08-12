﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests;
public class KiotaBuilderTests
{
    [Fact]
    public async Task ThrowsOnMissingServer() {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllLinesAsync(tempFilePath, new string[] {"openapi: 3.0.0", "info:", "  title: \"Todo API\"", "  version: \"1.0.0\""});
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath });
        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.GenerateSDK(new()));
        File.Delete(tempFilePath);
    }
    [Fact]
    public async Task ParsesEnumDescriptions() {
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
        - Standard_LRS
        - Standard_ZRS
        - Standard_GRS
        - Standard_RAGRS
        - Premium_LRS
      x-ms-enum:
        name: AccountType
        modelAsString: false
        values:
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath });
        using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = builder.CreateOpenApiDocument(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        var enumDef = modelsNS.FindChildByName<CodeEnum>("StorageAccountType", false);
        Assert.NotNull(enumDef);
        var firstOption = enumDef.Options.First();
        Assert.Equal("Standard_LRS", firstOption.SerializationName);
        Assert.Equal("StandardLocalRedundancy", firstOption.Name);
        Assert.NotEmpty(firstOption.Description);
        File.Delete(tempFilePath);
    }
    [Fact]
    public async Task DoesntThrowOnMissingServerForV2() {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllLinesAsync(tempFilePath, new string[] {"swagger: 2.0", "title: \"Todo API\"", "version: \"1.0.0\"", "host: mytodos.doesntexit", "basePath: v2", "schemes:", " - https"," - http"});
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath });
        await builder.GenerateSDK(new());
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
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, returnType.CollectionKind);
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
    public void TextPlainEndpointsAreSupported() {
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["users/$count"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse() {
                                    Content = {
                                        ["text/plain"] = new OpenApiMediaType() {
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
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        builder.CreateUriSpace(document);//needed so the component index exists
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
            Reference = new OpenApiReference()
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
            Reference = new OpenApiReference()
            {
                Id = "#/components/schemas/microsoft.graph.rolePermission"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument()
        {
            Paths = new OpenApiPaths()
            {
                ["/deviceManagement/microsoft.graph.getEffectivePermissions(scope='{scope}')"] = new OpenApiPathItem()
                {
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
            Components = new OpenApiComponents()
            {
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
            Reference = new OpenApiReference()
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
            Reference = new OpenApiReference()
            {
                Id = "#/components/schemas/microsoft.graph.rolePermission"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument()
        {
            Paths = new OpenApiPaths()
            {
                ["/deviceManagement/microsoft.graph.getEffectivePermissions(scope='{scope}')"] = new OpenApiPathItem()
                {
                    Parameters = {
                        new OpenApiParameter() {
                            Name = "scope",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter()
                        {
                            Name = "select",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        },
                        new OpenApiParameter()
                        {
                            Name = "If-Match",
                            In = ParameterLocation.Header,
                            Description = "ETag",
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            },
                        },
                        new OpenApiParameter()
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
                        [OperationType.Get] = new OpenApiOperation() {
                            Responses = new OpenApiResponses {
                                ["200"] = new OpenApiResponse() {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType() {
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
            Components = new OpenApiComponents()
            {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    { "microsoft.graph.rolePermission", permissionSchema },
                    { "microsoft.graph.resourceAction", resourceActionSchema },
                }
            }
        };
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost", Language = GenerationLanguage.Shell });
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
        var parameters = getEffectivePermissionsRequestBuilder
            .Methods
            .SingleOrDefault(cm => cm.IsOfKind(CodeMethodKind.RequestGenerator) && cm.HttpMethod == HttpMethod.Get)?
            .PathQueryAndHeaderParameters;
        Assert.Equal(4, parameters.Count());
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "IfMatch" && p.Kind == CodeParameterKind.Headers));
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "ConsistencyLevel" && p.Kind == CodeParameterKind.Headers));
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "select" && p.Kind == CodeParameterKind.QueryParameter));
        Assert.NotNull(parameters.SingleOrDefault(p => p.Name == "scope" && p.Kind == CodeParameterKind.Path));
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
        var keys = executorMethod.ErrorMappings.Select(x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.Contains("5XX", keys);
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
    public void IgnoresErrorCodesWithNoSchema(){
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
                            }
                        },
                        ["5XX"] = new OpenApiResponse()
                        {
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        },
                        ["401"] = new OpenApiResponse()
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var executorMethod = codeModel.FindChildByName<CodeMethod>("get", true);
        Assert.NotNull(executorMethod);
        Assert.Empty(executorMethod.ErrorMappings);
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
        var keys = executorMethod.ErrorMappings.Select(x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.Contains("5XX", keys);
        var errorType = codeModel.FindChildByName<CodeClass>("Error", true);
        Assert.NotNull(errorType);
        Assert.True(errorType.IsErrorDefinition);
        Assert.NotNull(errorType.FindChildByName<CodeProperty>("errorId", true));
        
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks401Error", true));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks4XXError", true));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks5XXError", true));
    }
    [Fact]
    public void DoesntAddPropertyHolderOnNonAdditionalModels(){
        var weatherForecastSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference {
                Id = "weatherForecast",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var forecastResponse = new OpenApiResponse()
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = weatherForecastSchema
                }
            },
            Reference = new OpenApiReference {
                Id = "weatherForecast",
                Type = ReferenceType.Response
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["weatherforecast"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Responses = new OpenApiResponses
                            {
                                ["200"] = forecastResponse
                            }
                        }
                    } 
                }
            },
            Components = new OpenApiComponents() {
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
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var weatherType = codeModel.FindChildByName<CodeClass>("WeatherForecast", true);
        Assert.NotNull(weatherType);
        Assert.Empty(weatherType.StartBlock.Implements.Where(x => x.Name.Equals("IAdditionalDataHolder", StringComparison.OrdinalIgnoreCase)));
        Assert.Empty(weatherType.Properties.Where(x => x.IsOfKind(CodePropertyKind.AdditionalData)));
    }
    [Fact]
    public void SquishesLonelyNullables(){
        var uploadSessionSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference {
                Id = "microsoft.graph.uploadSession",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["createUploadSession"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse() {
                                    Content = new Dictionary<string, OpenApiMediaType> {
                                        ["application/json"] = new OpenApiMediaType() {
                                            Schema = new OpenApiSchema() {
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
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.uploadSession", uploadSessionSchema
                    }
                },
            },
        };
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var responseClass = codeModel.FindChildByName<CodeClass>("CreateUploadSessionResponse", true);
        Assert.Null(responseClass);
        var sessionClass = codeModel.FindChildByName<CodeClass>("UploadSession", true);
        Assert.NotNull(sessionClass);
        var requestBuilderClass = codeModel.FindChildByName<CodeClass>("createUploadSessionRequestBuilder", true);
        Assert.NotNull(requestBuilderClass);
        var executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        Assert.True(executorMethod.ReturnType is CodeType); // not union
        Assert.Null(codeModel.FindChildByName<CodeClass>("createUploadSessionResponseMember1", true));
    }
    [Fact]
    public void SquishesLonelyNullablesBothAnyOf(){
        var uploadSessionSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference {
                Id = "microsoft.graph.uploadSession",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["createUploadSession"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse() {
                                    Content = new Dictionary<string, OpenApiMediaType> {
                                        ["application/json"] = new OpenApiMediaType() {
                                            Schema = new OpenApiSchema() {
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
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "microsoft.graph.uploadSession", uploadSessionSchema
                    }
                },
            },
        };
        var node = OpenApiUrlTreeNode.Create(document, "default");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var codeModel = builder.CreateSourceModel(node);
        var responseClass = codeModel.FindChildByName<CodeClass>("CreateUploadSessionResponse", true);
        Assert.Null(responseClass);
        var sessionClass = codeModel.FindChildByName<CodeClass>("UploadSession", true);
        Assert.NotNull(sessionClass);
        var requestBuilderClass = codeModel.FindChildByName<CodeClass>("createUploadSessionRequestBuilder", true);
        Assert.NotNull(requestBuilderClass);
        var executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        Assert.True(executorMethod.ReturnType is CodeType); // not union
        Assert.Null(codeModel.FindChildByName<CodeClass>("createUploadSessionResponseMember1", true));
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
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.entity")
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
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
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = "string",
                        Default = new OpenApiString("#microsoft.graph.directoryObject")
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
        var factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", factoryMethod.DiscriminatorPropertyName);
        Assert.NotEmpty(factoryMethod.DiscriminatorMappings);
        var doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Empty(doFactoryMethod.DiscriminatorMappings);
        if(factoryMethod.GetDiscriminatorMappingValue("#microsoft.graph.directoryObject") is not CodeType castType)
            throw new InvalidOperationException("Discriminator mapping value is not a CodeType");
        Assert.NotNull(castType.TypeDefinition);
        Assert.Equal(directoryObjectClass, castType.TypeDefinition);
        var doTypeProperty = directoryObjectClass.Properties.First(x => x.Name.Equals("ODataType", StringComparison.OrdinalIgnoreCase));
        Assert.True(doTypeProperty.ExistsInBaseType);
        Assert.Equal("\"#microsoft.graph.directoryObject\"", doTypeProperty.DefaultValue);
    }
    [Fact]
    public void UnionOfPrimitiveTypesWorks() {
        var simpleObjet = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "subNS.simpleObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["unionType"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.simpleObject", simpleObjet
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeComposedTypeBase;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        var typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("int64", typeNames);
    }
    [Fact]
    public void UnionOfInlineSchemasWorks() {
        var simpleObjet = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "subNS.simpleObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["unionType"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new OpenApiComponents() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subNS.simpleObject", simpleObjet
                    }
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        var requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        var requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        var executorReturnType = requestExecutorMethod.ReturnType as CodeComposedTypeBase;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        var typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("unionTypeResponseMember1", typeNames);
    }
    [Fact]
    public void InheritedTypeWithInlineSchemaWorks() {
        var baseObjet = new OpenApiSchema {
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
            Discriminator = new OpenApiDiscriminator {
                PropertyName = "kind",
                Mapping = new Dictionary<string, string> {
                    {
                        "derivedObject", "#/components/schemas/subNS.derivedObject"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "subNS.baseObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var derivedObjet = new OpenApiSchema {
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
            Reference = new OpenApiReference {
                Id = "subNS.derivedObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var secondLevelDerivedObject = new OpenApiSchema {
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
            Reference = new OpenApiReference {
                Id = "subNS.secondLevelDerivedObject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["derivedType"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new OpenApiComponents() {
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
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
        Assert.Contains("DerivedObject", requestExecutorMethod.ReturnType.Name);
        var secondLevelDerivedClass = codeModel.FindChildByName<CodeClass>("derivedObject", true);
        Assert.NotNull(secondLevelDerivedObject);
        var factoryMethod = secondLevelDerivedClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("kind", factoryMethod.DiscriminatorPropertyName);
        Assert.NotEmpty(factoryMethod.DiscriminatorMappings);
    }
    [InlineData("string", "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData("string", "commonmark", "string")]
    [InlineData("string", "html", "string")]
    [InlineData("string", "date-time", "DateTimeOffset")]
    [InlineData("string", "duration", "TimeSpan")]
    [InlineData("string", "date", "DateOnly")]
    [InlineData("string", "time", "TimeOnly")]
    [InlineData("string", "base64url", "binary")]
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
    [InlineData("number", "", "int64")]
    [InlineData("integer", "", "integer")]
    [InlineData("boolean", "", "boolean")]
    [InlineData("", "byte", "binary")]
    [InlineData("", "binary", "binary")]
    [InlineData("file", null, "binary")]
    [Theory]
    public void MapsPrimitiveFormats(string type, string format, string expected){
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["primitive"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var requestBuilder = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilder", true);
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
    [InlineData("string", "base64url", "binary")]
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
    [InlineData("number", "", "int64")]
    [InlineData("integer", "", "integer")]
    [InlineData("boolean", "", "boolean")]
    [InlineData("", "byte", "binary")]
    [InlineData("", "binary", "binary")]
    [InlineData("file", null, "binary")]
    [Theory]
    public void MapsQueryParameterTypes(string type, string format, string expected){
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["primitive"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
                                ["204"] = new OpenApiResponse {}
                            }
                        }
                    } 
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters", true);
        Assert.NotNull(queryParameters);
        var property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal(expected, property.Type.Name);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void MapsQueryParameterCollectionKinds(bool isArray){
        var baseSchema = new OpenApiSchema {
            Type = "number",
            Format = "int64"
        };
        var arraySchema = new OpenApiSchema {
            Type = "array",
            Items = baseSchema
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["primitive"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = isArray ? arraySchema : baseSchema
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse {}
                            }
                        }
                    } 
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters", true);
        Assert.NotNull(queryParameters);
        var property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("int64", property.Type.Name);
        Assert.Equal(isArray ? CodeTypeBase.CodeTypeCollectionKind.Array : CodeTypeBase.CodeTypeCollectionKind.None, property.Type.CollectionKind);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void DefaultsQueryParametersWithNoSchemaToString(){
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["primitive"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse {}
                            }
                        }
                    } 
                }
            },
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters", true);
        Assert.NotNull(queryParameters);
        var property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("string", property.Type.Name);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void DoesntGenerateNamespacesWhenNotRequired(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
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
    public void GeneratesNamesapacesWhenRequired(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "subns.myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "subns.myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
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
    public void IdsResultInIndexers(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answers/{id}"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
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
    public void InlinePropertiesGenerateTypes(){
        var myObjectSchema = new OpenApiSchema {
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
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
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
    public void ModelsDoesntUsePathDescriptionWhenAvailable(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Description = "some path item description",
                    Summary = "some path item summary",
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Null(responseClass.Description);
    }
    [Fact]
    public void CleansUpInvalidDescriptionCharacters(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
            Description = @"	some description with invalid characters: 
",
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        var responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Equal("some description with invalid characters: ", responseClass.Description);
    }
    [InlineData("application/json")]
    [InlineData("application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8")]
    [InlineData("application/vnd.github.mercy-preview+json")]
    [InlineData("application/vnd.github.mercy-preview+json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8")]
    [Theory]
    public void AcceptVendorsTypes(string contentType){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string"
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        var rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get);
        Assert.NotNull(executorMethod);
        Assert.Equal("Myobject", executorMethod.ReturnType.Name);
    }
    [Fact]
    public void ModelsUseDescriptionWhenAvailable(){
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Description = "some path item description",
                    Summary = "some path item summary",
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var modelsSubNS = codeModel.FindNamespaceByName("TestSdk.answer");
        Assert.NotNull(modelsSubNS);
        var responseClass = modelsSubNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Equal("some description", responseClass.Description);
    }
    [InlineData("application/json", "204", true, "default", "void")]
    [InlineData("application/json", "204", false, "default", "void")]
    [InlineData("application/json", "200", true, "default", "Myobject")]
    [InlineData("application/json", "200", false, "default", "binary")]
    [InlineData("application/xml", "204", true, "default", "void")]
    [InlineData("application/xml", "204", false, "default", "void")]
    [InlineData("application/xml", "200", true, "default", "Myobject")]
    [InlineData("application/xml", "200", false, "default", "binary")]
    [InlineData("text/xml", "204", true, "default", "void")]
    [InlineData("text/xml", "204", false, "default", "void")]
    [InlineData("text/xml", "200", true, "default", "Myobject")]
    [InlineData("text/xml", "200", false, "default", "binary")]
    [InlineData("text/yaml", "204", true, "default", "void")]
    [InlineData("text/yaml", "204", false, "default", "void")]
    [InlineData("text/yaml", "200", true, "default", "Myobject")]
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
    [InlineData("text/plain", "200", true, "default", "Myobject")]
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
    public void GeneratesTheRightReturnTypeBasedOnContentAndStatus(string contentType, string statusCode, bool addModel, string acceptedContentType, string returnType) {
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
            Components = new() {
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
            new GenerationConfiguration() {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = acceptedContentType.Equals("default", StringComparison.OrdinalIgnoreCase) ? 
                                            new GenerationConfiguration().StructuredMimeTypes:
                                            new (StringComparer.OrdinalIgnoreCase) { acceptedContentType }
        });
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
    [InlineData("application/json", true, "default", "Myobject")]
    [InlineData("application/json", false, "default", "binary")]
    [InlineData("application/xml", false, "default", "binary")]
    [InlineData("application/xml", true, "default", "Myobject")]
    [InlineData("text/xml", false, "default", "binary")]
    [InlineData("text/xml", true, "default", "Myobject")]
    [InlineData("text/yaml", false, "default", "binary")]
    [InlineData("text/yaml", true, "default", "Myobject")]
    [InlineData("application/octet-stream", true, "default", "binary")]
    [InlineData("application/octet-stream", false, "default", "binary")]
    [InlineData("text/html", true, "default", "binary")]
    [InlineData("text/html", false, "default", "binary")]
    [InlineData("*/*", true, "default", "binary")]
    [InlineData("*/*", false, "default", "binary")]
    [InlineData("text/plain", false, "default", "binary")]
    [InlineData("text/plain", true, "default", "Myobject")]
    [InlineData("text/plain", true, "application/json", "binary")]
    [InlineData("text/plain", false, "application/json", "binary")]
    [InlineData("text/yaml", true, "application/json", "binary")]
    [InlineData("text/yaml", false, "application/json", "binary")]
    [Theory]
    public void GeneratesTheRightParameterTypeBasedOnContentAndStatus(string contentType, bool addModel, string acceptedContentType, string parameterType) {
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Post] = new OpenApiOperation() {
                            RequestBody = new OpenApiRequestBody {
                                Content = {
                                    [contentType] = new OpenApiMediaType {
                                        Schema = addModel ? myObjectSchema : null
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse {},
                            }
                        }
                    } 
                }
            },
            Components = new() {
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
            new GenerationConfiguration() {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = acceptedContentType.Equals("default", StringComparison.OrdinalIgnoreCase) ? 
                                            new GenerationConfiguration().StructuredMimeTypes:
                                            new (StringComparer.OrdinalIgnoreCase) { acceptedContentType }
        });
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
    public void DoesntGenerateVoidExecutorOnMixed204(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse {
                                    Content = {
                                        ["application/json"] = new OpenApiMediaType {
                                            Schema = myObjectSchema
                                        }
                                    }
                                },
                                ["204"] = new OpenApiResponse {},
                            }
                        }
                    } 
                }
            },
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
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
    [InlineData(new string[] {"microsoft.graph.user", "microsoft.graph.termstore.term"}, "microsoft.graph")]
    [InlineData(new string[] {"microsoft.graph.user", "odata.errors.error"}, "")]
    [InlineData(new string[] {}, "")]
    [Theory]
    public void StripsCommonModelsPrefix(string[] componentNames, string stripPrefix) {
        var paths = new OpenApiPaths();
        var components = new OpenApiComponents() {
            Schemas = new Dictionary<string, OpenApiSchema>()
        };
        foreach(var componentName in componentNames) {
            var myObjectSchema = new OpenApiSchema {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema> {
                    {
                        "id", new OpenApiSchema {
                            Type = "string",
                        }
                    }
                },
                Reference = new OpenApiReference {
                    Id = componentName,
                    Type = ReferenceType.Schema
                },
                UnresolvedReference = false
            };
            paths.Add($"answer{componentName}", new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() { 
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
        var document = new OpenApiDocument() {
            Paths = paths,
            Components = components,
        };
        var result = KiotaBuilder.GetDeeperMostCommonNamespaceNameForModels(document);
        Assert.Equal(stripPrefix, result);
    }
    [Fact]
    public void HandlesContentParameters(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["answer(ids={ids}"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
                            Parameters = new List<OpenApiParameter> {
                                new OpenApiParameter {
                                    Name = "ids",
                                    In = ParameterLocation.Path,
                                    Content = new Dictionary<string, OpenApiMediaType>() {
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
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
        Assert.Equal(CodeType.CodeTypeCollectionKind.None, idsParam.Type.CollectionKind);
    }
    
    [Fact]
    public void HandlesPagingExtension(){
        var myObjectSchema = new OpenApiSchema {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = "string",
                    }
                }
            },
            Reference = new OpenApiReference {
                Id = "myobject",
                Type = ReferenceType.Schema
            },
            UnresolvedReference = false,
        };
        var document = new OpenApiDocument() {
            Paths = new OpenApiPaths() {
                ["users"] = new OpenApiPathItem() {
                    Operations = {
                        [OperationType.Get] = new OpenApiOperation() {
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
            Components = new() {
                Schemas = new Dictionary<string, OpenApiSchema> {
                    {
                        "myobject", myObjectSchema
                    }
                }
            }
        };
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" });
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        var answersNS = codeModel.FindNamespaceByName("TestSdk.users");
        Assert.NotNull(answersNS);
        var rbClass = answersNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        var executorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == HttpMethod.Get);
        Assert.NotNull(executorMethod);
        Assert.Equal("@odata.nextLink", executorMethod.PagingInformation?.NextLinkName);
    }
}
