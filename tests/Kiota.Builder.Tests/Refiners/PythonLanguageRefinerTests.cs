using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class PythonLanguageRefinerTests
{
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    public PythonLanguageRefinerTests()
    {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
        parentClass = new()
        {
            Name = "parentClass"
        };
        graphNS.AddClass(parentClass);
    }
    #region commonrefiner
    [Fact]
    public async Task AddsDefaultImportsAsync()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "someModel",
            Kind = CodeClassKind.Model
        }).First();

        Assert.Empty(model.Methods);
        var declaration = model.StartBlock;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Contains("annotations", declaration.Usings.Select(x => x.Name));
    }
    [Fact]
    public async Task AddsQueryParameterMapperMethodAsync()
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

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(model.Methods, x => x.IsOfKind(CodeMethodKind.QueryParametersMapper));
    }
    [Fact]
    public async Task AddsQueryParameterMapperMethodAfterManglingAsync()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty
        {
            Name = "ifExists",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.QueryParameter
        });

        Assert.Empty(model.Methods);

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(model.Properties, x => x.Name.Equals("if_exists"));
        Assert.Single(model.Properties, x => x.IsNameEscaped);
        Assert.Single(model.Methods, x => x.IsOfKind(CodeMethodKind.QueryParametersMapper));
    }

    [Fact]
    public async Task AddsQueryParameterDefaultValueToNonNullListsAsync()
    {
        var model = graphNS.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty
        {
            Name = "sortBy",
            Type = new CodeType
            {
                Name = "string",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                IsNullable = false
            },

            Kind = CodePropertyKind.QueryParameter
        });

        Assert.Empty(model.Methods);

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(model.Properties, x => x.Name.Equals("sort_by"));
        Assert.Single(model.Properties, x => x.IsNameEscaped);
        Assert.Single(model.Properties, x => x.DefaultValue.Equals("field(default_factory=list)"));
    }

    [Theory]
    [InlineData("None")]
    [InlineData("while")]
    public async Task EnumWithReservedName_IsRenamedAsync(string input)
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        var option = new CodeEnumOption { Name = input, SerializationName = input };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        Assert.Equal(input.ToFirstCharacterUpperCase() + "_", model.Options.First().Name);// we need to escape this in python
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        var declaration = model.StartBlock;

        Assert.Contains("APIError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("APIError", declaration.Inherits.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("other_prop"));
        Assert.Contains(model.Methods, x => x.Name.Equals("other_method"));
        Assert.Contains(model.Usings, x => x.Name.Equals("otherNs"));
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    #endregion
    #region python
    private const string HttpCoreDefaultName = "IRequestAdapter";
    private const string FactoryDefaultName = "ISerializationWriterFactory";
    private const string DeserializeDefaultName = "dict[str, Callable[[ParseNode], None]]";
    private const string PathParametersDefaultName = "Dictionary<string, object>";
    private const string PathParametersDefaultValue = "new Dictionary<string, object>";
    private const string DateTimeOffsetDefaultName = "DateTimeOffset";
    private const string AdditionalDataDefaultName = "Dictionary<string, object>";
    [Fact]
    public async Task EscapesReservedKeywordsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        var voidMethod = model.AddMethod(new CodeMethod
        {
            Name = "continue",// this is a keyword
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEqual("break", model.Name);
        Assert.EndsWith("_", voidMethod.Name);
    }
    [Fact]
    public async Task EscapesExceptionPropertiesNamesAsync()
    {
        var exception = root.AddClass(new CodeClass
        {
            Name = "Error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        exception.AddProperty(new CodeProperty
        {
            Name = "with_traceback",
            Type = new CodeType
            {
                Name = "boolean"
            }

        },
        new CodeProperty
        {
            Type = new CodeType
            {
                Name = "integer"
            },
            Name = "response_status_code",
        }
        ).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        var declaration = exception.StartBlock;

        Assert.Contains("APIError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("APIError", declaration.Inherits.Name);
        Assert.Contains("with_traceback_", exception.Properties.Select(x => x.Name));
        Assert.Contains("response_status_code_", exception.Properties.Select(x => x.Name));
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.True(property.Type is CodeType);
        Assert.True(parameter.Type is CodeType);
        Assert.True(method.ReturnType is CodeType);
        var resultingWrapper = root.FindChildByName<CodeClass>("union");
        Assert.NotNull(resultingWrapper);
        Assert.NotNull(resultingWrapper.OriginalComposedType);
        Assert.Contains("ComposedTypeWrapper", resultingWrapper.StartBlock.Implements.Select(static x => x.Name));
        Assert.Null(resultingWrapper.Methods.SingleOrDefault(static x => x.IsOfKind(CodeMethodKind.ComposedTypeMarker)));
    }
    [Fact]
    public async Task CorrectsCoreTypeAsync()
    {

        var model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
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
            Name = "handler",
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.DoesNotContain(model.Properties, x => HttpCoreDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, x => FactoryDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, x => DateTimeOffsetDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, x => AdditionalDataDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, x => PathParametersDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, x => PathParametersDefaultValue.Equals(x.DefaultValue));
        Assert.DoesNotContain(model.Methods, x => DeserializeDefaultName.Equals(x.ReturnType.Name));
        Assert.DoesNotContain(model.Methods.SelectMany(x => x.Parameters), x => serializerDefaultName.Equals(x.Type.Name));
        Assert.Single(constructorMethod.Parameters, x => x.Type is CodeTypeBase);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.datetime", method.ReturnType.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.date", method.ReturnType.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.time", method.ReturnType.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("datetime.timedelta", method.ReturnType.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task AddsPropertiesAndMethodTypesImportsPythonAsync()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var model = root.AddClass(new CodeClass
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
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();

        Assert.Empty(model.Methods);
        var declaration = model.StartBlock;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, graphNS);
        Assert.Single(requestBuilder.Methods, x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.DoesNotContain("QueryParameters", declaration.Usings.Select(x => x.Name));
    }
    [Fact]
    public async Task ReplacesUntypedNodeInMethodParameterAndReturnTypeAsync()
    {
        var requestBuilderClass = root.AddClass(new CodeClass() { Name = "NodeRequestBuilder" }).First();
        var method = new CodeMethod
        {
            Name = "getAsync",
            ReturnType = new CodeType()
            {
                Name = KiotaBuilder.UntypedNodeName,//Returns untyped node
                IsExternal = true
            },
            Kind = CodeMethodKind.RequestExecutor
        };
        method.AddParameter(new CodeParameter()
        {
            Name = "jsonData",
            Type = new CodeType()
            {
                Name = KiotaBuilder.UntypedNodeName, //Has untyped node parameter
                IsExternal = true
            },
            Kind = CodeParameterKind.RequestBody
        });
        requestBuilderClass.AddMethod(method);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);
        Assert.Equal("bytes", method.Parameters.First().Type.Name);// type is renamed to use the stream type
        Assert.Equal("bytes", method.ReturnType.Name);// return type is renamed to use the stream type
    }

    [Fact]
    public async Task AddsConstructorsForErrorClasses()
    {
        // Given
        var errorClass = root.AddClass(new CodeClass
        {
            Name = "Error401",
            IsErrorDefinition = true
        }).First();

        // When
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        // Then
        var parameterlessConstructor = errorClass.Methods
            .FirstOrDefault(m => m.IsOfKind(CodeMethodKind.Constructor) && !m.Parameters.Any());

        Assert.NotNull(parameterlessConstructor);
        Assert.Equal("__init__", parameterlessConstructor.Name);
        Assert.Equal(AccessModifier.Public, parameterlessConstructor.Access);

        var messageConstructor = errorClass.Methods
            .FirstOrDefault(m => m.IsOfKind(CodeMethodKind.Constructor) &&
                                m.Parameters.Any(p => p.Type.Name.Equals("str", StringComparison.OrdinalIgnoreCase) && p.Name.Equals("message", StringComparison.OrdinalIgnoreCase)));

        Assert.NotNull(messageConstructor);
        Assert.Single(messageConstructor.Parameters);
        Assert.Equal("message", messageConstructor.Parameters.First().Name);
        Assert.Equal("str", messageConstructor.Parameters.First().Type.Name);
        Assert.True(messageConstructor.Parameters.First().Optional);
        Assert.Equal("None", messageConstructor.Parameters.First().DefaultValue);
    }

    [Fact]
    public async Task DoesNotAddConstructorsToNonErrorClasses()
    {
        // Given
        var regularClass = root.AddClass(new CodeClass
        {
            Name = "RegularModel",
            IsErrorDefinition = false
        }).First();

        // When
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        // Then
        var messageConstructor = regularClass.Methods
            .FirstOrDefault(m => m.IsOfKind(CodeMethodKind.Constructor) &&
                                m.Parameters.Any(p => p.Type.Name.Equals("str", StringComparison.OrdinalIgnoreCase) && p.Name.Equals("message", StringComparison.OrdinalIgnoreCase)));

        Assert.Null(messageConstructor);
    }

    [Fact]
    public async Task AddsMessageFactoryMethodToErrorClasses()
    {
        // Given
        var errorClass = root.AddClass(new CodeClass
        {
            Name = "Error401",
            IsErrorDefinition = true
        }).First();

        // When
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        // Then
        var messageFactoryMethod = errorClass.Methods
            .FirstOrDefault(m => m.IsOfKind(CodeMethodKind.FactoryWithErrorMessage) &&
                                m.Name.Equals("create_from_discriminator_value_with_message", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(messageFactoryMethod);
        Assert.Equal(2, messageFactoryMethod.Parameters.Count());

        var parseNodeParam = messageFactoryMethod.Parameters.FirstOrDefault(p => p.Name.Equals("parse_node", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(parseNodeParam);
        Assert.Equal("ParseNode", parseNodeParam.Type.Name);

        var messageParam = messageFactoryMethod.Parameters.FirstOrDefault(p => p.Name.Equals("message", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(messageParam);
        Assert.Equal("str", messageParam.Type.Name);
        Assert.True(messageParam.Optional);
        Assert.Equal("None", messageParam.DefaultValue);

        Assert.True(messageFactoryMethod.IsStatic);
        Assert.Equal(AccessModifier.Public, messageFactoryMethod.Access);
    }

    [Fact]
    public async Task DoesNotDuplicateExistingConstructors()
    {
        // Given
        var errorClass = root.AddClass(new CodeClass
        {
            Name = "Error401",
            IsErrorDefinition = true
        }).First();

        // Add an existing message constructor
        var existingConstructor = new CodeMethod
        {
            Name = "__init__",
            Kind = CodeMethodKind.Constructor,
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = "None", IsExternal = true }
        };
        existingConstructor.AddParameter(new CodeParameter
        {
            Name = "message",
            Type = new CodeType { Name = "str", IsExternal = true }
        });
        errorClass.AddMethod(existingConstructor);

        var initialConstructorCount = errorClass.Methods.Count(m => m.IsOfKind(CodeMethodKind.Constructor));

        // When
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root);

        // Then - should add only the parameterless constructor, not duplicate the message one
        var finalConstructorCount = errorClass.Methods.Count(m => m.IsOfKind(CodeMethodKind.Constructor));
        Assert.Equal(initialConstructorCount + 1, finalConstructorCount); // Only parameterless constructor added
    }
    #endregion
}
