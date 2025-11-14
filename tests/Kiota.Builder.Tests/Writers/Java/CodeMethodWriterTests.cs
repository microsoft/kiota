using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Java;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Java;

public sealed class CodeMethodWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private CodeMethod method;
    private CodeClass parentClass;
    private readonly CodeNamespace root;
    private const string ExecuterExceptionVar = "executionException";
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";
    private const string MethodDescription = "some description & some special character <>";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";
    public CodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    private void setup(bool withInheritance = false)
    {
        if (parentClass != null)
            throw new InvalidOperationException("setup() must only be called once");
        CodeClass baseClass = default;
        if (withInheritance)
        {
            baseClass = root.AddClass(new CodeClass
            {
                Name = "SomeParentClass",
            }).First();
            baseClass.AddProperty(new CodeProperty
            {
                Name = "definedInParent",
                Type = new CodeType
                {
                    Name = "String"
                },
                Kind = CodePropertyKind.Custom,
            });
        }
        parentClass = new CodeClass
        {
            Name = "ParentClass"
        };
        if (withInheritance)
        {
            parentClass.StartBlock.Inherits = new CodeType
            {
                Name = "SomeParentClass",
                TypeDefinition = baseClass
            };
        }
        root.AddClass(parentClass);
        method = new CodeMethod
        {
            Name = MethodName,
            ReturnType = new CodeType
            {
                Name = ReturnTypeName
            }
        };
        parentClass.AddMethod(method);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    private void AddRequestProperties()
    {
        parentClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseRequestBuilder",
            IsExternal = true,
        };
        parentClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "String",
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "String",
            }
        });
    }
    private void AddSerializationProperties()
    {
        parentClass.AddProperty(new CodeProperty
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = "String"
            },
            Getter = new CodeMethod
            {
                Name = "getAdditionalData",
                ReturnType = new CodeType
                {
                    Name = "String"
                }
            },
            Setter = new CodeMethod
            {
                Name = "setAdditionalData",
                ReturnType = new CodeType
                {
                    Name = "String"
                }
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyProp",
            Type = new CodeType
            {
                Name = "String"
            },
            Getter = new CodeMethod
            {
                Name = "getDummyProp",
                ReturnType = new CodeType
                {
                    Name = "String"
                },
            },
            Setter = new CodeMethod
            {
                Name = "setDummyProp",
                ReturnType = new CodeType
                {
                    Name = "void"
                }
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "noAccessors",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "String"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyColl",
            Type = new CodeType
            {
                Name = "String",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            },
            Getter = new CodeMethod
            {
                Name = "getDummyColl",
                ReturnType = new CodeType
                {
                    Name = "String",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                },
            },
            Setter = new CodeMethod
            {
                Name = "setDummyColl",
                ReturnType = new CodeType
                {
                    Name = "void",
                }
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyComplexColl",
            Type = new CodeType
            {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = new CodeClass
                {
                    Name = "SomeComplexType"
                }
            },
            Getter = new CodeMethod
            {
                Name = "getDummyComplexColl",
                ReturnType = new CodeType
                {
                    Name = "String",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                },
            },
            Setter = new CodeMethod
            {
                Name = "setDummyComplexColl",
                ReturnType = new CodeType
                {
                    Name = "void"
                }
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyEnumCollection",
            Type = new CodeType
            {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum
                {
                    Name = "EnumType"
                }
            },
            Getter = new CodeMethod
            {
                Name = "getDummyEnumCollection",
                ReturnType = new CodeType
                {
                    Name = "String"
                },
            },
            Setter = new CodeMethod
            {
                Name = "setDummyEnumCollection",
                ReturnType = new CodeType
                {
                    Name = "void"
                }
            }
        });
    }
    private CodeClass AddUnionTypeWrapper()
    {
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
            OriginalComposedType = new CodeUnionType
            {
                Name = "UnionTypeWrapper",
            },
            DiscriminatorInformation = new()
            {
                DiscriminatorPropertyName = "@odata.type",
            },
        }).First();
        var cType1 = new CodeType
        {
            Name = "ComplexType1",
            TypeDefinition = complexType1
        };
        var cType2 = new CodeType
        {
            Name = "ComplexType2",
            TypeDefinition = complexType2,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        var sType = new CodeType
        {
            Name = "String",
        };
        unionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType
        {
            Name = "ComplexType1",
            TypeDefinition = cType1
        });
        unionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType
        {
            Name = "ComplexType2",
            TypeDefinition = cType2
        });
        unionTypeWrapper.OriginalComposedType.AddType(cType1);
        unionTypeWrapper.OriginalComposedType.AddType(cType2);
        unionTypeWrapper.OriginalComposedType.AddType(sType);
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType1Value",
            Type = cType1,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setComplexType1Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getComplexType1Value",
                ReturnType = cType1,
                Kind = CodeMethodKind.Getter,
            }
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType2Value",
            Type = cType2,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setComplexType2Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getComplexType2Value",
                ReturnType = cType2,
                Kind = CodeMethodKind.Getter,
            }
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "StringValue",
            Type = sType,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setStringValue",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getStringValue",
                ReturnType = sType,
                Kind = CodeMethodKind.Getter,
            }
        });
        return unionTypeWrapper;
    }
    private CodeClass AddIntersectionTypeWrapper()
    {
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
        var complexType3 = root.AddClass(new CodeClass
        {
            Name = "ComplexType3",
            Kind = CodeClassKind.Model,
        }).First();
        var intersectionTypeWrapper = root.AddClass(new CodeClass
        {
            Name = "IntersectionTypeWrapper",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeIntersectionType
            {
                Name = "IntersectionTypeWrapper",
            },
            DiscriminatorInformation = new()
            {
                DiscriminatorPropertyName = "@odata.type",
            },
        }).First();
        var cType1 = new CodeType
        {
            Name = "ComplexType1",
            TypeDefinition = complexType1
        };
        var cType2 = new CodeType
        {
            Name = "ComplexType2",
            TypeDefinition = complexType2,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        var cType3 = new CodeType
        {
            Name = "ComplexType3",
            TypeDefinition = complexType3
        };
        intersectionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType
        {
            Name = "ComplexType1",
            TypeDefinition = cType1
        });
        intersectionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType
        {
            Name = "ComplexType2",
            TypeDefinition = cType2
        });
        intersectionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType3", new CodeType
        {
            Name = "ComplexType3",
            TypeDefinition = cType3
        });
        var sType = new CodeType
        {
            Name = "String",
        };
        intersectionTypeWrapper.OriginalComposedType.AddType(cType1);
        intersectionTypeWrapper.OriginalComposedType.AddType(cType2);
        intersectionTypeWrapper.OriginalComposedType.AddType(cType3);
        intersectionTypeWrapper.OriginalComposedType.AddType(sType);
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType1Value",
            Type = cType1,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setComplexType1Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getComplexType1Value",
                ReturnType = cType1,
                Kind = CodeMethodKind.Getter,
            }
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType2Value",
            Type = cType2,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setComplexType2Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getComplexType2Value",
                ReturnType = cType2,
                Kind = CodeMethodKind.Getter,
            }
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType3Value",
            Type = cType3,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setComplexType3Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getComplexType3Value",
                ReturnType = cType3,
                Kind = CodeMethodKind.Getter,
            }
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "StringValue",
            Type = sType,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setStringValue",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getStringValue",
                ReturnType = sType,
                Kind = CodeMethodKind.Getter,
            }
        });
        return intersectionTypeWrapper;
    }
    private void AddRequestBodyParameters(bool useComplexTypeForBody = false)
    {
        var stringType = new CodeType
        {
            Name = "String",
        };
        var requestConfigClass = parentClass.AddInnerClass(new CodeClass
        {
            Name = "RequestConfig",
            Kind = CodeClassKind.RequestConfiguration,
        }).First();
        requestConfigClass.AddProperty(new()
        {
            Name = "h",
            Kind = CodePropertyKind.Headers,
            Type = stringType,
        },
        new()
        {
            Name = "q",
            Kind = CodePropertyKind.QueryParameters,
            Type = stringType,
        },
        new()
        {
            Name = "o",
            Kind = CodePropertyKind.Options,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "b",
            Kind = CodeParameterKind.RequestBody,
            Type = useComplexTypeForBody ? new CodeType
            {
                Name = "SomeComplexTypeForRequestBody",
                TypeDefinition = root.AddClass(new CodeClass
                {
                    Name = "SomeComplexTypeForRequestBody",
                    Kind = CodeClassKind.Model,
                }).First(),
            } : stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "c",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "RequestConfig",
                TypeDefinition = requestConfigClass,
                ActionOf = true,
            },
            Optional = true,
        });
    }
    [Fact]
    public void WritesNullableVoidTypeForExecutor()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType
        {
            Name = "void",
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("void", result);
        Assert.DoesNotContain("@jakarta.annotation", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestBuilder()
    {
        setup();
        method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesRequestBodiesThrowOnNullHttpMethod()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.Kind = CodeMethodKind.RequestGenerator;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesRequestExecutorBody()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        var error4XX = root.AddClass(new CodeClass
        {
            Name = "Error4XX",
        }).First();
        var error5XX = root.AddClass(new CodeClass
        {
            Name = "Error5XX",
        }).First();
        var error401 = root.AddClass(new CodeClass
        {
            Name = "Error401",
        }).First();
        method.AddErrorMapping("4XX", new CodeType { Name = "Error4XX", TypeDefinition = error4XX });
        method.AddErrorMapping("5XX", new CodeType { Name = "Error5XX", TypeDefinition = error5XX });
        method.AddErrorMapping("401", new CodeType { Name = "Error401", TypeDefinition = error401 });
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("final RequestInformation requestInfo", result);
        Assert.Contains("final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>", result);
        Assert.Contains("put(\"4XX\", Error4XX::createFromDiscriminatorValue);", result);
        Assert.Contains("put(\"5XX\", Error5XX::createFromDiscriminatorValue);", result);
        Assert.Contains("put(\"401\", Error401::createFromDiscriminatorValue);", result);
        Assert.Contains("send", result);
        Assert.Contains("@return", result);
        Assert.Contains("@link", result);
        Assert.Contains("@throws", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorOverloadBody()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.OriginalMethod = method;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("final RequestInformation requestInfo", result);
        Assert.DoesNotContain("final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>", result);
        Assert.DoesNotContain("put(\"4XX\", Error4XX::createFromDiscriminatorValue);", result);
        Assert.DoesNotContain("put(\"5XX\", Error5XX::createFromDiscriminatorValue);", result);
        Assert.DoesNotContain("put(\"401\", Error401::createFromDiscriminatorValue);", result);
        Assert.DoesNotContain("sendAsync", result);
        Assert.DoesNotContain($"java.util.concurrent.CompletableFuture<Somecustomtype> {ExecuterExceptionVar} = new java.util.concurrent.CompletableFuture<Somecustomtype>();", result);
        Assert.DoesNotContain($"{ExecuterExceptionVar}.completeExceptionally(ex);", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntCreateDictionaryOnEmptyErrorMapping()
    {
        setup();
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
    public void WritesModelFactoryBodyForUnionModels()
    {
        setup();
        var wrapper = AddUnionTypeWrapper();
        var factoryMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "UnionTypeWrapper",
                TypeDefinition = wrapper,
            },
            IsAsync = false,
            IsStatic = true,
        }).First();
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode"
            }
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.Contains("if (mappingValueNode != null) {", result);
        Assert.Contains("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
        Assert.Contains("final UnionTypeWrapper result = new UnionTypeWrapper()", result);
        Assert.Contains("if (\"#kiota.complexType1\".equalsIgnoreCase(mappingValue)) {", result);
        Assert.Contains("result.setComplexType1Value(new ComplexType1())", result);
        Assert.Contains("if (parseNode.getStringValue() != null) {", result);
        Assert.Contains("result.setStringValue(parseNode.getStringValue())", result);
        Assert.Contains("else if (parseNode.getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue) != null) {", result);
        Assert.Contains("result.setComplexType2Value(parseNode.getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue))", result);
        Assert.Contains("return result", result);
        Assert.DoesNotContain("return new UnionTypeWrapper()", result);
        AssertExtensions.Before("parseNode.getStringValue()", "getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue)", result);
        AssertExtensions.OutsideOfBlock("if (parseNode.getStringValue() != null) ", "if (\"#kiota.complexType1\".equalsIgnoreCase(mappingValue))", result);
        AssertExtensions.OutsideOfBlock("else if (parseNode.getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue) != null", "if (\"#kiota.complexType1\".equalsIgnoreCase(mappingValue))", result);
        AssertExtensions.OutsideOfBlock("return result", "mappingValueNode != null", result);
        AssertExtensions.OutsideOfBlock("result = new UnionTypeWrapper()", "mappingValueNode != null", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyForIntersectionModels()
    {
        setup();
        var wrapper = AddIntersectionTypeWrapper();
        var factoryMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "IntersectionTypeWrapper",
                TypeDefinition = wrapper,
            },
            IsAsync = false,
            IsStatic = true,
        }).First();
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode"
            }
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode != null) {", result);
        Assert.DoesNotContain("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if mappingValue != null {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
        Assert.Contains("final IntersectionTypeWrapper result = new IntersectionTypeWrapper()", result);
        Assert.DoesNotContain("if (\"#kiota.complexType1\".equalsIgnoreCase(mappingValue)) {", result);
        Assert.Contains("result.setComplexType1Value(new ComplexType1())", result);
        Assert.Contains("result.setComplexType3Value(new ComplexType3())", result);
        Assert.Contains("if (parseNode.getStringValue() != null) {", result);
        Assert.Contains("result.setStringValue(parseNode.getStringValue())", result);
        Assert.Contains("else if (parseNode.getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue) != null) {", result);
        Assert.Contains("result.setComplexType2Value(parseNode.getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue))", result);
        Assert.Contains("return result", result);
        Assert.DoesNotContain("return new IntersectionTypeWrapper()", result);
        AssertExtensions.Before("parseNode.getStringValue()", "getCollectionOfObjectValues(ComplexType2::createFromDiscriminatorValue)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBody()
    {
        setup();
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "ParentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "ChildModel",
            Kind = CodeClassKind.Model,
        }).First();
        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "ParentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "ParentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                TypeDefinition = new CodeClass
                {
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
    public void WritesModelSplitFactoryBody()
    {
        setup();
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "ParentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "ChildModel",
            Kind = CodeClassKind.Model,
        }).First();
        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "ParentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "ParentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        var factoryOverloadMethod = factoryMethod.Clone() as CodeMethod;
        factoryOverloadMethod.Access = AccessModifier.Private;
        factoryOverloadMethod.Name += "_1";
        factoryOverloadMethod.OriginalMethod = factoryMethod;
        factoryOverloadMethod.RemoveParametersByKind(CodeParameterKind.ParseNode);
        factoryOverloadMethod.AddParameter(new CodeParameter
        {
            Name = "value",
            Type = new CodeType
            {
                Name = "String",
                IsNullable = true,
                IsExternal = true,
            },
            Optional = false,
        });
        parentModel.AddMethod(factoryOverloadMethod);
        Enumerable.Range(0, 1500).ToList().ForEach(x => parentModel.DiscriminatorInformation.AddDiscriminatorMapping($"#microsoft.graph.{x}", new CodeType
        {
            Name = $"microsoft.graph.{x}",
            TypeDefinition = childModel,
        }));
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                TypeDefinition = new CodeClass
                {
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
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
        Assert.Contains("final ParentModel factory_1_result = factory_1(mappingValue);", result);
        Assert.Contains("if (factory_1_result != null) {", result);
        Assert.Contains("return new ParentModel()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelSplitFactoryOverloadBody()
    {
        setup();
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
            IsStatic = true,
        }).First();
        var factoryOverloadMethod = factoryMethod.Clone() as CodeMethod;
        factoryOverloadMethod.Access = AccessModifier.Private;
        factoryOverloadMethod.Name += "_1";
        factoryOverloadMethod.OriginalMethod = factoryMethod;
        factoryOverloadMethod.RemoveParametersByKind(CodeParameterKind.ParseNode);
        factoryOverloadMethod.AddParameter(new CodeParameter
        {
            Name = "value",
            Type = new CodeType
            {
                Name = "String",
                IsNullable = true,
                IsExternal = true,
            },
            Optional = false,
        });
        parentModel.AddMethod(factoryOverloadMethod);
        Enumerable.Range(0, 1500).Select(static x => $"Foo{x}").ToList().ForEach(x => parentModel.DiscriminatorInformation.AddDiscriminatorMapping($"#microsoft.graph.{x}", new CodeType
        {
            Name = $"microsoft.graph.{x}",
            TypeDefinition = childModel,
        }));
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                TypeDefinition = new CodeClass
                {
                    Name = "ParseNode",
                },
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryOverloadMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("final ParseNode mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode != null) {", result);
        Assert.DoesNotContain("final String mappingValue = mappingValueNode.getStringValue()", result);
        Assert.Contains("switch (value) {", result);
        Assert.Contains("case \"#microsoft.graph.Foo535\": return new childModel();", result);
        Assert.DoesNotContain("final ParentModel factory_1_result = factory_1(mappingValue);", result);
        Assert.DoesNotContain("if (factory_1_result != null) {", result);
        Assert.DoesNotContain("return new ParentModel()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnMissingParameter()
    {
        setup();
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
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        Assert.Throws<InvalidOperationException>(() => writer.Write(factoryMethod));
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyPropertyName()
    {
        setup();
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "ParentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass
        {
            Name = "ChildModel",
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
                Name = "ParentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "ChildModel",
            TypeDefinition = childModel,
        });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = string.Empty;
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                TypeDefinition = new CodeClass
                {
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
    public void DoesntWriteFactorySwitchOnEmptyMappings()
    {
        setup();
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "ParentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "ParentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                TypeDefinition = new CodeClass
                {
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
    public void WritesRequestGeneratorBodyForMultipart()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.Parameters.First(static x => x.IsOfKind(CodeParameterKind.RequestBody)).Type = new CodeType { Name = "MultipartBody", IsExternal = true };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("setContentFromParsable", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForCollections()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("sendCollection", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForScalar()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters)", result);
        Assert.Contains("requestInfo.headers.tryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("configure(c", result);
        Assert.Contains("::new", result);
        Assert.Contains("x -> x.q", result);
        Assert.Contains("setContentFromScalar", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForScalarCollection()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.AcceptedResponseTypes.Add("application/json");
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters)", result);
        Assert.Contains("requestInfo.headers.tryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("configure(c", result);
        Assert.Contains("::new", result);
        Assert.Contains("x -> x.q", result);
        Assert.Contains("setContentFromScalarCollection", result);
        Assert.Contains("toArray", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForParsable()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters)", result);
        Assert.Contains("requestInfo.headers.tryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("configure(c", result);
        Assert.Contains("::new", result);
        Assert.Contains("x -> x.q", result);
        Assert.Contains("setContentFromParsable", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyWhenUrlTemplateIsOverrode()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        method.AcceptedResponseTypes.Add("application/json");
        method.UrlTemplateOverride = "{baseurl+}/foo/bar";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, \"{baseurl+}/foo/bar\", pathParameters)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorOverloadBody()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        method.OriginalMethod = method;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters)", result);
        Assert.DoesNotContain("configure(c", result);
        Assert.DoesNotContain("::new", result);
        Assert.DoesNotContain("x -> x.q", result);
        Assert.DoesNotContain("return requestInfo;", result);
        Assert.Contains("return methodName(b, c)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesUnionDeSerializerBody()
    {
        setup();
        var wrapper = AddUnionTypeWrapper();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "getFieldDeserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "Map<String, java.util.function.Consumer<ParseNode>>",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("final UnionTypeWrapper res =", result);
        Assert.Contains("this.getComplexType1Value() != null", result);
        Assert.Contains("return this.getComplexType1Value().getFieldDeserializers()", result);
        Assert.Contains("new HashMap<String, java.util.function.Consumer<ParseNode>>()", result);
        AssertExtensions.Before("return this.getComplexType1Value().getFieldDeserializers()", "new HashMap<String, java.util.function.Consumer<ParseNode>>", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIntersectionDeSerializerBody()
    {
        setup();
        var wrapper = AddIntersectionTypeWrapper();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "GetFieldDeserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "Map<String, java.util.function.Consumer<ParseNode>>",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("final IntersectionTypeWrapper res =", result);
        Assert.Contains("this.getComplexType1Value() != null || this.getComplexType3Value() != null", result);
        Assert.Contains("return ParseNodeHelper.mergeDeserializersForIntersectionWrapper(this.getComplexType1Value(), this.getComplexType3Value())", result);
        Assert.Contains("new HashMap<String, java.util.function.Consumer<ParseNode>>()", result);
        AssertExtensions.Before("return ParseNodeHelper.mergeDeserializersForIntersectionWrapper(this.getComplexType1Value(), this.getComplexType3Value())", "new HashMap<String, java.util.function.Consumer<ParseNode>>()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyKnownRequestBodyType()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = new JavaConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.RequestBodyContentType = "application/json";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("setStreamContent", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/json", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyUnknownRequestBodyType()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = new JavaConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.AddParameter(new CodeParameter
        {
            Name = "requestContentType",
            Type = new CodeType()
            {
                Name = "string",
                IsExternal = true,
            },
            Kind = CodeParameterKind.RequestBodyContentType,
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("setStreamContent", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("application/json", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(", requestContentType", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesInheritedDeSerializerBody()
    {
        setup(true);
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("super.methodName()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesDeSerializerBody()
    {
        setup();
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
    public void WritesInheritedSerializerBody()
    {
        setup(true);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("super.serialize", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesUnionSerializerBody()
    {
        setup();
        var wrapper = AddUnionTypeWrapper();
        var serializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Serializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "void",
            },
        }).First();
        serializationMethod.AddParameter(new CodeParameter
        {
            Name = "writer",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = "SerializationWriter"
            }
        });
        writer.Write(serializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("super.serialize(writer)", result);
        Assert.Contains("if (this.getComplexType1Value() != null) {", result);
        Assert.Contains("writer.writeObjectValue(null, this.getComplexType1Value())", result);
        Assert.Contains("this.getStringValue() != null", result);
        Assert.Contains("writer.writeStringValue(null, this.getStringValue())", result);
        Assert.Contains("this.getComplexType2Value() != null", result);
        Assert.Contains("writer.writeCollectionOfObjectValues(null, this.getComplexType2Value())", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIntersectionSerializerBody()
    {
        setup();
        var wrapper = AddIntersectionTypeWrapper();
        var serializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Serializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "void",
            },
        }).First();
        serializationMethod.AddParameter(new CodeParameter
        {
            Name = "writer",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = "SerializationWriter"
            }
        });
        writer.Write(serializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("super.serialize(writer)", result);
        Assert.DoesNotContain("if(this.getComplexType1Value() != null) {", result);
        Assert.Contains("writer.writeObjectValue(null, this.getComplexType1Value(), this.getComplexType3Value())", result);
        Assert.Contains("(this.getStringValue() != null)", result);
        Assert.Contains("writer.writeStringValue(null, this.getStringValue())", result);
        Assert.Contains("(this.getComplexType2Value() != null)", result);
        Assert.Contains("writer.writeCollectionOfObjectValues(null, this.getComplexType2Value())", result);
        AssertExtensions.Before("writer.writeStringValue(null, this.getStringValue())", "writer.writeObjectValue(null, this.getComplexType1Value(), this.getComplexType3Value())", result);
        AssertExtensions.Before("writer.writeCollectionOfObjectValues(null, this.getComplexType2Value())", "writer.writeObjectValue(null, this.getComplexType1Value(), this.getComplexType3Value())", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesSerializerBody()
    {
        setup();
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("writeStringValue", result);
        Assert.Contains("writeCollectionOfPrimitiveValues", result);
        Assert.Contains("writeCollectionOfObjectValues", result);
        Assert.Contains("writeEnumValue", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("writeAdditionalData(this.getAdditionalData());", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesMethodSyncDescription()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                DescriptionTemplate = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "String"
            }
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("@return a CompletableFuture of", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesMethodDescriptionLink()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.Documentation.DocumentationLabel = "see more";
        method.Documentation.DocumentationLink = new("https://foo.org/docs");
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                DescriptionTemplate = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "String"
            }
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("@see <a href=", result);
        // special xml characters are escaped for javadoc generation
        Assert.DoesNotContain("some description & some special character <>", result);
        Assert.Contains("some description", result);
        Assert.Contains("some special character", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive()
    {
        setup();
        var codeMethodWriter = new CodeMethodWriter(new JavaConventionService());
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
        var originalParent = method.Parent;
        method.Parent = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
        method.Parent = originalParent;
    }
    [Fact]
    public void ThrowsIfParentIsNotClass()
    {
        setup();
        method.Parent = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesReturnType()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"{ReturnTypeName} {MethodName}", result);// async default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPublicMethodByDefault()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("public ", result);// public default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPrivateMethod()
    {
        setup();
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("private ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesProtectedMethod()
    {
        setup();
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("protected ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIndexer()
    {
        setup();
        AddRequestProperties();
        method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
        method.OriginalIndexer = new CodeIndexer
        {
            Name = "idx",
            ReturnType = new CodeType
            {
                Name = "String"
            },
            IndexParameter = new()
            {
                Name = "id",
                SerializationName = "collectionId",
                Type = new CodeType
                {
                    Name = "int"
                },
            }
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
    public void WritesPathParameterRequestBuilder()
    {
        setup();
        AddRequestProperties();
        method.Kind = CodeMethodKind.RequestBuilderWithParameters;
        method.AddParameter(new CodeParameter
        {
            Name = "pathParam",
            Kind = CodeParameterKind.Path,
            Type = new CodeType
            {
                Name = "String"
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
    public void WritesGetterToBackingStore()
    {
        setup();
        parentClass.GetGreatestGrandparent().AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.backingStore.get(\"someProperty\")", result);
    }
    [Fact]
    public void WritesGetterToBackingStoreWithNonnullProperty()
    {
        setup();
        method.AddAccessedProperty();
        parentClass.GetGreatestGrandparent().AddBackingStoreProperty();
        method.AccessedProperty.Type = new CodeType
        {
            Name = "String",
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
    public void WritesSetterToBackingStore()
    {
        setup();
        parentClass.GetGreatestGrandparent().AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.backingStore.set(\"someProperty\", value)", result);
    }
    [Fact]
    public void WritesGetterToField()
    {
        setup();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty", result);
    }
    [Fact]
    public void WritesSetterToField()
    {
        setup();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty = value", result);
    }
    [Fact]
    public void WritesGetterToFieldWithEnumSetParameter()
    {
        setup();
        var codeEnumType = new CodeType
        {
            Name = "customEnum",
            TypeDefinition = new CodeEnum
            {
                Name = "customEnumType",
                Flags = true
            }
        };
        method.AccessedProperty = new CodeProperty
        {
            Name = "someProperty",
            Type = codeEnumType
        };
        (method.Parent as CodeClass)?.AddProperty(method.AccessedProperty);
        method.Kind = CodeMethodKind.Getter;
        method.ReturnType = codeEnumType;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty", result);
        Assert.Contains("EnumSet<", result);
    }
    [Fact]
    public void WritePeriodAndDurationSetterToField()
    {
        setup();
        method.AccessedProperty = new CodeProperty
        {
            Name = "someProperty",
            Type = new CodeType
            {
                Name = "PeriodAndDuration",
            },
        };
        (method.Parent as CodeClass)?.AddProperty(method.AccessedProperty);
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty = PeriodAndDuration.ofPeriodAndDuration(value);", result);
    }
    [Fact]
    public void WritesConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "\"someVal\"";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "String"
            }
        });
        var defaultValueNull = "\"null\"";
        var nullPropName = "propWithDefaultNullValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = nullPropName,
            DefaultValue = defaultValueNull,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "int",
                IsNullable = true
            }
        });
        var defaultValueBool = "\"true\"";
        var boolPropName = "propWithDefaultBoolValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = boolPropName,
            DefaultValue = defaultValueBool,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "boolean",
                IsNullable = true
            }
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = "Map<String, String>"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name, result);
        Assert.Contains($"this.set{propName.ToFirstCharacterUpperCase()}({defaultValue})", result);
        Assert.Contains($"this.set{nullPropName.ToFirstCharacterUpperCase()}({defaultValueNull.TrimQuotes()})", result);
        Assert.Contains($"this.set{boolPropName.ToFirstCharacterUpperCase()}({defaultValueBool.TrimQuotes()})", result);
        Assert.Contains("super", result);
    }
    [Fact]
    public void WritesWithUrl()
    {
        setup();
        method.Kind = CodeMethodKind.RawUrlBuilder;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.AddParameter(new CodeParameter
        {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType
            {
                Name = "String"
            },
        });
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        AddRequestProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"return new {parentClass.Name}", result);
    }
    [Fact]
    public void DoesNotWriteConstructorWithDefaultFromComposedType()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "\"Test Value\"";
        var propName = "size";
        var unionTypeWrapper = root.AddClass(new CodeClass
        {
            Name = "UnionTypeWrapper",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeUnionType
            {
                Name = "UnionTypeWrapper",
            },
            DiscriminatorInformation = new()
            {
                DiscriminatorPropertyName = "@odata.type",
            },
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = unionTypeWrapper }
        });
        var sType = new CodeType
        {
            Name = "String",
        };
        var arrayType = new CodeType
        {
            Name = "array",
        };
        unionTypeWrapper.OriginalComposedType.AddType(sType);
        unionTypeWrapper.OriginalComposedType.AddType(arrayType);

        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name, result);
        Assert.DoesNotContain(defaultValue, result);//ensure the composed type is not referenced
    }
    [Fact]
    public void WritesRawUrlConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.RawUrlConstructor;
        var defaultValue = "\"someVal\"";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "String"
            }
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType
            {
                Name = "String"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name, result);
        Assert.Contains($"this.set{propName.ToFirstCharacterUpperCase()}({defaultValue})", result);
        Assert.Contains("super", result);
    }
    [Fact]
    public void WritesApiConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.ClientConstructor;
        method.BaseUrl = "https://graph.microsoft.com/v1.0";
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "Dictionary<string, string>",
                IsExternal = true,
            }
        });
        var coreProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "HttpCore",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        method.DeserializerModules = new() { "com.microsoft.kiota.serialization.Deserializer" };
        method.SerializerModules = new() { "com.microsoft.kiota.serialization.Serializer" };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name, result);
        Assert.Contains("registerDefaultSerializer", result);
        Assert.Contains("registerDefaultDeserializer", result);
        Assert.Contains($"put(\"baseurl\", core.getBaseUrl())", result);
        Assert.Contains($"setBaseUrl(\"{method.BaseUrl}\")", result);
    }
    [Fact]
    public void WritesApiConstructorWithBackingStore()
    {
        setup();
        method.Kind = CodeMethodKind.ClientConstructor;
        var coreProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "HttpCore",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        var backingStoreParam = new CodeParameter
        {
            Name = "backingStore",
            Kind = CodeParameterKind.BackingStore,
            Type = new CodeType
            {
                Name = "BackingStore",
                IsExternal = true,
            }
        };
        method.AddParameter(backingStoreParam);
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("enableBackingStore", result);
    }
    [Fact]
    public void WritesQueryParametersExtractor()
    {
        setup();
        method.Kind = CodeMethodKind.QueryParametersMapper;
        var defaultValue = "\"someVal\"";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.QueryParameters;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.QueryParameter,
            Type = new CodeType
            {
                Name = "String"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("allQueryParams.put(\"propWithDefaultValue\", propWithDefaultValue);", result);
    }
    [Fact]
    public async Task AccessorsTargetingEscapedPropertiesAreNotEscapedThemselvesAsync()
    {
        setup();
        var model = root.AddClass(new CodeClass
        {
            Name = "SomeClass",
            Kind = CodeClassKind.Model
        }).First();
        model.AddProperty(new CodeProperty
        {
            Name = "short",
            Type = new CodeType { Name = "String" },
            Access = AccessModifier.Public,
            Kind = CodePropertyKind.Custom,
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
        var getter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Getter));
        var setter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Setter));
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(getter);
        var result = tw.ToString();
        Assert.Contains("getShort", result);
        Assert.DoesNotContain("getShort_escaped", result);

        await using var tw2 = new StringWriter();
        tempWriter.SetTextWriter(tw2);
        tempWriter.Write(setter);
        result = tw2.ToString();
        Assert.Contains("setShort", result);
        Assert.DoesNotContain("setShort_escaped", result);
    }
    [Fact]
    public void DoesntWriteReadOnlyPropertiesInSerializerBody()
    {
        setup(true);
        method.Kind = CodeMethodKind.Serializer;
        AddSerializationProperties();
        parentClass.AddProperty(new CodeProperty
        {
            Name = "readOnlyProperty",
            ReadOnly = true,
            Type = new CodeType
            {
                Name = "String",
            },
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("readOnlyProperty", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesDeprecationInformation()
    {
        setup();
        method.Deprecation = new("This method is deprecated", DateTimeOffset.Parse("2020-01-01T00:00:00Z"), DateTimeOffset.Parse("2021-01-01T00:00:00Z"), "v2.0");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("This method is deprecated", result);
        Assert.Contains("2020-01-01", result);
        Assert.Contains("2021-01-01", result);
        Assert.Contains("v2.0", result);
        Assert.Contains("@Deprecated", result);
    }
    [Fact]
    public void WritesDeprecationInformationFromBuilder()
    {
        setup();
        var newMethod = method.Clone() as CodeMethod;
        newMethod.Name = "NewAwesomeMethod";// new method replacement
        method.Deprecation = new("This method is obsolete. Use NewAwesomeMethod instead.", IsDeprecated: true, TypeReferences: new() { { "TypeName", new CodeType { TypeDefinition = newMethod, IsExternal = false } } });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("This method is obsolete. Use NewAwesomeMethod instead.", result);
    }
    [Fact]
    public void WritesMessageOverrideOnPrimary()
    {
        // Given
        parentClass = root.AddClass(new CodeClass
        {
            Name = "parentClass",
            IsErrorDefinition = true,
            Kind = CodeClassKind.Model,
        }).First();
        var prop1 = parentClass.AddProperty(new CodeProperty
        {
            Name = "prop1",
            Kind = CodePropertyKind.Custom,
            IsPrimaryErrorMessage = true,
            Type = new CodeType
            {
                Name = "string",
            },
        }).First();
        parentClass.AddMethod(new CodeMethod
        {
            Name = "GetProp1",
            Kind = CodeMethodKind.Getter,
            ReturnType = prop1.Type,
            Access = AccessModifier.Public,
            AccessedProperty = prop1,
            IsAsync = false,
            IsStatic = false,
        });
        var method = parentClass.AddMethod(new CodeMethod
        {
            Kind = CodeMethodKind.ErrorMessageOverride,
            ReturnType = new CodeType
            {
                Name = "String",
                IsNullable = false,
            },
            IsAsync = false,
            IsStatic = false,
            Name = "getErrorMessage"
        }).First();

        // When
        writer.Write(method);
        var result = tw.ToString();

        // Then
        Assert.Contains("@Override", result);
        Assert.Contains("String getErrorMessage() ", result);
        Assert.Contains("return this.getProp1()", result);
    }

    [Fact]
    public void WritesRequestGeneratorAcceptHeaderQuotes()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        method.AcceptedResponseTypes.Add("application/json; profile=\"CamelCase\"");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("requestInfo.headers.tryAdd(\"Accept\", \"application/json; profile=\\\"CamelCase\\\"\");", result);
    }

    [Fact]
    public void WritesRequestGeneratorContentTypeQuotes()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.RequestBodyContentType = "application/json; profile=\"CamelCase\"";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("\"application/json; profile=\\\"CamelCase\\\"\"", result);
    }
}
