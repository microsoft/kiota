using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class DartLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task EscapesReservedKeywordsInInternalDeclaration()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.NotEqual("break", nUsing.Declaration.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("_", nUsing.Declaration.Name);
    }
    [Fact]
    public async Task EscapesReservedKeywords()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.NotEqual("break", model.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("_", model.Name);
    }
    [Fact]
    public async Task AddsDefaultImports()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.NotEmpty(requestBuilder.StartBlock.Usings);
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeType()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateTime", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesTimeSpanByNativeType()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Duration", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesIndexersByMethodsWithParameter()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.Single(requestBuilder.Properties);
        Assert.Empty(requestBuilder.GetChildElements(true).OfType<CodeIndexer>());
        Assert.Single(requestBuilder.Methods, static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility));
        Assert.Single(collectionRequestBuilder.Properties);
    }
    [Fact]
    public async Task DoesNotKeepCancellationParametersInRequestExecutors()
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
            Name = "cancellationToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Documentation = new()
            {
                DescriptionTemplate = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task NormalizeInheritedClassesNames()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.Equal("ParentModel", childModel.StartBlock.Inherits.Name);
        Assert.Equal("ImplementsModel", childModel.StartBlock.Implements.First().Name);
    }
    #endregion
    #region DartLanguageRefinerTests
    [Fact]
    public async Task CorrectsCoreType()
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
            Name = "serialization",
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.DoesNotContain(model.Properties, static x => requestAdapterDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => factoryDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => dateTimeOffsetDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => additionalDataDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Methods, static x => deserializeDefaultName.Equals(x.ReturnType.Name));
        Assert.DoesNotContain(model.Methods.SelectMany(static x => x.Parameters), static x => serializerDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.StartBlock.Implements, static x => additionalDataHolderDefaultName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(additionalDataHolderDefaultName[1..], model.StartBlock.Implements.Select(static x => x.Name).ToList());
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddsUsingForUntypedNode(bool usesBackingStore)
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart, UsesBackingStore = usesBackingStore }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, property.Type.Name);
        Assert.NotEmpty(model.StartBlock.Usings);
        var nodeUsing = model.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("microsoft_kiota_abstractions/microsoft_kiota_abstractions", nodeUsing[0].Declaration.Name);
    }
    [Fact]
    public async Task AddsCustomMethods()
    {
        var builder = root.AddClass(new CodeClass
        {
            Name = "builder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        var buildermethods = builder.Methods;
        var modelmethods = model.Methods;
        Assert.Contains(buildermethods, x => x.IsOfKind(CodeMethodKind.Custom) && x.Name.Equals("clone", StringComparison.Ordinal));
        Assert.Contains(modelmethods, x => x.IsOfKind(CodeMethodKind.Custom) && x.Name.Equals("copyWith", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PreservesPropertyNames()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model,
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "Property",
            Type = new CodeType
            {
                Name = "string",
            },
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.Equal("property", model.Properties.First().Name);
        Assert.Equal("Property", model.Properties.First().WireName);
    }

    [Fact]
    public async Task DoesntOverwriteSerializationNameIfAlreadySet()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model,
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "CustomType",
            SerializationName = "$type",
            Type = new CodeType
            {
                Name = "string",
            },
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root);
        Assert.Equal("customType", model.Properties.First().Name);
        Assert.Equal("\\$type", model.Properties.First().WireName);
    }
    #endregion
}
