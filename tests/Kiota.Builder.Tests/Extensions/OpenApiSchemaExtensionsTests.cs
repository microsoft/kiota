using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
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
                new OpenApiSchema()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.entity"
                    }
                },
                new OpenApiSchema()
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
                        new OpenApiSchema()
                        {
                            Reference = new()
                            {
                                Id = "microsoft.graph.entity"
                            }
                        },
                        new OpenApiSchema()
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
                new OpenApiSchema()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.entity"
                    }
                },
                new OpenApiSchema()
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
                new OpenApiSchema()
                {
                    Reference = new()
                    {
                        Id = "microsoft.graph.entity"
                    }
                },
                new OpenApiSchema()
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
            Items = new OpenApiSchema()
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
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
                new OpenApiSchema() {
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
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    AllOf = new List<IOpenApiSchema> {
                        new OpenApiSchema() {
                            Reference = new() {
                                Id = "microsoft.graph.entity"
                            }
                        },
                        new OpenApiSchema() {
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
            AnyOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
                new OpenApiSchema() {
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
            OneOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
                new OpenApiSchema() {
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
            Items = new OpenApiSchema()
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
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    },
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
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    },
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    },
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["firstName"] = new OpenApiSchema()
                    }
                }
            }
        };
        Assert.False(schema.IsInherited());
        Assert.True(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            AllOf = new List<IOpenApiSchema> {
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
            }
        };
        Assert.False(schema.IsInherited());
        Assert.True(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                }
            }
        };
        Assert.False(schema.IsInherited());
        Assert.False(schema.IsIntersection());

        schema = new OpenApiSchema
        {
            Title = "Trader Id",
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema()
                {
                    Title = "UserId",
                    Description = "unique identifier",
                    Type = JsonSchemaType.String,
                    Pattern = "^[1-9][0-9]*$",
                    Example = "1323232",
                    Reference = new OpenApiReference
                    {
                        Id = "UserId" // This property makes the schema "meaningful"
                    }
                }
            },
            Reference = new OpenApiReference
            {
                Id = "TraderId" // This property makes the schema "meaningful"
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
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    },
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Reference = new() {
                        Id = "microsoft.graph.user"
                    },
                    Properties = new Dictionary<string, IOpenApiSchema>() {
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
            AllOf = new List<IOpenApiSchema> {
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>() {
                        ["id"] = new OpenApiSchema()
                    }
                },
                new OpenApiSchema() {
                    Type = JsonSchemaType.Object,
                    AllOf = new List<IOpenApiSchema>() {
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

    public class MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries
    {
        [Fact]
        public void DoesMergeWithInheritance()
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
                                Reference = new()
                                {
                                    Id = "BaseClass"
                                },
                            },
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
                                Reference = new()
                                {
                                    Id = "BaseClass"
                                },
                            },
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
                    new() { Type = JsonSchemaType.Object },
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
                                Reference = new()
                                {
                                    Id = "BaseClass"
                                },
                            },
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
                                Reference = new()
                                {
                                    Id = "BaseClass"
                                },
                            },
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
                    new() { Type = JsonSchemaType.Object },
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
            Enum = new List<JsonNode>(),
        };
        Assert.False(schema.IsEnum());

        schema.Enum.Add("");
        Assert.False(schema.IsEnum());
    }
    private static readonly OpenApiSchema enumSchema = new OpenApiSchema
    {
        Title = "riskLevel",
        Enum = new List<JsonNode>
            {
            "low",
            "medium",
            "high",
            "hidden",
            "none",
            "unknownFutureValue"
        },
        Type = JsonSchemaType.String
    };
    [Fact]
    public void IsEnumIgnoresNullableUnions()
    {
        var schema = new OpenApiSchema
        {
            AnyOf = new List<IOpenApiSchema>
            {
                enumSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
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
            AllOf = new List<IOpenApiSchema>
            {
                enumSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
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
            OneOf = new List<IOpenApiSchema>
            {
                enumSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Nullable = true
                }
            }
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
            OneOf = new List<IOpenApiSchema>
            {
                enumSchema,
                numberSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
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
            AnyOf = new List<IOpenApiSchema>
            {
                enumSchema,
                numberSchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Nullable = true
                }
            }
        };
        Assert.False(schema.IsEnum());
    }
    [Fact]
    public void IsOdataPrimitive()
    {
        var schema = new OpenApiSchema
        {
            OneOf = new List<IOpenApiSchema>
            {
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.Number,
                    Format = "double",
                    Nullable = true
                },
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.String,
                    Nullable = true
                },
                new OpenApiSchema()
                {
                    Enum = new List<JsonNode>
                    {
                        "INF",
                        "INF",
                        "NaN",
                    },
                    Type = JsonSchemaType.String,
                    Nullable = true
                }
            }
        };
        Assert.True(schema.IsODataPrimitiveType());
    }
    [Fact]
    public void IsOdataPrimitiveBackwardCompatible()
    {
        var schema = new OpenApiSchema
        {
            OneOf = new List<IOpenApiSchema>
            {
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.Number,
                    Format = "double",
                },
                new OpenApiSchema()
                {
                    Type = JsonSchemaType.String,
                },
                new OpenApiSchema()
                {
                    Enum = new List<JsonNode>()
                    {
                        "INF",
                        "INF",
                        "NaN",
                    }
                }
            }
        };
        Assert.True(schema.IsODataPrimitiveType());
    }
    [Fact]
    public void ReturnsEmptyPropertyNameOnCircularReferences()
    {
        var entitySchema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.entity"
            },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["id"] = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Id = "microsoft.graph.entity"
                    }
                }
            }
        };
        var userSchema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = "microsoft.graph.user"
            },
            OneOf =
            [
                entitySchema,
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["firstName"] = new OpenApiSchema
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "microsoft.graph.entity"
                            }
                        }
                    }
                }
            ],
            Discriminator = new OpenApiDiscriminator
            {
                Mapping = new Dictionary<string, string>
                {
                    ["microsoft.graph.entity"] = "entity",
                    ["microsoft.graph.user"] = "user"
                }
            }
        };
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
