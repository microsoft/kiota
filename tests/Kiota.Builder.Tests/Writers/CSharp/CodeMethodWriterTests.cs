using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests;
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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
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
        var model = root.AddClass(new CodeClass {
            Name = ReturnTypeName,
            Kind = CodeClassKind.Model
        }).First();
        method.ReturnType = new CodeType {
            Name = ReturnTypeName,
            TypeDefinition = model,
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
            Name = "RequestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "urlTemplate",
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
        var complexTypeClass = root.AddClass(new CodeClass
        {
            Name = "SomeComplexType"
        }).First();
        dummyComplexCollection.Type = new CodeType {
            Name = "Complex",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            TypeDefinition = complexTypeClass
        };
        var dummyEnumProp = parentClass.AddProperty(new CodeProperty{
            Name = "dummyEnumCollection",
        }).First();
        var enumDefinition = root.AddEnum(new CodeEnum
        {
            Name = "EnumType"
        }).First();
        dummyEnumProp.Type = new CodeType {
            Name = "SomeEnum",
            TypeDefinition = enumDefinition
        };
        parentClass.AddProperty(new CodeProperty {
            Name = "definedInParent",
            Type = new CodeType {
                Name = "string"
            },
            OriginalPropertyFromBaseType = new CodeProperty {
                Name = "definedInParent",
                Type = new CodeType {
                    Name = "string"
                }
            }
        });
    }
    private CodeClass AddUnionTypeWrapper() {
        var complexType1 = root.AddClass(new CodeClass
        {
            Name = "ComplexType1",
            Kind = CodeClassKind.Model,
        }).First();
        var complexType2 = root.AddClass(new CodeClass
        {
            Name = "ComplexType2",
            Kind = CodeClassKind.Model,
        }).First();
        var unionTypeWrapper = root.AddClass(new CodeClass
        {
            Name = "UnionTypeWrapper",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeUnionType {
                Name = "UnionTypeWrapper",
            }
        }).First();
        var cType1 = new CodeType {
            Name = "ComplexType1",
            TypeDefinition = complexType1
        };
        var cType2 = new CodeType {
            Name = "ComplexType2",
            TypeDefinition = complexType2,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        var sType = new CodeType {
            Name = "string",
        };
        unionTypeWrapper.OriginalComposedType.AddType(cType1);
        unionTypeWrapper.OriginalComposedType.AddType(cType2);
        unionTypeWrapper.OriginalComposedType.AddType(sType);
        unionTypeWrapper.AddProperty(new CodeProperty {
            Name = "ComplexType1Value",
            Type = cType1,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty {
            Name = "ComplexType2Value",
            Type = cType2,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty {
            Name = "StringValue",
            Type = sType,
            Kind = CodePropertyKind.Custom
        });
        return unionTypeWrapper;
    }
    private void AddInheritanceClass() {
        (parentClass.StartBlock as ClassDeclaration).Inherits = new CodeType {
            Name = "someParentClass"
        };
    }
    private void AddRequestBodyParameters(bool useComplexTypeForBody = false) {
        var stringType = new CodeType {
            Name = "string",
        };
        var requestConfigClass = parentClass.AddInnerClass(new CodeClass {
            Name = "RequestConfig",
            Kind = CodeClassKind.RequestConfiguration,
        }).First();
        requestConfigClass.AddProperty(new() {
            Name = "h",
            Kind = CodePropertyKind.Headers,
            Type = stringType,
        },
        new () {
            Name = "q",
            Kind = CodePropertyKind.QueryParameters,
            Type = stringType,
        },
        new () {
            Name = "o",
            Kind = CodePropertyKind.Options,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter{
            Name = "b",
            Kind = CodeParameterKind.RequestBody,
            Type = useComplexTypeForBody ? new CodeType {
                Name = "SomeComplexTypeForRequestBody",
                TypeDefinition = root.AddClass(new CodeClass {
                    Name = "SomeComplexTypeForRequestBody",
                    Kind = CodeClassKind.Model,
                }).First(),
            } : stringType,
        });
        method.AddParameter(new CodeParameter{
            Name = "config",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType {
                Name = "RequestConfig",
                TypeDefinition = requestConfigClass,
                ActionOf = true,
            },
            Optional = true,
        });
        method.AddParameter(new CodeParameter{
            Name = "r",
            Kind = CodeParameterKind.ResponseHandler,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "c",
            Kind = CodeParameterKind.Cancellation,
            Type = stringType,
        });
    }
    [Fact]
    public void WritesRequestBuilder() {
        method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
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
        Assert.Contains("var requestInfo", result);
        Assert.Contains("var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>", result);
        Assert.Contains("{\"4XX\", Error4XX.CreateFromDiscriminatorValue},", result);
        Assert.Contains("{\"5XX\", Error5XX.CreateFromDiscriminatorValue},", result);
        Assert.Contains("{\"403\", Error403.CreateFromDiscriminatorValue},", result);
        Assert.Contains("SendAsync", result);
        Assert.Contains($"{ReturnTypeName}.CreateFromDiscriminatorValue", result);
        Assert.Contains(AsyncKeyword, result);
        Assert.Contains("await", result);
        Assert.Contains("cancellationToken", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForCollection() {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        var error4XX = root.AddClass(new CodeClass{
            Name = "Error4XX",
        }).First();
        method.AddErrorMapping("4XX", new CodeType {Name = "Error4XX", TypeDefinition = error4XX});
        AddRequestBodyParameters();
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo", result);
        Assert.Contains("var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>", result);
        Assert.Contains("{\"4XX\", Error4XX.CreateFromDiscriminatorValue},", result);
        Assert.Contains("SendCollectionAsync", result);
        Assert.Contains("return collectionResult.ToList()", result);
        Assert.Contains($"{ReturnTypeName}.CreateFromDiscriminatorValue", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntCreateDictionaryOnEmptyErrorMapping() {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("var errorMapping = new Dictionary<string, Func<IParsable>>", result);
        Assert.Contains("default", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyForUnionModels() {
        var wrapper = AddUnionTypeWrapper();
        var factoryMethod = wrapper.AddMethod(new CodeMethod{
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            DiscriminatorInformation = new() {
                DiscriminatorPropertyName = "@odata.type",
            },
            ReturnType = new CodeType {
                Name = "UnionTypeWrapper",
                TypeDefinition = wrapper,
            },
        }).First();
        factoryMethod.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType {
            Name = "ComplexType1",
            TypeDefinition = root.FindChildByName<CodeClass>("ComplexType1", true)
        });
        factoryMethod.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType {
            Name = "ComplexType2",
            TypeDefinition = root.FindChildByName<CodeClass>("ComplexType2", true)
        });
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode"
            }
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch {", result);
        Assert.Contains("var result = new UnionTypeWrapper()", result);
        Assert.Contains("if(\"#kiota.complexType1\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))", result);
        Assert.Contains("ComplexType1Value = new ComplexType1()", result);
        Assert.Contains("else if(parseNode.GetStringValue() is string stringValueValue)", result);
        Assert.Contains("StringValue = stringValueValue", result);
        Assert.Contains("parseNode.GetCollectionOfObjectValues<ComplexType2>(ComplexType2.CreateFromDiscriminatorValue)?.ToList() is List<ComplexType2> complexType2ValueValue", result);
        Assert.Contains("ComplexType2Value = complexType2ValueValue", result);
        Assert.Contains("return result", result);
        Assert.InRange(result.IndexOf("GetStringValue() is string stringValueValue", StringComparison.Ordinal), 0, result.IndexOf("GetCollectionOfObjectValues<ComplexType2>"));
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyForInheritedModels() {
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
        factoryMethod.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.Contains("return mappingValue switch {", result);
        Assert.Contains("\"ns.childmodel\" => new ChildModel()", result);
        Assert.Contains("_ => new ParentModel()", result);
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
        factoryMethod.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        Assert.Throws<InvalidOperationException>(() => writer.Write(factoryMethod));
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
        factoryMethod.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorInformation.DiscriminatorPropertyName = string.Empty;
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("var mappingValue = mappingValueNode?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch {", result);
        Assert.DoesNotContain("\"ns.childmodel\" => new ChildModel()", result);
        Assert.Contains("return new ParentModel()", result);
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
        factoryMethod.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("var mappingValue = mappingValueNode?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch {", result);
        Assert.DoesNotContain("\"ns.childmodel\" => new ChildModel()", result);
        Assert.Contains("return new ParentModel()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForCollections() {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("SendCollectionAsync", result);
        Assert.Contains("cancellationToken", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForScalar() {
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo = new RequestInformation", result);
        Assert.Contains("HttpMethod = Method.GET", result);
        Assert.Contains("UrlTemplate = ", result);
        Assert.Contains("PathParameters = ", result);
        Assert.Contains("if (config != null)", result);
        Assert.Contains("var requestConfig = new RequestConfig()", result);
        Assert.Contains("config.Invoke(requestConfig)", result);
        Assert.Contains("requestInfo.Headers.Add(\"Accept\", \"application/json\")", result);
        Assert.Contains("requestInfo.AddHeaders(requestConfig.H)", result);
        Assert.Contains("requestInfo.AddQueryParameters(requestConfig.Q)", result);
        Assert.Contains("requestInfo.AddRequestOptions(requestConfig.O)", result);
        Assert.Contains("SetContentFromScalar", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForParsable() {
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo = new RequestInformation", result);
        Assert.Contains("HttpMethod = Method.GET", result);
        Assert.Contains("UrlTemplate = ", result);
        Assert.Contains("PathParameters = ", result);
        Assert.Contains("if (config != null)", result);
        Assert.Contains("var requestConfig = new RequestConfig()", result);
        Assert.Contains("config.Invoke(requestConfig)", result);
        Assert.Contains("requestInfo.Headers.Add(\"Accept\", \"application/json\")", result);
        Assert.Contains("requestInfo.AddHeaders(requestConfig.H)", result);
        Assert.Contains("requestInfo.AddQueryParameters(requestConfig.Q)", result);
        Assert.Contains("requestInfo.AddRequestOptions(requestConfig.O)", result);
        Assert.Contains("SetContentFromParsable", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForCollection() {
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        method.AcceptedResponseTypes.Add("application/json");
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(".ToArray()", result);
        Assert.Contains("SetContentFromParsable", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesInheritedDeSerializerBody() {
        method.Kind = CodeMethodKind.Deserializer;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("base.", result);
        Assert.Contains("new", result);
    }
    [Fact]
    public void WritesDeSerializerBody() {
        method.Kind = CodeMethodKind.Deserializer;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("GetStringValue", result);
        Assert.Contains("GetCollectionOfPrimitiveValues", result);
        Assert.Contains("GetCollectionOfObjectValues", result);
        Assert.Contains("GetEnumValue", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void WritesInheritedSerializerBody() {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("base.Serialize(writer);", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesSerializerBody() {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("WriteStringValue", result);
        Assert.Contains("WriteCollectionOfPrimitiveValues", result);
        Assert.Contains("WriteCollectionOfObjectValues", result);
        Assert.Contains("WriteEnumValue", result);
        Assert.Contains("WriteAdditionalData(additionalData);", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesMethodAsyncDescription() {
        
        method.Description = MethodDescription;
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
        Assert.Contains("/// <summary>", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("<param name=", result);
        Assert.Contains("</param>", result);
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result); 
        Assert.Contains("</summary>", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
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
        Assert.DoesNotContain("@returns a Promise of", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive() {
        var codeMethodWriter = new CodeMethodWriter(new CSharpConventionService());
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
    private const string TaskPrefix = "Task<";
    private const string AsyncKeyword = "async";
    [Fact]
    public void WritesReturnType() {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"{AsyncKeyword} {TaskPrefix}{ReturnTypeName}> {MethodName.ToFirstCharacterUpperCase()}", result); // async default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType(){
        method.ReturnType.IsNullable = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("?", result);
    }
    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods() {
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain(TaskPrefix, result);
        Assert.DoesNotContain(AsyncKeyword, result);
    }
    [Fact]
    public void WritesPublicMethodByDefault() {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("public ", result);// public default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPrivateMethod() {
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("private ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesProtectedMethod() {
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("protected ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
        Assert.Contains("RequestAdapter", result);
        Assert.Contains("PathParameters", result);
        Assert.Contains("pathParam", result);
        Assert.Contains("return new", result);
    }
    [Fact]
    public void WritesConstructor() {
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.AddProperty(new CodeProperty {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"{propName.ToFirstCharacterUpperCase()} = {defaultValue}", result);
    }
    [Fact]
    public void WritesApiConstructor() {
        method.Kind = CodeMethodKind.ClientConstructor;
        var coreProp = parentClass.AddProperty(new CodeProperty {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
        }).First();
        coreProp.Type = new CodeType {
            Name = "RequestAdapter",
            IsExternal = true,
        };
        method.AddParameter(new CodeParameter {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        method.DeserializerModules = new() {"com.microsoft.kiota.serialization.Deserializer"};
        method.SerializerModules = new() {"com.microsoft.kiota.serialization.Serializer"};
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
            Name = "RequestAdapter",
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
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("EnableBackingStore", result);
    }
    [Fact]
    public void ThrowsOnGetter() {
        method.Kind = CodeMethodKind.Getter;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void ThrowsOnSetter() {
        method.Kind = CodeMethodKind.Setter;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
}    
