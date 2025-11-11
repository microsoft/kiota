

using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;
using Xunit;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace Kiota.Builder.Tests.Extensions;

public class OpenApiDeprecationExtensionExtensions
{
    [Fact]
    public void ToDeprecationInformation()
    {
        var openApiExtension = new OpenApiDeprecationExtension
        {
            Description = "description",
            Version = "version",
            RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
            Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
        };
        var deprecationInformation = openApiExtension.ToDeprecationInformation();
        Assert.Equal(openApiExtension.Description, deprecationInformation.DescriptionTemplate);
        Assert.Equal(openApiExtension.Version, deprecationInformation.Version);
        Assert.Equal(openApiExtension.RemovalDate.Value.Year, deprecationInformation.RemovalDate.Value.Year);
        Assert.Equal(openApiExtension.Date.Value.Month, deprecationInformation.Date.Value.Month);
    }
    [Fact]
    public void GetsDeprecationInformationFromOpenApiSchema()
    {
        var openApiSchema = new OpenApiSchema
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var deprecationInformation = openApiSchema.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsEmptyDeprecationInformationFromSchema()
    {
        var openApiSchema = new OpenApiSchema
        {
            Deprecated = true,
        };
        var deprecationInformation = openApiSchema.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromNonDeprecatedSchema()
    {
        var openApiSchema = new OpenApiSchema
        {
            Deprecated = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var deprecationInformation = openApiSchema.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationDirect()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationOnNonDeprecatedOperation()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithNullResponseContentTypeInstance()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, IOpenApiMediaType>()
                        {
                            { "application/json", null
                            }
                        }
                    }
                }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithDeprecatedInlineResponseSchema()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, IOpenApiMediaType>()
                        {
                            { "application/json", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Deprecated = true,
                                        Extensions = new Dictionary<string, IOpenApiExtension>
                                        {
                                            { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                                                Description = "description",
                                                Version = "version",
                                                RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                                Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                            } }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationOnOperationWithDeprecatedReferenceResponseSchema()
    {
        var schema = new OpenApiSchema
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var document = new OpenApiDocument();
        document.AddComponent("schema", schema);
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, IOpenApiMediaType>()
                        {
                            { "application/json", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchemaReference("schema", document)
                                }
                            }
                        }
                    }
                }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithDeprecatedInlineRequestSchema()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, IOpenApiMediaType>()
                {
                    { "application/json", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Deprecated = true,
                                Extensions = new Dictionary<string, IOpenApiExtension>
                                {
                                    { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                                        Description = "description",
                                        Version = "version",
                                        RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                        Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                    } }
                                }
                            }
                        }
                    }
                }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithNullRequestBodyContentTypeInstance()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, IOpenApiMediaType>()
                {
                    { "application/json", null
                    }
                }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationOnOperationWithDeprecatedReferenceRequestSchema()
    {
        var schema = new OpenApiSchema
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var document = new OpenApiDocument();
        document.AddComponent("schema", schema);
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, IOpenApiMediaType>()
                {
                    { "application/json", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchemaReference("schema", document)
                        }
                    }
                }
            }
        };
        var deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameter()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnNonDeprecatedParameter()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameterWithDeprecatedInlineSchema()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Schema = new OpenApiSchema
            {
                Deprecated = true,
                Extensions = new Dictionary<string, IOpenApiExtension>
                {
                    { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                        Description = "description",
                        Version = "version",
                        RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                        Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    } }
                }
            }
        };
        var deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnParameterWithDeprecatedReferenceSchema()
    {
        var schema = new OpenApiSchema
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var document = new OpenApiDocument();
        document.AddComponent("schema", schema);
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Schema = new OpenApiSchemaReference("schema", document)
        };
        var deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameterWithDeprecatedInlineContentSchema()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Content = new Dictionary<string, IOpenApiMediaType>() {
                { "application/json", new OpenApiMediaType()
                    {
                        Schema = new OpenApiSchema
                        {
                            Deprecated = true,
                            Extensions = new Dictionary<string, IOpenApiExtension>
                            {
                                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                                    Description = "description",
                                    Version = "version",
                                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                } }
                            }
                        }
                    }
                }
            }
        };
        var deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnParameterWithDeprecatedReferenceContentSchema()
    {
        var schema = new OpenApiSchema
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        var document = new OpenApiDocument();
        document.AddComponent("schema", schema);
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Content = new Dictionary<string, IOpenApiMediaType>() {
                { "application/json", new OpenApiMediaType()
                    {
                        Schema = new OpenApiSchemaReference("schema", document)
                    }
                }
            }
        };
        var deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationFromTreeNodeWhenAllOperationsDeprecated()
    {
        var rootNode = OpenApiUrlTreeNode.Create();
        var treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<NetHttpMethod, OpenApiOperation>()
            {
                {NetHttpMethod.Get, new OpenApiOperation{
                    Deprecated = true,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                            Description = "description",
                            Version = "version",
                            RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                            Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                        } }
                    }
                } },
            }
        }, Constants.DefaultOpenApiLabel);
        var deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromTreeNodeOnNoOperation()
    {
        var rootNode = OpenApiUrlTreeNode.Create();
        var treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<NetHttpMethod, OpenApiOperation>()
            {
            }
        }, Constants.DefaultOpenApiLabel);
        var deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromTreeNodeWhenOneOperationNonDeprecated()
    {
        var rootNode = OpenApiUrlTreeNode.Create();
        var treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<NetHttpMethod, OpenApiOperation>()
            {
                {NetHttpMethod.Get, new OpenApiOperation{
                    Deprecated = true,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                            Description = "description",
                            Version = "version",
                            RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                            Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                        } }
                    }
                } },
                {NetHttpMethod.Post, new OpenApiOperation{
                    Deprecated = false,
                }}
            }
        }, Constants.DefaultOpenApiLabel);
        var deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
}
