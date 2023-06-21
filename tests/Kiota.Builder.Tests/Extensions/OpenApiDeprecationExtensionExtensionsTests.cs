

using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Xunit;

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
        Assert.Equal(openApiExtension.Description, deprecationInformation.Description);
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
        Assert.Equal("description", deprecationInformation.Description);
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
        Assert.Null(deprecationInformation.Description);
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
        Assert.Null(deprecationInformation.Description);
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
        Assert.Equal("description", deprecationInformation.Description);
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
        Assert.Null(deprecationInformation.Description);
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
                        Content = new Dictionary<string, OpenApiMediaType>()
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
        Assert.Equal("description", deprecationInformation.Description);
    }
    [Fact]
    public void GetsNoDeprecationOnOperationWithDeprecatedReferenceResponseSchema()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, OpenApiMediaType>()
                        {
                            { "application/json", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "someSchema"
                                        },
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
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.Description);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithDeprecatedInlineRequestSchema()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>()
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
        Assert.Equal("description", deprecationInformation.Description);
    }
    [Fact]
    public void GetsNoDeprecationOnOperationWithDeprecatedReferenceRequestSchema()
    {
        var operation = new OpenApiOperation
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>()
                {
                    { "application/json", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.Schema,
                                    Id = "someSchema"
                                },
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
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.Description);
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
        Assert.Equal("description", deprecationInformation.Description);
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
        Assert.Null(deprecationInformation.Description);
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
        Assert.Equal("description", deprecationInformation.Description);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnParameterWithDeprecatedReferenceSchema()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Schema = new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Id = "id",
                    Type = ReferenceType.Schema
                },
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
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.Description);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameterWithDeprecatedInlineContentSchema()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Content = new Dictionary<string, OpenApiMediaType>() {
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
        Assert.Equal("description", deprecationInformation.Description);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnParameterWithDeprecatedReferenceContentSchema()
    {
        var parameter = new OpenApiParameter
        {
            Deprecated = false,
            Content = new Dictionary<string, OpenApiMediaType>() {
                { "application/json", new OpenApiMediaType()
                    {
                        Schema = new OpenApiSchema
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.Schema,
                                Id = "id"
                            },
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
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.Description);
    }
    [Fact]
    public void GetsDeprecationInformationFromTreeNodeWhenAllOperationsDeprecated()
    {
        var rootNode = OpenApiUrlTreeNode.Create();
        var treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>()
            {
                {OperationType.Get, new OpenApiOperation{
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
        Assert.Equal("description", deprecationInformation.Description);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromTreeNodeOnNoOperation()
    {
        var rootNode = OpenApiUrlTreeNode.Create();
        var treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>()
            {
            }
        }, Constants.DefaultOpenApiLabel);
        var deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.Description);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromTreeNodeWhenOneOperationNonDeprecated()
    {
        var rootNode = OpenApiUrlTreeNode.Create();
        var treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>()
            {
                {OperationType.Get, new OpenApiOperation{
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
                {OperationType.Post, new OpenApiOperation{
                    Deprecated = false,
                }}
            }
        }, Constants.DefaultOpenApiLabel);
        var deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.Description);
    }
}
