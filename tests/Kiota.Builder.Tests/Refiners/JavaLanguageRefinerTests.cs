using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class JavaLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task DoesNotReplacesReservedEnumOptionsAsync()
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "model",
        }).First();
        var option = new CodeEnumOption
        {
            Name = "Void", // this a keyword
        };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal("Void", option.Name);
        Assert.Empty(option.SerializationName);
    }
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClassesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
    }
    [Fact]
    public async Task InlineParentOnErrorClassesWhichAlreadyInheritAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        var otherModel = root.AddClass(new CodeClass
        {
            Name = "otherModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = false,
        }).First();
        otherModel.AddProperty(
        new CodeProperty
        {
            Name = "otherProp",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        otherModel.AddMethod(
        new CodeMethod
        {
            Name = "otherMethod",
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
        otherModel.AddUsing(
        new CodeUsing
        {
            Name = "otherNs",
        });
        otherModel.StartBlock.AddImplements(new CodeType
        {
            Name = "IAdditionalDataHolder",
            IsExternal = true
        });
        var declaration = model.StartBlock;
        declaration.Inherits = new CodeType
        {
            TypeDefinition = otherModel
        };
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("otherProp"));
        Assert.Contains(model.Methods, x => x.Name.Equals("otherMethod"));
        Assert.Contains(model.Usings, x => x.Name.Equals("otherNs"));
        Assert.Equal("ApiException", model.StartBlock.Inherits.Name);
        Assert.Contains(model.StartBlock.Implements, x => x.Name.Equals("AdditionalDataHolder", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutorAsync()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var errorClass = subNS.AddClass(new CodeClass
        {
            Name = "Error4XX",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();
        requestExecutor.AddErrorMapping("4XX", new CodeType
        {
            Name = "Error4XX",
            TypeDefinition = errorClass,
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task EscapesReservedKeywordsInInternalDeclarationAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        var nUsing = new CodeUsing
        {
            Name = "some.ns",
        };
        nUsing.Declaration = new CodeType
        {
            IsExternal = false,
            TypeDefinition = model
        };
        model.AddUsing(nUsing);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("break", nUsing.Declaration.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Escaped", nUsing.Declaration.Name);
    }
    [Fact]
    public async Task EscapesReservedKeywordsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("break", model.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Escaped", model.Name);
    }
    [Fact]
    public async Task EscapeReservedKeywordsInNamespaceToLowercaseAsync()
    {
        var ns = root.AddNamespace("new");
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal(ns.Name.ToLower(), ns.Name);
    }
    [Fact]
    public async Task ConvertEnumsToPascalCaseAsync()
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "foo_bar"
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEqual("foo_bar", model.Name);
        Assert.Contains("FooBar", model.Name);
    }
    [Fact]
    public async Task AddsDefaultImportsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "rb",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string",
            },
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEmpty(requestBuilder.StartBlock.Usings);
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateTimeOffset"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("OffsetDateTime", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateOnly"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("LocalDate", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeOnly"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("LocalTime", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDurationByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeSpan"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("PeriodAndDuration", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesBinaryByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "binary"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEqual("binary", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesIndexersByMethodsWithParameterAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var collectionNS = root.AddNamespace("collection");
        var itemsNs = collectionNS.AddNamespace($"{collectionNS.Name}.items");
        var requestBuilder = itemsNs.AddClass(new CodeClass
        {
            Name = "requestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        requestBuilder.AddProperty(new CodeProperty
        {
            Name = "urlTemplate",
            DefaultValue = "path",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "string",
            }
        });
        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "idx",
            ReturnType = new CodeType
            {
                Name = requestBuilder.Name,
                TypeDefinition = requestBuilder,
            },
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string",
                },
            }
        });
        var collectionRequestBuilder = collectionNS.AddClass(new CodeClass
        {
            Name = "CollectionRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        collectionRequestBuilder.AddProperty(new CodeProperty
        {
            Name = "collection",
            Kind = CodePropertyKind.RequestBuilder,
            Type = new CodeType
            {
                Name = requestBuilder.Name,
                TypeDefinition = requestBuilder,
            },
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Single(requestBuilder.Properties);
        Assert.Empty(requestBuilder.GetChildElements(true).OfType<CodeIndexer>());
        Assert.Single(requestBuilder.Methods, static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility));
        Assert.Single(collectionRequestBuilder.Properties);
    }
    [Fact]
    public async Task DoesNotKeepCancellationParametersInRequestExecutorsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "getMethod",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        var cancellationParam = new CodeParameter
        {
            Name = "cancelletionToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Documentation = new()
            {
                DescriptionTemplate = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task NormalizeMethodTypesNamesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "getMethod",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        var nonNormalizedParam = new CodeParameter
        {
            Name = "something",
            Type = new CodeType { Name = "foo_bar", IsExternal = true },
        };
        var normalizedModel = root.AddClass(new CodeClass { Name = "foo_baz" }).First();
        var normalizedParam = new CodeParameter
        {
            Name = "somethingElse",
            Type = new CodeType { TypeDefinition = normalizedModel },
        };
        method.AddParameter(nonNormalizedParam);
        method.AddParameter(normalizedParam);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal("Foo_bar", method.Parameters.First().Type.Name);
        Assert.Equal("FooBaz", method.Parameters.Last().Type.Name);
    }
    [Fact]
    public async Task NormalizeInheritedClassesNamesAsync()
    {
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "parent_Model",
            Kind = CodeClassKind.Model,
        }).First();
        var implementsModel = root.AddClass(new CodeClass
        {
            Name = "implements_Model",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "parent_Model",
            TypeDefinition = parentModel,
        };
        childModel.StartBlock.AddImplements(new CodeType
        {
            Name = "implements_Model",
            TypeDefinition = implementsModel,
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal("ParentModel", childModel.StartBlock.Inherits.Name);
        Assert.Equal("ImplementsModel", childModel.StartBlock.Implements.First().Name);
    }
    #endregion
    #region JavaLanguageRefinerTests
    [Fact]
    public async Task AddsEnumSetImportAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "prop1",
            Type = new CodeType
            {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum
                {
                    Name = "SomeEnum",
                    Flags = true,
                    Parent = root,
                }
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Contains(model.StartBlock.Usings, x => "EnumSet".Equals(x.Name));
    }
    [Fact]
    public async Task CorrectsCoreTypeAsync()
    {
        const string requestAdapterDefaultName = "IRequestAdapter";
        const string factoryDefaultName = "ISerializationWriterFactory";
        const string deserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
        const string dateTimeOffsetDefaultName = "DateTimeOffset";
        const string additionalDataDefaultName = "new Dictionary<string, object>()";
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new()
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = requestAdapterDefaultName
            }
        }, new()
        {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = dateTimeOffsetDefaultName,
            }
        }, new()
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = additionalDataDefaultName
            }
        });
        const string additionalDataHolderDefaultName = "IAdditionalDataHolder";
        model.StartBlock.AddImplements(new CodeType
        {
            Name = additionalDataHolderDefaultName,
            IsExternal = true,
        });
        var executorMethod = model.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }, new()
        {
            Name = "deserializeFields",
            ReturnType = new CodeType
            {
                Name = deserializeDefaultName,
            },
            Kind = CodeMethodKind.Deserializer
        }).First();
        const string serializerDefaultName = "ISerializationWriter";
        var serializationMethod = model.AddMethod(new CodeMethod
        {
            Name = "seriailization",
            Kind = CodeMethodKind.Serializer,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        serializationMethod.AddParameter(new CodeParameter
        {
            Name = "handler",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = serializerDefaultName,
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.DoesNotContain(model.Properties, static x => requestAdapterDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => factoryDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => dateTimeOffsetDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => additionalDataDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Methods, static x => deserializeDefaultName.Equals(x.ReturnType.Name));
        Assert.DoesNotContain(model.Methods.SelectMany(static x => x.Parameters), static x => serializerDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.StartBlock.Implements, static x => additionalDataHolderDefaultName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(additionalDataHolderDefaultName[1..], model.StartBlock.Implements.Select(static x => x.Name).ToList());
    }
    [Fact]
    public async Task ProduceCorrectNamesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var custom = new CodeProperty
        {
            Name = "custom",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string",
            }
        };
        model.AddProperty(custom);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.True(string.IsNullOrEmpty(model.Properties.First(static x => "custom".Equals(x.Name))!.NamePrefix));
    }
    [Fact]
    public async Task AddsMethodsOverloadsAsync()
    {
        var builder = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var executor = builder.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        executor.AddParameter(new()
        {
            Name = "config",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        },
        new()
        {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        var generator = builder.AddMethod(new CodeMethod
        {
            Name = "generator",
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        generator.AddParameter(executor.Parameters.ToArray());
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        var childMethods = builder.Methods;
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 1);//only the body
        Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 1);//only the body
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 2);// body + query config
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 2);// body + query config
        Assert.Equal(4, childMethods.Count());
        Assert.Equal(2, childMethods.Count(x => x.IsOverload));
    }
    [Fact]
    public async Task SplitsLongRefinersAsync()
    {
        var model = new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "model",
        };
        model.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";

        var otherModel = new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "otherModel"
        };
        root.AddClass(otherModel);

        Enumerable.Range(0, 1500).ToList().ForEach(x => model.DiscriminatorInformation.AddDiscriminatorMapping($"#microsoft.graph.{x}", new CodeType
        {
            Name = $"microsoft.graph.{x}",
            TypeDefinition = otherModel,
        }));
        model.AddMethod(new CodeMethod
        {
            Kind = CodeMethodKind.Factory,
            Name = "factory",
            ReturnType = new CodeType
            {
                Name = "model",
                TypeDefinition = model,
            },
            IsAsync = false,
            IsStatic = true,
        });
        root.AddClass(model);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal(4, model.Methods.Count());
        Assert.Equal("String", model.Methods.First(static x => x.IsOverload).Parameters.First().Type.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddsUsingForUntypedNodeAsync(bool usesBackingStore)
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var property = model.AddProperty(new CodeProperty
        {
            Name = "property",
            Type = new CodeType
            {
                Name = KiotaBuilder.UntypedNodeName,
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java, UsesBackingStore = usesBackingStore }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, property.Type.Name);
        Assert.NotEmpty(model.StartBlock.Usings);
        var nodeUsing = model.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Equal(2, nodeUsing.Length); // one for the getter and another for setter. Writer will unionise
        Assert.Equal("com.microsoft.kiota.serialization", nodeUsing[0].Declaration.Name);
    }
    [Fact]
    public async Task AddsUsingForUntypedNodeInMethodParameterAsync()
    {
        var requestBuilderClass = root.AddClass(new CodeClass() { Name = "NodeRequestBuilder" }).First();
        var method = new CodeMethod
        {
            Name = "getAsync",
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
            Kind = CodeMethodKind.RequestExecutor
        };
        method.AddParameter(new CodeParameter()
        {
            Name = "jsonData",
            Type = new CodeType()
            {
                Name = KiotaBuilder.UntypedNodeName,
                IsExternal = true
            },
            Kind = CodeParameterKind.RequestBody
        });
        requestBuilderClass.AddMethod(method);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, method.Parameters.First().Type.Name);// type is renamed
        Assert.NotEmpty(requestBuilderClass.StartBlock.Usings);
        var nodeUsing = requestBuilderClass.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("com.microsoft.kiota.serialization", nodeUsing[0].Declaration.Name);
    }
    #endregion
}
