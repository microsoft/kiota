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

namespace Kiota.Builder.Tests
{
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
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
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
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
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
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
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
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
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
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
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
    }
}
