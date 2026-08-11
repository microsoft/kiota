using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class CSharpLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task EnumHasEscapedOption_UsesEnumMemberAttributeAsync()
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        var option = new CodeEnumOption { Name = "kiotaCsharpName", SerializationName = "Kiota:CSharp:Enum" };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        var declaration = model.StartBlock;

        Assert.Contains("EnumMemberAttribute", declaration.Usings.Select(x => x.Name));
    }
    [Theory]
    [InlineData("operator")]
    [InlineData("string")]
    public async Task EnumWithReservedName_IsNotRenamedAsync(string input)
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        var option = new CodeEnumOption { Name = input, SerializationName = input };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Equal(input, model.Options.First().Name);
    }
    [Fact]
    public async Task EnumDoesntHaveEscapedOption_DoesntUseEnumMemberAttributeAsync()
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        var option = new CodeEnumOption { Name = "item1" };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        var declaration = model.StartBlock;

        Assert.DoesNotContain("EnumMemberAttribute", declaration.Usings.Select(x => x.Name));
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

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
            IsAsync = false,
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("OtherProp"));
        Assert.Contains(model.Methods, x => x.Name.Equals("otherMethod"));
        Assert.Contains(model.Usings, x => x.Name.Equals("otherNs"));
        Assert.Contains(model.StartBlock.Implements, x => x.Name.Equals("IAdditionalDataHolder"));
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task DoesNotEscapesReservedKeywordsForClassOrPropertyKindAsync()
    {
        // Arrange
        var model = root.AddClass(new CodeClass
        {
            Name = "break", // this a keyword
            Kind = CodeClassKind.Model,
        }).First();
        var propertyWithCsharpReservedName = model.AddProperty(new CodeProperty
        {
            Name = "alias",// this a keyword
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        var propertyWithReservedTypeName = model.AddProperty(new CodeProperty
        {
            Name = "task",// this a type name reserved in C#
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.Equal("break", model.Name);
        Assert.DoesNotContain("@", model.Name); // classname will be capitalized
        Assert.Equal("Alias", propertyWithCsharpReservedName.Name);
        Assert.DoesNotContain("@", propertyWithCsharpReservedName.Name); // classname will be capitalized
        Assert.Equal("Task", propertyWithReservedTypeName.Name);
        Assert.DoesNotContain("@", propertyWithReservedTypeName.Name); // classname will be capitalized
        Assert.DoesNotContain("Escaped", propertyWithReservedTypeName.Name); // classname will be capitalized
    }

    [Fact]
    public async Task DoesNotEscapesReservedKeywordsForClassOrPropertyKindEnhancedAsync()
    {
        // Arrange
        var reservedModel = root.AddClass(new CodeClass
        {
            Name = "file", // this a keyword
            Kind = CodeClassKind.Model,
        }).First();
        var reservedObjectModel = root.AddClass(new CodeClass
        {
            Name = "fileObject", // this a what the renaming of the keyword would cause
            Kind = CodeClassKind.Model,
        }).First();
        var property = reservedModel.AddProperty(new CodeProperty
        {
            Name = "alias",// this a keyword
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        var secondProperty = reservedModel.AddProperty(new CodeProperty
        {
            Name = "file",// this a keyword
            Type = new CodeType
            {
                TypeDefinition = reservedModel
            }
        }).First();
        var thirdProperty = reservedModel.AddProperty(new CodeProperty
        {
            Name = "fileObject",// this a keyword
            Type = new CodeType
            {
                TypeDefinition = reservedObjectModel
            }
        }).First();
        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.Equal("fileObject1", reservedModel.Name);// classes/models will be renamed if reserved without conflicts
        Assert.Equal("fileObject", reservedObjectModel.Name);// original stays the same
        Assert.Equal("Alias", property.Name);// property names don't bring issue in dotnet
        Assert.Equal("File", secondProperty.Name);// property names don't bring issue in dotnet
        Assert.Equal("fileObject1", secondProperty.Type.Name);// property type was renamed 
        Assert.Equal("FileObject", thirdProperty.Name);// property names don't bring issue in dotnet
        Assert.Equal("fileObject", thirdProperty.Type.Name);// property type was renamed 

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
    public async Task EscapesReservedTypeNamesAsync(string typeName)
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.NotEqual(typeName, model.Name);
        Assert.Equal($"{typeName}Object", model.Name);//our defined model is renamed
        Assert.Equal(typeName, property.Type.Name);//external type is unchanged
        Assert.Equal(typeName.ToPascalCase(new[] { '_' }), property.Name.ToFirstCharacterUpperCase());//external type property name is in pascal-case
    }

    [Fact]
    public async Task EscapesReservedKeywordsForReservedNamespaceNameSegmentsAsync()
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
            ReturnType = indexerCodeType,
            IndexParameter = new()
            {
                Name = "id",
                SerializationName = "id",
                Type = new CodeType
                {
                    Name = "string",
                },
            }
        };
        requestBuilder.AddIndexer(indexer);


        var itemSubNamespace = root.AddNamespace($"{subNS.Name}.item"); // otherwise the import gets trimmed
        var itemRequestBuilder = itemSubNamespace.AddClass(new CodeClass
        {
            Name = "taskItemRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        itemRequestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.IndexerBackwardCompatibility,
            ReturnType = new CodeType
            {
                Name = "String"
            },
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Contains("TaskNamespace", subNS.Name);
        Assert.Contains("TaskNamespace", itemSubNamespace.Name);
    }
    [Fact]
    public async Task ConvertsUnionTypesToWrapperAsync()
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
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string"
                },
            }
        };
        model.AddIndexer(indexer);
        method.AddParameter(parameter);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the indexer doesn't get removed
        Assert.True(property.Type is CodeType);
        Assert.True(parameter.Type is CodeType);
        Assert.True(method.ReturnType is CodeType);
        Assert.True(indexer.ReturnType is CodeType);
        var resultingWrapper = root.FindChildByName<CodeClass>("union");
        Assert.NotNull(resultingWrapper);
        Assert.NotNull(resultingWrapper.OriginalComposedType);
        Assert.Contains("IComposedTypeWrapper", resultingWrapper.StartBlock.Implements.Select(static x => x.Name));
        Assert.Null(resultingWrapper.Methods.SingleOrDefault(static x => x.IsOfKind(CodeMethodKind.ComposedTypeMarker)));
    }
    [Fact]
    public async Task MovesClassesWithNamespaceNamesUnderNamespaceAsync()
    {
        var graphNS = root.AddNamespace("graph");
        var modelNS = graphNS.AddNamespace("graph.model");
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Single(root.GetChildElements(true));
        Assert.Single(graphNS.GetChildElements(true));
        Assert.Single(modelNS.GetChildElements(true));
        Assert.Equal(modelNS, model.Parent);
    }
    [Fact]
    public async Task KeepsCancellationParametersInRequestExecutorsAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the cancellationToken doesn't get removed
        Assert.True(method.Parameters.Any());
        Assert.Contains(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task ReplacesExceptionPropertiesNamesAsync()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "stacktrace",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("stacktraceEscaped", propToAdd.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("stacktrace", propToAdd.SerializationName, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task RenamesMatchAndAddsPrimaryErrorMessageIfMatchAlreadyExistsAsync()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "message",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        Assert.False(exception.Properties.First().IsOfKind(CodePropertyKind.ErrorMessageOverride));// property is NOT message override
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        var properties = exception.Properties.ToArray();
        Assert.Equal("messageEscaped", propToAdd.Name, StringComparer.OrdinalIgnoreCase);// property remains
        Assert.Equal(2, properties.Length); // no primary message property added
        Assert.Equal("message", properties[0].Name, StringComparer.OrdinalIgnoreCase); // name is expected.
        Assert.True(properties[0].IsOfKind(CodePropertyKind.ErrorMessageOverride));// property is now message override
        Assert.Equal("messageEscaped", properties[1].Name, StringComparer.OrdinalIgnoreCase); // renamed property is renamed as expected.
        Assert.True(properties[1].IsPrimaryErrorMessage);// property is IsPrimaryErrorMessage so that information deserialized into it shows up in the error information.
    }
    [Fact]
    public async Task RenamesExceptionClassWithReservedPropertyNameAsync()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "message",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "message",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        var properties = exception.Properties.ToArray();
        Assert.Equal("messageEscaped", exception.Name, StringComparer.OrdinalIgnoreCase);// class is renamed to avoid removing special overidden property
        Assert.Equal("messageEscapedProp", propToAdd.Name, StringComparer.OrdinalIgnoreCase); // property renamed to avoid conflicting with base
        Assert.Equal(2, properties.Length); // primary message is added
        Assert.Equal("message", properties[0].Name, StringComparer.OrdinalIgnoreCase); // we can still override exception message
        Assert.Equal("messageEscapedProp", properties[1].Name, StringComparer.OrdinalIgnoreCase); // collision with class name
    }
    [Fact]
    public async Task RenamesExceptionClassWithReservedPropertyNameWhenPropertyIsInitiallyAbsentAsync()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "message",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "something",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("messageEscaped", exception.Name, StringComparer.OrdinalIgnoreCase);// class is renamed to avoid removing special overidden property
        Assert.Equal("something", propToAdd.Name, StringComparer.OrdinalIgnoreCase); // existing property remains
        Assert.Equal(2, exception.Properties.Count()); // initial property plus primary message
        Assert.Equal("message", exception.Properties.ToArray()[0].Name, StringComparer.OrdinalIgnoreCase); // primary error message is present
        Assert.Equal("something", exception.Properties.ToArray()[1].Name, StringComparer.OrdinalIgnoreCase);// existing property remains
    }
    [Fact]
    public async Task DoesNotReplaceNonExceptionPropertiesNamesAsync()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = false,
        }).First();
        var propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "message",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("message", propToAdd.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("message", propToAdd.SerializationName, StringComparer.OrdinalIgnoreCase);
    }
    #endregion
    #region CSharp
    [Fact]
    public async Task DisambiguatePropertiesWithClassNamesAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("ModelProp", propToAdd.Name);
        Assert.Equal("model", propToAdd.SerializationName);
    }
    [Fact]
    public async Task AvoidsPropertyNameReplacementIfDuplicatedGeneratedAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        var firstProperty = model.AddProperty(new CodeProperty
        {
            Name = "summary",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        var secondProperty = model.AddProperty(new CodeProperty
        {
            Name = "_summary",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        var thirdProperty = model.AddProperty(new CodeProperty
        {
            Name = "_replaced",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("Summary", firstProperty.Name);// remains as is. No refinement needed
        Assert.Equal("_summary", secondProperty.Name);// No refinement as it will create a duplicate with firstProperty
        Assert.Equal("Replaced", thirdProperty.Name);// Base case. Proper refinements
    }
    [Fact]
    public async Task DisambiguatePropertiesWithClassNames_DoesntReplaceSerializationNameAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal(serializationName, propToAdd.SerializationName);
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
                Name = "DateOnly",
                IsExternal = true// this is external from the Kiota abstractions
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeTypeInNestedClassAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var nestedModel = model.AddInnerClass(new CodeClass
        {
            Name = "nestedModel",
            Kind = CodeClassKind.Model
        }).First();
        var propertyInNestedModel = nestedModel.AddProperty(new CodeProperty
        {
            Name = "nestedModelProperty",
            Type = new CodeType
            {
                Name = "DateOnly",
                IsExternal = true// this is external from the Kiota abstractions
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings); // using is added to outer class.
        Assert.Empty(nestedModel.StartBlock.Usings); // using is not added to nested model
        Assert.Equal("Date", propertyInNestedModel.Type.Name);
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
                Name = "TimeOnly",
                IsExternal = true // this is external from the Kiota abstractions
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesLocallyDefinedDateOnlyByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var dateOnlyModel = root.AddClass(new CodeClass
        {
            Name = "DateOnly",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateOnly",
                IsExternal = false,// this is internal from the description
                TypeDefinition = dateOnlyModel
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnlyObject", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesLocallyDefinedTimeOnlyByNativeTypeAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var timeOnlyModel = root.AddClass(new CodeClass
        {
            Name = "TimeOnly",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeOnly",
                IsExternal = false, // this is internal from the description
                TypeDefinition = timeOnlyModel
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnlyObject", method.ReturnType.Name);
    }

    [Fact]
    public async Task ReplacesIndexerDateOnlyTypeWithAbstractedDateTypeAsync()
    {
        // Arrange
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "DateOnly",
                    IsExternal = true
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("Date", requestBuilder.Indexer.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplacesIndexerTimeOnlyTypeWithAbstractedTimeTypeAsync()
    {
        // Arrange
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "TimeOnly",
                    IsExternal = true
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("Time", requestBuilder.Indexer.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplacesIndexerLocallyDefinedDateOnlyTypeWithAbstractedDateTypeAsync()
    {
        // Arrange
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        var dateOnlyModel = root.AddClass(new CodeClass
        {
            Name = "DateOnly",
            Kind = CodeClassKind.Model
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "DateOnly",
                    IsExternal = false,
                    TypeDefinition = dateOnlyModel
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("DateOnlyObject", requestBuilder.Indexer.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplacesIndexerLocallyDefinedTimeOnlyTypeWithAbstractedTimeTypeAsync()
    {
        // Arrange
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        var timeOnlyModel = root.AddClass(new CodeClass
        {
            Name = "TimeOnly",
            Kind = CodeClassKind.Model
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "TimeOnly",
                    IsExternal = false,
                    TypeDefinition = timeOnlyModel
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("TimeOnlyObject", requestBuilder.Indexer.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddsUsingForUntypedNodeAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, property.Type.Name);
        Assert.NotEmpty(model.StartBlock.Usings);
        var nodeUsing = model.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("Microsoft.Kiota.Abstractions.Serialization", nodeUsing[0].Declaration.Name);

    }
    [Theory]
    [InlineData(AccessModifier.Public)]
    [InlineData(AccessModifier.Internal)]
    public async Task SetTypeAccessModifierAsync(AccessModifier accessModifier)
    {
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "Class1",
            Kind = CodeClassKind.Model
        }).First();
        var codeEnum = root.AddEnum(new CodeEnum
        {
            Name = "Enum1",
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp, TypeAccessModifier = accessModifier }, root);
        Assert.Equal(codeClass.Access, accessModifier);
        Assert.Equal(codeEnum.Access, accessModifier);
    }
    #endregion
}
