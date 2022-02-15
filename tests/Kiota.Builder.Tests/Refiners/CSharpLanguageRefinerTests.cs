using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;
public class CSharpLanguageRefinerTests {
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public void AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        
        var declaration = model.StartBlock as CodeClass.Declaration;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits.Name);
    }
    [Fact]
    public void FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock as CodeClass.Declaration;
        declaration.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        Assert.Throws<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root));
    }
    [Fact]
    public void AddsUsingsForErrorTypesForRequestExecutor() {
        var requestBuilder = root.AddClass(new CodeClass {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var errorClass = subNS.AddClass(new CodeClass {
            Name = "Error4XX",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            },
        }).First();
        requestExecutor.ErrorMappings.TryAdd("4XX", new CodeType {
                        Name = "Error4XX",
                        TypeDefinition = errorClass,
                    });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        
        var declaration = requestBuilder.StartBlock as CodeClass.Declaration;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public void DoesNotEscapesReservedKeywordsForClassOrPropertyKind() {
        // Arrange
        var model = root.AddClass(new CodeClass {
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.Equal("break", model.Name);
        Assert.DoesNotContain("@", model.Name); // classname will be capitalized
        Assert.Equal("alias", property.Name);
        Assert.DoesNotContain("@", property.Name); // classname will be capitalized
    }
    [Fact]
    public void ConvertsUnionTypesToWrapper() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var union = new CodeUnionType {
            Name = "union",
        };
        union.AddType(new () {
            Name = "type1",
        }, new() {
            Name = "type2"
        });
        var property = model.AddProperty(new CodeProperty {
            Name = "deserialize",
            Kind = CodePropertyKind.Custom,
            Type = union.Clone() as CodeTypeBase,
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = union.Clone() as CodeTypeBase
        }).First();
        var parameter = new CodeParameter {
            Name = "param1",
            Type = union.Clone() as CodeTypeBase
        };
        var indexer = new CodeIndexer {
            Name = "idx",
            ReturnType = union.Clone() as CodeTypeBase,
        };
        model.SetIndexer(indexer);
        method.AddParameter(parameter);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the indexer doesn't get removed
        Assert.True(property.Type is CodeType);
        Assert.True(parameter.Type is CodeType);
        Assert.True(method.ReturnType is CodeType);
        Assert.True(indexer.ReturnType is CodeType);
    }
    [Fact]
    public void MovesClassesWithNamespaceNamesUnderNamespace() {
        var graphNS = root.AddNamespace("graph");
        var modelNS = graphNS.AddNamespace("graph.model");
        var model = graphNS.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Single(root.GetChildElements(true));
        Assert.Single(graphNS.GetChildElements(true));
        Assert.Single(modelNS.GetChildElements(true));
        Assert.Equal(modelNS, model.Parent);
    }
    [Fact]
    public void KeepsCancellationParametersInRequestExecutors()
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
            ReturnType = new CodeType { 
                Name = "string"
            }
        }).First();
        var cancellationParam = new CodeParameter
        {
            Name = "cancelletionToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Description = "Cancellation token to use when cancelling requests",
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.True(method.Parameters.Any());
        Assert.Contains(cancellationParam, method.Parameters);
    }
    #endregion
    #region CSharp
    [Fact]
    public void DisambiguatePropertiesWithClassNames() {
        var model = root.AddClass(new CodeClass {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        var propToAdd = model.AddProperty(new CodeProperty{
            Name = "model",
            Type = new CodeType {
                Name = "string"
            }
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("model_prop", propToAdd.Name);
        Assert.Equal("model", propToAdd.SerializationName);
    }
    [Fact]
    public void DisambiguatePropertiesWithClassNames_DoesntReplaceSerializationName() {
        var serializationName = "serializationName";
        var model = root.AddClass(new CodeClass {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        var propToAdd = model.AddProperty(new CodeProperty{
            Name = "model",
            Type = new CodeType {
                Name = "string"
            },
            SerializationName = serializationName,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal(serializationName, propToAdd.SerializationName);
    }
    [Fact]
    public void ReplacesDateOnlyByNativeType()
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesTimeOnlyByNativeType()
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    #endregion
}
