﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class CSharpLanguageRefinerTests
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
    }
    [Fact]
    public async Task FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock;
        declaration.Inherits = new CodeType
        {
            Name = "SomeOtherModel"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root));
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task DoesNotEscapesReservedKeywordsForClassOrPropertyKind()
    {
        // Arrange
        var model = root.AddClass(new CodeClass
        {
            Name = "break", // this a keyword
            Kind = CodeClassKind.Model,
        }).First();
        var property = model.AddProperty(new CodeProperty
        {
            Name = "alias",// this a keyword
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        // Act
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.Equal("break", model.Name);
        Assert.DoesNotContain("@", model.Name); // classname will be capitalized
        Assert.Equal("alias", property.Name);
        Assert.DoesNotContain("@", property.Name); // classname will be capitalized
    }

    [Theory]
    [InlineData("integer")]
    [InlineData("boolean")]
    [InlineData("tuple")]
    [InlineData("single")]
    [InlineData("random")]
    [InlineData("buffer")]
    [InlineData("convert")]
    [InlineData("action")]
    [InlineData("valueType")]
    public async Task EscapesReservedTypeNames(string typeName)
    {
        // Arrange
        var model = root.AddClass(new CodeClass
        {
            Name = typeName,
            Kind = CodeClassKind.Model,
        }).First();
        var property = model.AddProperty(new CodeProperty
        {
            Name = typeName,// this a keyword
            Type = new CodeType
            {
                Name = typeName,
                IsExternal = true
            }
        }).First();
        // Act
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.NotEqual(typeName, model.Name);
        Assert.Equal($"{typeName}Object", model.Name);//our defined model is renamed
        Assert.Equal(typeName, property.Type.Name);//external type is unchanged
        Assert.Equal(typeName, property.Name);//external type property name is unchanged
    }

    [Fact]
    public async Task EscapesReservedKeywordsForReservedNamespaceNameSegments()
    {
        var subNS = root.AddNamespace($"{root.Name}.task"); // otherwise the import gets trimmed
        var requestBuilder = subNS.AddClass(new CodeClass
        {
            Name = "tasksRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        var indexerCodeType = new CodeType { Name = "taskItemRequestBuilder" };
        var indexer = new CodeIndexer
        {
            Name = "idx",
            SerializationName = "id",
            IndexType = new CodeType
            {
                Name = "string",
            },
            ReturnType = indexerCodeType
        };
        requestBuilder.SetIndexer(indexer);


        var itemSubNamespace = root.AddNamespace($"{subNS.Name}.item"); // otherwise the import gets trimmed
        var itemRequestBuilder = itemSubNamespace.AddClass(new CodeClass
        {
            Name = "taskItemRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        var requestExecutor = itemRequestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.IndexerBackwardCompatibility,
            ReturnType = new CodeType
            {
                Name = "String"
            },
        }).First();

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Contains("TaskNamespace", subNS.Name);
        Assert.Contains("TaskNamespace", itemSubNamespace.Name);
    }
    [Fact]
    public async Task ConvertsUnionTypesToWrapper()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var union = new CodeUnionType
        {
            Name = "union",
        };
        union.AddType(new()
        {
            Name = "type1",
        }, new()
        {
            Name = "type2"
        });
        var property = model.AddProperty(new CodeProperty
        {
            Name = "deserialize",
            Kind = CodePropertyKind.Custom,
            Type = union.Clone() as CodeTypeBase,
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = union.Clone() as CodeTypeBase
        }).First();
        var parameter = new CodeParameter
        {
            Name = "param1",
            Type = union.Clone() as CodeTypeBase
        };
        var indexer = new CodeIndexer
        {
            Name = "idx",
            ReturnType = union.Clone() as CodeTypeBase,
            IndexType = new CodeType
            {
                Name = "string"
            }
        };
        model.SetIndexer(indexer);
        method.AddParameter(parameter);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the indexer doesn't get removed
        Assert.True(property.Type is CodeType);
        Assert.True(parameter.Type is CodeType);
        Assert.True(method.ReturnType is CodeType);
        Assert.True(indexer.ReturnType is CodeType);
        var resultingWrapper = root.FindChildByName<CodeClass>("union");
        Assert.NotNull(resultingWrapper);
        Assert.NotNull(resultingWrapper.OriginalComposedType);
    }
    [Fact]
    public async Task MovesClassesWithNamespaceNamesUnderNamespace()
    {
        var graphNS = root.AddNamespace("graph");
        var modelNS = graphNS.AddNamespace("graph.model");
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Single(root.GetChildElements(true));
        Assert.Single(graphNS.GetChildElements(true));
        Assert.Single(modelNS.GetChildElements(true));
        Assert.Equal(modelNS, model.Parent);
    }
    [Fact]
    public async Task KeepsCancellationParametersInRequestExecutors()
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
                Description = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the cancellationToken doesn't get removed
        Assert.True(method.Parameters.Any());
        Assert.Contains(cancellationParam, method.Parameters);
    }
    #endregion
    #region CSharp
    [Fact]
    public async Task DisambiguatePropertiesWithClassNames()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        var propToAdd = model.AddProperty(new CodeProperty
        {
            Name = "model",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("model_prop", propToAdd.Name);
        Assert.Equal("model", propToAdd.SerializationName);
    }
    [Fact]
    public async Task DisambiguatePropertiesWithClassNames_DoesntReplaceSerializationName()
    {
        var serializationName = "serializationName";
        var model = root.AddClass(new CodeClass
        {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        var propToAdd = model.AddProperty(new CodeProperty
        {
            Name = "model",
            Type = new CodeType
            {
                Name = "string"
            },
            SerializationName = serializationName,
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal(serializationName, propToAdd.SerializationName);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeType()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeType()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    #endregion
}
