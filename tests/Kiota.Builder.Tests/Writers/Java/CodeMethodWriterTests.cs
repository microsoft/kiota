using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests;
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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
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
                Name = "string",
            }
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
        });
    }
    private void AddSerializationProperties() {
        parentClass.AddProperty(new CodeProperty {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType {
                Name = "string"
            },
            Getter = new CodeMethod {
                Name = "GetAdditionalData",
                ReturnType = new CodeType {
                    Name = "string"
                }
            },
            Setter = new CodeMethod {
                Name = "SetAdditionalData",
            }
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "dummyProp",
            Type = new CodeType {
                Name = "string"
            },
            Getter = new CodeMethod {
                Name = "GetDummyProp",
                ReturnType = new CodeType {
                    Name = "string"
                },
            },
            Setter = new CodeMethod {
                Name = "SetDummyProp",
            },
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "dummyColl",
            Type = new CodeType {
                Name = "string",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            },
            Getter = new CodeMethod {
                Name = "GetDummyColl",
                ReturnType = new CodeType {
                    Name = "string",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                },
            },
            Setter = new CodeMethod {
                Name = "SetDummyColl",
                ReturnType = new CodeType {
                    Name = "void",
                }
            },
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "dummyComplexColl",
            Type = new CodeType {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = new CodeClass {
                    Name = "SomeComplexType"
                }
            },
            Getter = new CodeMethod {
                Name = "GetDummyComplexColl",
            },
            Setter = new CodeMethod {
                Name = "SetDummyComplexColl",
            }
        });
        parentClass.AddProperty(new CodeProperty{
            Name = "dummyEnumCollection",
            Type = new CodeType {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum {
                    Name = "EnumType"
                }
            },
            Getter = new CodeMethod {
                Name = "GetDummyEnumCollection",
            },
            Setter = new CodeMethod {
                Name = "SetDummyEnumCollection",
            }
        });
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
            Name = "c",
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
        Assert.Contains("CompletableFuture<Void>", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
        Assert.Contains("final RequestInformation requestInfo", result);
        Assert.Contains("final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<>", result);
        Assert.Contains("put(\"4XX\", Error4XX::createFromDiscriminatorValue);", result);
        Assert.Contains("put(\"5XX\", Error5XX::createFromDiscriminatorValue);", result);
        Assert.Contains("put(\"403\", Error403::createFromDiscriminatorValue);", result);
        Assert.Contains("sendAsync", result);
        Assert.Contains("CompletableFuture.failedFuture(ex)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntCreateDictionaryOnEmptyErrorMapping() {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("final HashMap<String, Class<? extends Parsable>> errorMapping = new HashMap<String, Class<? extends Parsable>>", result);
        Assert.Contains("null", result);
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
        Assert.Contains("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.Contains("if (mappingValueNode != null) {", result);
        Assert.Contains("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.Contains("switch (mappingValue) {", result);
        Assert.Contains("case \"ns.childmodel\": return new ChildModel();", result);
        Assert.Contains("return new ParentModel()", result);
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
        Assert.DoesNotContain("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode != null) {", result);
        Assert.DoesNotContain("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
        Assert.Contains("return new ParentModel()", result);
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
        Assert.DoesNotContain("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode != null) {", result);
        Assert.DoesNotContain("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
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
        Assert.DoesNotContain("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode != null) {", result);
        Assert.DoesNotContain("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
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
        Assert.Contains("sendCollectionAsync", result);
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
        Assert.Contains("final RequestInformation requestInfo = new RequestInformation()", result);
        Assert.Contains("urlTemplate =", result);
        Assert.Contains("pathParameters =", result);
        Assert.Contains("httpMethod = HttpMethod.GET", result);
        Assert.Contains("requestInfo.addRequestHeader(\"Accept\", \"application/json\")", result);
        Assert.Contains("if (c != null)", result);
        Assert.Contains("final RequestConfig requestConfig = new RequestConfig()", result);
        Assert.Contains("c.accept(requestConfig)", result);
        Assert.Contains("addQueryParameters", result);
        Assert.Contains("addRequestHeaders", result);
        Assert.Contains("addRequestOptions", result);
        Assert.Contains("setContentFromScalar", result);
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
        Assert.Contains("final RequestInformation requestInfo = new RequestInformation()", result);
        Assert.Contains("urlTemplate =", result);
        Assert.Contains("pathParameters =", result);
        Assert.Contains("httpMethod = HttpMethod.GET", result);
        Assert.Contains("requestInfo.addRequestHeader(\"Accept\", \"application/json\")", result);
        Assert.Contains("if (c != null)", result);
        Assert.Contains("final RequestConfig requestConfig = new RequestConfig()", result);
        Assert.Contains("c.accept(requestConfig)", result);
        Assert.Contains("addQueryParameters", result);
        Assert.Contains("addRequestHeaders", result);
        Assert.Contains("addRequestOptions", result);
        Assert.Contains("setContentFromParsable", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorOverloadBody() {
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        method.OriginalMethod = method;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("final RequestInformation requestInfo = new RequestInformation()", result);
        Assert.DoesNotContain("httpMethod = HttpMethod.GET", result);
        Assert.DoesNotContain("final RequestConfig requestConfig = new RequestConfig()", result);
        Assert.DoesNotContain("c.accept(requestConfig)", result);
        Assert.DoesNotContain("addQueryParameters", result);
        Assert.DoesNotContain("addRequestHeaders", result);
        Assert.DoesNotContain("addRequestOptions", result);
        Assert.DoesNotContain("return requestInfo;", result);
        Assert.Contains("return methodName(b, c)", result);
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
        Assert.Contains("super.methodName()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesDeSerializerBody() {
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("getStringValue", result);
        Assert.Contains("getCollectionOfPrimitiveValues", result);
        Assert.Contains("getCollectionOfObjectValues", result);
        Assert.Contains("getEnumValue", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("super.serialize", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesSerializerBody() {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("writeStringValue", result);
        Assert.Contains("writeCollectionOfPrimitiveValues", result);
        Assert.Contains("writeCollectionOfObjectValues", result);
        Assert.Contains("writeEnumValue", result);
        Assert.Contains("writeAdditionalData(this.getAdditionalData());", result);
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
        Assert.Contains("/**", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("@param ", result);
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result); 
        Assert.Contains("@return a CompletableFuture of", result);
        Assert.Contains("*/", result);
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
        Assert.DoesNotContain("@return a CompletableFuture of", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive() {
        var codeMethodWriter = new CodeMethodWriter(new JavaConventionService());
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
    private const string TaskPrefix = "CompletableFuture<";
    [Fact]
    public void WritesReturnType() {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"{TaskPrefix}{ReturnTypeName}> {MethodName}", result);// async default
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
    public void WritesIndexer() {
        AddRequestProperties();
        method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
        method.OriginalIndexer = new CodeIndexer {
            Name = "idx",
            IndexType = new CodeType {
                Name = "int"
            },
            SerializationName = "collectionId"
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("collectionId", result);
        Assert.Contains("requestAdapter", result);
        Assert.Contains("pathParameters", result);
        Assert.Contains("id", result);
        Assert.Contains("return new", result);
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
        Assert.Contains("requestAdapter", result);
        Assert.Contains("pathParameters", result);
        Assert.Contains("pathParam", result);
        Assert.Contains("return new", result);
    }
    [Fact]
    public void WritesGetterToBackingStore() {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.getBackingStore().get(\"someProperty\")", result);
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
        Assert.Contains("if(value == null)", result);
        Assert.Contains(defaultValue, result);
    }
    [Fact]
    public void WritesSetterToBackingStore() {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.getBackingStore().set(\"someProperty\", value)", result);
    }
    [Fact]
    public void WritesGetterToField() {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty", result);
    }
    [Fact]
    public void WritesSetterToField() {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty = value", result);
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
                Name = "Map<String, String>"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"this.{propName} = {defaultValue}", result);
        Assert.Contains("new Map<String, String>(pathParameters)", result);
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"this.{propName} = {defaultValue}", result);
        Assert.Contains($"urlTplParams.put(\"request-raw-url\", rawUrl);", result);
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
        method.DeserializerModules = new() {"com.microsoft.kiota.serialization.Deserializer"};
        method.SerializerModules = new() {"com.microsoft.kiota.serialization.Serializer"};
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("registerDefaultSerializer", result);
        Assert.Contains("registerDefaultDeserializer", result);
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
            Name = "BackingStore",
            IsExternal = true,
        };
        method.AddParameter(backingStoreParam);
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("enableBackingStore", result);
    }
    [Fact]
    public void AccessorsTargetingEscapedPropertiesAreNotEscapedThemselves() {
        var model = root.AddClass(new CodeClass {
            Name = "SomeClass",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty {
            Name = "short",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
            Kind = CodePropertyKind.Custom,
        });
        ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        var getter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Getter));
        var setter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Setter));
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(getter);
        var result = tw.ToString();
        Assert.Contains("getShort", result);
        Assert.DoesNotContain("getShort_escaped", result);
        
        using var tw2 = new StringWriter();
        tempWriter.SetTextWriter(tw2);
        tempWriter.Write(setter);
        result = tw2.ToString();
        Assert.Contains("setShort", result);
        Assert.DoesNotContain("setShort_escaped", result);
    }
}
