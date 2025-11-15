using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class GoLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLangRefinerTests
    [Fact]
    public async Task AddsInnerClassesAsync()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true
            }
        }).First();
        var customTypeDefinition = requestBuilder.AddInnerClass(new CodeClass
        {
            Name = "SomeCustomType"
        }).First();
        var parameter = new CodeParameter
        {
            Name = "param1",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "SomeCustomType",
                ActionOf = true,
                TypeDefinition = customTypeDefinition
            }
        };
        method.AddParameter(parameter);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(2, requestBuilder.GetChildElements(true).Count());
    }


    [Theory]
    [InlineData("break")]
    [InlineData("case")]
    public async Task EnumWithReservedName_IsNotRenamedAsync(string input)
    {
        var model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        var option = new CodeEnumOption { Name = input, SerializationName = input };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Equal(input, model.Options.First().Name);
    }
    [Fact]
    public async Task TrimsCircularDiscriminatorReferencesAsync()
    {
        var modelsNS = root.AddNamespace("ApiSdk.models");
        var baseModel = modelsNS.AddClass(new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "BaseModel",
        }).First();
        baseModel.AddProperty(new CodeProperty
        {
            Name = "Discriminator",
            Type = new CodeType { Name = "string" },
        });
        var subNamespace = modelsNS.AddNamespace($"{modelsNS.Name}.sub");
        var derivedModel = subNamespace.AddClass(new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "DerivedModel",
        }).First();
        derivedModel.StartBlock.Inherits = new CodeType
        {
            Name = baseModel.Name,
            TypeDefinition = baseModel,
        };
        var factoryMethod = baseModel.AddMethod(new CodeMethod
        {
            Kind = CodeMethodKind.Factory,
            Name = "factory",
            ReturnType = new CodeType
            {
                Name = baseModel.Name,
                TypeDefinition = baseModel,
            },
        }).First();
        baseModel.DiscriminatorInformation.DiscriminatorPropertyName = "Discriminator";
        baseModel.DiscriminatorInformation.AddDiscriminatorMapping("DerivedModel", new CodeType { Name = derivedModel.Name, TypeDefinition = derivedModel });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(baseModel.DiscriminatorInformation.DiscriminatorMappings);
        Assert.DoesNotContain(baseModel.Usings, x => x.Name.Equals("models.sub", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddCodeFileToHierarchyAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go, UsesBackingStore = true }, root);
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());

        var codeFile = root.GetChildElements(true).OfType<CodeFile>().First();

        Assert.Single(root.GetChildElements(true).OfType<CodeFile>());

        Assert.Single(codeFile.GetChildElements(true).OfType<CodeInterface>());
        Assert.Single(codeFile.GetChildElements(true).OfType<CodeClass>());
    }

    [Fact]
    public async Task TestBackingStoreTypesUseInterfacesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();

        var modelB = root.AddClass(new CodeClass
        {
            Name = "somemodelB",
            Kind = CodeClassKind.Model,
        }).First();

        var property = model.AddProperty(new CodeProperty
        {
            Name = "Getter",
            Type = new CodeType { Name = "somemodelB", TypeDefinition = modelB },
            Access = AccessModifier.Public,
            Kind = CodePropertyKind.Custom,
        }).First();

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go, UsesBackingStore = true }, root);
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());

        var codeFile = root.GetChildElements(true).OfType<CodeFile>().First();

        Assert.Equal(2, root.GetChildElements(true).OfType<CodeFile>().Count());

        Assert.Single(codeFile.GetChildElements(true).OfType<CodeInterface>());
        Assert.Single(codeFile.GetChildElements(true).OfType<CodeClass>());
        Assert.Equal("somemodelBable", property.Type.Name);
    }
    [Fact]
    public async Task ReplacesModelsByInterfacesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        var executorMethod = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "Execute",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = model.Name,
                TypeDefinition = model,
            },
        }).First();
        var executorParameter = new CodeParameter
        {
            Name = "requestBody",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType
            {
                Name = model.Name,
                TypeDefinition = model,
            },
        };
        executorMethod.AddParameter(executorParameter);
        var property = model.AddProperty(new CodeProperty
        {
            Name = "someProp",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = model.Name,
                TypeDefinition = model,
            },
        }).First();
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        var codeFile = root.GetChildElements(true).OfType<CodeFile>().First();
        Assert.Single(codeFile.GetChildElements(true).OfType<CodeInterface>());
        var inter = codeFile.GetChildElements(true).OfType<CodeInterface>().First();

        Assert.NotEqual(model.Name, inter.Name);
        var propertyType = property.Type as CodeType;
        Assert.NotNull(propertyType);
        Assert.Equal(inter, propertyType.TypeDefinition);
        var executorParameterType = executorParameter.Type as CodeType;
        Assert.NotNull(executorParameterType);
        Assert.Equal(inter, executorParameterType.TypeDefinition);
        var executorMethodReturnType = executorMethod.ReturnType as CodeType;
        Assert.NotNull(executorMethodReturnType);
        Assert.Equal(inter, executorMethodReturnType.TypeDefinition);
    }
    [Fact]
    public async Task EnsuresMethodNamesAreNotOverLoadedAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "additional_data",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = model.Name, TypeDefinition = model, },
        }, new CodeProperty
        {
            Name = "property_a",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = model.Name, TypeDefinition = model, },
        }, new CodeProperty
        {
            Name = "propertyA",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = model.Name, TypeDefinition = model, },
        });

        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Equal("SetProperty_a", model.FindChildByName<CodeMethod>("setProperty_a").Name);
        Assert.Equal("SetPropertyA", model.FindChildByName<CodeMethod>("setPropertyA").Name);
    }
    [Fact]
    public async Task ReplacesModelsByInnerInterfacesAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }).First();
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var responseModel = requestBuilder.AddInnerClass(new CodeClass
        {
            Name = "someresponsemodel",
            Kind = CodeClassKind.Model,
        }).First();


        var executorMethod = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "Execute",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = responseModel.Name,
                TypeDefinition = responseModel,
            },
        }).First();
        var executorParameter = new CodeParameter
        {
            Name = "requestBody",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType
            {
                Name = model.Name,
                TypeDefinition = model,
            },
        };
        executorMethod.AddParameter(executorParameter);
        Assert.Empty(root.GetChildElements(true).OfType<CodeInterface>());
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        var codeFile = root.GetChildElements(true).OfType<CodeFile>().First();
        Assert.Single(codeFile.GetChildElements(true).OfType<CodeInterface>());
        var responseInter = requestBuilder.GetChildElements(true).OfType<CodeInterface>().LastOrDefault();
        Assert.NotNull(responseInter);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.True(property.Type is CodeType);
        Assert.True(parameter.Type is CodeType);
        Assert.True(method.ReturnType is CodeType);
        var resultingWrapper = root.FindChildByName<CodeClass>("union");
        Assert.NotNull(resultingWrapper);
        Assert.NotNull(resultingWrapper.OriginalComposedType);
        Assert.DoesNotContain("IComposedTypeWrapper", resultingWrapper.StartBlock.Implements.Select(static x => x.Name));
        Assert.NotNull(resultingWrapper.Methods.Single(static x => x.IsOfKind(CodeMethodKind.ComposedTypeMarker)));
    }
    [Fact]
    public async Task SupportsTypeSpecificOverrideIndexersAsync()
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
            Name = "idx-string",
            ReturnType = union.Clone() as CodeTypeBase,
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string"
                },
            },
            Deprecation = new("foo"),
            IsLegacyIndexer = true
        };
        var typeSpecificIndexer = new CodeIndexer
        {
            Name = "idx",
            ReturnType = new CodeType
            {
                Name = "type1",
                TypeDefinition = union.Types.First(),
            },
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "integer"
                },
            }
        };
        model.AddIndexer(indexer, typeSpecificIndexer);
        Assert.NotNull(model.Indexer);
        method.AddParameter(parameter);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Null(model.Indexer);
        Assert.NotNull(model.Methods.SingleOrDefault(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) && x.Name.Equals("ByIdInteger") && x.OriginalIndexer != null && x.OriginalIndexer.IndexParameter.Type.Name.Equals("Integer", StringComparison.OrdinalIgnoreCase)));
        Assert.NotNull(model.Methods.SingleOrDefault(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) && x.Name.Equals("ById") && x.OriginalIndexer != null && x.OriginalIndexer.IndexParameter.Type.Name.Equals("string", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task ValidatesNamingOfRequestBuilderDoesNotRepeatIdCharacterAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        await using var fs = await KiotaBuilderTests.GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /groups/{id}:
    get:
      summary: Get Group by Id
      operationId: groups_GetById
      parameters:
        - name: id
          in: path
          description: The id of the group
          required : true
          schema :
            type: string
            format: uuid
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
  /groups/{groupId}/member/{id}:
    get:
      summary: Get member by Id
      operationId: member_GetById
      parameters:
        - name: groupId
          in: path
          description: The id of the group
          required : true
          schema :
            type: string
            format: uuid
        - name: id
          in: path
          description: The id of the member
          required : true
          schema :
            type: string
            format: uuid
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.directoryObject:
      title: directoryObject
      type: object
      properties:
        deletedDateTime:
          type: string
          pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
          format: date-time
          nullable: true");
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, new HttpClient());
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        var codeModel = builder.CreateSourceModel(node);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, codeModel);
        var requestBuilder = codeModel.FindChildByName<CodeClass>("groupsRequestBuilder");
        var indexerMethods = requestBuilder.Methods.Where(static method =>
            method.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)).ToArray();
        Assert.Equal(2, indexerMethods.Length);
        Assert.Equal("ByGroupId", indexerMethods[0].Name);
        Assert.Equal("ByGroupIdGuid", indexerMethods[1].Name);
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
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Contains("ApiError", model.StartBlock.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", model.StartBlock.Inherits.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("otherProp"));
        Assert.Contains(model.Methods, x => x.Name.Equals("otherMethod"));
        Assert.Contains(model.Usings, x => x.Name.Equals("otherNs"));

        var modelInterface = root.FindChildByName<CodeInterface>("somemodelable");
        Assert.NotNull(modelInterface);
        Assert.Contains(modelInterface.StartBlock.Implements, x => x.Name.Equals("AdditionalDataHolder", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutorAsync()
    {
        var main = root.AddNamespace("main");
        var models = main.AddNamespace($"{main.Name}.models");
        models.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
        }); // so move to models namespace finds the models namespace
        var requestBuilder = main.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = models.AddNamespace($"{models.Name}.subns"); // otherwise the import gets trimmed
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { ClientNamespaceName = "main", Language = GenerationLanguage.Go }, root);

        Assert.Contains("Error4XX", requestBuilder.StartBlock.Usings.Select(static x => x.Declaration?.Name));
    }
    [Fact]
    public async Task AddsUsingsForDiscriminatorTypesAsync()
    {
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
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
        }).First();
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "foo";
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        Assert.Empty(parentModel.StartBlock.Usings);
        var ns = root.AddNamespace("ApiSdk/models");
        ns.AddClass(childModel);// so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(childModel, parentModel.StartBlock.Usings.First(x => x.Declaration.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
        Assert.Null(parentModel.StartBlock.Usings.FirstOrDefault(x => x.Declaration.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task AddsUsingsForFactoryMethodsAsync()
    {
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
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
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        var requestBuilderClass = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var requestExecutor = requestBuilderClass.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = parentModel.Name,
                TypeDefinition = parentModel,
            },
        }).First();
        Assert.Empty(requestBuilderClass.StartBlock.Usings);
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(factoryMethod, requestBuilderClass.StartBlock.Usings.First(x => x.Declaration.Name.Equals("factory", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
    }
    [Fact]
    public async Task RenamesCancellationParametersInRequestExecutorsAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.True(method.Parameters.Any());
        Assert.Contains(cancellationParam, method.Parameters);
        Assert.Equal("ctx", cancellationParam.Name);
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
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
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
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnly", method.ReturnType.Name);
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
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnly", method.ReturnType.Name);
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
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("ISODuration", method.ReturnType.Name);
    }
    [Fact]
    public async Task CorrectesCircularDependencies()
    {
        var modelsNS = root.AddNamespace("ApiSdk.models");

        var subANamespace = modelsNS.AddNamespace($"{modelsNS.Name}.suba");
        var modelA = subANamespace.AddClass(new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "ModelA",
        }).First();
        subANamespace.AddEnum(new CodeEnum
        {
            Name = "ModelAEnum",
        });
        subANamespace.AddInterface(new CodeInterface
        {
            Name = "ModelAInterface",
            OriginalClass = modelA,
        });
        subANamespace.AddFunction(new CodeFunction(
            new CodeMethod
            {
                Name = "ModelAFunction",
                IsStatic = true,
                Parent = modelA,
                ReturnType = new CodeType()
            })
        );

        var subBNamespace = modelsNS.AddNamespace($"{modelsNS.Name}.subb");
        var modelB = subBNamespace.AddClass(new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "ModelB",
        }).First();

        modelA.StartBlock.AddUsings(new CodeUsing
        {
            Name = subBNamespace.Name,
            Declaration = new()
            {
                Name = modelB.Name,
                TypeDefinition = modelB,
                IsExternal = false
            }
        });

        modelB.StartBlock.AddUsings(new CodeUsing
        {
            Name = subANamespace.Name,
            Declaration = new()
            {
                Name = modelA.Name,
                TypeDefinition = modelA,
                IsExternal = false
            }
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("ApiSdk.models", modelB.GetImmediateParentOfType<CodeNamespace>().Name); // migrated to root namespace
        Assert.Equal("ApiSdk.models", modelA.GetImmediateParentOfType<CodeNamespace>().Name); // migrated to root namespace
        Assert.Equal("SubaModelA", modelA.Name); // renamed to avoid conflict
        Assert.Equal("SubbModelB", modelB.Name); // renamed to avoid conflict

        Assert.Empty(subANamespace.GetChildElements(true));
    }
    #endregion

    #region GoRefinerTests
    [Fact]
    public async Task DoesNotEscapePublicPropertiesReservedKeywordsForQueryParametersAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "SomeClass",
            Kind = CodeClassKind.QueryParameters
        }).First();
        var property = model.AddProperty(new CodeProperty
        {
            Name = "Select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("Select", property.Name);
        Assert.False(property.IsNameEscaped);
    }
    [Fact]
    public async Task EscapesPublicPropertiesReservedKeywordsForModelsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "SomeClass",
            Kind = CodeClassKind.Model
        }).First();
        var property = model.AddProperty(new CodeProperty
        {
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal("selectEscaped", property.Name);
        Assert.True(property.IsNameEscaped);
    }
    [Fact]
    public async Task ReplacesRequestBuilderPropertiesByMethodsAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "someModel",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var rb = model.AddProperty(new CodeProperty
        {
            Name = "someProperty",
            Kind = CodePropertyKind.RequestBuilder,
            Type = new CodeType { Name = "SomeType" },
        }).First();
        rb.Type = new CodeType
        {
            Name = "someType",
        };
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(model.Properties);
        Assert.Single(model.Methods, x => x.IsOfKind(CodeMethodKind.RequestBuilderBackwardCompatibility));
    }
    [Fact]
    public async Task AddsErrorImportForEnumsForMultiValueEnumAsync()
    {
        var testEnum = root.AddEnum(new CodeEnum
        {
            Name = "TestEnum",
            Flags = true
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.DoesNotContain(testEnum.Usings, static x => "errors".Equals(x.Name, StringComparison.Ordinal));
        Assert.Single(testEnum.Usings, static x => "strings".Equals(x.Name, StringComparison.Ordinal));
    }
    [Fact]
    public async Task AddsErrorImportForEnumsForSingleValueEnumAsync()
    {
        var testEnum = root.AddEnum(new CodeEnum
        {
            Name = "TestEnum",
            Flags = false
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.DoesNotContain(testEnum.Usings, static x => "errors".Equals(x.Name, StringComparison.Ordinal));
        Assert.DoesNotContain(testEnum.Usings, static x => "strings".Equals(x.Name, StringComparison.Ordinal));
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
        var constructorMethod = model.AddMethod(new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        var rawUrlParam = new CodeParameter
        {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType
            {
                Name = "string",
                IsNullable = true,
                IsExternal = true
            }
        };
        constructorMethod.AddParameter(rawUrlParam);
        var pathParamsProp = model.AddProperty(new CodeProperty
        {
            Name = "name",
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
            Kind = CodePropertyKind.PathParameters,
            DefaultValue = "wrongDefaultValue"
        }).First();
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.DoesNotContain(model.Properties, static x => requestAdapterDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => factoryDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => dateTimeOffsetDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Properties, static x => additionalDataDefaultName.Equals(x.Type.Name));
        Assert.DoesNotContain(model.Methods, static x => deserializeDefaultName.Equals(x.ReturnType.Name));
        Assert.DoesNotContain(model.Methods.SelectMany(static x => x.Parameters), static x => serializerDefaultName.Equals(x.Type.Name));
        Assert.Equal("make(map[string]string)", pathParamsProp.DefaultValue);
        Assert.Equal("map[string]string", pathParamsProp.Type.Name);
        Assert.False(rawUrlParam.Type.IsNullable);
    }
    [Fact]
    public async Task RemovesPropertyRelyingOnSubModulesAsync()
    {
        var models = root.AddNamespace("ApiSdk.models");
        var submodels = models.AddNamespace($"{models.Name}.submodels");
        var propertyModel = submodels.AddClass(new CodeClass
        {
            Name = "propertyModel",
            Kind = CodeClassKind.Model
        }).First();
        var mainModel = models.AddClass(new CodeClass
        {
            Name = "mainModel",
            Kind = CodeClassKind.Model
        }).First();
        var property = mainModel.AddProperty(new CodeProperty
        {
            Name = "property",
            Type = new CodeType
            {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            },
            Kind = CodePropertyKind.Custom
        }).First();
        mainModel.AddMethod(new CodeMethod
        {
            Name = $"get{property.Name}",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType
            {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            },
            AccessedProperty = property
        });
        var setter = mainModel.AddMethod(new CodeMethod
        {
            Name = $"get{property.Name}",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType
            {
                Name = "void",
                IsExternal = true,
            },
            AccessedProperty = property
        }).First();
        setter.AddParameter(new CodeParameter
        {
            Name = "value",
            Type = new CodeType
            {
                Name = "propertyModel",
                TypeDefinition = propertyModel
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Empty(mainModel.Properties);
        Assert.DoesNotContain(mainModel.Methods, x => x.IsAccessor);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        var childMethods = builder.Methods;
        Assert.DoesNotContain(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor)); // no executor overloads
        Assert.DoesNotContain(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator)); // no generator overloads
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count() == 2);// body + query
        Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count() == 3);// ctx + body + query config
        Assert.Equal(2, childMethods.Count());
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(GoRefiner.UntypedNodeName, property.Type.Name);// type is renamed
        Assert.NotEmpty(model.StartBlock.Usings);
        var nodeUsing = model.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("github.com/microsoft/kiota-abstractions-go/serialization", nodeUsing[0].Declaration.Name);
    }

    [Fact]
    public async Task NormalizeNamespaceNameAsync()
    {
        root.Name = "github.com/OrgName/RepoName";
        var models = root.AddNamespace("ApiSdk.models");
        var submodels = models.AddNamespace("ApiSdk.models.submodels");
        var camelCaseModel = submodels.AddNamespace("ApiSdk.models.submodels.camelCase");
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go, ClientNamespaceName = "github.com/OrgName/RepoName" }, root);
        Assert.Equal("github.com/OrgName/RepoName.apisdk.models.submodels", submodels.Name);
        Assert.Equal("github.com/OrgName/RepoName.apisdk.models", models.Name);
        Assert.Equal("github.com/OrgName/RepoName.apisdk.models.submodels.camelcase", camelCaseModel.Name);
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(GoRefiner.UntypedNodeName, method.Parameters.First().Type.Name);// type is renamed
        Assert.NotEmpty(requestBuilderClass.StartBlock.Usings);
        var nodeUsing = requestBuilderClass.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("github.com/microsoft/kiota-abstractions-go/serialization", nodeUsing[0].Declaration.Name);
    }
    [Theory]
    [InlineData("ISODuration", false)]
    [InlineData("DateOnly", false)]
    [InlineData("TimeOnly", false)]
    [InlineData("Time", false)]
    [InlineData("DateTimeOffset", false)]
    [InlineData("Guid", false)]
    [InlineData("string", false)]
    [InlineData("boolean", true)]
    [InlineData("int64", true)]
    [InlineData("integer", true)]
    [InlineData("long", true)]
    [InlineData("float", true)]
    public async Task ImportsStrConvForRelevantTypesOnly(string pathParameterType, bool isImported)
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "RequestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var constructor = model.AddMethod(new CodeMethod
        {
            Name = "NewRequestBuilder",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        constructor.AddParameter(new CodeParameter
        {
            Name = "daysInPast",
            Kind = CodeParameterKind.Path,
            Type = new CodeType
            {
                Name = pathParameterType
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        Assert.Equal(isImported, model.StartBlock.Usings.Any(static x => x.Declaration.Name.Equals("strconv", StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task EscapesReservedKeywordsInMethodParametersAsync()
    {
        var model = root.AddClass(new CodeClass
        {
            Name = "RequestBuilder",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        var method = model.AddMethod(new CodeMethod
        {
            Name = "Execute",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        // Add a parameter with a reserved keyword name
        method.AddParameter(new CodeParameter
        {
            Name = "type",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        method.AddParameter(new CodeParameter
        {
            Name = "select",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);

        // Verify that reserved keyword parameters are escaped
        Assert.Contains(method.Parameters, p => p.Name == "typeEscaped");
        Assert.Contains(method.Parameters, p => p.Name == "selectEscaped");
        Assert.DoesNotContain(method.Parameters, p => p.Name == "type");
        Assert.DoesNotContain(method.Parameters, p => p.Name == "select");
    }
    #endregion
}
