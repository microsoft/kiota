using System;
using System.Collections.Generic;

using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Any;
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
        Assert.False(OpenApiSchemaExtensions.IsInherited(null));
        Assert.False(OpenApiSchemaExtensions.IsIntersection(null));
        Assert.False(OpenApiSchemaExtensions.IsInclusiveUnion(null));
        Assert.False(OpenApiSchemaExtensions.IsExclusiveUnion(null));
        Assert.False(OpenApiSchemaExtensions.IsArray(null));
        Assert.False(OpenApiSchemaExtensions.IsObject(null));
        Assert.False(OpenApiSchemaExtensions.IsReferencedSchema(null));
        Assert.Null(OpenApiSchemaExtensions.MergeIntersectionSchemaEntries(null));

        Assert.False(new OpenApiSchema { Reference = null }.IsReferencedSchema());
        Assert.False(new OpenApiSchema { Type = null }.IsArray());
        Assert.False(new OpenApiSchema { Type = null }.IsObject());
        Assert.False(new OpenApiSchema { AnyOf = null }.IsInclusiveUnion());
        Assert.False(new OpenApiSchema { AllOf = null }.IsInherited());
        Assert.False(new OpenApiSchema { AllOf = null }.IsIntersection());
        Assert.False(new OpenApiSchema { OneOf = null }.IsExclusiveUnion());
        var original = new OpenApiSchema { AllOf = null };
        Assert.Equal(original, original.MergeIntersectionSchemaEntries());

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
    public void GetSchemaNameAllOfTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            AllOf = [
                new()
                {
                    Title = "microsoft.graph.entity"
                },
                new()
                {
                    Title = "microsoft.graph.user"
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAllOfReference()
    {
        var schema = new OpenApiSchema
        {
            AllOf = [
                new()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.entity"
                    }
                },
                new()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.user"
                    }
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("entity", names);
        Assert.Contains("user", names);
        Assert.Equal("user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAllOfNestedTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            AllOf = [
                new()
                {
                    AllOf = [
                        new()
                        {
                            Title = "microsoft.graph.entity"
                        },
                        new()
                        {
                            Title = "microsoft.graph.user"
                        }
                    ]
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAllOfNestedReference()
    {
        var schema = new OpenApiSchema
        {
            AllOf = [
                new()
                {
                    AllOf = [
                        new()
                        {
                            Reference = new()
                            {
                                Id = "microsoft.graph.entity"
                            }
                        },
                        new()
                        {
                            Reference = new()
                            {
                                Id = "microsoft.graph.user"
                            }
                        }
                    ]
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("entity", names);
        Assert.Contains("user", names);
        Assert.Equal("user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAnyOfTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = [
                new()
                {
                    Title = "microsoft.graph.entity"
                },
                new()
                {
                    Title = "microsoft.graph.user"
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameAnyOfReference()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = [
                new()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.entity"
                    }
                },
                new()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.user"
                    }
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("entity", names);
        Assert.Contains("user", names);
        Assert.Equal("user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameOneOfTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            OneOf = [
                new()
                {
                    Title = "microsoft.graph.entity"
                },
                new()
                {
                    Title = "microsoft.graph.user"
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameOneOfReference()
    {
        var schema = new OpenApiSchema
        {
            OneOf = [
                new()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.entity"
                    }
                },
                new()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.user"
                    }
                }
            ]
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("entity", names);
        Assert.Contains("user", names);
        Assert.Equal("user", schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameItemsTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            Items = new()
            {
                Title = "microsoft.graph.entity"
            },
        };
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameItemsReference()
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
        var names = schema.GetSchemaNames();
        Assert.Contains("entity", names);
        Assert.Equal("entity", schema.GetSchemaName());
        Assert.Single(names);
    }
    [Fact]
    public void GetSchemaNameTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            Title = "microsoft.graph.entity"
        };
        var names = schema.GetSchemaNames();
        Assert.Empty(names);
        Assert.Empty(schema.GetSchemaName());
    }
    [Fact]
    public void GetSchemaNameReference()
    {
        var schema = new OpenApiSchema
        {
            Reference = new()
            {
                Id = "microsoft.graph.entity"
            }
        };
        var names = schema.GetSchemaNames();
        Assert.Contains("entity", names);
        Assert.Equal("entity", schema.GetSchemaName());
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
    [Fact]
    public void IsInherited()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    Type = "object",
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    },
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new() {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            }
        };
        Assert.True(schema.IsInherited());
        Assert.False(schema.IsIntersection());
    }
    [Fact]
    public void IsIntersection()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    Type = "object",
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    },
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new() {
                    Type = "object",
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    },
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            }
        };
        Assert.False(schema.IsInherited());
        Assert.True(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new() {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            }
        };
        Assert.False(schema.IsInherited());
        Assert.True(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema> {
                new() {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                }
            }
        };
        Assert.False(schema.IsInherited());
        Assert.False(schema.IsIntersection());
    }
    [Fact]
    public void MergesIntersection()
    {
        var schema = new OpenApiSchema
        {
            Description = "description",
            Deprecated = true,
            AllOf = new List<OpenApiSchema> {
                new() {
                    Type = "object",
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    },
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new() {
                    Type = "object",
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    },
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            }
        };
        var result = schema.MergeIntersectionSchemaEntries();
        Assert.False(schema.IsInherited());
        Assert.Equal(2, result.Properties.Count);
        Assert.Contains("id", result.Properties.Keys);
        Assert.Contains("firstName", result.Properties.Keys);
        Assert.Equal("description", result.Description);
        Assert.True(result.Deprecated);
    }
    [Fact]
    public void MergesIntersectionRecursively()
    {
        var schema = new OpenApiSchema
        {
            Description = "description",
            Deprecated = true,
            AllOf = new List<OpenApiSchema> {
                new() {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new() {
                    Type = "object",
                    AllOf = new List<OpenApiSchema>() {
                        new () {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>() {
                                ["firstName"] = new OpenApiSchema(),
                                ["lastName"] = new OpenApiSchema()
                            }
                        },
                        new () {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>() {
                                ["lastName"] = new OpenApiSchema()
                            }
                        },
                    }
                }
            }
        };
        var result = schema.MergeIntersectionSchemaEntries();
        Assert.False(schema.IsInherited());
        Assert.Equal(3, result.Properties.Count);
        Assert.Contains("id", result.Properties.Keys);
        Assert.Contains("firstName", result.Properties.Keys);
        Assert.Contains("lastName", result.Properties.Keys);
        Assert.Equal("description", result.Description);
        Assert.True(result.Deprecated);
    }
    [Fact]
    public void IsArrayFalseOnEmptyItems()
    {
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema(),
        };
        Assert.False(schema.IsArray());
    }
    [Fact]
    public void IsArrayFalseOnNullItems()
    {
        var schema = new OpenApiSchema
        {
            Type = "array",
        };
        Assert.False(schema.IsArray());
    }
    [Fact]
    public void IsEnumFailsOnEmptyMembers()
    {
        var schema = new OpenApiSchema
        {
            Type = "string",
            Enum = new List<IOpenApiAny>(),
        };
        Assert.False(schema.IsEnum());

        schema.Enum.Add(new OpenApiString(""));
        Assert.False(schema.IsEnum());
    }
    private static readonly OpenApiSchema enumSchema = new OpenApiSchema
    {
        Title = "riskLevel",
        Enum = new List<IOpenApiAny>
            {
                new OpenApiString("low"),
                new OpenApiString("medium"),
                new OpenApiString("high"),
                new OpenApiString("hidden"),
                new OpenApiString("none"),
                new OpenApiString("unknownFutureValue")
            },
        Type = "string"
    };
    [Fact]
    public void IsEnumIgnoresNullableUnions()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = new List<OpenApiSchema>
            {
                enumSchema,
                new OpenApiSchema
                {
                    Type = "object",
                    Nullable = true
                }
            }
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsEnumFailsOnNullableInheritance()
    {
        var schema = new OpenApiSchema
        {
            AllOf = new List<OpenApiSchema>
            {
                enumSchema,
                new OpenApiSchema
                {
                    Type = "object",
                    Nullable = true
                }
            }
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsEnumIgnoresNullableExclusiveUnions()
    {
        var schema = new OpenApiSchema
        {
            OneOf = new List<OpenApiSchema>
            {
                enumSchema,
                new OpenApiSchema
                {
                    Type = "object",
                    Nullable = true
                }
            }
        };
        Assert.False(schema.IsEnum());
    }
    private static readonly OpenApiSchema numberSchema = new OpenApiSchema
    {
        Type = "number",
        Format = "double",
    };
    [Fact]
    public void IsEnumDoesNotMaskExclusiveUnions()
    {
        var schema = new OpenApiSchema
        {
            OneOf = new List<OpenApiSchema>
            {
                enumSchema,
                numberSchema,
                new OpenApiSchema
                {
                    Type = "object",
                    Nullable = true
                }
            }
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsEnumDoesNotMaskUnions()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = new List<OpenApiSchema>
            {
                enumSchema,
                numberSchema,
                new OpenApiSchema
                {
                    Type = "object",
                    Nullable = true
                }
            }
        };
        Assert.False(schema.IsEnum());
    }
}
