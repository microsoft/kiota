using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.Extensions;

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public class OpenApiUrlTreeNodeExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.False(OpenApiUrlTreeNodeExtensions.IsComplexPathWithAnyNumberOfParameters(null));
        Assert.False(OpenApiUrlTreeNodeExtensions.IsPathSegmentWithSingleSimpleParameter(null));
        Assert.False(OpenApiUrlTreeNodeExtensions.DoesNodeBelongToItemSubnamespace(null));
        Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, null));
        Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, Label));
        Assert.Null(OpenApiUrlTreeNode.Create().GetPathItemDescription(null));
        Assert.Null(OpenApiUrlTreeNode.Create().GetPathItemDescription(Label));
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
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.False(node.IsComplexPathWithAnyNumberOfParameters());
        Assert.True(node.Children.First().Value.IsComplexPathWithAnyNumberOfParameters());
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
        Assert.Equal("graph.deviceManagement.getRoleScopeTagsByIdsWithIds", node.Children.First().Value.GetNodeNamespaceFromPath("graph"));
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
    public void GetUrlTemplateCleansInvalidParameters()
    {
        var doc = new OpenApiDocument
        {
            Paths = new(),
        };
        doc.Paths.Add("{param-with-dashes}\\existing-segment", new()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> {
                { OperationType.Get, new() {
                        Parameters = new List<OpenApiParameter> {
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
                        }
                    }
                }
            }
        });
        var node = OpenApiUrlTreeNode.Create(doc, Label);
        Assert.Equal("{+baseurl}/{param%2Dwith%2Ddashes}/existing-segment{?%24select,api%2Dversion,api%7Etopic,api%2Eencoding}", node.Children.First().Value.GetUrlTemplate());
        // the query parameters will be decoded by a middleware at runtime before the request is executed
    }
}
