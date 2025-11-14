using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Dart;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

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
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";
    public CodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, DefaultPath, DefaultName);
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
    private CodeClass AddUnionType()
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
        var enumType = root.AddEnum(new CodeEnum
        {
            Name = "EnumType",
        }).First();
        var unionType = root.AddClass(new CodeClass
        {
            Name = "UnionType",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeUnionType
            {
                Name = "UnionType",
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
        var eType = new CodeType
        {
            Name = "SomeEnum",
            TypeDefinition = enumType,
        };
        unionType.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType
        {
            Name = "ComplexType1",
            TypeDefinition = cType1
        });
        unionType.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType
        {
            Name = "ComplexType2",
            TypeDefinition = cType2
        });
        unionType.OriginalComposedType.AddType(cType1);
        unionType.OriginalComposedType.AddType(cType2);
        unionType.OriginalComposedType.AddType(sType);
        unionType.OriginalComposedType.AddType(eType);
        unionType.AddProperty(new CodeProperty
        {
            Name = "complexType1Value",
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
        unionType.AddProperty(new CodeProperty
        {
            Name = "complexType2Value",
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
        unionType.AddProperty(new CodeProperty
        {
            Name = "stringValue",
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
        unionType.AddProperty(new CodeProperty
        {
            Name = "enumValue",
            Type = eType,
            Kind = CodePropertyKind.Custom,
            Setter = new CodeMethod
            {
                Name = "setEnumValue",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new CodeMethod
            {
                Name = "getEnumValue",
                ReturnType = eType,
                Kind = CodeMethodKind.Getter,
            },
        });
        return unionType;
    }
    private CodeClass AddIntersectionType()
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
        var enumType = root.AddEnum(new CodeEnum
        {
            Name = "EnumType",
        }).First();
        var intersectionType = root.AddClass(new CodeClass
        {
            Name = "IntersectionType",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeIntersectionType
            {
                Name = "IntersectionType",
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
        intersectionType.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType
        {
            Name = "ComplexType1",
            TypeDefinition = cType1
        });
        intersectionType.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType
        {
            Name = "ComplexType2",
            TypeDefinition = cType2
        });
        intersectionType.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType3", new CodeType
        {
            Name = "ComplexType3",
            TypeDefinition = cType3
        });
        var sType = new CodeType
        {
            Name = "String",
        };
        var eType = new CodeType
        {
            Name = "SomeEnum",
            TypeDefinition = enumType,
        };
        intersectionType.OriginalComposedType.AddType(cType1);
        intersectionType.OriginalComposedType.AddType(cType2);
        intersectionType.OriginalComposedType.AddType(cType3);
        intersectionType.OriginalComposedType.AddType(sType);
        intersectionType.OriginalComposedType.AddType(eType);
        intersectionType.AddProperty(new CodeProperty
        {
            Name = "complexType1Value",
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
        intersectionType.AddProperty(new CodeProperty
        {
            Name = "complexType2Value",
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
        intersectionType.AddProperty(new CodeProperty
        {
            Name = "complexType3Value",
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
        intersectionType.AddProperty(new CodeProperty
        {
            Name = "stringValue",
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
        intersectionType.AddProperty(new CodeProperty
        {
            Name = "enumValue",
            Type = eType,
            Kind = CodePropertyKind.Custom,
            Setter = new()
            {
                Name = "setEnumValue",
                ReturnType = new CodeType
                {
                    Name = "void",
                },
                Kind = CodeMethodKind.Setter,
            },
            Getter = new()
            {
                Name = "getEnumValue",
                ReturnType = eType,
                Kind = CodeMethodKind.Getter,
            },
        });
        return intersectionType;
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
        var configType = new CodeType
        {
            Name = "RequestConfig",
            TypeDefinition = requestConfigClass,
            ActionOf = true,
        };
        configType.AddGenericTypeParameterValue(new CodeType { Name = "DefaultQueryParameters" });
        method.AddParameter(new CodeParameter
        {
            Name = "c",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = configType,
            Optional = true,
        });
    }
    [Fact]
    public void WritesVoidTypeForExecutor()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.AddParameter(new CodeParameter()
        {
            Type = new CodeType(),
            Kind = CodeParameterKind.RequestConfiguration
        });
        method.ReturnType = new CodeType
        {
            Name = "void",
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("Future<void>", result);
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
        Assert.Contains("var requestInfo", result);
        Assert.Contains("final errorMapping = <String, ParsableFactory<Parsable>>{", result);
        Assert.Contains("'401' :  Error401.createFromDiscriminatorValue,", result);
        Assert.Contains("'4XX' :  Error4XX.createFromDiscriminatorValue,", result);
        Assert.Contains("'5XX' :  Error5XX.createFromDiscriminatorValue,", result);
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
        Assert.DoesNotContain("Map<String, ParsableFactory<Parsable>> errorMapping = {", result);
        Assert.Contains("{}", result);
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
        Assert.Contains("var mappingValue = parseNode.getChildNode('@odata.type')", result);
        Assert.Contains("return switch(mappingValue) {", result);
        Assert.Contains("'ns.childmodel' => ChildModel(),", result);
        Assert.Contains("_ => ParentModel()", result);
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
        Assert.DoesNotContain("var mappingValue = parseNode.getChildNode('@odata.type')", result);
        Assert.DoesNotContain("return switch(mappingValue) {", result);
        Assert.DoesNotContain("'ns.childmodel' => ChildModel(),", result);
        Assert.Contains("return ParentModel()", result);
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
        Assert.Contains("return ParentModel()", result);
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

    //vanaf hier, heeft ook configure nodig, dus wel generics
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
        Assert.Contains("var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);", result);
        Assert.Contains("requestInfo.configure<DefaultQueryParameters>(c, () => DefaultQueryParameters());", result);
        Assert.Contains("requestInfo.headers.put('Accept', 'application/json');", result);
        Assert.Contains("requestInfo.setContentFromScalar(requestAdapter, '', b)", result);
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
        Assert.Contains("var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters)", result);
        Assert.Contains("requestInfo.configure<DefaultQueryParameters>(c, () => DefaultQueryParameters());", result);
        Assert.Contains("requestInfo.headers.put('Accept', 'application/json');", result);
        Assert.Contains("setContentFromScalarCollection", result);
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
        Assert.Contains("var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters)", result);
        Assert.Contains("requestInfo.configure<DefaultQueryParameters>(c, () => DefaultQueryParameters());", result);
        Assert.Contains("requestInfo.headers.put('Accept', 'application/json')", result);
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
        Assert.Contains("var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : '{baseurl+}/foo/bar', pathParameters :  pathParameters)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesUnionDeSerializerBody()
    {
        setup();
        var wrapper = AddUnionType();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "getFieldDeserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "Map<String, void Function(ParseNode)>",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.Contains("complexType1Value != null", result);
        Assert.Contains("return complexType1Value!.getFieldDeserializers()", result);
        Assert.Contains("<String, void Function(ParseNode)>{}", result);
        AssertExtensions.Before("return complexType1Value!.getFieldDeserializers()", "<String, void Function(ParseNode)>{}", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIntersectionDeSerializerBody()
    {
        setup();
        var wrapper = AddIntersectionType();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "getFieldDeserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "Map<String, void Function(ParseNode)>",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.Contains("var deserializers = <String, void Function(ParseNode)>{};", result);
        Assert.Contains("if(complexType1Value != null){complexType1Value!.getFieldDeserializers().forEach((k,v) => deserializers.putIfAbsent(k, ()=>v));}", result);
        Assert.Contains("if(complexType3Value != null){complexType3Value!.getFieldDeserializers().forEach((k,v) => deserializers.putIfAbsent(k, ()=>v));}", result);
        Assert.Contains("return deserializers", result);
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
        Assert.Contains("super.getFieldDeserializers()", result);
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
        var wrapper = AddUnionType();
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
        Assert.Contains("if(complexType1Value != null) {", result);
        Assert.Contains("writer.writeObjectValue<ComplexType1>(null, complexType1Value)", result);
        Assert.Contains("stringValue != null", result);
        Assert.Contains("writer.writeStringValue(null, stringValue)", result);
        Assert.Contains("complexType2Value != null", result);
        Assert.Contains("writer.writeCollectionOfObjectValues<ComplexType2>(null, complexType2Value)", result);
        Assert.Contains("enumValue != null", result);
        Assert.Contains("writer.writeEnumValue<EnumType>(null, enumValue, (e) => e?.value)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesIntersectionSerializerBody()
    {
        setup();
        var wrapper = AddIntersectionType();
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
        Assert.DoesNotContain("complexType1Value != null) {", result);
        Assert.Contains("writer.writeObjectValue<ComplexType1>(null, complexType1Value, [complexType3Value])", result);
        Assert.Contains("stringValue != null", result);
        Assert.Contains("writer.writeStringValue(null, stringValue)", result);
        Assert.Contains("enumValue != null", result);
        Assert.Contains("writer.writeEnumValue<EnumType>(null, enumValue, (e) => e?.value)", result);
        Assert.Contains("complexType2Value != null", result);
        Assert.Contains("writer.writeCollectionOfObjectValues<ComplexType2>(null, complexType2Value)", result);
        AssertExtensions.Before("writer.writeStringValue(null, stringValue)", "writer.writeEnumValue<EnumType>(null, enumValue, (e) => e?.value)", result);
        AssertExtensions.Before("writer.writeEnumValue<EnumType>(null, enumValue, (e) => e?.value)", "writer.writeCollectionOfObjectValues<ComplexType2>(null, complexType2Value)", result);
        AssertExtensions.Before("writer.writeCollectionOfObjectValues<ComplexType2>(null, complexType2Value)", "writer.writeObjectValue<ComplexType1>(null, complexType1Value, [complexType3Value])", result);
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
        Assert.Contains("writeAdditionalData(additionalData);", result);
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
        Assert.Contains("[see more](https://foo.org/docs)", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive()
    {
        setup();
        var codeMethodWriter = new CodeMethodWriter(new DartConventionService());
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
        Assert.Contains($"{ReturnTypeName} {MethodName}", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPublicMethodByDefault()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("_", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesPrivateMethod()
    {
        setup();
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("_", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
        Assert.Contains("return", result);
    }
    [Fact]
    public void WritesConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "'someVal'";
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
        var defaultValueNull = "'null'";
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
        var defaultValueBool = "true";
        var boolPropName = "propWithDefaultBoolValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = boolPropName,
            DefaultValue = defaultValueBool,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "Boolean",
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
        Assert.Contains($"{propName} = '{defaultValue}'", result);
        Assert.Contains($"{nullPropName} = {defaultValueNull}", result);
        Assert.Contains($"{boolPropName} = {defaultValueBool}", result);
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
        Assert.Contains($"return {parentClass.Name}", result);
    }
    [Fact]
    public void DoesNotWriteConstructorWithDefaultFromComposedType()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "Test Value";
        var propName = "size";
        var unionType = root.AddClass(new CodeClass
        {
            Name = "UnionType",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeUnionType
            {
                Name = "UnionType",
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
            Type = new CodeType { TypeDefinition = unionType }
        });
        var sType = new CodeType
        {
            Name = "String",
        };
        var arrayType = new CodeType
        {
            Name = "array",
        };
        unionType.OriginalComposedType.AddType(sType);
        unionType.OriginalComposedType.AddType(arrayType);

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
        Assert.Contains($"{propName} = '{defaultValue.TrimQuotes()}'", result);
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
        Assert.Contains($"pathParameters['baseurl'] = core.baseUrl", result);
        Assert.Contains($"core.baseUrl = '{method.BaseUrl}'", result);
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
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("enableBackingStore", result);
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
    public void WritesRequestGeneratorAcceptHeaderQuotes()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        method.AcceptedResponseTypes.Add("application/json; profile=\"CamelCase\"");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("requestInfo.headers.put('Accept', 'application/json; profile=\"CamelCase\"')", result);
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
        Assert.Contains("'application/json; profile=\"CamelCase\"'", result);
    }
}
