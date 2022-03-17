using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Tests;
using Moq;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests;
public class CodeMethodWriterTests : IDisposable {
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeMethod method;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";
    public CodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
        method = new CodeMethod {
            Name = MethodName,
        };
        method.ReturnType = new CodeType {
            Name = ReturnTypeName
        };
        parentClass.AddMethod(method);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    private void AddRequestProperties() {
        parentClass.AddProperty(new CodeProperty {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType {
                Name = "string"
            },
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "UrlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
        });
    }
    private void AddSerializationProperties() {
        var addData = parentClass.AddProperty(new CodeProperty {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
        }).First();
        addData.Type = new CodeType {
            Name = "string"
        };
        var dummyProp = parentClass.AddProperty(new CodeProperty {
            Name = "dummyProp",
        }).First();
        dummyProp.Type = new CodeType {
            Name = "string"
        };
        var dummyCollectionProp = parentClass.AddProperty(new CodeProperty {
            Name = "dummyColl",
        }).First();
        dummyCollectionProp.Type = new CodeType {
            Name = "string",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
        };
        var dummyComplexCollection = parentClass.AddProperty(new CodeProperty {
            Name = "dummyComplexColl"
        }).First();
        dummyComplexCollection.Type = new CodeType {
            Name = "Complex",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            TypeDefinition = new CodeClass {
                Name = "SomeComplexType"
            }
        };
        var dummyEnumProp = parentClass.AddProperty(new CodeProperty{
            Name = "dummyEnumCollection",
        }).First();
        dummyEnumProp.Type = new CodeType {
            Name = "SomeEnum",
            TypeDefinition = new CodeEnum {
                Name = "EnumType"
            }
        };
    }
    private void AddInheritanceClass() {
        (parentClass.StartBlock as ClassDeclaration).Inherits = new CodeType {
            Name = "someParentClass"
        };
    }
    private void AddRequestBodyParameters(CodeMethod target = default) {
        var stringType = new CodeType {
            Name = "string",
        };
        target ??= method;
        target.AddParameter(new CodeParameter {
            Name = "h",
            Kind = CodeParameterKind.Headers,
            Type = stringType,
        });
        target.AddParameter(new CodeParameter{
            Name = "q",
            Kind = CodeParameterKind.QueryParameter,
            Type = stringType,
        });
        target.AddParameter(new CodeParameter{
            Name = "b",
            Kind = CodeParameterKind.RequestBody,
            Type = stringType,
        });
        target.AddParameter(new CodeParameter{
            Name = "r",
            Kind = CodeParameterKind.ResponseHandler,
            Type = stringType,
        });
        target.AddParameter(new CodeParameter {
            Name = "o",
            Kind = CodeParameterKind.Options,
            Type = stringType,
        });
    }
    [Fact]
    public void WritesNullableVoidTypeForExecutor(){
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType {
            Name = "void",
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("(error)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestBuilder() {
        AddRequestProperties();
        method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.pathParameters", result);
        Assert.Contains("m.requestAdapter", result);
        Assert.Contains("return", result);
        Assert.Contains("func (m", result);
        Assert.Contains("New", result);
    }
    [Fact]
    public void WritesRequestBodiesThrowOnNullHttpMethod() {
        method.Kind = CodeMethodKind.RequestExecutor;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.Kind = CodeMethodKind.RequestGenerator;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesRequestExecutorBody() {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        var error4XX = root.AddClass(new CodeClass{
            Name = "Error4XX",
        }).First();
        var error5XX = root.AddClass(new CodeClass{
            Name = "Error5XX",
        }).First();
        var error401 = root.AddClass(new CodeClass{
            Name = "Error401",
        }).First();
        method.AddErrorMapping("4XX", new CodeType {Name = "Error4XX", TypeDefinition = error4XX});
        method.AddErrorMapping("5XX", new CodeType {Name = "Error5XX", TypeDefinition = error5XX});
        method.AddErrorMapping("403", new CodeType {Name = "Error403", TypeDefinition = error401});
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("requestInfo, err :=", result);
        Assert.Contains($"errorMapping := {AbstractionsPackageHash}.ErrorMappings", result);
        Assert.Contains($"\"4XX\": CreateError4XXFromDiscriminatorValue", result);
        Assert.Contains($"\"5XX\": CreateError5XXFromDiscriminatorValue", result);
        Assert.Contains($"\"403\": CreateError403FromDiscriminatorValue", result);
        Assert.Contains("m.requestAdapter.SendAsync", result);
        Assert.Contains("return res.(", result);
        Assert.Contains("err != nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntCreateDictionaryOnEmptyErrorMapping() {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain($"errorMapping := {AbstractionsPackageHash}.ErrorMappings", result);
        Assert.Contains("nil)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBody() {
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
        factoryMethod.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                TypeDefinition = new CodeClass {
                    Name = "ParseNode",
                },
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.Contains("if mappingValueNode != nil {", result);
        Assert.Contains("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.Contains("if mappingValue != nil {", result);
        Assert.Contains("mappingStr := *mappingValue", result);
        Assert.Contains("switch mappingStr {", result);
        Assert.Contains("case \"ns.childmodel\":", result);
        Assert.Contains("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnMissingParameter() {
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
        factoryMethod.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorPropertyName = "@odata.type";
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if mappingValueNode != nil {", result);
        Assert.DoesNotContain("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.DoesNotContain("if mappingValue != nil {", result);
        Assert.DoesNotContain("mappingStr := *mappingValue", result);
        Assert.DoesNotContain("switch mappingStr {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyPropertyName() {
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
        factoryMethod.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorPropertyName = string.Empty;
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                TypeDefinition = new CodeClass {
                    Name = "ParseNode",
                },
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if mappingValueNode != nil {", result);
        Assert.DoesNotContain("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.DoesNotContain("if mappingValue != nil {", result);
        Assert.DoesNotContain("mappingStr := *mappingValue", result);
        Assert.DoesNotContain("switch mappingStr {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyMappings() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        factoryMethod.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                TypeDefinition = new CodeClass {
                    Name = "ParseNode",
                },
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if mappingValueNode != nil {", result);
        Assert.DoesNotContain("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.DoesNotContain("if mappingValue != nil {", result);
        Assert.DoesNotContain("mappingStr := *mappingValue", result);
        Assert.DoesNotContain("switch mappingStr {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    private const string AbstractionsPackageHash = "ida96af0f171bb75f894a4013a6b3146a4397c58f11adb81a2b7cbea9314783a9";
    [Fact]
    public void WritesRequestGeneratorBody() {
        var configurationMock = new Mock<GenerationConfiguration>();
        var refiner = new GoRefiner(configurationMock.Object);
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        var executor = parentClass.AddMethod(new CodeMethod {
            Name = "executor",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType {
                Name = "string",
                IsExternal = true,
            }
        }).First();
        AddRequestBodyParameters(executor);
        AddRequestBodyParameters();
        AddRequestProperties();
        refiner.Refine(parentClass.Parent as CodeNamespace);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformation()", result);
        Assert.Contains("requestInfo.UrlTemplate = ", result);
        Assert.Contains("requestInfo.PathParameters", result);
        Assert.Contains($"Method = {AbstractionsPackageHash}.GET", result);
        Assert.Contains("err != nil", result);
        Assert.Contains("H != nil", result);
        Assert.Contains("requestInfo.Headers =", result);
        Assert.Contains("Q != nil", result);
        Assert.Contains("requestInfo.AddQueryParameters(*(options.Q))", result);
        Assert.Contains("O) != 0", result);
        Assert.Contains("requestInfo.AddRequestOptions(", result);
        Assert.Contains("requestInfo.SetContentFromParsable(m.requestAdapter", result);
        Assert.Contains("return requestInfo, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesInheritedDeSerializerBody() {
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.SomeParentClass.MethodName()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesDeSerializerBody() {
        var parameter = new CodeParameter{
            Description = ParamDescription,
            Name = ParamName
        };
        parameter.Type = new CodeType {
            Name = "string"
        };
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("GetStringValue", result);
        Assert.Contains("GetCollectionOfPrimitiveValues", result);
        Assert.Contains("GetCollectionOfObjectValues", result);
        Assert.Contains("GetEnumValue", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesInheritedSerializerBody() {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.SomeParentClass.Serialize", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesSerializerBody() {
        var parameter = new CodeParameter{
            Description = ParamDescription,
            Name = ParamName
        };
        parameter.Type = new CodeType {
            Name = "string"
        };
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("WriteStringValue", result);
        Assert.Contains("WriteCollectionOfStringValues", result);
        Assert.Contains("WriteCollectionOfObjectValues", result);
        Assert.Contains("WriteAdditionalData(m.GetAdditionalData())", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact(Skip = "descriptions are not supported")]
    public void WritesMethodSyncDescription() {
        method.Description = MethodDescription;
        method.IsAsync = false;
        var parameter = new CodeParameter{
            Description = ParamDescription,
            Name = ParamName
        };
        parameter.Type = new CodeType {
            Name = "string"
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("@return a CompletableFuture of", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive() {
        var codeMethodWriter = new CodeMethodWriter(new GoConventionService());
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
        var originalParent = method.Parent;
        method.Parent = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
        method.Parent = originalParent;
        method.ReturnType = null;
        Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
    }
    [Fact]
    public void ThrowsIfParentIsNotClass() {
        method.Parent = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void ThrowsIfReturnTypeIsMissing() {
        method.ReturnType = null;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    private const string TaskPrefix = "func() (";
    [Fact]
    public void WritesReturnType() {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"{MethodName.ToFirstCharacterUpperCase()}()(*{ReturnTypeName}, error)", result);// async default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods() {
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain(TaskPrefix, result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPublicMethodByDefault() {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName.ToFirstCharacterUpperCase(), result);// public default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPrivateMethod() {
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName.ToFirstCharacterLowerCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesProtectedMethod() {
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName.ToFirstCharacterLowerCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIndexer() {
        AddRequestProperties();
        method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
        method.OriginalIndexer = new () {
            Name = "indx",
            ParameterName = "id",
            IndexType = new CodeType {
                Name = "string",
                IsNullable = true,
            }
        };
        method.AddParameter(new CodeParameter {
            Name = "id",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType {
                Name = "string",
                IsNullable = true,
            },
            Optional = true
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.requestAdapter", result);
        Assert.Contains("m.pathParameters", result);
        Assert.Contains("= *id", result);
        Assert.Contains("return", result);
        Assert.Contains("New", result);
    }
    [Fact]
    public void WritesPathParameterRequestBuilder() {
        AddRequestProperties();
        method.Kind = CodeMethodKind.RequestBuilderWithParameters;
        method.AddParameter(new CodeParameter {
            Name = "pathParam",
            Kind = CodeParameterKind.Path,
            Type = new CodeType {
                Name = "string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.requestAdapter", result);
        Assert.Contains("m.pathParameters", result);
        Assert.Contains("pathParam", result);
        Assert.Contains("return New", result);
    }
    [Fact]
    public void WritesDescription() {
        AddRequestProperties();
        method.Kind = CodeMethodKind.RequestBuilderWithParameters;
        method.AddParameter(new CodeParameter {
            Name = "pathParam",
            Kind = CodeParameterKind.Path,
            Type = new CodeType {
                Name = "string"
            }
        });
        method.Description = "Some description";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"// {method.Name.ToFirstCharacterUpperCase()} some description", result);
    }
    [Fact]
    public void WritesGetterToBackingStore() {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.GetBackingStore().Get(\"someProperty\")", result);
    }
    [Fact]
    public void WritesGetterToBackingStoreWithNonnullProperty() {
        method.AddAccessedProperty();
        parentClass.AddBackingStoreProperty();
        method.AccessedProperty.Type = new CodeType {
            Name = "string",
            IsNullable = false,
        };
        var defaultValue = "someDefaultValue";
        method.AccessedProperty.DefaultValue = defaultValue; 
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("if value == nil", result);
        Assert.Contains(defaultValue, result);
    }
    [Fact]
    public void WritesSetterToBackingStore() {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.GetBackingStore().Set(\"someProperty\", value)", result);
    }
    [Fact]
    public void WritesGetterToField() {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.someProperty", result);
    }
    [Fact]
    public void WritesSetterToField() {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.someProperty = value", result);
    }
    [Fact]
    public void WritesConstructor() {
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType {
                Name = "map[string]string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"m.{propName} = {defaultValue}", result);
        Assert.Contains("m.pathParameters = urlTplParams", result);
        Assert.Contains("make(map[string]string)", result);
    }
    [Fact]
    public void WritesRawUrlConstructor() {
        method.Kind = CodeMethodKind.RawUrlConstructor;
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType {
                Name = "string"
            }
        });
        method.AddParameter(new CodeParameter {
            Name = "requestAdapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType {
                Name = "string"
            }
        });
        method.OriginalMethod = new ();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"urlParams := make(map[string]string)", result);
        Assert.Contains($"urlParams[\"request-raw-url\"] = rawUrl", result);
    }
    [Fact]
    public void WritesInheritedConstructor() {
        method.Kind = CodeMethodKind.Constructor;
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("SomeParentClass: *NewSomeParentClass", result);
    }
    [Fact]
    public void WritesApiConstructor() {
        method.Kind = CodeMethodKind.ClientConstructor;
        var coreProp = parentClass.AddProperty(new CodeProperty {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
        }).First();
        coreProp.Type = new CodeType {
            Name = "HttpCore",
            IsExternal = true,
        };
        method.AddParameter(new CodeParameter {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        method.DeserializerModules = new() {"github.com/microsoft/kiota/serialization/go/json.Deserializer"};
        method.SerializerModules = new() {"github.com/microsoft/kiota/serialization/go/json.Serializer"};
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("RegisterDefaultSerializer", result);
        Assert.Contains("RegisterDefaultDeserializer", result);
    }
    [Fact]
    public void WritesApiConstructorWithBackingStore() {
        method.Kind = CodeMethodKind.ClientConstructor;
        var coreProp = parentClass.AddProperty(new CodeProperty {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
        }).First();
        coreProp.Type = new CodeType {
            Name = "HttpCore",
            IsExternal = true,
        };
        method.AddParameter(new CodeParameter {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        var backingStoreParam = new CodeParameter {
            Name = "backingStore",
            Kind = CodeParameterKind.BackingStore,
        };
        backingStoreParam.Type = new CodeType {
            Name = "IBackingStore",
            IsExternal = true,
        };
        method.AddParameter(backingStoreParam);
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("EnableBackingStore", result);
    }
    [Fact]
    public void AccessorsTargetingEscapedPropertiesAreNotEscapedThemselves() {
        var model = root.AddClass(new CodeClass {
            Name = "SomeClass",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty {
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
            Kind = CodePropertyKind.Custom,
        });
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        var getter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Getter));
        var setter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Setter));
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(getter);
        var result = tw.ToString();
        Assert.Contains("GetSelect", result);
        Assert.DoesNotContain("GetSelect_escaped", result);
        
        using var tw2 = new StringWriter();
        tempWriter.SetTextWriter(tw2);
        tempWriter.Write(setter);
        result = tw2.ToString();
        Assert.Contains("SetSelect", result);
        Assert.DoesNotContain("SetSelect_escaped", result);
    }
}
