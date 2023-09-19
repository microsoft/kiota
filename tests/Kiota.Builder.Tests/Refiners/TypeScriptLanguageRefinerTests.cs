﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;
public class TypeScriptLanguageRefinerTests
{
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    public TypeScriptLanguageRefinerTests()
    {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
    }

    #region commonrefiner
    [Fact]
    public async Task AddsQueryParameterMapperMethod()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty
        {
            Name = "Select",
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string"
            },
        });

        Assert.Empty(model.Methods);

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.QueryParametersMapper)));
    }
    [Fact]
    public async Task AddStaticMethodsUsingsForDeserializer()
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

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        var propertyFactoryMethod = subNs.FindChildByName<CodeFunction>("createPropertyModelFromDiscriminatorValue");
        var deserializerFunction = graphNS.FindChildByName<CodeFunction>("DeserializeIntoModel");

        Assert.NotNull(propertyFactoryMethod);
        Assert.Contains(deserializerFunction.Usings, x => x.Declaration.TypeDefinition == propertyFactoryMethod);
    }
    [Fact]
    public async Task AddsExceptionImplementsOnErrorClasses()
    {
        var apiErrorClassName = "ApiError";
        var model = TestHelper.CreateModelClass(root, "ErrorModel");
        model.IsErrorDefinition = true;
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = model.StartBlock;

        Assert.True(declaration.Usings.First(x => x.Name.EqualsIgnoreCase(apiErrorClassName)).IsErasable);
        Assert.Contains(apiErrorClassName, declaration.Usings.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
        Assert.Contains(apiErrorClassName, declaration.Implements.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task InlineParentOnErrorClassesWhichAlreadyInherit()
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        Assert.DoesNotContain("otherProp", model.Properties.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase); // we're not inlining since base error for TS is an interface
        Assert.DoesNotContain("otherMethod", model.Methods.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("otherNs", model.Usings.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

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

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.True(factoryMethod.Parent is CodeFunction);

        Assert.Contains((factoryMethod.Parent as CodeFunction).StartBlock.Usings, x => x.Declaration.Name.Equals("deserializeIntoChildModel", StringComparison.OrdinalIgnoreCase));
    }
    #endregion
    #region typescript
    private const string HttpCoreDefaultName = "IRequestAdapter";
    private const string FactoryDefaultName = "ISerializationWriterFactory";
    private const string DeserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
    private const string PathParametersDefaultName = "Dictionary<string, object>";
    private const string PathParametersDefaultValue = "new Dictionary<string, object>";
    private const string DateTimeOffsetDefaultName = "DateTimeOffset";
    private const string AdditionalDataDefaultName = "new Dictionary<string, object>()";
    [Fact]
    public async Task EscapesReservedKeywords()
    {
        var model = TestHelper.CreateModelClass(root, "break");
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var interFaceModel = codeFile.Interfaces.First(x => "BreakEscaped".Equals(x.Name, StringComparison.Ordinal));
        Assert.NotEqual("break", interFaceModel.Name);
        Assert.Contains("Escaped", interFaceModel.Name);
    }

    [Fact]
    public async Task CorrectsCoreType()
    {
        var model = TestHelper.CreateModelClass(root);

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
        }); ;
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
        var executorMethod = model.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
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

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());

        var interFaceModel = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        var deserializerFunction = codeFile.FindChildByName<CodeFunction>($"DeserializeInto{model.Name.ToFirstCharacterUpperCase()}");
        var serializationFunction = codeFile.FindChildByName<CodeFunction>($"Serialize{model.Name.ToFirstCharacterUpperCase()}");
        Assert.Empty(interFaceModel.Properties.Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => FactoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => AdditionalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => PathParametersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => PathParametersDefaultValue.Equals(x.DefaultValue)));
        Assert.Contains(deserializerFunction.OriginalLocalMethod.Parameters, x => interFaceModel.Name.Equals(x.Type.Name));
        Assert.Contains(serializationFunction.OriginalLocalMethod.Parameters, x => "SerializationWriter".Equals(x.Type.Name));

    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeType()
    {
        var model = TestHelper.CreateModelClass(root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "DateTimeOffset",
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("Date", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task ReplacesGuidsByRespectiveType()
    {
        var model = TestHelper.CreateModelClass(root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "Guid",
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var modelInterface = codeFile.Interfaces.First(x => x.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.NotEmpty(modelInterface.StartBlock.Usings.Where(static x => x.Name.Equals("Guid", StringComparison.Ordinal)));
        Assert.Equal("Guid", modelInterface.Properties.First(x => x.Name.Equals(codeProperty.Name, StringComparison.OrdinalIgnoreCase)).Type.Name, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeType()
    {

        var model = TestHelper.CreateModelClass(root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "DateOnly"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("DateOnly", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeType()
    {
        var model = TestHelper.CreateModelClass(root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "TimeOnly"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);

        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("TimeOnly", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);

    }
    [Fact]
    public async Task ReplacesDurationByNativeType()
    {

        var model = TestHelper.CreateModelClass(root);
        var codeProperty = model.AddProperty(new CodeProperty
        {
            Name = "method",
            Type = new CodeType
            {
                Name = "TimeSpan"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var modelInterface = codeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("Duration", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task AliasesDuplicateUsingSymbols()
    {
        var modelsNS = graphNS.AddNamespace($"{graphNS.Name}.models");
        var submodelsNS = modelsNS.AddNamespace($"{modelsNS.Name}.submodels");

        var model = TestHelper.CreateModelClass(graphNS);
        var source1 = TestHelper.CreateModelClass(modelsNS, "source");
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var modelCodeFile = graphNS.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        var modelInterface = modelCodeFile.Interfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());

        var source1CodeFile = modelsNS.FindChildByName<CodeFile>(source1.Name.ToFirstCharacterUpperCase());
        var source1Interface = source1CodeFile.Interfaces.First(x => x.Name == source1.Name.ToFirstCharacterUpperCase());

        var source2CodeFile = submodelsNS.FindChildByName<CodeFile>(source2.Name.ToFirstCharacterUpperCase());
        var source2Interface = source2CodeFile.Interfaces.First(x => x.Name == source2.Name.ToFirstCharacterUpperCase());

        var modelUsing1 = modelInterface.Usings.First(x => x.Declaration.TypeDefinition == source2Interface);
        var modelUsing2 = modelInterface.Usings.First(x => x.Declaration.TypeDefinition == source1Interface);
        Assert.Equal(modelUsing1.Declaration.Name, modelUsing2.Declaration.Name);
        Assert.NotEmpty(modelUsing1.Alias);
        Assert.NotEmpty(modelUsing2.Alias);
        Assert.NotEqual(modelUsing1.Alias, modelUsing2.Alias);
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
            Name = "cancelletionToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Documentation = new()
            {
                Description = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }

    [Fact]
    public async Task AddsModelInterfaceForAModelClass()
    {
        var testNS = CodeNamespace.InitRootNamespace();
        var model = TestHelper.CreateModelClass(testNS, "modelA");

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, testNS);

        var codeFile = testNS.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
        Assert.Contains(codeFile.Interfaces, x => x.Name == "ModelA");
    }

    [Fact]
    public async Task ReplaceRequestConfigsQueryParams()
    {
        var testNS = CodeNamespace.InitRootNamespace();
        var requestConfig = testNS.AddClass(new CodeClass
        {
            Name = "requestConfig",
            Kind = CodeClassKind.RequestConfiguration
        }).First();

        var queryParam = testNS.AddClass(new CodeClass
        {
            Name = "queryParams",
            Kind = CodeClassKind.QueryParameters
        }).First();

        requestConfig.AddProperty(new CodeProperty { Name = queryParam.Name, Type = new CodeType { Name = queryParam.Name, TypeDefinition = queryParam } });
        queryParam.AddProperty(new CodeProperty { Name = "stringProp", Type = new CodeType { Name = "string" } });

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, testNS);
        Assert.Contains(testNS.Interfaces, x => x.Name == "requestConfig");
        Assert.Contains(testNS.Interfaces, x => x.Name == "queryParams");
        Assert.False(testNS.Classes.Any());
        Assert.DoesNotContain(testNS.Classes, x => x.Name == "requestConfig");
        Assert.DoesNotContain(testNS.Classes, x => x.Name == "queryParams");
    }


    [Fact]
    public async Task GeneratesCodeFiles()
    {
        var model = TestHelper.CreateModelClass(root);

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
        }); ;
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
        var executorMethod = model.AddMethod(new CodeMethod
        {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
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

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var codeFile = root.FindChildByName<CodeFile>(model.Name.ToFirstCharacterUpperCase());
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
    #endregion
}
