using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public sealed class OpenApiUrlTreeNodeExtensionsTests : IDisposable
{
    [Fact]
    public void Defensive()
    {
        Assert.False(OpenApiUrlTreeNodeExtensions.IsComplexPathMultipleParameters(null));
        Assert.False(OpenApiUrlTreeNodeExtensions.IsPathSegmentWithSingleSimpleParameter(null));
        Assert.False(OpenApiUrlTreeNodeExtensions.DoesNodeBelongToItemSubnamespace(null));
        Assert.Empty(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, null));
        Assert.Empty(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, Label));
        Assert.Empty(OpenApiUrlTreeNode.Create().GetPathItemDescription(null));
        Assert.Empty(OpenApiUrlTreeNode.Create().GetPathItemDescription(Label));
    }
    private const string Label = "default";
    [Fact]
    public void GetsDescription()
    {
        var node = OpenApiUrlTreeNode.Create();
        node.PathItems.Add(Label, new()
        {
            Description = "description",
            Summary = "summary"
        });
        Assert.Equal(Label, OpenApiUrlTreeNode.Create().GetPathItemDescription(Label, Label));
        Assert.Equal("description", node.GetPathItemDescription(Label, Label));
        node.PathItems[Label].Description = null;
        Assert.Equal("summary", node.GetPathItemDescription(Label, Label));
    }
    [Fact]
    public void IsComplexPathWithAnyNumberOfParameters()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("function()", new());
        doc.Paths.Add("function({param})", new());
        doc.Paths.Add("function({param}, {param2})", new());
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.IsComplexPathMultipleParameters());
        Assert.False(node.Children.First().Value.IsComplexPathMultipleParameters());
        Assert.True(node.Children.Skip(1).First().Value.IsComplexPathMultipleParameters());
        Assert.True(node.Children.Skip(2).First().Value.IsComplexPathMultipleParameters());
    }
    [Fact]
    public void IsPathWithSingleSimpleParameter()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("{param}", new());
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.IsPathSegmentWithSingleSimpleParameter());
        Assert.True(node.Children.First().Value.IsPathSegmentWithSingleSimpleParameter());
    }
    [Fact]
    public void DoesNodeBelongToItemSubnamespace()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("{param}", new());
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.DoesNodeBelongToItemSubnamespace());
        Assert.True(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

        doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("param}", new());
        node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

        doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("{param", new());
        node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());
    }
    [Fact]
    public void GetNodeNamespaceFromPath()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("\\users\\messages", new());
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("graph.users.messages", node.Children.First().Value.GetNodeNamespaceFromPath("graph"));
        Assert.Equal("users.messages", node.Children.First().Value.GetNodeNamespaceFromPath(null));
    }
    [Fact]
    public void SanitizesAtSign()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("\\deviceManagement\\microsoft.graph.getRoleScopeTagsByIds(ids=@ids)", new());
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("graph.deviceManagement.microsoftGraphGetRoleScopeTagsByIdsWithIds", node.Children.First().Value.GetNodeNamespaceFromPath("graph"));
    }
    [InlineData("$select", "select")]
    [InlineData("api-version", "apiVersion")]
    [InlineData("api~topic", "apiTopic")]
    [InlineData("api.encoding", "apiEncoding")]
    [Theory]
    public void SanitizesParameterNameForSymbols(string original, string result)
    {
        Assert.Equal(result, original.SanitizeParameterNameForCodeSymbols());
    }

    [Fact]
    public void GetUrlTemplateSelectsDistinctQueryParameters()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                        Parameters = [
                            new() {
                                Name = "param-with-dashes",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new() {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new (){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                },
                {
                    OperationType.Put, new() {
                        Parameters = [
                            new() {
                                Name = "param-with-dashes",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new() {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new (){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                }
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select}", node.Children.First().Value.GetUrlTemplate());
        // the query parameters will be decoded by a middleware at runtime before the request is executed
    }
    [Fact]
    public void DifferentUrlTemplatesPerOperation()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new()
        {
            Parameters = [
                new()
                {
                    Name = "param-with-dashes",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new()
                    {
                        Type = "string"
                    },
                    Style = ParameterStyle.Simple,
                },
            ],
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                        Parameters = [

                            new (){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                },
                {
                    OperationType.Put, new() {}
                }
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select}", node.Children.First().Value.GetUrlTemplate());
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select}", node.Children.First().Value.GetUrlTemplate(OperationType.Get));
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment", node.Children.First().Value.GetUrlTemplate(OperationType.Put));
        // the query parameters will be decoded by a middleware at runtime before the request is executed
    }
    [Fact]
    public void GeneratesRequiredQueryParametersAndOptionalMixInPathItem()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new()
        {
            Parameters = {
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter {
                            Name = "filter",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter {
                            Name = "apikey",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    },
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/users/{id}/manager?apikey={apikey}{&filter*}", node.Children.First().Value.GetUrlTemplate());
    }
    [Fact]
    public void GeneratesRequiredQueryParametersAndOptionalMixInOperation()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                              Parameters = {
                                new OpenApiParameter {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "filter",
                                    In = ParameterLocation.Query,
                                    Required = false,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "apikey",
                                    In = ParameterLocation.Query,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                }
                            },
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/users/{id}/manager?apikey={apikey}{&filter*}", node.Children.First().Value.GetUrlTemplate());
    }
    [Fact]
    public void GeneratesOnlyOptionalQueryParametersInPathItem()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new()
        {
            Parameters = {
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter {
                            Name = "filter",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter {
                            Name = "apikey",
                            In = ParameterLocation.Query,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    },
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/users/{id}/manager{?apikey*,filter*}", node.Children.First().Value.GetUrlTemplate());
    }
    [Fact]
    public void GeneratesOnlyOptionalQueryParametersInOperation()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                              Parameters = {
                                new OpenApiParameter {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "filter",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "apikey",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                }
                            },
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/users/{id}/manager{?apikey*,filter*}", node.Children.First().Value.GetUrlTemplate());
    }
    [Fact]
    public void GeneratesOnlyRequiredQueryParametersInPathItem()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new()
        {
            Parameters = {
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter {
                            Name = "filter",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        },
                        new OpenApiParameter {
                            Name = "apikey",
                            Required = true,
                            In = ParameterLocation.Query,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
                        }
                    },
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/users/{id}/manager?apikey={apikey}&filter={filter}", node.Children.First().Value.GetUrlTemplate());
    }
    [Fact]
    public void GeneratesOnlyRequiredQueryParametersInOperation()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                              Parameters = {
                                new OpenApiParameter {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "filter",
                                    Required = true,
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "apikey",
                                    Required = true,
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                }
                            },
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/users/{id}/manager?apikey={apikey}&filter={filter}", node.Children.First().Value.GetUrlTemplate());
    }

    [Fact]
    public void GetUrlTemplateCleansInvalidParameters()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                        Parameters = [
                            new() {
                                Name = "param-with-dashes",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new() {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new (){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new (){
                                Name = "api-version",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new (){
                                Name = "api~topic",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new (){
                                Name = "api.encoding",
                                In = ParameterLocation.Query,
                                Schema = new () {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                }
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select,api%2Dversion,api%2Eencoding,api%7Etopic}", node.Children.First().Value.GetUrlTemplate());
        // the query parameters will be decoded by a middleware at runtime before the request is executed
    }
    [InlineData("\\reviews\\search.json", "reviews.searchJson")]
    [InlineData("\\members\\microsoft.graph.$ref", "members.microsoftGraphRef")]
    [InlineData("\\feeds\\video-comments.{format}", "feeds.videoCommentsWithFormat")]
    [Theory]
    public void GetsNamespaceFromPath(string source, string expected)
    {
        Assert.Equal(expected, source.GetNamespaceFromPath(string.Empty));
    }
    [Fact]
    public void GetsClassNameWithIndexerAndExtension()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("/reviews/{resource-type}.json", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                        Parameters = new List<OpenApiParameter> {
                            new() {
                                Name = "resource-type",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new() {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            }
                        },
                        Responses = new OpenApiResponses() {
                            {"200", new() {
                                Content = new Dictionary<string, OpenApiMediaType>() {
                                    {"application/json", new() {
                                        Schema = new () {
                                            Type = "string"
                                        }
                                    }}
                                }
                            }}
                        }
                    }
                }
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        var result = node.Children["reviews"].Children["{resource-type}.json"].GetClassName(new() { "application/json" });
        Assert.Equal("ResourceType", result);
    }
    [Fact]
    public void GetsClassNameWithSegmentsToSkipForClassNames()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("/reviews/{resource-type}.json", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                {
                    OperationType.Get, new() {
                        Parameters = new List<OpenApiParameter> {
                            new() {
                                Name = "resource-type",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new() {
                                    Type = "string"
                                },
                                Style = ParameterStyle.Simple,
                            }
                        },
                        Responses = new OpenApiResponses()
                        {
                            {
                                "200", new()
                                {
                                    Content = new Dictionary<string, OpenApiMediaType>()
                                    {
                                        {
                                            "application/json", new()
                                            {
                                                Schema = new ()
                                                {
                                                    Type = "object",
                                                    Title = "json",
                                                    Reference = new OpenApiReference()
                                                    {
                                                        Id = "microsoft.graph.json"
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
        });

        var node = OpenApiUrlTreeNode.Create(doc, Label);
        var result = node.Children["reviews"].Children["{resource-type}.json"].GetClassName(new() { "application/json" });
        Assert.Equal("ResourceType", result);

        // Get the responseSchema with a type "microsoft.graph.json"
        var responseSchema = node.Children["reviews"].Children["{resource-type}.json"].PathItems["default"].Operations[0].Responses["200"].Content["application/json"].Schema;
        var responseClassName = node.Children["reviews"].Children["{resource-type}.json"]
            .GetClassName(new() { "application/json" }, schema: responseSchema);

        // validate that we get a valid class name
        Assert.Equal("json", responseClassName);
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
                ["users/{foo}/careerAdvisor/{id}"] = new OpenApiPathItem
                {
                    Parameters = {
                        new OpenApiParameter {
                            Name = "foo",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
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
                                            Schema = userSchema
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["users/{id}/careerAdvisor"] = new OpenApiPathItem
                {
                    Parameters = {
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = "string"
                            }
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
                            Parameters = {
                                new OpenApiParameter {
                                    Name = "user-id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = "string"
                                    }
                                },
                            },
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
        node.MergeIndexNodesAtSameLevel(mockLogger);
        var usersCollectionIndexNode = GetChildNodeByPath(node, "users/{foo-id}");
        Assert.NotNull(usersCollectionIndexNode);
        Assert.Equal("{+baseurl}/users/{foo%2Did}", usersCollectionIndexNode.GetUrlTemplate());

        var managerNode = GetChildNodeByPath(node, "users/{foo-id}/manager");
        Assert.NotNull(managerNode);
        Assert.Equal("{+baseurl}/users/{foo%2Did}/manager", managerNode.GetUrlTemplate());

        var careerAdvisorNode = GetChildNodeByPath(node, "users/{foo-id}/careerAdvisor");
        Assert.NotNull(careerAdvisorNode);
        Assert.Equal("{+baseurl}/users/{foo%2Did}/careerAdvisor", careerAdvisorNode.GetUrlTemplate());

        var careerAdvisorIndexNode = GetChildNodeByPath(node, "users/{foo-id}/careerAdvisor/{id}");
        Assert.NotNull(careerAdvisorIndexNode);
        Assert.Equal("{+baseurl}/users/{foo%2Did}/careerAdvisor/{id}", careerAdvisorIndexNode.GetUrlTemplate());
        var pathItem = careerAdvisorIndexNode.PathItems[Constants.DefaultOpenApiLabel];
        Assert.NotNull(pathItem);
        var parameter = pathItem.Parameters.FirstOrDefault(static p => p.Name == "foo-id");
        Assert.NotNull(parameter);
    }
    [Fact]
    public void SinglePathParametersAreDeduplicatedAndOrderIsRespected()
    {
        var ownerSchema = new OpenApiSchema
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
                Id = "#/components/schemas/owner"
            },
            UnresolvedReference = false
        };
        var repoSchema = new OpenApiSchema
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
                Id = "#/components/schemas/repo"
            },
            UnresolvedReference = false
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/repos/{owner}/{repo}"] = new OpenApiPathItem
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
                                            Schema = repoSchema
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["/repos/{template_owner}/{template_repo}/generate"] = new OpenApiPathItem
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
                                            Schema = repoSchema
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
                    {"owner", ownerSchema},
                    {"repo", repoSchema}
                }
            }
        };
        var mockLogger = new CountLogger<KiotaBuilder>();
        var builder = new KiotaBuilder(mockLogger, new GenerationConfiguration { ClientClassName = "GitHub", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        node.MergeIndexNodesAtSameLevel(mockLogger);

        // Expected
        var resultNode = GetChildNodeByPath(node, "repos/{owner-id}/{repo-id}/generate");
        Assert.NotNull(resultNode);
        Assert.Equal("\\repos\\{owner-id}\\{repo-id}\\generate", resultNode.Path);
        Assert.Equal("{+baseurl}/repos/{owner%2Did}/{repo%2Did}/generate", resultNode.GetUrlTemplate());
    }
    private static OpenApiUrlTreeNode GetChildNodeByPath(OpenApiUrlTreeNode node, string path)
    {
        var pathSegments = path.Split('/');
        if (pathSegments.Length == 0)
            return null;
        if (pathSegments.Length == 1 && node.Children.TryGetValue(pathSegments[0], out var result))
            return result;
        if (node.Children.TryGetValue(pathSegments[0], out var currentNode))
            return GetChildNodeByPath(currentNode, string.Join('/', pathSegments.Skip(1)));
        return null;
    }
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    private static readonly HttpClient _httpClient = new();
}
