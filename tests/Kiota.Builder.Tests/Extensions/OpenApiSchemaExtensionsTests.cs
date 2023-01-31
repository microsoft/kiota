﻿using System;
using System.Collections.Generic;

using Kiota.Builder.Extensions;

using Microsoft.OpenApi.Models;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public class OpenApiSchemaExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Empty(OpenApiSchemaExtensions.GetSchemaReferenceIds(null));
        var schema = new OpenApiSchema
        {
            AnyOf = null
        };
        Assert.Null(schema.AnyOf);
        Assert.Empty(schema.GetSchemaReferenceIds());
        schema = new()
        {
            AllOf = null
        };
        Assert.Null(schema.AllOf);
        Assert.Empty(schema.GetSchemaReferenceIds());
        schema = new()
        {
            OneOf = null
        };
        Assert.Null(schema.OneOf);
        Assert.Empty(schema.GetSchemaReferenceIds());
        schema = new()
        {
            Properties = null
        };
        Assert.Null(schema.Properties);
        Assert.Empty(schema.GetSchemaReferenceIds());
        Assert.False(OpenApiSchemaExtensions.IsAllOf(null));
        Assert.False(OpenApiSchemaExtensions.IsAnyOf(null));
        Assert.False(OpenApiSchemaExtensions.IsOneOf(null));
        Assert.False(OpenApiSchemaExtensions.IsArray(null));
        Assert.False(OpenApiSchemaExtensions.IsObject(null));
        Assert.False(OpenApiSchemaExtensions.IsReferencedSchema(null));

        Assert.False(new OpenApiSchema { Reference = null }.IsReferencedSchema());
        Assert.False(new OpenApiSchema { Type = null }.IsArray());
        Assert.False(new OpenApiSchema { Type = null }.IsObject());
        Assert.False(new OpenApiSchema { AnyOf = null }.IsAnyOf());
        Assert.False(new OpenApiSchema { AllOf = null }.IsAllOf());
        Assert.False(new OpenApiSchema { OneOf = null }.IsOneOf());

    }
    [Fact]
    public void ExternalReferencesAreNotSupported()
    {
        var mockSchema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = "example.json#/path/to/component",
                ExternalResource = "http://example.com/example.json",
            },
        };
        Assert.Throws<NotSupportedException>(() => mockSchema.IsReferencedSchema());
    }
    [Fact]
    public void LocalReferencesAreSupported()
    {
        var mockSchema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = "#/path/to/component",
            },
        };
        Assert.True(mockSchema.IsReferencedSchema());
    }
    [Fact]
    public void GetSchemaNameAllOf()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    Title = "microsoft.graph.entity"
                },
                new() {
                    Title = "microsoft.graph.user"
                }
            }
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
        Assert.Equal("microsoft.graph.user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAllOfNested()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    AllOf = new List<OpenApiSchema> {
                        new() {
                            Title = "microsoft.graph.entity"
                        },
                        new() {
                            Title = "microsoft.graph.user"
                        }
                    }
                }
            }
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
        Assert.Equal("microsoft.graph.user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAnyOf()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = new List<OpenApiSchema> {
                new() {
                    Title = "microsoft.graph.entity"
                },
                new() {
                    Title = "microsoft.graph.user"
                }
            }
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
        Assert.Equal("microsoft.graph.user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameOneOf()
    {
        var schema = new OpenApiSchema
        {
            OneOf = new List<OpenApiSchema> {
                new() {
                    Title = "microsoft.graph.entity"
                },
                new() {
                    Title = "microsoft.graph.user"
                }
            }
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
        Assert.Equal("microsoft.graph.user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameItems()
    {
        var schema = new OpenApiSchema
        {
            Items = new()
            {
                Title = "microsoft.graph.entity"
            },
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Equal("microsoft.graph.entity", schema.GetSchemaName());
        Assert.Single(names);
    }
    [Fact]
    public void GetSchemaNameTitle()
    {
        var schema = new OpenApiSchema
        {
            Title = "microsoft.graph.entity"
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Equal("microsoft.graph.entity", schema.GetSchemaName());
        Assert.Single(names);
    }
    [Fact]
    public void GetSchemaNameEmpty()
    {
        var schema = new OpenApiSchema();
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetReferenceIdsAllOf()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
                new() {
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    }
                }
            }
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
    }
    [Fact]
    public void GetReferenceIdsAllOfNested()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    AllOf = new List<OpenApiSchema> {
                        new() {
                            Reference = new() {
                                Id = "microsoft.graph.entity"
                            }
                        },
                        new() {
                            Reference = new() {
                                Id = "microsoft.graph.user"
                            }
                        }
                    }
                }
            }
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
    }
    [Fact]
    public void GetReferenceIdsAnyOf()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = new List<OpenApiSchema> {
                new() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
                new() {
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    }
                }
            }
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
    }
    [Fact]
    public void GetReferenceIdsOneOf()
    {
        var schema = new OpenApiSchema
        {
            OneOf = new List<OpenApiSchema> {
                new() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
                new() {
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    }
                }
            }
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Contains("microsoft.graph.user", names);
    }
    [Fact]
    public void GetReferenceIdsItems()
    {
        var schema = new OpenApiSchema
        {
            Items = new()
            {
                Reference = new()
                {
                    Id = "microsoft.graph.entity"
                }
            },
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Single(names);
    }
    [Fact]
    public void GetReferenceIdsTitle()
    {
        var schema = new OpenApiSchema
        {
            Reference = new()
            {
                Id = "microsoft.graph.entity"
            }
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Single(names);
    }
    [Fact]
    public void GetReferenceIdsEmpty()
    {
        var schema = new OpenApiSchema();
        var names = schema.GetSchemaReferenceIds();
        Assert.Empty(names);
    }
}
