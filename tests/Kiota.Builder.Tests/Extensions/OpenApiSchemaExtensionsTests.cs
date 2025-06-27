using System.Collections.Generic;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
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
        Assert.False(OpenApiSchemaExtensions.IsObjectType(null));
        Assert.False(OpenApiSchemaExtensions.HasAnyProperty(null));
        Assert.False(OpenApiSchemaExtensions.IsReferencedSchema(null));
        Assert.Null(OpenApiSchemaExtensions.MergeIntersectionSchemaEntries(null));

        Assert.False(new OpenApiSchema { }.IsReferencedSchema());
        Assert.False(new OpenApiSchema { Type = JsonSchemaType.Null }.IsArray());
        Assert.False(new OpenApiSchema { Type = JsonSchemaType.Null }.IsObjectType());
        Assert.False(new OpenApiSchema { AnyOf = null }.IsInclusiveUnion());
        Assert.False(new OpenApiSchema { AllOf = null }.IsInherited());
        Assert.False(new OpenApiSchema { AllOf = null }.IsIntersection());
        Assert.False(new OpenApiSchema { OneOf = null }.IsExclusiveUnion());
        Assert.False(new OpenApiSchema { Properties = null }.HasAnyProperty());
        var original = new OpenApiSchema { AllOf = null };
        Assert.Equal(original, original.MergeIntersectionSchemaEntries());

    }
    [Fact]
    public void IsExclusiveUnionMatchesTypeArrays()
    {
        Assert.True(new OpenApiSchema
        {
            Type = JsonSchemaType.String | JsonSchemaType.Number
        }.IsExclusiveUnion());
        Assert.True(new OpenApiSchema
        {
            Type = JsonSchemaType.String | JsonSchemaType.Number | JsonSchemaType.Null
        }.IsExclusiveUnion());
        Assert.False(new OpenApiSchema
        {
            Type = JsonSchemaType.Number | JsonSchemaType.Null
        }.IsExclusiveUnion());
    }
    [Fact]
    public void ExternalReferencesAreSupported()
    {
        var mockSchema = new OpenApiSchemaReference("example.json#/path/to/component", null, "http://example.com/example.json");
        Assert.True(mockSchema.IsReferencedSchema());
    }
    [Fact]
    public void SchemasAreNotConsideredReferences()
    {
        var mockSchema = new OpenApiSchema();
        Assert.False(mockSchema.IsReferencedSchema());
    }
    [Fact]
    public void LocalReferencesAreSupported()
    {
        var mockSchema = new OpenApiSchemaReference("#/path/to/component");
        Assert.True(mockSchema.IsReferencedSchema());
    }
    [Fact]
    public void GetSchemaNameAllOfTitleEmpty()
    {
        var schema = new OpenApiSchema
        {
            AllOf = [
                new OpenApiSchema()
                {
                    Title = "microsoft.graph.entity"
                },
                new OpenApiSchema()
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
                new OpenApiSchemaReference("microsoft.graph.entity"),
                new OpenApiSchemaReference("microsoft.graph.user"),
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
                new OpenApiSchema()
                {
                    AllOf = [
                        new OpenApiSchema()
                        {
                            Title = "microsoft.graph.entity"
                        },
                        new OpenApiSchema()
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
                new OpenApiSchema()
                {
                    AllOf = [
                        new OpenApiSchemaReference("microsoft.graph.entity"),
                        new OpenApiSchemaReference("microsoft.graph.user"),
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
                new OpenApiSchema()
                {
                    Title = "microsoft.graph.entity"
                },
                new OpenApiSchema()
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
                        new OpenApiSchemaReference("microsoft.graph.entity"),
                        new OpenApiSchemaReference("microsoft.graph.user"),
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
                new OpenApiSchema()
                {
                    Title = "microsoft.graph.entity"
                },
                new OpenApiSchema()
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
                        new OpenApiSchemaReference("microsoft.graph.entity"),
                        new OpenApiSchemaReference("microsoft.graph.user"),
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
            Items = new OpenApiSchema()
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
            Items = new OpenApiSchemaReference("microsoft.graph.entity")
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
        var schema = new OpenApiSchemaReference("microsoft.graph.entity");
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
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity"),
                new OpenApiSchemaReference("microsoft.graph.user")
            ]
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
            AllOf = [
                new OpenApiSchema() {
                    AllOf = [
                        new OpenApiSchemaReference("microsoft.graph.entity"),
                        new OpenApiSchemaReference("microsoft.graph.user")
                    ]
                }
            ]
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
            AnyOf = [
                new OpenApiSchemaReference("microsoft.graph.entity"),
                new OpenApiSchemaReference("microsoft.graph.user")
            ]
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
            OneOf = [
                new OpenApiSchemaReference("microsoft.graph.entity"),
                new OpenApiSchemaReference("microsoft.graph.user")
            ]
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
            Items = new OpenApiSchemaReference("microsoft.graph.entity"),
        };
        var names = schema.GetSchemaReferenceIds();
        Assert.Contains("microsoft.graph.entity", names);
        Assert.Single(names);
    }
    [Fact]
    public void GetReferenceIdsTitle()
    {
        var schema = new OpenApiSchemaReference("microsoft.graph.entity");
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
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity"),
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            ]
        };
        Assert.True(schema.IsInherited());
        Assert.False(schema.IsIntersection());
    }
    [Fact]
    public void IsIntersection()
    {
        var document = new OpenApiDocument()
        {
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["microsoft.graph.entity"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["id"] = new OpenApiSchema()
                        }
                    },
                    ["microsoft.graph.user"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["firstName"] = new OpenApiSchema()
                        }
                    }
                }
            }
        };
        document.RegisterComponents();
        var schema = new OpenApiSchema
        {
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity", document),
                new OpenApiSchemaReference("microsoft.graph.user", document),
            ]
        };
        Assert.False(schema.IsInherited());
        Assert.True(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            AllOf = [
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            ]
        };
        Assert.False(schema.IsInherited());
        Assert.True(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            AllOf = [
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                }
            ]
        };
        Assert.False(schema.IsInherited());
        Assert.False(schema.IsIntersection());
        var userIdSchema = new OpenApiSchema
        {
            Title = "UserId",
            Description = "unique identifier",
            Type = JsonSchemaType.String,
            Pattern = "^[1-9][0-9]*$",
            Example = "1323232",
        };
        var tmpDocument = new OpenApiDocument();
        tmpDocument.AddComponent("UserId", userIdSchema);

        schema = new OpenApiSchema
        {
            Title = "Trader Id",
            AllOf = [
                new OpenApiSchemaReference("UserId", tmpDocument),// This property makes the schema "meaningful"
            ],
        };
        tmpDocument.AddComponent("TraderId", schema);
        var schemaReference = new OpenApiSchemaReference("TraderId", tmpDocument);  // This property makes the schema "meaningful"

        Assert.False(schemaReference.IsInherited());
        Assert.False(schemaReference.IsIntersection());
    }
    [Fact]
    public void MergesIntersection()
    {
        var schema = new OpenApiSchema
        {
            Description = "description",
            Deprecated = true,
            AllOf = [
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            ]
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
            AllOf = [
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    AllOf = [
                        new OpenApiSchema() {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>() {
                                ["firstName"] = new OpenApiSchema(),
                                ["lastName"] = new OpenApiSchema()
                            }
                        },
                        new OpenApiSchema() {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>() {
                                ["lastName"] = new OpenApiSchema()
                            }
                        },
                    ]
                }
            ]
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

    public class MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries
    {
        [Fact]
        public void DoesMergeWithInheritance()
        {
            var baseClassSchema = new OpenApiSchema()
            {
            };
            var document = new OpenApiDocument();
            document.AddComponent("BaseClass", baseClassSchema);
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                AnyOf =
                [
                    new OpenApiSchema()
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["one"] = new OpenApiSchema(),
                        },
                        AllOf =
                        [
                            new OpenApiSchemaReference("BaseClass", document),
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["firstName"] = new OpenApiSchema(),
                                    ["lastName"] = new OpenApiSchema()
                                }
                            },
                        ]
                    },
                ],
            };

            var result = schema.MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.True(schema.AnyOf[0].IsInherited());
            Assert.NotNull(result);
            Assert.True(result.IsInherited());
            Assert.Contains("one", result.Properties.Keys);
            Assert.Empty(result.AnyOf);
            Assert.Equal(2, result.AllOf.Count);
        }
        [Fact]
        public void DoesMergeWithIntersection()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                AnyOf =
                [
                    new OpenApiSchema()
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["one"] = new OpenApiSchema(),
                        },
                        AllOf =
                        [
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["first"] = new OpenApiSchema(),
                                }
                            },
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["second"] = new OpenApiSchema(),
                                }
                            },
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["third"] = new OpenApiSchema(),
                                }
                            },
                        ]
                    },
                ],
            };

            var result = schema.MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.NotNull(result);
            Assert.True(schema.AnyOf[0].IsIntersection());
            Assert.True(result.IsIntersection());
            Assert.Contains("one", result.Properties.Keys);
            Assert.Empty(result.AnyOf);
            Assert.Equal(3, result.AllOf.Count);
        }
        [Fact]
        public void DoesNotMergeWithMoreThanOneInclusiveEntry()
        {
            var baseClassSchema = new OpenApiSchema()
            {
            };
            var document = new OpenApiDocument();
            document.AddComponent("BaseClass", baseClassSchema);
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                AnyOf =
                [
                    new OpenApiSchema()
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["one"] = new OpenApiSchema(),
                        },
                        AllOf =
                        [
                            new OpenApiSchemaReference("BaseClass", document),
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["firstName"] = new OpenApiSchema(),
                                    ["lastName"] = new OpenApiSchema()
                                }
                            },
                        ]
                    },
                    new OpenApiSchema() { Type = JsonSchemaType.Object },
                ],
            };

            var result = schema.MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.Null(result);
        }
        [Fact]
        public void DoesNotMergeWithoutInheritanceOrIntersection()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                AnyOf =
                [
                    new OpenApiSchema()
                    {
                        AllOf =
                        [
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["firstName"] = new OpenApiSchema(),
                                    ["lastName"] = new OpenApiSchema()
                                }
                            },
                        ]
                    },
                ],
            };

            var result = schema.MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.Null(result);
        }
    }

    public class MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries
    {
        [Fact]
        public void DoesMergeWithInheritance()
        {
            var baseClassSchema = new OpenApiSchema()
            {
            };
            var document = new OpenApiDocument();
            document.AddComponent("BaseClass", baseClassSchema);
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                OneOf =
                [
                    new OpenApiSchema()
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["one"] = new OpenApiSchema(),
                        },
                        AllOf =
                        [
                            new OpenApiSchemaReference("BaseClass", document),
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["firstName"] = new OpenApiSchema(),
                                    ["lastName"] = new OpenApiSchema()
                                }
                            },
                        ]
                    },
                ],
            };

            var result = schema.MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.True(schema.OneOf[0].IsInherited());
            Assert.NotNull(result);
            Assert.True(result.IsInherited());
            Assert.Contains("one", result.Properties.Keys);
            Assert.Empty(result.OneOf);
            Assert.Equal(2, result.AllOf.Count);
        }
        [Fact]
        public void DoesMergeWithIntersection()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                OneOf =
                [
                    new OpenApiSchema()
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["one"] = new OpenApiSchema(),
                        },
                        AllOf =
                        [
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["first"] = new OpenApiSchema(),
                                }
                            },
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["second"] = new OpenApiSchema(),
                                }
                            },
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["third"] = new OpenApiSchema(),
                                }
                            },
                        ]
                    },
                ],
            };

            var result = schema.MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.NotNull(result);
            Assert.True(schema.OneOf[0].IsIntersection());
            Assert.True(result.IsIntersection());
            Assert.Contains("one", result.Properties.Keys);
            Assert.Empty(result.OneOf);
            Assert.Equal(3, result.AllOf.Count);
        }
        [Fact]
        public void DoesNotMergeWithMoreThanOneExclusiveEntry()
        {
            var baseClassSchema = new OpenApiSchema()
            {
            };
            var document = new OpenApiDocument();
            document.AddComponent("BaseClass", baseClassSchema);
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                OneOf =
                [
                    new OpenApiSchema()
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>()
                        {
                            ["one"] = new OpenApiSchema(),
                        },
                        AllOf =
                        [
                            new OpenApiSchemaReference("BaseClass", document),
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["firstName"] = new OpenApiSchema(),
                                    ["lastName"] = new OpenApiSchema()
                                }
                            },
                        ]
                    },
                    new OpenApiSchema() { Type = JsonSchemaType.Object },
                ],
            };

            var result = schema.MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.Null(result);
        }
        [Fact]
        public void DoesNotMergeWithoutInheritanceOrIntersection()
        {
            var schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                OneOf =
                [
                    new OpenApiSchema()
                    {
                        AllOf =
                        [
                            new OpenApiSchema()
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>()
                                {
                                    ["firstName"] = new OpenApiSchema(),
                                    ["lastName"] = new OpenApiSchema()
                                }
                            },
                        ]
                    },
                ],
            };

            var result = schema.MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries();
            Assert.Null(result);
        }
    }

    [Fact]
    public void IsArrayFalseOnEmptyItems()
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema(),
        };
        Assert.False(schema.IsArray());
    }
    [Fact]
    public void IsArrayFalseOnNullItems()
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
        };
        Assert.False(schema.IsArray());
    }
    [Fact]
    public void IsEnumFailsOnEmptyMembers()
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = [],
        };
        Assert.False(schema.IsEnum());

        schema.Enum.Add("");
        Assert.False(schema.IsEnum());
    }
    private static readonly OpenApiSchema enumSchema = new OpenApiSchema
    {
        Title = "riskLevel",
        Enum =
            [
            "low",
            "medium",
            "high",
            "hidden",
            "none",
            "unknownFutureValue"
        ],
        Type = JsonSchemaType.String
    };
    [Fact]
    public void IsEnumIgnoresNullableUnions()
    {
        var schema = new OpenApiSchema
        {
            AnyOf =
            [
                enumSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                }
            ]
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsEnumFailsOnNullableInheritance()
    {
        var schema = new OpenApiSchema
        {
            AllOf =
            [
                enumSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                }
            ]
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsEnumIgnoresNullableExclusiveUnions()
    {
        var schema = new OpenApiSchema
        {
            OneOf =
            [
                enumSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                }
            ]
        };
        Assert.False(schema.IsEnum());
    }
    private static readonly OpenApiSchema numberSchema = new OpenApiSchema
    {
        Type = JsonSchemaType.Number,
        Format = "double",
    };
    [Fact]
    public void IsEnumDoesNotMaskExclusiveUnions()
    {
        var schema = new OpenApiSchema
        {
            OneOf =
            [
                enumSchema,
                numberSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                }
            ]
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsEnumDoesNotMaskUnions()
    {
        var schema = new OpenApiSchema
        {
            AnyOf =
            [
                enumSchema,
                numberSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                }
            ]
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsOdataPrimitive()
    {
        var schema = new OpenApiSchema
        {
            OneOf =
            [
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.Number | JsonSchemaType.Null,
                    Format = "double",
                },
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.String | JsonSchemaType.Null,
                },
                new OpenApiSchema()
                {
                    Enum =
                    [
                        "-INF",
                        "INF",
                        "NaN",
                    ],
                    Type = JsonSchemaType.String | JsonSchemaType.Null,
                }
            ]
        };
        Assert.True(schema.IsODataPrimitiveType());
    }
    [Fact]
    public void IsOdataPrimitiveBackwardCompatible()
    {
        var schema = new OpenApiSchema
        {
            OneOf =
            [
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.Number | JsonSchemaType.Null,
                    Format = "double",
                },
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.String | JsonSchemaType.Null,
                },
                new OpenApiSchema()
                {
                    Enum =
                    [
                        "-INF",
                        "INF",
                        "NaN",
                    ]
                }
            ]
        };
        Assert.True(schema.IsODataPrimitiveType());
    }
    [Fact]
    public void ReturnsEmptyPropertyNameOnCircularReferences()
    {
        var document = new OpenApiDocument();
        var entitySchema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["id"] = new OpenApiSchemaReference("microsoft.graph.entity")
            }
        };
        var userSchema = new OpenApiSchema
        {
            OneOf =
            [
                entitySchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["firstName"] = new OpenApiSchemaReference("microsoft.graph.entity")
                    }
                }
            ],
            Discriminator = new OpenApiDiscriminator
            {
                Mapping = new Dictionary<string, OpenApiSchemaReference>
                {
                    ["microsoft.graph.entity"] = new OpenApiSchemaReference("entity", document),
                    ["microsoft.graph.user"] = new OpenApiSchemaReference("user", document)
                }
            }
        };
        document.AddComponent("microsoft.graph.entity", entitySchema);
        document.AddComponent("microsoft.graph.user", userSchema);
        document.SetReferenceHostDocument();
        entitySchema.AllOf =
        [
            userSchema
        ];
        Assert.Empty(userSchema.GetDiscriminatorPropertyName());
    }
    [Fact]
    public void GetsClassName()
    {
        var reference = new OpenApiSchemaReference("microsoft.graph.user", new());
        Assert.Equal("user", reference.GetClassName());
    }
    [Fact]
    public void GetsClassNameDefensive()
    {
        var reference = new OpenApiSchema();
        Assert.Empty(reference.GetClassName());
    }
}
