using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

using Microsoft.OpenApi;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;

public sealed class OpenApiUrlTreeNodeExtensionsTests : IDisposable
{
    [Fact]
    public void Defensive()
    {
        Assert.False(OpenApiUrlTreeNodeExtensions.IsComplexPathMultipleParameters(null));
        Assert.False(OpenApiUrlTreeNodeExtensions.IsPathSegmentWithSingleSimpleParameter((OpenApiUrlTreeNode)null));
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
        node.PathItems.Add(Label, new OpenApiPathItem()
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
        doc.Paths.Add("function()", new OpenApiPathItem());
        doc.Paths.Add("function({param})", new OpenApiPathItem());
        doc.Paths.Add("function({param}, {param2})", new OpenApiPathItem());
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
        doc.Paths.Add("{param}", new OpenApiPathItem());
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
        doc.Paths.Add("{param}", new OpenApiPathItem());
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.DoesNodeBelongToItemSubnamespace());
        Assert.True(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

        doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("param}", new OpenApiPathItem());
        node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

        doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("{param", new OpenApiPathItem());
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
        doc.Paths.Add("\\users\\messages", new OpenApiPathItem());
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
        doc.Paths.Add("\\deviceManagement\\microsoft.graph.getRoleScopeTagsByIds(ids=@ids)", new OpenApiPathItem());
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
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                        Parameters = [
                            new OpenApiParameter() {
                                Name = "param-with-dashes",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new OpenApiParameter (){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                },
                {
                    HttpMethod.Put, new() {
                        Parameters = [
                            new OpenApiParameter() {
                                Name = "param-with-dashes",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new OpenApiParameter(){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema () {
                                    Type = JsonSchemaType.String
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
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new OpenApiPathItem()
        {
            Parameters = [
                new OpenApiParameter()
                {
                    Name = "param-with-dashes",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String
                    },
                    Style = ParameterStyle.Simple,
                },
            ],
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                        Parameters = [

                            new OpenApiParameter(){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                },
                {
                    HttpMethod.Put, new() {}
                }
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.HasRequiredQueryParametersAcrossOperations());
        Assert.False(node.Children.First().Value.HasRequiredQueryParametersAcrossOperations());
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select}", node.Children.First().Value.GetUrlTemplate());
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select}", node.Children.First().Value.GetUrlTemplate(HttpMethod.Get));
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment", node.Children.First().Value.GetUrlTemplate(HttpMethod.Put));
        // the query parameters will be decoded by a middleware at runtime before the request is executed
    }
    [Fact]
    public void DifferentUrlTemplatesPerOperationWithRequiredParameter()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new OpenApiPathItem()
        {
            Parameters = [
                new OpenApiParameter()
                {
                    Name = "param-with-dashes",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String
                    },
                    Style = ParameterStyle.Simple,
                },
            ],
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                        Parameters = [

                            new OpenApiParameter(){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                },
                { HttpMethod.Post, new() {
                        Parameters = [

                            new OpenApiParameter(){
                                Name = "$expand",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            }
                        ]
                    }
                },
                {
                    HttpMethod.Put, new() {}
                },
                { HttpMethod.Delete, new() {
                        Parameters = [

                            new OpenApiParameter (){
                                Name = "id",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                                Required = true
                            }
                        ]
                    }
                },
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.HasRequiredQueryParametersAcrossOperations());
        Assert.True(node.Children.First().Value.HasRequiredQueryParametersAcrossOperations());
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment?id={id}{&%24expand,%24select}", node.Children.First().Value.GetUrlTemplate());//the default contains a combination of everything.
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select}", node.Children.First().Value.GetUrlTemplate(HttpMethod.Get));
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24expand}", node.Children.First().Value.GetUrlTemplate(HttpMethod.Post));
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment", node.Children.First().Value.GetUrlTemplate(HttpMethod.Put));
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment?id={id}", node.Children.First().Value.GetUrlTemplate(HttpMethod.Delete));
        // the query parameters will be decoded by a middleware at runtime before the request is executed
    }
    [Fact]
    public void GeneratesRequiredQueryParametersAndOptionalMixInPathItem()
    {
        var doc = new OpenApiDocument
        {
            Paths = [],
        };
        doc.Paths.Add("users\\{id}\\manager", new OpenApiPathItem()
        {
            Parameters = [
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        new OpenApiParameter {
                            Name = "filter",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        new OpenApiParameter {
                            Name = "apikey",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
            ],
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
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
        doc.Paths.Add("users\\{id}\\manager", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                              Parameters = [
                                new OpenApiParameter {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "filter",
                                    In = ParameterLocation.Query,
                                    Required = false,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "apikey",
                                    In = ParameterLocation.Query,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                }
                              ],
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
        doc.Paths.Add("users\\{id}\\manager", new OpenApiPathItem()
        {
            Parameters = [
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        new OpenApiParameter {
                            Name = "filter",
                            In = ParameterLocation.Query,
                            Required = false,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        new OpenApiParameter {
                            Name = "apikey",
                            In = ParameterLocation.Query,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
            ],
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
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
        doc.Paths.Add("users\\{id}\\manager", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                              Parameters = [
                                new OpenApiParameter {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "filter",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "apikey",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                }
                              ],
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
        doc.Paths.Add("users\\{id}\\manager", new OpenApiPathItem()
        {
            Parameters = [
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        new OpenApiParameter {
                            Name = "filter",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        new OpenApiParameter {
                            Name = "apikey",
                            Required = true,
                            In = ParameterLocation.Query,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
            ],
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
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
        doc.Paths.Add("users\\{id}\\manager", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                              Parameters = [
                                new OpenApiParameter {
                                    Name = "id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "filter",
                                    Required = true,
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                                new OpenApiParameter {
                                    Name = "apikey",
                                    Required = true,
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                }
                              ],
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
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                        Parameters = [
                            new OpenApiParameter() {
                                Name = "param-with-dashes",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new OpenApiParameter(){
                                Name = "$select",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new OpenApiParameter(){
                                Name = "api-version",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new OpenApiParameter(){
                                Name = "api~topic",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            },
                            new OpenApiParameter(){
                                Name = "api.encoding",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
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
        doc.Paths.Add("/reviews/{resource-type}.json", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                { HttpMethod.Get, new() {
                        Parameters = new List<IOpenApiParameter> {
                            new OpenApiParameter() {
                                Name = "resource-type",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            }
                        },
                        Responses = new OpenApiResponses() {
                            {"200", new OpenApiResponse() {
                                Content = new Dictionary<string, IOpenApiMediaType>() {
                                    {"application/json", new OpenApiMediaType() {
                                        Schema = new OpenApiSchema() {
                                            Type = JsonSchemaType.String
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
        doc.AddComponent("microsoft.graph.json", new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Title = "json",
        });
        doc.Paths.Add("/reviews/{resource-type}.json", new OpenApiPathItem()
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> {
                {
                    HttpMethod.Get, new() {
                        Parameters = new List<IOpenApiParameter> {
                            new OpenApiParameter() {
                                Name = "resource-type",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema() {
                                    Type = JsonSchemaType.String
                                },
                                Style = ParameterStyle.Simple,
                            }
                        },
                        Responses = new OpenApiResponses()
                        {
                            {
                                "200", new OpenApiResponse()
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        {
                                            "application/json", new OpenApiMediaType()
                                            {
                                                Schema = new OpenApiSchemaReference("microsoft.graph.json"),
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
        doc.SetReferenceHostDocument();

        var node = OpenApiUrlTreeNode.Create(doc, Label);
        var result = node.Children["reviews"].Children["{resource-type}.json"].GetClassName(new() { "application/json" });
        Assert.Equal("ResourceType", result);

        // Get the responseSchema with a type "microsoft.graph.json"
        var responseSchema = node.Children["reviews"].Children["{resource-type}.json"].PathItems["default"].Operations[HttpMethod.Get].Responses["200"].Content["application/json"].Schema;
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
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "displayName", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["users/{foo}/careerAdvisor/{id}"] = new OpenApiPathItem
                {
                    Parameters = [
                        new OpenApiParameter {
                            Name = "foo",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                    ],
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("microsoft.graph.user")
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["users/{id}/careerAdvisor"] = new OpenApiPathItem
                {
                    Parameters = [
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                    ],
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("microsoft.graph.user")
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["users/{user-id}/manager"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter {
                                    Name = "user-id",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                },
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("microsoft.graph.user")
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
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {
                        "microsoft.graph.user", userSchema
                    }
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
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
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        var repoSchema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/repos/{owner}/{repo}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("repo")
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["/repos/{template_owner}/{template_repo}/generate"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("repo")
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
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {"owner", ownerSchema},
                    {"repo", repoSchema}
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
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
    [Fact]
    public void repro4085()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/path/{thingId}/abc/{second}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.String
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["/path/{differentThingId}/def/{second}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.String
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        var mockLogger = new CountLogger<KiotaBuilder>();
        var builder = new KiotaBuilder(mockLogger, new GenerationConfiguration { ClientClassName = "GitHub", ApiRootUrl = "https://localhost" }, _httpClient);
        var node = builder.CreateUriSpace(document);
        node.MergeIndexNodesAtSameLevel(mockLogger);

        // Expected
        var resultNode = GetChildNodeByPath(node, "path");
        Assert.NotNull(resultNode);
        Assert.Equal("\\path", resultNode.Path);

        Assert.Null(GetChildNodeByPath(resultNode, "{thingId}"));
        Assert.Null(GetChildNodeByPath(resultNode, "{differentThingId}"));

        var differentThingId = GetChildNodeByPath(resultNode, "{differentThing-id}");
        Assert.Equal("\\path\\{differentThing-id}", differentThingId.Path);
        Assert.Equal("{+baseurl}/path/{differentThing%2Did}", differentThingId.GetUrlTemplate());
    }

    [Theory]
    [InlineData("{path}", "WithPath")]
    [InlineData("archived{path}", "archivedWithPath")]
    [InlineData("files{path}", "filesWithPath")]
    [InlineData("name(idParam='{id}')", "nameWithId")]
    [InlineData("name(idParam={id})", "nameWithId")]
    [InlineData("name(idParamFoo={id})", "nameWithId")] // The current implementation only uses the placeholder i.e {id} for the naming to ignore `idParamFoo`
                                                        // and thus generates the same identifier as the previous case. This collision risk is unlikely and constrained to an odata service scenario
                                                        // which would be invalid for functions scenario(overloads with the same parameters))
    [InlineData("name(idParam='{id}',idParam2='{id2}')", "nameWithIdWithId2")]
    public void CleanupParametersFromPathGeneratesDifferentResultsWithPrefixPresent(string segmentName, string expectedIdentifer)
    {
        var result = OpenApiUrlTreeNodeExtensions.CleanupParametersFromPath(segmentName);
        Assert.Equal(expectedIdentifer, result);
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
