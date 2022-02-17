using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests;
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
#region commonrefiner
    [Fact]
    public void AddsExceptionInheritanceOnErrorClasses() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = model.StartBlock as ClassDeclaration;

        Assert.Contains("ApiError", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiError", declaration.Inherits.Name);
    }
    [Fact]
    public void FailsExceptionInheritanceOnErrorClassesWhichAlreadyInherit() {
        var model = root.AddClass(new CodeClass {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        var declaration = model.StartBlock as ClassDeclaration;
        declaration.Inherits = new CodeType {
            Name = "SomeOtherModel"
        };
        Assert.Throws<InvalidOperationException>(() => ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root));
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
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = requestBuilder.StartBlock as ClassDeclaration;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public void AddsUsingsForDiscriminatorTypes() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as ClassDeclaration).Inherits = new CodeType {
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
        factoryMethod.DiscriminatorMappings.TryAdd("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        Assert.False(factoryMethod.Parent is CodeFunction);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public void EscapesReservedKeywords() {
        var model = root.AddClass(new CodeClass {
            Name = "break",
            Kind = CodeClassKind.Model
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEqual("break", model.Name);
        Assert.Contains("escaped", model.Name);
    }
    [Fact]
    public void CorrectsCoreType() {

        var model = root.AddClass(new CodeClass () {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty() {
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
        ILanguageRefiner.Refine(new GenerationConfiguration{ Language = GenerationLanguage.TypeScript }, root);
        Assert.Empty(model.Properties.Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => FactoryDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => AddiationalDataDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => PathParametersDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Properties.Where(x => PathParametersDefaultValue.Equals(x.DefaultValue)));
        Assert.Empty(model.Methods.Where(x => DeserializeDefaultName.Equals(x.ReturnType.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => HandlerDefaultName.Equals(x.Type.Name)));
        Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        Assert.Single(constructorMethod.Parameters.Where(x => x.Type is CodeUnionType));
    }
    [Fact]
    public void ReplacesDateTimeOffsetByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "DateTimeOffset"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesDateOnlyByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "DateOnly"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnly", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesTimeOnlyByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "TimeOnly"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnly", method.ReturnType.Name);
    }
    [Fact]
    public void ReplacesDurationByNativeType() {
        var model = root.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var method = model.AddMethod(new CodeMethod {
            Name = "method",
            ReturnType = new CodeType {
                Name = "TimeSpan"
            },
        }).First();
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Duration", method.ReturnType.Name);
    }
    [Fact]
    public void AliasesDuplicateUsingSymbols() {
        var model = graphNS.AddClass(new CodeClass {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        var modelsNS = graphNS.AddNamespace($"{graphNS.Name}.models");
        var source1 = modelsNS.AddClass(new CodeClass {
            Name = "source",
            Kind = CodeClassKind.Model
        }).First();
        var submodelsNS = modelsNS.AddNamespace($"{modelsNS.Name}.submodels");
        var source2 = submodelsNS.AddClass(new CodeClass {
            Name = "source",
            Kind = CodeClassKind.Model
        }).First();

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
        model.AddUsing(using2);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        Assert.NotEmpty(using1.Alias);
        Assert.NotEmpty(using2.Alias);
        Assert.NotEqual(using1.Alias, using2.Alias);
    }
    [Fact]
    public void DoesNotKeepCancellationParametersInRequestExecutors()
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
            Description = "Cancellation token to use when cancelling requests",
            Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root); //using CSharp so the cancelletionToken doesn't get removed
        Assert.False(method.Parameters.Any());
        Assert.DoesNotContain(cancellationParam, method.Parameters);
    }
#endregion
}
