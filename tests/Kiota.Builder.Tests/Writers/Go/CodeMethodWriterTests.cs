using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public sealed class CodeMethodWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private CodeMethod method;
    private CodeClass parentClass;
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
                Name = "someParentClass",
            }).First();
            baseClass.AddProperty(new CodeProperty
            {
                Name = "definedInParent",
                Type = new CodeType
                {
                    Name = "string"
                },
                Kind = CodePropertyKind.Custom,
            });
        }
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        if (withInheritance)
        {
            parentClass.StartBlock.Inherits = new CodeType
            {
                Name = "someParentClass",
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
            IsExternal = true
        };
        parentClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "string"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "UrlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "string"
            },
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
                Name = "string"
            },
            Getter = new CodeMethod
            {
                Name = "GetAdditionalData",
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            },
            Setter = new CodeMethod
            {
                Name = "SetAdditionalData",
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyProp",
            Type = new CodeType
            {
                Name = "string"
            },
            Getter = new CodeMethod
            {
                Name = "GetDummyProp",
                ReturnType = new CodeType
                {
                    Name = "string"
                },
            },
            Setter = new CodeMethod
            {
                Name = "SetDummyProp",
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "noAccessors",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyColl",
            Type = new CodeType
            {
                Name = "string",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            },
            Getter = new CodeMethod
            {
                Name = "GetDummyColl",
                ReturnType = new CodeType
                {
                    Name = "string",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                },
            },
            Setter = new CodeMethod
            {
                Name = "SetDummyColl",
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
                Name = "GetDummyComplexColl",
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            },
            Setter = new CodeMethod
            {
                Name = "SetDummyComplexColl",
                ReturnType = new CodeType
                {
                    Name = "void",
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
                Name = "GetDummyEnumCollection",
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            },
            Setter = new CodeMethod
            {
                Name = "SetDummyEnumCollection",
                ReturnType = new CodeType
                {
                    Name = "void",
                }
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            }
        });
    }
    private void AddSerializationBackingStoreMethods()
    {
        parentClass.AddMethod(new CodeMethod
        {
            ReturnType = new CodeType
            {
                Name = "map[string]any",
                IsExternal = true
            },
            AccessedProperty = new CodeProperty
            {
                Type = new CodeType
                {
                    Name = "additionalData",
                },
                Kind = CodePropertyKind.AdditionalData,
                Name = "additionalData"
            }
        });
    }
    private CodeClass AddUnionTypeWrapper()
    {
        var complexType1 = root.AddInterface(new CodeInterface
        {
            Name = "ComplexType1",
            Kind = CodeInterfaceKind.Model,
            OriginalClass = new CodeClass { Name = "ComplexType1", Kind = CodeClassKind.Model }
        }).First();

        var complexType2 = root.AddInterface(new CodeInterface
        {
            Name = "ComplexType2",
            Kind = CodeInterfaceKind.Model,
            OriginalClass = new CodeClass { Name = "ComplexType2", Kind = CodeClassKind.Model }
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
            Name = "string",
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
                Name = "SetComplexType1Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetComplexType1Value",
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
                Name = "SetComplexType2Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetComplexType2Value",
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
                Name = "SetStringValue",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetStringValue",
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
            Name = "string",
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
                Name = "SetComplexType1Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetComplexType1Value",
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
                Name = "SetComplexType2Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetComplexType2Value",
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
                Name = "SetComplexType3Value",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetComplexType3Value",
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
                Name = "SetStringValue",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "GetStringValue",
                ReturnType = sType,
                Kind = CodeMethodKind.Getter,
            }
        });
        return intersectionTypeWrapper;
    }
    private void AddRequestBodyParameters(CodeMethod target = default, bool useComplexTypeForBody = false)
    {
        var stringType = new CodeType
        {
            Name = "string",
        };
        target ??= method;
        var requestConfigClass = (target.Parent as CodeClass).AddInnerClass(new CodeClass
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
        target.AddParameter(new CodeParameter
        {
            Name = "c",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "RequestConfig",
                TypeDefinition = requestConfigClass,
            },
            Optional = true,
        });
        target.AddParameter(new CodeParameter
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
        Assert.Contains("(error)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestBuilder()
    {
        setup();
        AddRequestProperties();
        method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.BaseRequestBuilder.PathParameters", result);
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("return", result);
        Assert.Contains("func (m", result);
        Assert.Contains("New", result);
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
        Assert.Contains("SetContentFromParsable", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
        method.AddParameter(new CodeParameter
        {
            Name = "ctx",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "context.Context",
                IsExternal = true,
                IsNullable = false,
            },
            Optional = false,
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("requestInfo, err :=", result);
        Assert.Contains($"errorMapping := {AbstractionsPackageHash}.ErrorMappings", result);
        Assert.Contains("\"4XX\": CreateError4XXFromDiscriminatorValue", result);
        Assert.Contains("\"5XX\": CreateError5XXFromDiscriminatorValue", result);
        Assert.Contains("\"401\": CreateError401FromDiscriminatorValue", result);
        Assert.Contains("ctx context.Context,", result);
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter.Send(ctx,", result);
        Assert.Contains("return res.(", result);
        Assert.Contains("err != nil", result);
        Assert.Contains("return nil, err", result);
        Assert.Contains("if res == nil", result);
        Assert.Contains("return nil, nil", result);
        Assert.Contains("returns", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForEnum()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        method.ReturnType = new CodeType
        {
            Name = "SomeEnum",
            TypeDefinition = new CodeEnum
            {
                Name = "SomeEnum"
            }
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("m.BaseRequestBuilder.RequestAdapter.Send(", result);
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter.SendEnum", result);
        Assert.Contains("ParseSomeEnum", result);
        Assert.Contains("return nil, err", result);
        Assert.Contains("if res == nil", result);
        Assert.Contains("return nil, nil", result);
        Assert.Contains("res.(*SomeEnum)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForEnumCollection()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        method.ReturnType = new CodeType
        {
            Name = "SomeEnum",
            TypeDefinition = new CodeEnum
            {
                Name = "SomeEnum"
            },
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("m.requestAdapter.Send(", result);
        Assert.DoesNotContain("m.requestAdapter.SendEnum(", result);
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter.SendEnumCollection", result);
        Assert.Contains("ParseSomeEnum", result);
        Assert.DoesNotContain("val[i] = *(v.(*SomeEnum))", result);
        Assert.Contains("val[i] = v.(SomeEnum)", result);
        Assert.Contains("return nil, err", result);
        Assert.DoesNotContain("if res == nil", result);
        Assert.DoesNotContain("return nil, nil", result);
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
        Assert.DoesNotContain($"errorMapping := {AbstractionsPackageHash}.ErrorMappings", result);
        Assert.Contains("nil)", result);
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
        Assert.Contains("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.Contains("if mappingValueNode != nil {", result);
        Assert.Contains("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.Contains("if mappingValue != nil {", result);
        Assert.DoesNotContain("switch *mappingValue {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.Contains("result := NewUnionTypeWrapper()", result);
        Assert.Contains("if ie967d16dae74a49b5e0e051225c5dac0d76e5e38f13dd1628028cbce108c25b6.EqualFold(*mappingValue, \"#kiota.complexType1\") {", result);
        Assert.Contains("result.SetComplexType1Value(NewComplexType1())", result);
        Assert.Contains("if val, err := parseNode.GetStringValue(); val != nil {", result);
        Assert.Contains("result.SetStringValue(val)", result);
        Assert.Contains("else if val, err := parseNode.GetCollectionOfObjectValues(CreateComplexType2FromDiscriminatorValue); val != nil {", result);
        Assert.Contains("cast := make([]ComplexType2, len(val))", result);
        Assert.Contains("if v != nil ", result);
        Assert.Contains("for i, v := range val", result);
        Assert.Contains("result.SetComplexType2Value(cast)", result);
        Assert.Contains("return result, nil", result);
        Assert.DoesNotContain("return NewUnionTypeWrapper(), nil", result);
        AssertExtensions.Before("parseNode.GetStringValue()", "GetCollectionOfObjectValues(CreateComplexType2FromDiscriminatorValue)", result);
        AssertExtensions.OutsideOfBlock("if val, err := parseNode.GetStringValue(); val != nil", "mappingValue != nil", result);
        AssertExtensions.OutsideOfBlock("else if val, err := parseNode.GetCollectionOfObjectValues(CreateComplexType2FromDiscriminatorValue); val != ni", "mappingValue != nil", result);
        AssertExtensions.OutsideOfBlock("return result, nil", "mappingValueNode != nil", result);
        AssertExtensions.OutsideOfBlock("result := NewUnionTypeWrapper()", "mappingValueNode != nil", result);
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
        Assert.DoesNotContain("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if mappingValueNode != nil {", result);
        Assert.DoesNotContain("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.DoesNotContain("if mappingValue != nil {", result);
        Assert.DoesNotContain("switch *mappingValue {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.Contains("result := NewIntersectionTypeWrapper()", result);
        Assert.DoesNotContain("if ie967d16dae74a49b5e0e051225c5dac0d76e5e38f13dd1628028cbce108c25b6.EqualFold(*mappingValue, \"#kiota.complexType1\") {", result);
        Assert.Contains("result.SetComplexType1Value(NewComplexType1())", result);
        Assert.Contains("result.SetComplexType3Value(NewComplexType3())", result);
        Assert.Contains("if val, err := parseNode.GetStringValue(); val != nil {", result);
        Assert.Contains("result.SetStringValue(val)", result);
        Assert.Contains("else if val, err := parseNode.GetCollectionOfObjectValues(CreateComplexType2FromDiscriminatorValue); val != nil {", result);
        Assert.Contains("cast := make([]ComplexType2, len(val))", result);
        Assert.Contains("cast[i] = *(v.(*ComplexType2))", result);
        Assert.Contains("for i, v := range val", result);
        Assert.Contains("result.SetComplexType2Value(cast)", result);
        Assert.Contains("return result, nil", result);
        Assert.DoesNotContain("return NewIntersectionTypeWrapper(), nil", result);
        AssertExtensions.Before("parseNode.GetStringValue()", "GetCollectionOfObjectValues(CreateComplexType2FromDiscriminatorValue)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBody()
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
        Assert.Contains("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.Contains("if mappingValueNode != nil {", result);
        Assert.Contains("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.Contains("if mappingValue != nil {", result);
        Assert.Contains("switch *mappingValue {", result);
        Assert.Contains("case \"ns.childmodel\":", result);
        Assert.Contains("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIndexerWithUuidParam()
    {

        setup();
        AddRequestProperties();
        parentClass.AddIndexer(new CodeIndexer
        {
            Name = "indx",
            ReturnType = new CodeType
            {
                Name = "Somecustomtype",
            },
            IndexParameter = new()
            {
                Name = "id",
                SerializationName = "id",
                Type = new CodeType
                {
                    Name = "UUID",
                    IsNullable = true,
                },
            }
        });
        if (parentClass.Indexer is null)
            throw new InvalidOperationException("Indexer is null");
        var methodForTest = parentClass.AddMethod(CodeMethod.FromIndexer(parentClass.Indexer, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterLowerCase(), false)).First();
        writer.Write(methodForTest);
        var result = tw.ToString();
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("WithId(id i561e97a8befe7661a44c8f54600992b4207a3a0cf6770e5559949bc276de2e22.UUID)(Somecustomtype)", result);
        Assert.Contains("m.BaseRequestBuilder.PathParameters", result);
        Assert.Contains("[\"id\"] = id.String()", result);
        Assert.Contains("return", result);
        Assert.Contains("NewSomecustomtypeInternal(urlTplParams, m.BaseRequestBuilder.RequestAdapter)", result); // checking the parameter is passed to the constructor
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
        Assert.DoesNotContain("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if mappingValueNode != nil {", result);
        Assert.DoesNotContain("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.DoesNotContain("if mappingValue != nil {", result);
        Assert.DoesNotContain("switch *mappingValue {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyMappings()
    {
        setup();
        var parentModel = root.AddClass(new CodeClass
        {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
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
        Assert.DoesNotContain("mappingValueNode, err := parseNode.GetChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if mappingValueNode != nil {", result);
        Assert.DoesNotContain("mappingValue, err := mappingValueNode.GetStringValue()", result);
        Assert.DoesNotContain("if mappingValue != nil {", result);
        Assert.DoesNotContain("switch *mappingValue {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return NewChildModel(), nil", result);
        Assert.Contains("return NewParentModel(), nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    private const string AbstractionsPackageHash = "i2ae4187f7daee263371cb1c977df639813ab50ffa529013b7437480d1ec0158f";
    [Fact]
    public async Task WritesRequestGeneratorBodyForScalarAsync()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        var executor = parentClass.AddMethod(new CodeMethod
        {
            Name = "executor",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
            }
        }).First();
        AddRequestBodyParameters(executor);
        AddRequestBodyParameters();
        AddRequestProperties();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, parentClass.Parent as CodeNamespace);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformationWithMethodAndUrlTemplateAndPathParameters({AbstractionsPackageHash}.GET, m.urlTemplate, m.pathParameters)", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("if c != nil", result);
        Assert.Contains("requestInfo.Headers.AddAll(", result);
        Assert.Contains("if c.Q != nil", result);
        Assert.Contains("requestInfo.AddQueryParameters(", result);
        Assert.Contains("requestInfo.AddRequestOptions(", result);
        Assert.DoesNotContain("cast[i] = v", result);
        Assert.Contains("requestInfo.SetContentFromScalar(ctx, m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("return requestInfo, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesRequestGeneratorBodyForScalarCollectionAsync()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        var executor = parentClass.AddMethod(new CodeMethod
        {
            Name = "executor",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
            }
        }).First();
        AddRequestBodyParameters(executor);
        AddRequestBodyParameters();
        AddRequestProperties();
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, parentClass.Parent as CodeNamespace);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformationWithMethodAndUrlTemplateAndPathParameters({AbstractionsPackageHash}.GET, m.urlTemplate, m.pathParameters)", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("if c != nil", result);
        Assert.Contains("requestInfo.Headers.AddAll(", result);
        Assert.Contains("if c.Q != nil", result);
        Assert.Contains("requestInfo.AddQueryParameters(", result);
        Assert.Contains("requestInfo.AddRequestOptions(", result);
        Assert.Contains("cast := make([]interface{}, ", result);
        Assert.Contains("cast[i] = v", result);
        Assert.Contains("requestInfo.SetContentFromScalarCollection(ctx, m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("return requestInfo, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesRequestGeneratorBodyForParsableAsync()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        var executor = parentClass.AddMethod(new CodeMethod
        {
            Name = "executor",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
            }
        }).First();
        AddRequestBodyParameters(executor, true);
        AddRequestBodyParameters(useComplexTypeForBody: true);
        AddRequestProperties();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, parentClass.Parent as CodeNamespace);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformationWithMethodAndUrlTemplateAndPathParameters({AbstractionsPackageHash}.GET, m.urlTemplate, m.pathParameters)", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("if c != nil", result);
        Assert.Contains("requestInfo.Headers.AddAll(", result);
        Assert.Contains("if c.Q != nil", result);
        Assert.Contains("requestInfo.AddQueryParameters(", result);
        Assert.Contains("requestInfo.AddRequestOptions(", result);
        Assert.Contains("requestInfo.SetContentFromParsable(ctx, m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("return requestInfo, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesRequestGeneratorBodyWhenUrlTemplateIsOverrodeAsync()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        var executor = parentClass.AddMethod(new CodeMethod
        {
            Name = "executor",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
            }
        }).First();
        AddRequestBodyParameters(executor, true);
        AddRequestBodyParameters(useComplexTypeForBody: true);
        AddRequestProperties();
        method.UrlTemplateOverride = "{baseurl+}/foo/bar";
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, parentClass.Parent as CodeNamespace);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformationWithMethodAndUrlTemplateAndPathParameters({AbstractionsPackageHash}.GET, \"{{baseurl+}}/foo/bar\", m.pathParameters)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesRequestGeneratorBodyForParsableCollectionAsync()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        var executor = parentClass.AddMethod(new CodeMethod
        {
            Name = "executor",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
            }
        }).First();
        AddRequestBodyParameters(executor, true);
        AddRequestBodyParameters(useComplexTypeForBody: true);
        AddRequestProperties();
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, parentClass.Parent as CodeNamespace);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformationWithMethodAndUrlTemplateAndPathParameters({AbstractionsPackageHash}.GET, m.urlTemplate, m.pathParameters)", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("if c != nil", result);
        Assert.Contains("requestInfo.Headers.AddAll(", result);
        Assert.Contains("if c.Q != nil", result);
        Assert.Contains("requestInfo.AddQueryParameters(", result);
        Assert.Contains("requestInfo.AddRequestOptions(", result);
        Assert.Contains("cast[i] =", result);
        Assert.Contains("requestInfo.SetContentFromParsableCollection(ctx, m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("return requestInfo, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyKnownRequestBodyType()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(useComplexTypeForBody: false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = new GoConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.RequestBodyContentType = "application/json";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("SetStreamContentAndContentType", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/json", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesRequestExecutorForScalarTypesAsync()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType
        {
            Name = "DateOnly",
            IsExternal = true,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        AddRequestBodyParameters(method, false);
        AddRequestBodyParameters(useComplexTypeForBody: false);
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "ctx",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "context.Context",
                IsExternal = true,
                IsNullable = false,
            },
            Optional = false,
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, parentClass.Parent as CodeNamespace);
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"func (m *ParentClass) MethodName(ctx context.Context, b []RequestOption, c *RequestConfig)([]i878a80d2330e89d26896388a3f487eef27b0a0e6c010c493bf80be1452208f91.DateOnly, error)", result);
        Assert.Contains("res, err := m.BaseRequestBuilder.RequestAdapter.SendPrimitiveCollection(ctx, requestInfo, \"dateonly\", nil)", result);
        Assert.Contains($"val := make([]i878a80d2330e89d26896388a3f487eef27b0a0e6c010c493bf80be1452208f91.DateOnly, len(res))", result);
        Assert.Contains("return val, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyUnknownRequestBodyType()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(useComplexTypeForBody: false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = new GoConventionService().StreamTypeName,
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
        Assert.Contains("SetStreamContentAndContentType", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("application/json", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(", *requestContentType", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("m.SomeParentClass.MethodName()", result);
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
        Assert.Contains("res := make([]string, len(val))", result);
        Assert.Contains("res[i] = *(v.(*string))", result);
        Assert.Contains("res := make([]SomeComplexType, len(val))", result);
        Assert.Contains("res[i] = *(v.(*SomeComplexType))", result);
        Assert.Contains("m.SetDummyEnumCollection(val.(*EnumType))", result);
        Assert.Contains("m.SetDummyProp(val)", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesUnionDeSerializerBody()
    {
        setup();
        var wrapper = AddUnionTypeWrapper();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "GetFieldDeserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "map[string, func (ParseNode) (error)]",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("res :=", result);
        Assert.Contains("m.GetComplexType1Value() != nil", result);
        Assert.Contains("return m.GetComplexType1Value().GetFieldDeserializers()", result);
        Assert.Contains("make(map[string, func (ParseNode) (error)])", result);
        AssertExtensions.Before("return m.GetComplexType1Value().GetFieldDeserializers()", "make(map[string, func (ParseNode) (error)])", result);
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
                Name = "map[string, func (ParseNode) (error)]",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("res :=", result);
        Assert.Contains("m.GetComplexType1Value() != nil || m.GetComplexType3Value() != nil", result);
        Assert.Contains($"return {new GoConventionService().SerializationHash}.MergeDeserializersForIntersectionWrapper(m.GetComplexType1Value(), m.GetComplexType3Value())", result);
        Assert.Contains("make(map[string, func (ParseNode) (error)])", result);
        AssertExtensions.Before($"return {new GoConventionService().SerializationHash}.MergeDeserializersForIntersectionWrapper(m.GetComplexType1Value(), m.GetComplexType3Value())", "make(map[string, func (ParseNode) (error)])", result);
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
        Assert.Contains("m.SomeParentClass.Serialize", result);
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
        Assert.DoesNotContain("Serialize(writer)", result);
        Assert.Contains("if m.GetComplexType1Value() != nil {", result);
        Assert.Contains("err := writer.WriteObjectValue(\"\", m.GetComplexType1Value())", result);
        Assert.Contains("m.GetStringValue() != nil", result);
        Assert.Contains("writer.WriteStringValue(\"\", m.GetStringValue())", result);
        Assert.Contains("m.GetComplexType2Value() != nil", result);
        Assert.Contains("err := writer.WriteCollectionOfObjectValues(\"\", cast)", result);
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
        Assert.DoesNotContain("Serialize(writer)", result);
        Assert.DoesNotContain("if m.GetComplexType1Value() != nil {", result);
        Assert.Contains("err := writer.WriteObjectValue(\"\", m.GetComplexType1Value(), m.GetComplexType3Value())", result);
        Assert.Contains("m.GetStringValue() != nil", result);
        Assert.Contains("writer.WriteStringValue(\"\", m.GetStringValue())", result);
        Assert.Contains("m.GetComplexType2Value() != nil", result);
        Assert.Contains("writer.WriteCollectionOfObjectValues(\"\", cast)", result);
        AssertExtensions.Before("writer.WriteStringValue(\"\", m.GetStringValue())", "writer.WriteObjectValue(\"\", m.GetComplexType1Value(), m.GetComplexType3Value())", result);
        AssertExtensions.Before("writer.WriteCollectionOfObjectValues(\"\", cast)", "writer.WriteObjectValue(\"\", m.GetComplexType1Value(), m.GetComplexType3Value())", result);
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
        Assert.Contains("WriteStringValue", result);
        Assert.Contains("WriteCollectionOfStringValues", result);
        Assert.Contains("WriteCollectionOfObjectValues", result);
        Assert.Contains("WriteAdditionalData(m.GetAdditionalData())", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesSerializerBackingStoreBody()
    {
        setup();
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationBackingStoreMethods();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("WriteAdditionalData(m.GetAdditionalData())", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact(Skip = "descriptions are not supported")]
    public void WritesMethodSyncDescription()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                DescriptionTemplate = ParamDescription
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
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
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("[see more]: ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive()
    {
        setup();
        var codeMethodWriter = new CodeMethodWriter(new GoConventionService(), false);
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
    private const string TaskPrefix = "func() (";
    [Fact]
    public void WritesReturnType()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"{MethodName.ToFirstCharacterUpperCase()}()(*{ReturnTypeName}, error)", result);// async default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    {
        setup();
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain(TaskPrefix, result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPublicMethodByDefault()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName.ToFirstCharacterUpperCase(), result);// public default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPrivateMethod()
    {
        setup();
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName.ToFirstCharacterLowerCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesProtectedMethod()
    {
        setup();
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName.ToFirstCharacterLowerCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIndexer()
    {
        setup();
        AddRequestProperties();
        parentClass.AddIndexer(new CodeIndexer
        {
            Name = "indx",
            ReturnType = new CodeType
            {
                Name = "Somecustomtype",
            },
            IndexParameter = new()
            {
                Name = "id",
                SerializationName = "id",
                Type = new CodeType
                {
                    Name = "string",
                    IsNullable = true,
                },
            }
        });
        if (parentClass.Indexer is null)
            throw new InvalidOperationException("Indexer is null");
        var methodForTest = parentClass.AddMethod(CodeMethod.FromIndexer(parentClass.Indexer, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterLowerCase(), false)).First();
        writer.Write(methodForTest);
        var result = tw.ToString();
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("m.BaseRequestBuilder.PathParameters", result);
        Assert.Contains("[\"id\"] = id", result);
        Assert.Contains("return", result);
        Assert.Contains("NewSomecustomtypeInternal(urlTplParams, m.BaseRequestBuilder.RequestAdapter)", result); // checking the parameter is passed to the constructor
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
                Name = "string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.BaseRequestBuilder.RequestAdapter", result);
        Assert.Contains("m.BaseRequestBuilder.PathParameters", result);
        Assert.Contains("pathParam", result);
        Assert.Contains("return New", result);
    }
    [Fact]
    public void WritesDescription()
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
                Name = "string"
            }
        });
        method.Documentation.DescriptionTemplate = "Some description";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"// {method.Name.ToFirstCharacterUpperCase()} some description", result);
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
        Assert.Contains("m.GetBackingStore().Get(\"someProperty\")", result);
    }
    [Fact]
    public void WritesGetterToBackingStoreWithNonnullProperty()
    {
        setup();
        method.AddAccessedProperty();
        parentClass.GetGreatestGrandparent().AddBackingStoreProperty();
        method.AccessedProperty.Type = new CodeType
        {
            Name = "string",
            IsNullable = false,
        };
        var defaultValue = "someDefaultValue";
        method.AccessedProperty.DefaultValue = defaultValue;
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("if val == nil", result);
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
        Assert.Contains("m.GetBackingStore().Set(\"someProperty\", value)", result);
    }
    [Fact]
    public void WritesGetterToField()
    {
        setup();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.someProperty", result);
        Assert.DoesNotContain("if m == nil", result);
    }
    [Fact]
    public void WritesSetterToField()
    {
        setup();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("m.someProperty = value", result);
        Assert.DoesNotContain("if m != nil", result);
    }
    [Fact]
    public void WritesConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string",
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
                Name = "map[string]string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"m.Set{propName.ToFirstCharacterUpperCase()}({defaultValue})", result);
        Assert.Contains($"m.SetPropWithDefaultNullValue(nil)", result);
        Assert.Contains($"propWithDefaultBoolValueValue := true", result);
        Assert.Contains($"m.SetPropWithDefaultBoolValue(&propWithDefaultBoolValueValue)", result);
        Assert.Contains("NewBaseRequestBuilder", result);
    }
    [Fact]
    public void WritesConstructorWithEnumValue()
    {
        setup();
        var modelsNamespace = root.AddNamespace("models");
        method.Kind = CodeMethodKind.Constructor;
        var serializationName = "1024x1024";
        var defaultValue = "TENTWENTYFOUR_BY_TENTWENTYFOUR";
        var propName = "size";
        var codeEnum = modelsNamespace.AddEnum(new CodeEnum
        {
            Name = "pictureSize"
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            SerializationName = serializationName,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"Set{propName.ToFirstCharacterUpperCase()}({defaultValue})", result);//ensure symbol is cleaned up
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
                Name = "string"
            },
        });
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        AddRequestProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"return New{parentClass.Name.ToFirstCharacterUpperCase()}", result);
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
            Name = "string",
        };
        var arrayType = new CodeType
        {
            Name = "array",
        };
        unionTypeWrapper.OriginalComposedType.AddType(sType);
        unionTypeWrapper.OriginalComposedType.AddType(arrayType);

        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.DoesNotContain(defaultValue, result);//ensure the composed type is not referenced
    }
    [Fact]
    public void WritesRawUrlConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.RawUrlConstructor;
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        method.AddParameter(new CodeParameter
        {
            Name = "requestAdapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        method.OriginalMethod = new()
        {
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("urlParams := make(map[string]string)", result);
        Assert.Contains("urlParams[\"request-raw-url\"] = rawUrl", result);
    }
    [Fact]
    public void WritesInheritedConstructor()
    {
        setup(true);
        method.Kind = CodeMethodKind.Constructor;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("SomeParentClass: *NewSomeParentClass", result);
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
        method.DeserializerModules = new() { "github.com/microsoft/kiota/serialization/go/json.Deserializer" };
        method.SerializerModules = new() { "github.com/microsoft/kiota/serialization/go/json.Serializer" };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("RegisterDefaultSerializer", result);
        Assert.Contains("RegisterDefaultDeserializer", result);
        Assert.Contains($"[\"baseurl\"] = m.BaseRequestBuilder.Core.GetBaseUrl", result);
        Assert.Contains($"SetBaseUrl(\"{method.BaseUrl}\")", result);
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
                Name = "IBackingStore",
                IsExternal = true,
            }
        };
        method.AddParameter(backingStoreParam);
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("EnableBackingStore", result);
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
            Name = "select",
            Type = new CodeType { Name = "string" },
            Access = AccessModifier.Public,
            Kind = CodePropertyKind.Custom,
        });
        root.AddNamespace("ApiSdk/models"); // so the interface copy refiner goes through
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
        var getter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Getter));
        var setter = model.Methods.First(x => x.IsOfKind(CodeMethodKind.Setter));
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(getter);
        var result = tw.ToString();
        Assert.Contains("GetSelect", result);
        Assert.DoesNotContain("GetSelect_escaped", result);

        await using var tw2 = new StringWriter();
        tempWriter.SetTextWriter(tw2);
        tempWriter.Write(setter);
        result = tw2.ToString();
        Assert.Contains("SetSelect", result);
        Assert.DoesNotContain("SetSelect_escaped", result);
    }
    [Fact]
    public void DoesntWriteReadOnlyPropertiesInSerializerBody()
    {
        setup(true);
        method.Kind = CodeMethodKind.Serializer;
        AddSerializationProperties();
        parentClass.AddProperty(new CodeProperty
        {
            Name = "ReadOnlyProperty",
            ReadOnly = true,
            Type = new CodeType
            {
                Name = "string",
            },
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("ReadOnlyProperty", result);
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
        Assert.Contains("// Deprecated:", result);
    }
    [Fact]
    public void WritesDeprecationInformationFromBuilder()
    {
        setup();
        var newMethod = method.Clone() as CodeMethod;
        newMethod.Name = "NewAwesomeMethod";// new method replacement
        method.Deprecation = new("This method is obsolete. Use {TypeName} instead.", IsDeprecated: true, TypeReferences: new() { { "TypeName", new CodeType { TypeDefinition = newMethod, IsExternal = false } } });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("This method is obsolete. Use NewAwesomeMethod instead.", result);
    }
    [Fact]
    public void WritesComposedMarker()
    {
        setup();
        method.Kind = CodeMethodKind.ComposedTypeMarker;
        method.ReturnType = new CodeType { Name = "boolean", IsNullable = false };
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("return true", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("error", result, StringComparison.OrdinalIgnoreCase);
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
                Name = "string",
                IsNullable = false,
            },
            IsAsync = false,
            IsStatic = false,
            Name = "Error"
        }).First();
        var parentInterface = root.AddInterface(new CodeInterface
        {
            Name = "parentInterface",
            OriginalClass = parentClass,
        }).First();
        parentInterface.AddMethod(new CodeMethod
        {
            Name = "GetProp1",
            Kind = CodeMethodKind.Getter,
            ReturnType = prop1.Type,
            Access = AccessModifier.Public,
            AccessedProperty = prop1,
            IsAsync = false,
            IsStatic = false,
        });
        parentClass.AssociatedInterface = parentInterface;

        // When
        writer.Write(method);
        var result = tw.ToString();

        // Then
        Assert.Contains("Error()(string) {", result);
        Assert.Contains("return *(m.GetProp1()", result);
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
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json; profile=\\\"CamelCase\\\"\")", result);
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
