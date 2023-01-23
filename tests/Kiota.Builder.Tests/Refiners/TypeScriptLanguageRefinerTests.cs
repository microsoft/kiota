using System;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

using Xunit;
using Xunit.Sdk;

namespace Kiota.Builder.Tests.Refiners;

public class TypeScriptLanguageRefinerTests {
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    public TypeScriptLanguageRefinerTests() {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
        parentClass = new () {
            Name = "parentClass"
        };
        graphNS.AddClass(parentClass);
    }

    private CodeClass CreateCodeClass(string className = "model")
    {
         var testClass = new CodeClass
         {
             Name = className,
             Kind = CodeClassKind.Model
         };

        var deserializer = new CodeMethod {
            Name = "DeserializerMethod",
            ReturnType = new CodeType { },
            Kind = CodeMethodKind.Deserializer
        };

        var serializer = new CodeMethod
        {
            Name = "SerializerMethod",
            ReturnType = new CodeType { },
            Kind = CodeMethodKind.Serializer
        };
        testClass.AddMethod(deserializer);
        testClass.AddMethod(serializer);
        return testClass;
    }
    
    

    

#region commonrefiner
[Fact]
    public async Task AddsQueryParameterMapperMethod() {
        var model = graphNS.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.QueryParameters,
        }).First();

        model.AddProperty(new CodeProperty {
            Name = "Select",
            SerializationName = "%24select",
            Type = new CodeType {
                Name = "string"
            },
        });

        Assert.Empty(model.Methods);

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.QueryParametersMapper)));
    }
    [Fact]
    public async Task AddStaticMethodsUsingsForDeserializer() {
        var model = graphNS.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        model.AddMethod(new CodeMethod {
            Name = "Deserialize",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType {
                Name = "void",
                IsExternal = true,
            },
        });

        var subNs = graphNS.AddNamespace($"{graphNS.Name}.subns");

        var propertyModel = subNs.AddClass(new CodeClass {
            Name = "somepropertyModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        propertyModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            IsAsync = false,
            IsStatic = true,
            ReturnType = new CodeType {
                Name = "void",
                IsExternal = true,
            },
        });

        model.AddProperty(new CodeProperty {
            Name = "someProperty",
            Type = new CodeType {
                Name = "somepropertyModel",
                TypeDefinition = propertyModel,
            },
        });

        Assert.Empty(graphNS.GetChildElements(true).OfType<CodeFunction>());
        Assert.Single(model.GetChildElements(true).OfType<CodeMethod>());

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, graphNS);
        Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().Where(x => x.IsOfKind(CodeMethodKind.Factory)));
        Assert.Single(subNs.GetChildElements(true).OfType<CodeFunction>());

        var function = subNs.GetChildElements(true).OfType<CodeFunction>().First();
        Assert.Single(model.Usings.Where(x => !x.IsExternal && x.Declaration.TypeDefinition == function));

    }
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = model.StartBlock;

        Assert.Contains("ApiError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", declaration.Inherits.Name);
    }
    [Fact]
    public async Task FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock;
        declaration.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor() {
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
        requestExecutor.AddErrorMapping("4XX", new CodeType {
                        Name = "Error4XX",
                        TypeDefinition = errorClass,
                    });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task AddsUsingsForDiscriminatorTypes() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        childModel.StartBlock.Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        Assert.False(factoryMethod.Parent is CodeFunction);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.Equal(childModel, (factoryMethod.Parent as CodeFunction).StartBlock.Usings.First(x => x.Name.Equals("childModel", StringComparison.OrdinalIgnoreCase)).Declaration.TypeDefinition);
    }
#endregion
#region typescript
    private const string HttpCoreDefaultName = "IRequestAdapter";
    private const string FactoryDefaultName = "ISerializationWriterFactory";
    private const string DeserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
    private const string PathParametersDefaultName = "Dictionary<string, object>";
    private const string PathParametersDefaultValue = "new Dictionary<string, object>";
    private const string DateTimeOffsetDefaultName = "DateTimeOffset";
    private const string AddiationalDataDefaultName = "new Dictionary<string, object>()";
    private const string HandlerDefaultName = "IResponseHandler";
    [Fact]
    public async Task EscapesReservedKeywords() {
        //var model = root.AddClass(new CodeClass {
        //    Name = "break",
        //    Kind = CodeClassKind.Model
        //}).First();
        var model = CreateCodeClass();
        root.AddClass(model);
        model.Name = "break";
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var interFaceModel = root.CodeInterfaces.First(x => x.Name == "Break_escaped");
        Assert.NotEqual("break", interFaceModel.Name);
        Assert.Contains("escaped", interFaceModel.Name);
    }
    [Fact]
    public async Task CorrectsCoreType() {

        //var model = root.AddClass(new CodeClass
        //{
        //    Name = "model",
        //    Kind = CodeClassKind.Model
        //}).First();
        var model = CreateCodeClass();
        root.AddClass(model);
        model.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType {
                Name = HttpCoreDefaultName
            }
        }, new () {
            Name = "someDate",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType {
                Name = DateTimeOffsetDefaultName,
            }
        }, new () {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType {
                Name = AddiationalDataDefaultName
            }
        }, new () {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType {
                Name = PathParametersDefaultName
            },
            DefaultValue = PathParametersDefaultValue
        });
        var executorMethod = model.AddMethod(new CodeMethod {
            Name = "executor",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        executorMethod.AddParameter(new CodeParameter {
            Name = "handler",
            Kind = CodeParameterKind.ResponseHandler,
            Type = new CodeType {
                Name = HandlerDefaultName,
            }
        });
        const string serializerDefaultName = "ISerializationWriter";
        var serializationMethod = model.AddMethod(new CodeMethod {
            Name = "seriailization",
            Kind = CodeMethodKind.Serializer,
            ReturnType = new CodeType {
                Name = "string"
            }
        }).First();
        serializationMethod.AddParameter(new CodeParameter {
            Name = "handler",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType {
                Name = serializerDefaultName,
            }
        });
        var constructorMethod = model.AddMethod(new CodeMethod {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType {
                Name = "void"
            }
        }).First();
        constructorMethod.AddParameter(new CodeParameter {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType {
                Name = PathParametersDefaultName
            },
        });
        var interFaceModel = root.FindChildByName<CodeInterface>(model.Name);
        await ILanguageRefiner.Refine(new GenerationConfiguration{ Language = GenerationLanguage.TypeScript }, root);
        Assert.Empty(interFaceModel.Properties.Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => FactoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => AddiationalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => PathParametersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(interFaceModel.Properties.Where(x => PathParametersDefaultValue.Equals(x.DefaultValue)));
        Assert.Empty(model.Methods.Where(x => DeserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => HandlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
    }
    [Fact]
    public async Task ReplacesDateTimeOffsetByNativeType() {
        var model = CreateCodeClass();
        root.AddClass(model);
        var codeProperty = model.AddProperty(new CodeProperty {
            Name = "method",
            Type = new CodeType {
                Name = "DateTimeOffset",
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var modelInterface = root.CodeInterfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("Date", modelInterface.Properties.First(x=> x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeType() {

        var model = CreateCodeClass();
        root.AddClass(model);
        var codeProperty = model.AddProperty(new CodeProperty{
            Name = "method",
            Type = new CodeType {
                Name = "DateOnly"
            },
        }).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        var modelInterface = root.CodeInterfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("DateOnly", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeType() {
        var model = CreateCodeClass();
        root.AddClass(model);
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
        var modelInterface = root.CodeInterfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("TimeOnly", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
       
    }
    [Fact]
    public async Task ReplacesDurationByNativeType() {

        var model = CreateCodeClass();
        root.AddClass(model);
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
        var modelInterface = root.CodeInterfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        Assert.NotEmpty(modelInterface.StartBlock.Usings);
        Assert.Equal("Duration", modelInterface.Properties.First(x => x.Name == codeProperty.Name).Type.Name);
    }
    [Fact]
    public async Task AliasesDuplicateUsingSymbols() {

        var model = CreateCodeClass();
        graphNS.AddClass(model);
        var modelsNS = graphNS.AddNamespace($"{graphNS.Name}.models");

        var source1 = CreateCodeClass();
        modelsNS.AddClass(source1);
        source1.Name = "source";

        
        var source2 = CreateCodeClass();
        modelsNS.AddClass(source2);
        source2.Name = "source";
        var submodelsNS = modelsNS.AddNamespace($"{modelsNS.Name}.submodels");
        
        var using1 = new CodeUsing {
            Name = modelsNS.Name,
            Declaration = new CodeType {
                Name = source1.Name,
                TypeDefinition = source1,
                IsExternal = false,
            }
        };
        var using2 = new CodeUsing {
            Name = submodelsNS.Name,
            Declaration = new CodeType {
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
        var modelInterface = graphNS.CodeInterfaces.First(x => x.Name == model.Name.ToFirstCharacterUpperCase());
        var modelUsing1 = modelInterface.Usings.ElementAt(0);
        var modelUsing2 = modelInterface.Usings.ElementAt(1);
        Assert.Equal(modelUsing1.Declaration.Name, modelUsing2.Declaration.Name);
        Assert.NotEmpty(modelUsing1.Alias);
        Assert.NotEmpty(modelUsing2.Alias);
        Assert.NotEqual(modelUsing1.Alias, modelUsing2.Alias);
        Assert.NotEmpty(using1.Alias);
        Assert.NotEmpty(using2.Alias);
        Assert.NotEqual(using1.Alias, using2.Alias);
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
            Documentation = new() {
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
        var model = CreateCodeClass("modelA");
            testNS.AddClass(model);

        //var property = CreateCodeClass("propertyB");
        //testNS.AddClass(property);

        //model.AddProperty(new CodeProperty { Name = property.Name, Type = new CodeType { Name = property.Name, TypeDefinition = property } });


        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, testNS);
        Assert.Contains(testNS.CodeInterfaces, x =>x.Name == "ModelA");
        //Assert.Contains(testNS.CodeInterfaces, x => x.Name == "PropertyB");
        //Assert.Contains(testNS.CodeInterfaces.FirstOrDefault(x => x.Name == "ModelA").Properties, x => x.Name == "propertyB");
        //Assert.Contains(testNS.Classes, x => x.Name == "modelAImpl");
        //Assert.Contains(testNS.Classes, x => x.Name == "propertyBImpl");     
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
        requestConfig.AddProperty(new CodeProperty { Name = queryParam.Name, Type = new CodeType { Name = queryParam.Name, TypeDefinition = queryParam } });



        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, testNS);
        Assert.Contains(testNS.CodeInterfaces, x => x.Name == "requestConfig");
        Assert.Contains(testNS.CodeInterfaces, x => x.Name == "queryParams");
        Assert.False(testNS.Classes.Any());
        Assert.DoesNotContain(testNS.Classes, x => x.Name == "requestConfig");
        Assert.DoesNotContain(testNS.Classes, x => x.Name == "queryParams");
    }
    #endregion
}
