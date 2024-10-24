using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Tests.OpenApiSampleFiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public sealed class TypeScriptLanguageRefinerTests : IDisposable
{
    private readonly HttpClient _httpClient = new();

    private readonly List<string> _tempFiles = new();
    public void Dispose()
    {
        foreach (var file in _tempFiles)
            File.Delete(file);
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    public TypeScriptLanguageRefinerTests()
    {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
    }

    #region commonrefiner
    [Fact]
    public async Task AddStaticMethodsUsingsForDeserializerAsync()
    {
        var model = TestHelper.CreateModelClass(graphNS, "Model");

        var subNs = graphNS.AddNamespace($"{graphNS.Name}.subns");

        var propertyModel = TestHelper.CreateModelClass(subNs, "PropertyModel");

        model.AddMethod(new CodeMethod
        {
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "void",
                TypeDefinition = model,
            },
        });

        propertyModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "void",
                TypeDefinition = propertyModel,
            },
        });

        model.AddProperty(new CodeProperty
        {
            Name = "someProperty",
            Type = new CodeType
            {
                Name = "somepropertyModel",
                TypeDefinition = propertyModel,
            },
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        var propertyFactoryMethod = subNs.FindChildByName<CodeFunction>("createPropertyModelFromDiscriminatorValue");
        var deserializerFunction = graphNS.FindChildByName<CodeFunction>("DeserializeIntoModel");

        Assert.NotNull(propertyFactoryMethod);
        Assert.NotNull(deserializerFunction);
        Assert.Contains(deserializerFunction.Usings, x => x.Declaration?.TypeDefinition == propertyFactoryMethod);
    }
    [Fact]
    public async Task AddsExceptionImplementsOnErrorClassesAsync()
    {
        var apiErrorClassName = "ApiError";
        var model = TestHelper.CreateModelClass(root, "ErrorModel");
        model.IsErrorDefinition = true;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = model.StartBlock;

        Assert.True(declaration.Usings.First(x => x.Name.EqualsIgnoreCase(apiErrorClassName)).IsErasable);
        Assert.Contains(apiErrorClassName, declaration.Usings.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
        Assert.Contains(apiErrorClassName, declaration.Implements.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
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

        model.AddMethod(
            new CodeMethod
            {
                Name = "serializer",
                Kind = CodeMethodKind.Serializer,
                ReturnType = new CodeType
                {
                    Name = "void"
                }
            },
            new CodeMethod
            {
                Name = "deserializer",
                Kind = CodeMethodKind.Deserializer,
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            });
        otherModel.AddMethod(
            new CodeMethod
            {
                Name = "serializer",
                Kind = CodeMethodKind.Serializer,
                ReturnType = new CodeType
                {
                    Name = "void"
                }
            },
            new CodeMethod
            {
                Name = "deserializer",
                Kind = CodeMethodKind.Deserializer,
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            },
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
        model.StartBlock.Inherits = new CodeType
        {
            TypeDefinition = otherModel
        };
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        Assert.DoesNotContain("otherProp", model.Properties.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase); // we're not inlining since base error for TS is an interface
        Assert.DoesNotContain("otherMethod", model.Methods.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("otherNs", model.Usings.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
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
        var errorClass = TestHelper.CreateModelClass(subNS, "Error4XX");
        errorClass.IsErrorDefinition = true;
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var childModel = TestHelper.CreateModelClass(root, "childModel");
        var parentModel = TestHelper.CreateModelClass(root, "parentModel");

        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        if (factoryMethod.Parent is not CodeFunction parentCodeFunction) throw new InvalidOperationException("Parent is not a CodeFunction");

        Assert.Contains(parentCodeFunction.StartBlock.Usings, x => "deserializeIntoChildModel".Equals(x.Declaration?.Name, StringComparison.OrdinalIgnoreCase));
    }
    #endregion
    #region typescript
    private const string HttpCoreDefaultName = "IRequestAdapter";
    private const string FactoryDefaultName = "ISerializationWriterFactory";
    private const string PathParametersDefaultName = "Dictionary<string, object>";
    private const string PathParametersDefaultValue = "new Dictionary<string, object>";
    private const string DateTimeOffsetDefaultName = "DateTimeOffset";
    private const string AdditionalDataDefaultName = "new Dictionary<string, object>()";
    [Fact]
    public async Task EscapesReservedKeywordsAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "break");
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        var interFaceModel = codeFile.Interfaces.First(x => "BreakEscaped".Equals(x.Name, StringComparison.Ordinal));
        Assert.NotEqual("break", interFaceModel.Name);
        Assert.Contains("Escaped", interFaceModel.Name);
    }

    [Fact]
    public async Task CorrectsCoreTypeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);

        model.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "void",
                TypeDefinition = model
            },
        });
        model.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = HttpCoreDefaultName
            }
        }, new()
        {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = DateTimeOffsetDefaultName,
            }
        }, new()
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = AdditionalDataDefaultName
            }
        }, new()
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = PathParametersDefaultName
            },
            DefaultValue = PathParametersDefaultValue
        });
        model.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
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
            Name = serializerDefaultName,
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = serializerDefaultName,
            }
        });
        var constructorMethod = model.AddMethod(new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        constructorMethod.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = PathParametersDefaultName
            },
        });

        await ILanguageRefiner.RefineAsync(generationConfiguration, root);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);

        var interFaceModel = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotNull(interFaceModel);
        var deserializerFunction = codeFile.FindChildByName<CodeFunction>($"DeserializeInto{model.Name.ToFirstCharacterUpperCase()}");
        var serializationFunction = codeFile.FindChildByName<CodeFunction>($"Serialize{model.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(deserializerFunction);
        Assert.NotNull(serializationFunction);
        Assert.DoesNotContain(interFaceModel.Properties, x => HttpCoreDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(interFaceModel.Properties, x => FactoryDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(interFaceModel.Properties, x => DateTimeOffsetDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(interFaceModel.Properties, x => AdditionalDataDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(interFaceModel.Properties, x => PathParametersDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(interFaceModel.Properties, x => PathParametersDefaultValue.Equals(x.DefaultValue));
        Assert.Contains(deserializerFunction.OriginalLocalMethod.Parameters, x => interFaceModel.Name.Equals(x.Type.Name));
        Assert.Contains(serializationFunction.OriginalLocalMethod.Parameters, x => "SerializationWriter".Equals(x.Type.Name));

    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeTypeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "DateTimeOffset",
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("Date", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task ReplacesGuidsByRespectiveTypeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "Guid",
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        var modelInterface = codeFile.Interfaces.First(x => x.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Contains(modelInterface.StartBlock.Usings, static x => x.Name.Equals("Guid", StringComparison.Ordinal));
        Assert.Equal("Guid", modelInterface.Properties.First(x => x.Name.Equals(codeProperty.Name, StringComparison.OrdinalIgnoreCase)).Type.Name, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeTypeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "DateOnly"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("DateOnly", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeTypeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "TimeOnly"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        Assert.NotEmpty(model.StartBlock.Usings);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("TimeOnly", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);

    }
    private const string IndexFileName = "index";
    [Fact]
    public async Task ReplacesDurationByNativeTypeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "TimeSpan"
            },
        }).First();
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("Duration", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task AliasesDuplicateUsingSymbolsAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);

        var source1 = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "source");

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        Assert.NotNull(modelsNS);
        var submodelsNS = modelsNS.AddNamespace($"{generationConfiguration.ModelsNamespaceName}.submodels");
        var source2 = TestHelper.CreateModelClass(submodelsNS, "source");

        source1.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "void",
                TypeDefinition = source2,
            },
        });

        var using1 = new CodeUsing
        {
            Name = modelsNS.Name,
            Declaration = new CodeType
            {
                Name = source1.Name,
                TypeDefinition = source1,
                IsExternal = false,
            }
        };
        source2.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "source",
                TypeDefinition = source2
            },
        });
        var using2 = new CodeUsing
        {
            Name = submodelsNS.Name,
            Declaration = new CodeType
            {
                Name = source2.Name,
                TypeDefinition = source2,
                IsExternal = false,
            }
        };
        model.AddUsing(using1);
        model.AddProperty(
            new CodeProperty
            {
                Name = "source1",
                Type = new CodeType
                {
                    TypeDefinition = source1,
                }
            });
        model.AddProperty(
            new CodeProperty
            {
                Name = "source2",
                Type = new CodeType
                {
                    TypeDefinition = source2,
                }
            });
        model.AddUsing(using2);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var modelCodeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(modelCodeFile);
        var modelInterface = modelCodeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        var source1Interface = modelCodeFile.Interfaces.First(x => x.Name == source1.Name.ToFirstCharacterUpperCase());
        var source2Interface = modelCodeFile.Interfaces.First(x => x.Name == source2.Name.ToFirstCharacterUpperCase());

        Assert.DoesNotContain(modelInterface.Usings, x => x.Declaration?.TypeDefinition == source2Interface);
        Assert.DoesNotContain(modelInterface.Usings, x => x.Declaration?.TypeDefinition == source1Interface);
    }

    [Fact]
    public async Task AliasesDuplicateUsingSymbolsAsyncWithAliasedReferencesToParentNamespaceAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var source1 = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "source");
        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        Assert.NotNull(modelsNS);
        var submodelsNS = modelsNS.AddNamespace($"{generationConfiguration.ModelsNamespaceName}.submodels");
        var source2 = TestHelper.CreateModelClass(submodelsNS, "source");
        var model = TestHelper.CreateModelClass(submodelsNS, "model");

        var source1Factory = new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType { Name = "source", TypeDefinition = source2, },
        };
        source1.AddMethod(source1Factory);

        var using1 = new CodeUsing
        {
            Name = modelsNS.Name,
            Declaration = new CodeType
            {
                Name = source1.Name,
                TypeDefinition = source1,
                IsExternal = false,
            }
        };

        var source2Factory = new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType { Name = "source", TypeDefinition = source2 },
        };

        source2.AddMethod(source2Factory);
        var using2 = new CodeUsing
        {
            Name = submodelsNS.Name,
            Declaration = new CodeType
            {
                Name = source2.Name,
                TypeDefinition = source2,
                IsExternal = false,
            }
        };
        model.AddUsing(using1);
        var property1 = new CodeProperty { Name = "source1", Type = new CodeType { TypeDefinition = source1, } };
        model.AddProperty(property1);
        var property2 = new CodeProperty { Name = "source2", Type = new CodeType { TypeDefinition = source2, } };
        model.AddProperty(property2);
        model.AddUsing(using2);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var sourceCodeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(sourceCodeFile);
        var source1Interface = sourceCodeFile.Interfaces.First(x => x.Name == source1.Name.ToFirstCharacterUpperCase());

        var modelCodeFile = submodelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(modelCodeFile);
        var source2Interface = modelCodeFile.Interfaces.First(x => x.Name == source1.Name.ToFirstCharacterUpperCase());

        var result1 = Kiota.Builder.Writers.TypeScript.TypeScriptConventionService.GetFactoryMethod(source1Interface, "createSourceFromDiscriminatorValue");
        Assert.NotNull(result1);
        var result2 = Kiota.Builder.Writers.TypeScript.TypeScriptConventionService.GetFactoryMethod(source2Interface, "createSourceFromDiscriminatorValue");
        Assert.NotNull(result2);
        Assert.NotEqual(result1, result2);// they should be different discriminators despite having the same name
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }

    [Fact]
    public async Task AddsModelInterfaceForAModelClassAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "modelA");

        await ILanguageRefiner.RefineAsync(generationConfiguration, root);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        Assert.Contains(codeFile.Interfaces, static x => "ModelA".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddsModelInterfaceForAModelClassWithoutCollisionAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "hostModel");
        TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "hostModelInterface");// a second model with the `Interface` suffix

        await ILanguageRefiner.RefineAsync(generationConfiguration, root);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile);
        Assert.Equal(2, codeFile.Interfaces.Count());
        Assert.Contains(codeFile.Interfaces, static x => "hostModel".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(codeFile.Interfaces, static x => "hostModelInterface".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReplaceRequestConfigsQueryParamsAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var testNS = root.FindOrAddNamespace(generationConfiguration.ClientNamespaceName);
        var requestBuilder = testNS.AddClass(new CodeClass
        {
            Name = "requestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        requestBuilder.AddProperty(new CodeProperty
        {
            Kind = CodePropertyKind.UrlTemplate,
            Name = "urlTemplate",
            DefaultValue = "{baseurl+}",
            Type = new CodeType
            {
                Name = "string"
            }
        });

        var requestConfig = requestBuilder.AddInnerClass(new CodeClass
        {
            Name = "requestConfig",
            Kind = CodeClassKind.RequestConfiguration
        }).First();

        var queryParam = requestBuilder.AddInnerClass(new CodeClass
        {
            Name = "queryParams",
            Kind = CodeClassKind.QueryParameters
        }).First();

        queryParam.AddProperty(new CodeProperty
        {
            Name = "Select",
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string"
            },
        });

        requestConfig.AddProperty(new CodeProperty { Name = queryParam.Name, Type = new CodeType { Name = queryParam.Name, TypeDefinition = queryParam } });
        queryParam.AddProperty(new CodeProperty { Name = "stringProp", Type = new CodeType { Name = "string" } });

        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        Assert.DoesNotContain(testNS.Interfaces, static x => x.Name.Equals("requestConfig", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(testNS.Interfaces, static x => x.Name.Equals("queryParams", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(testNS.Classes);
        Assert.NotEmpty(testNS.Files);
        Assert.Empty(requestBuilder.InnerClasses);
        Assert.DoesNotContain(testNS.Classes, static x => x.Name.Equals("requestConfig", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(testNS.Classes, static x => x.Name.Equals("queryParams", StringComparison.OrdinalIgnoreCase));
        Assert.Single(testNS.Constants, static x => x.IsOfKind(CodeConstantKind.QueryParametersMapper));
    }

    [Fact]
    public async Task GeneratesCodeFilesAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);

        model.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType
            {
                Name = "void",
                TypeDefinition = model
            },
        });
        model.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = HttpCoreDefaultName
            }
        }, new()
        {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = DateTimeOffsetDefaultName,
            }
        }, new()
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = AdditionalDataDefaultName
            }
        }, new()
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = PathParametersDefaultName
            },
            DefaultValue = PathParametersDefaultValue
        });
        model.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
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
            Name = serializerDefaultName,
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = serializerDefaultName,
            }
        });
        var constructorMethod = model.AddMethod(new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        constructorMethod.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = PathParametersDefaultName
            },
        });

        await ILanguageRefiner.RefineAsync(generationConfiguration, root);

        var modelsNS = root.FindNamespaceByName(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>(IndexFileName, false);
        Assert.NotNull(codeFile); // codefile exists

        // model , interface, deserializer, serializer should be direct descendant of the codefile
        Assert.NotNull(codeFile.FindChildByName<CodeFunction>($"DeserializeInto{model.Name.ToFirstCharacterUpperCase()}", false));
        Assert.NotNull(codeFile.FindChildByName<CodeFunction>($"Serialize{model.Name.ToFirstCharacterUpperCase()}", false));
        Assert.NotNull(codeFile.FindChildByName<CodeFunction>($"create{model.Name.ToFirstCharacterUpperCase()}FromDiscriminatorValue", false));
        Assert.NotNull(codeFile.FindChildByName<CodeInterface>($"{model.Name.ToFirstCharacterUpperCase()}", false));

        // model , interface, deserializer, serializer should be a direct descendant of the namespace
        Assert.Null(root.FindChildByName<CodeFunction>($"DeserializeInto{model.Name.ToFirstCharacterUpperCase()}", false));
        Assert.Null(root.FindChildByName<CodeFunction>($"Serialize{model.Name.ToFirstCharacterUpperCase()}", false));
        Assert.Null(root.FindChildByName<CodeFunction>($"create{model.Name.ToFirstCharacterUpperCase()}FromDiscriminatorValue", false));
        Assert.Null(root.FindChildByName<CodeInterface>($"{model.Name.ToFirstCharacterUpperCase()}", false));

    }
    [Fact]
    public async Task AddsUsingForUntypedNodeAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var model = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root);
        var property = model.AddProperty(new CodeProperty
        {
            Name = "property",
            Type = new CodeType
            {
                Name = KiotaBuilder.UntypedNodeName,
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, property.Type.Name);// type is renamed
        Assert.NotEmpty(model.StartBlock.Usings);
        var nodeUsing = model.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("@microsoft/kiota-abstractions", nodeUsing[0].Declaration.Name);
    }
    [Fact]
    public async Task ParsesAndRefinesUnionOfPrimitiveValuesAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, UnionOfPrimitiveValuesSample.Yaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Primitives", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Primitives", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.primitives");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("primitivesRequestBuilder", false);
        Assert.NotNull(modelCodeFile);
        var unionType = modelCodeFile.GetChildElements().Where(x => x is CodeFunction function && TypeScriptRefiner.GetOriginalComposedType(function.OriginalLocalMethod.ReturnType) is not null).ToList();
        Assert.True(unionType.Count > 0);
    }
    [Fact]
    public async Task ParsesAndRefinesPathsWithTrailingSlashAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllTextAsync(tempFilePath, TrailingSlashSampleYml.OpenApiYaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Testing", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);

        var fooNS = rootNS.FindNamespaceByName("ApiSdk.foo");
        Assert.NotNull(fooNS);
        var fooCodeFile = fooNS.FindChildByName<CodeFile>("fooRequestBuilder", false);
        Assert.NotNull(fooCodeFile);
        var fooRequestBuilder = fooCodeFile.FindChildByName<CodeInterface>("fooRequestBuilder", false);
        var fooSlashRequestBuilder = fooCodeFile.FindChildByName<CodeInterface>("fooSlashRequestBuilder", false);
        Assert.NotNull(fooRequestBuilder);
        Assert.NotNull(fooSlashRequestBuilder);

        var messageNS = rootNS.FindNamespaceByName("ApiSdk.message");
        Assert.NotNull(messageNS);
        var messageCodeFile = messageNS.FindChildByName<CodeFile>("messageRequestBuilder", false);
        Assert.NotNull(messageCodeFile);
        var messageRequestBuilder = messageCodeFile.FindChildByName<CodeInterface>("messageRequestBuilder", false);
        Assert.NotNull(messageRequestBuilder);
        var messageWithIdSlashMethod = messageRequestBuilder.FindChildByName<CodeMethod>("withIdSlash", false);
        var messageByIdMethod = messageRequestBuilder.FindChildByName<CodeMethod>("byId", false);
        Assert.NotNull(messageWithIdSlashMethod);
        Assert.NotNull(messageByIdMethod);

        var bucketNS = rootNS.FindNamespaceByName("ApiSdk.bucket");
        Assert.NotNull(bucketNS);
        var bucketItemNS = bucketNS.FindChildByName<CodeNamespace>("ApiSdk.bucket.item", false);
        Assert.NotNull(bucketItemNS);
        var bucketItemCodeFile = bucketItemNS.FindChildByName<CodeFile>("WithNameItemRequestBuilder", false);
        Assert.NotNull(bucketItemCodeFile);
        var bucketWithNameItemRequestBuilder = bucketItemCodeFile.FindChildByName<CodeInterface>("WithNameItemRequestBuilder", false);
        var bucketWithNameSlashRequestBuilder = bucketItemCodeFile.FindChildByName<CodeInterface>("WithNameSlashRequestBuilder", false);
        Assert.NotNull(bucketWithNameItemRequestBuilder);
        Assert.NotNull(bucketWithNameSlashRequestBuilder);
    }

    [Fact]
    public void GetOriginalComposedType_ReturnsNull_WhenElementIsNull()
    {
        var codeElement = new Mock<CodeElement>();
        var result = TypeScriptRefiner.GetOriginalComposedType(codeElement.Object);
        Assert.Null(result);
    }

    [Fact]
    public void GetOriginalComposedType_ReturnsComposedType_WhenElementIsComposedType()
    {
        var composedType = new Mock<CodeComposedTypeBase>();
        var result = TypeScriptRefiner.GetOriginalComposedType(composedType.Object);
        Assert.Equal(composedType.Object, result);
    }

    [Fact]
    public void GetOriginalComposedType_ReturnsComposedType_WhenElementIsParameter()
    {
        var composedType = new Mock<CodeComposedTypeBase>();

        var codeClass = new CodeClass
        {
            OriginalComposedType = composedType.Object
        };

        var codeType = new CodeType()
        {
            TypeDefinition = codeClass,
        };

        var parameter = new CodeParameter() { Type = codeType };

        var result = TypeScriptRefiner.GetOriginalComposedType(parameter);
        Assert.Equal(composedType.Object, result);
    }

    [Fact]
    public void GetOriginalComposedType_ReturnsComposedType_WhenElementIsCodeType()
    {
        var composedType = new Mock<CodeComposedTypeBase>();

        var codeClass = new CodeClass
        {
            OriginalComposedType = composedType.Object
        };

        var codeType = new CodeType()
        {
            TypeDefinition = codeClass,
        };

        var result = TypeScriptRefiner.GetOriginalComposedType(codeType);
        Assert.Equal(composedType.Object, result);
    }

    [Fact]
    public void GetOriginalComposedType_ReturnsComposedType_WhenElementIsCodeClass()
    {
        var composedType = new Mock<CodeComposedTypeBase>();

        CodeElement codeClass = new CodeClass
        {
            OriginalComposedType = composedType.Object
        };

        var result = TypeScriptRefiner.GetOriginalComposedType(codeClass);
        Assert.Equal(composedType.Object, result);
    }

    [Fact]
    public void GetOriginalComposedType_ReturnsComposedType_WhenElementIsCodeInterface()
    {
        var composedType = new Mock<CodeComposedTypeBase>();

        var codeClass = new CodeClass
        {
            OriginalComposedType = composedType.Object
        };

        CodeElement codeInterface = new CodeInterface()
        {
            OriginalClass = codeClass,
        };

        var result = TypeScriptRefiner.GetOriginalComposedType(codeInterface);
        Assert.Equal(composedType.Object, result);
    }
    [Fact]
    public async Task AddsUsingForUntypedNodeInReturnTypeAsync()
    {
        var requestBuilderClass = root.AddClass(new CodeClass() { Name = "NodeRequestBuilder" }).First();
        var model = new CodeMethod
        {
            Name = "getAsync",
            ReturnType = new CodeType
            {
                Name = KiotaBuilder.UntypedNodeName
            }
        };
        requestBuilderClass.AddMethod(model);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, model.ReturnType.Name);// type is renamed
        Assert.NotEmpty(requestBuilderClass.StartBlock.Usings);
        var nodeUsing = requestBuilderClass.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("@microsoft/kiota-abstractions", nodeUsing[0].Declaration.Name);
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
            }
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.Equal(KiotaBuilder.UntypedNodeName, method.Parameters.First().Type.Name);// type is renamed
        Assert.NotEmpty(requestBuilderClass.StartBlock.Usings);
        var nodeUsing = requestBuilderClass.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("@microsoft/kiota-abstractions", nodeUsing[0].Declaration.Name);
    }
    #endregion
}
