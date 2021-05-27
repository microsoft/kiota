using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Kiota.Builder.Extensions.Tests {
    public class OpenApiSchemaExtensionsTests {
        [Fact]
        public void Defensive() {
            Assert.Empty(OpenApiSchemaExtensions.GetSchemaReferenceIds(null));
        }
        [Fact]
        public void GetSchemaTitleAllOf() {
            var schema = new OpenApiSchema {
                AllOf = new List<OpenApiSchema> {
                    new() {
                        Title = "microsoft.graph.entity"
                    },
                    new() {
                        Title = "microsoft.graph.user"
                    }
                }
            };
            var names = schema.GetSchemaTitles();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Contains("microsoft.graph.user", names);
            Assert.Equal("microsoft.graph.user", schema.GetSchemaTitle());
        }
        [Fact]
        public void GetSchemaTitleAllOfNested() {
            var schema = new OpenApiSchema {
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
            var names = schema.GetSchemaTitles();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Contains("microsoft.graph.user", names);
            Assert.Equal("microsoft.graph.user", schema.GetSchemaTitle());
        }
        [Fact]
        public void GetSchemaTitleAnyOf() {
            var schema = new OpenApiSchema {
                AnyOf = new List<OpenApiSchema> {
                    new() {
                        Title = "microsoft.graph.entity"
                    },
                    new() {
                        Title = "microsoft.graph.user"
                    }
                }
            };
            var names = schema.GetSchemaTitles();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Contains("microsoft.graph.user", names);
            Assert.Equal("microsoft.graph.user", schema.GetSchemaTitle());
        }
        [Fact]
        public void GetSchemaTitleOneOf() {
            var schema = new OpenApiSchema {
                OneOf = new List<OpenApiSchema> {
                    new() {
                        Title = "microsoft.graph.entity"
                    },
                    new() {
                        Title = "microsoft.graph.user"
                    }
                }
            };
            var names = schema.GetSchemaTitles();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Contains("microsoft.graph.user", names);
            Assert.Equal("microsoft.graph.user", schema.GetSchemaTitle());
        }
        [Fact]
        public void GetSchemaTitleItems() {
            var schema = new OpenApiSchema {
                Items = new() {
                    Title = "microsoft.graph.entity"
                },
            };
            var names = schema.GetSchemaTitles();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Equal("microsoft.graph.entity", schema.GetSchemaTitle());
            Assert.Single(names);
        }
        [Fact]
        public void GetSchemaTitleTitle() {
            var schema = new OpenApiSchema {
                Title = "microsoft.graph.entity"
            };
            var names = schema.GetSchemaTitles();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Equal("microsoft.graph.entity", schema.GetSchemaTitle());
            Assert.Single(names);
        }
        [Fact]
        public void GetSchemaTitleEmpty() {
            var schema = new OpenApiSchema {
            };
            var names = schema.GetSchemaTitles();
            Assert.Empty(names);
            Assert.Null(schema.GetSchemaTitle());
        }
        [Fact]
        public void GetReferenceIdsAllOf() {
            var schema = new OpenApiSchema {
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
        public void GetReferenceIdsAllOfNested() {
            var schema = new OpenApiSchema {
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
        public void GetReferenceIdsAnyOf() {
            var schema = new OpenApiSchema {
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
        public void GetReferenceIdsOneOf() {
            var schema = new OpenApiSchema {
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
        public void GetReferenceIdsItems() {
            var schema = new OpenApiSchema {
                Items = new() {
                    Reference = new() {
                        Id = "microsoft.graph.entity"
                    }
                },
            };
            var names = schema.GetSchemaReferenceIds();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Single(names);
        }
        [Fact]
        public void GetReferenceIdsTitle() {
            var schema = new OpenApiSchema {
                Reference = new() {
                    Id = "microsoft.graph.entity"
                }
            };
            var names = schema.GetSchemaReferenceIds();
            Assert.Contains("microsoft.graph.entity", names);
            Assert.Single(names);
        }
        [Fact]
        public void GetReferenceIdsEmpty() {
            var schema = new OpenApiSchema {
            };
            var names = schema.GetSchemaReferenceIds();
            Assert.Empty(names);
        }
    }
}
