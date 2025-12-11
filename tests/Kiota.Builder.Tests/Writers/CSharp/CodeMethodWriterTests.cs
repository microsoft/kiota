using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.CSharp;

using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
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
        var model = root.AddClass(new CodeClass
        {
            Name = ReturnTypeName,
            Kind = CodeClassKind.Model
        }).First();
        method = new CodeMethod
        {
            Name = MethodName,
            ReturnType = new CodeType
            {
                Name = ReturnTypeName,
                TypeDefinition = model,
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
        parentClass.AddProperty(new CodeProperty
        {
            Name = "RequestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "IRequestAdapter"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "IDictionary<string, object>"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "urlTemplate",
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
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyProp",
            Type = new CodeType
            {
                Name = "string"
            }
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
            Name = "dummyUCaseProp",
            SerializationName = "DummyUCaseProp",
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
            }
        });
        var complexTypeClass = root.AddClass(new CodeClass
        {
            Name = "SomeComplexType"
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyComplexColl",
            Type = new CodeType
            {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = complexTypeClass
            }
        });
        var enumDefinition = root.AddEnum(new CodeEnum
        {
            Name = "EnumType"
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyEnumCollection",
            Type = new CodeType
            {
                Name = "SomeEnum",
                TypeDefinition = enumDefinition
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
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType2Value",
            Type = cType2,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "StringValue",
            Type = sType,
            Kind = CodePropertyKind.Custom
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
            Kind = CodePropertyKind.Custom
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType2Value",
            Type = cType2,
            Kind = CodePropertyKind.Custom
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "ComplexType3Value",
            Type = cType3,
            Kind = CodePropertyKind.Custom
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "StringValue",
            Type = sType,
            Kind = CodePropertyKind.Custom
        });
        return intersectionTypeWrapper;
    }
    private void AddRequestBodyParameters(bool useComplexTypeForBody = false)
    {
        var stringType = new CodeType
        {
            Name = "string",
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
            Name = "config",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "RequestConfig",
                TypeDefinition = requestConfigClass,
                ActionOf = true,
            },
            Optional = true,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "c",
            Kind = CodeParameterKind.Cancellation,
            Type = stringType,
        });
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
        Assert.Contains("var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>", result);
        Assert.Contains("<exception cref=", result);
        Assert.Contains("{ \"4XX\", Error4XX.CreateFromDiscriminatorValue },", result);
        Assert.Contains("{ \"5XX\", Error5XX.CreateFromDiscriminatorValue },", result);
        Assert.Contains("{ \"401\", Error401.CreateFromDiscriminatorValue },", result);
        Assert.Contains("SendAsync", result);
        Assert.Contains($"{ReturnTypeName}.CreateFromDiscriminatorValue", result);
        Assert.Contains(AsyncKeyword, result);
        Assert.Contains("await", result);
        Assert.Contains("cancellationToken", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesRequestExecutorBodyWithUntypedReturnValue()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType { TypeDefinition = null, Name = KiotaBuilder.UntypedNodeName };
        var errorXXX = root.AddClass(new CodeClass
        {
            Name = "ErrorXXX",
        }).First();
        method.AddErrorMapping("XXX", new CodeType { Name = "ErrorXXX", TypeDefinition = errorXXX });
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo", result);
        Assert.Contains("var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>", result);
        Assert.Contains("<exception cref=", result);
        Assert.Contains("{ \"XXX\", ErrorXXX.CreateFromDiscriminatorValue },", result);
        Assert.Contains("SendAsync", result);
        Assert.Contains("UntypedNode.CreateFromDiscriminatorValue", result);
        Assert.Contains(AsyncKeyword, result);
        Assert.Contains("await", result);
        Assert.Contains("cancellationToken", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForEnum()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.Parameters.First(static x => x.IsOfKind(CodeParameterKind.RequestBody)).Type = new CodeType { Name = "testEnum", IsExternal = true, TypeDefinition = new CodeEnum() { Name = "TestEnum" } };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("SetContentFromEnum", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForUntypedNode()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.Parameters.First(static x => x.IsOfKind(CodeParameterKind.RequestBody)).Type = new CodeType { Name = "UntypedNode", IsExternal = true };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("SetContentFromParsable", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesRequestExecutorBodyForCollection()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        var error4XX = root.AddClass(new CodeClass
        {
            Name = "Error4XX",
        }).First();
        method.AddErrorMapping("4XX", new CodeType { Name = "Error4XX", TypeDefinition = error4XX });
        AddRequestBodyParameters();
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo", result);
        Assert.Contains("var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>", result);
        Assert.Contains("{ \"4XX\", Error4XX.CreateFromDiscriminatorValue },", result);
        Assert.Contains("SendCollectionAsync", result);
        Assert.Contains("return collectionResult?.AsList()", result);
        Assert.Contains($"{ReturnTypeName}.CreateFromDiscriminatorValue", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
        Assert.DoesNotContain("var errorMapping = new Dictionary<string, Func<IParsable>>", result);
        Assert.Contains("default", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
        Assert.Contains("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch", result);
        Assert.Contains("var result = new UnionTypeWrapper()", result);
        Assert.Contains("if(\"#kiota.complexType1\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))", result);
        Assert.Contains("ComplexType1Value = new ComplexType1()", result);
        Assert.Contains("else if(parseNode.GetStringValue() is string stringValueValue)", result);
        Assert.Contains("StringValue = stringValueValue", result);
        Assert.Contains("parseNode.GetCollectionOfObjectValues<ComplexType2>(ComplexType2.CreateFromDiscriminatorValue)?.AsList() is List<ComplexType2> complexType2ValueValue", result);
        Assert.Contains("ComplexType2Value = complexType2ValueValue", result);
        Assert.Contains("return result", result);
        AssertExtensions.Before("GetStringValue() is string stringValueValue", "GetCollectionOfObjectValues<ComplexType2>", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyForUnionPrimitiveTypes()
    {
        setup();
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
            Name = "string",
        };
        var cType2 = new CodeType
        {
            Name = "double",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        var cType3 = new CodeType
        {
            Name = "integer",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        var cType4 = new CodeType
        {
            Name = "boolean",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
        };
        unionTypeWrapper.OriginalComposedType.AddType(cType1);
        unionTypeWrapper.OriginalComposedType.AddType(cType2);
        unionTypeWrapper.OriginalComposedType.AddType(cType3);
        unionTypeWrapper.OriginalComposedType.AddType(cType4);
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "StringValue",
            Type = cType1,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "DoubleValue",
            Type = cType2,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "IntegerValue",
            Type = cType3,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "BooleanValue",
            Type = cType4,
            Kind = CodePropertyKind.Custom
        });


        var factoryMethod = unionTypeWrapper.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "UnionTypeWrapper",
                TypeDefinition = unionTypeWrapper,
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
        Assert.Contains("parseNode.GetCollectionOfPrimitiveValues<double?>()?.AsList() is List<double?> doubleValue", result);
        Assert.Contains("parseNode.GetCollectionOfPrimitiveValues<int?>()?.AsList() is List<int?> integerValue", result);
        Assert.Contains("parseNode.GetCollectionOfPrimitiveValues<bool?>()?.AsList() is List<bool?> booleanValue", result);
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
        Assert.DoesNotContain("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch", result);
        Assert.Contains("var result = new IntersectionTypeWrapper()", result);
        Assert.DoesNotContain("if(\"#kiota.complexType1\".Equals(mappingValue, StringComparison.OrdinalIgnoreCase))", result);
        Assert.Contains("if(parseNode.GetStringValue() is string stringValueValue)", result);
        Assert.Contains("StringValue = stringValueValue", result);
        Assert.Contains("parseNode.GetCollectionOfObjectValues<ComplexType2>(ComplexType2.CreateFromDiscriminatorValue)?.AsList() is List<ComplexType2> complexType2ValueValue", result);
        Assert.Contains("ComplexType2Value = complexType2ValueValue", result);
        Assert.Contains("ComplexType1Value = new ComplexType1()", result);
        Assert.Contains("return result", result);
        AssertExtensions.Before("GetStringValue() is string stringValueValue", "GetCollectionOfObjectValues<ComplexType2>", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyAndDisambiguateAmbiguousImportedTypes()
    {
        setup();
        // Arrange : Adding a model with conflicting discriminator
        var modelsNamespace = root.AddNamespace("models");
        var modelWithConflictingDiscriminatorTypes = modelsNamespace.AddClass(
            new CodeClass
            {
                Name = "ConflictingModelBaseClass"
            }).First();
        modelsNamespace.AddClass(modelWithConflictingDiscriminatorTypes);
        // Adding a model in another namespace discriminator
        var levelOneNameSpace = modelsNamespace.AddNamespace("namespaceLevelOne");
        var conflictingModelInOtherNamespace = levelOneNameSpace.AddClass(
            new CodeClass
            {
                Name = "ConflictingModel",
                StartBlock = new ClassDeclaration()
                {
                    Inherits = new CodeType()
                    {
                        Name = modelWithConflictingDiscriminatorTypes.Name,
                        TypeDefinition = modelWithConflictingDiscriminatorTypes
                    }
                }

            }).First();
        levelOneNameSpace.AddClass(conflictingModelInOtherNamespace);
        // Adding an enum in another namespace that conflicts
        var levelTwoNameSpace = modelsNamespace.AddNamespace("namespaceLevelTwo");
        var conflictingEnumInOtherNamespace = levelTwoNameSpace.AddEnum(
            new CodeEnum()
            {
                Name = "ConflictingModel"
            }).First();
        modelsNamespace.AddEnum(conflictingEnumInOtherNamespace);// add enum to root models with same name

        // setup the usings
        modelWithConflictingDiscriminatorTypes.AddUsing(
            new CodeUsing()
            {
                Name = levelOneNameSpace.Name,
                Declaration = new CodeType()
                {
                    Name = conflictingModelInOtherNamespace.Name,
                    TypeDefinition = conflictingModelInOtherNamespace
                },
            });
        modelWithConflictingDiscriminatorTypes.AddUsing(
            new CodeUsing()
            {
                Name = levelTwoNameSpace.Name,
                Declaration = new CodeType()
                {
                    Name = conflictingEnumInOtherNamespace.Name,
                    TypeDefinition = conflictingEnumInOtherNamespace
                },
            });
        modelWithConflictingDiscriminatorTypes.DiscriminatorInformation = new DiscriminatorInformation()
        {
            Name = "type",
            DiscriminatorPropertyName = "@odata.type"
        };
        modelWithConflictingDiscriminatorTypes.DiscriminatorInformation.AddDiscriminatorMapping(levelOneNameSpace.Name + ".ConflictingModel", new CodeType()
        {
            Name = conflictingModelInOtherNamespace.Name,
            TypeDefinition = conflictingModelInOtherNamespace
        });

        var factoryMethod = modelWithConflictingDiscriminatorTypes.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "parentModel",
                TypeDefinition = modelWithConflictingDiscriminatorTypes,
            },
            IsStatic = true,
        }).First();

        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });

        writer.Write(factoryMethod);
        var result = tw.ToString();

        Assert.Contains("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.Contains("return mappingValue switch", result);
        Assert.Contains("\"namespaceLevelOne.ConflictingModel\" => new global::namespaceLevelOne.ConflictingModel(),", result); //Assert the disambiguation happens due to the enum imported
        Assert.Contains("_ => new global::models.ConflictingModelBaseClass()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyForInheritedModels()
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
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.Contains("return mappingValue switch", result);
        Assert.Contains("\"ns.childmodel\" => new ChildModel()", result);
        Assert.Contains("_ => new ParentModel()", result);
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
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("var mappingValue = mappingValueNode?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch", result);
        Assert.DoesNotContain("\"ns.childmodel\" => new ChildModel()", result);
        Assert.Contains("return new ParentModel()", result);
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
                IsExternal = true,
            },
            Optional = false,
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("var mappingValue = parseNode.GetChildNode(\"@odata.type\")?.GetStringValue()", result);
        Assert.DoesNotContain("var mappingValue = mappingValueNode?.GetStringValue()", result);
        Assert.DoesNotContain("return mappingValue switch", result);
        Assert.DoesNotContain("\"ns.childmodel\" => new ChildModel()", result);
        Assert.Contains("return new ParentModel()", result);
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
        Assert.Contains("SendCollectionAsync", result);
        Assert.Contains("cancellationToken", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForNullableScalar()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo = new RequestInformation(Method.GET, UrlTemplate, PathParameters)", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("requestInfo.Configure(config)", result);
        Assert.Contains("SetContentFromScalar", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
        method.ReturnType = new CodeType { Name = "double", IsNullable = true, IsExternal = true };//use a nullable value type
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("var requestInfo = new RequestInformation(Method.GET, UrlTemplate, PathParameters", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("requestInfo.Configure(config)", result);
        Assert.Contains("SetContentFromScalar", result);
        Assert.Contains("return requestInfo;", result);
        Assert.Contains("async Task<double?>", result);//verify we only have one nullable marker
        Assert.DoesNotContain("async Task<double??>", result);//verify we only have one nullable marker
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
        Assert.Contains("var requestInfo = new RequestInformation(Method.GET, UrlTemplate, PathParameters)", result);
        Assert.Contains("requestInfo.Headers.TryAdd(\"Accept\", \"application/json\")", result);
        Assert.Contains("requestInfo.Configure(config)", result);
        Assert.Contains("SetContentFromParsable", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
        Assert.Contains("var requestInfo = new RequestInformation(Method.GET, \"{baseurl+}/foo/bar\", PathParameters)", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForScalarCollection()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        method.AcceptedResponseTypes.Add("application/json");
        var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
        bodyParameter.Type = new CodeType
        {
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
            IsExternal = true,
            Name = "string",
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("SetContentFromScalarCollection", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
            Name = new CSharpConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.RequestBodyContentType = "application/json";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("SetStreamContent", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/json", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
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
            Name = new CSharpConventionService().StreamTypeName,
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
        Assert.Contains("SetStreamContent", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("application/json", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(", requestContentType", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesInheritedDeSerializerBody()
    {
        setup(true);
        method.Kind = CodeMethodKind.Deserializer;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("base.", result);
        Assert.Contains("new", result);
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
                Name = "IDictionary<string, Action<IParseNode>>",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("base.", result);
        Assert.Contains("ComplexType1Value != null", result);
        Assert.Contains("return ComplexType1Value.GetFieldDeserializers()", result);
        Assert.Contains("new", result);
        Assert.Contains("return new Dictionary<string, Action<IParseNode>>()", result);
        AssertExtensions.Before("return ComplexType1Value.GetFieldDeserializers()", "return new Dictionary<string, Action<IParseNode>>()", result);
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
                Name = "IDictionary<string, Action<IParseNode>>",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("base.", result);
        Assert.Contains("ComplexType1Value != null || ComplexType3Value != null", result);
        Assert.Contains("return ParseNodeHelper.MergeDeserializersForIntersectionWrapper(ComplexType1Value, ComplexType3Value)", result);
        Assert.Contains("return new Dictionary<string, Action<IParseNode>>()", result);
        AssertExtensions.Before("return ParseNodeHelper.MergeDeserializersForIntersectionWrapper(ComplexType1Value, ComplexType3Value)", "return new Dictionary<string, Action<IParseNode>>()", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesDeSerializerBody()
    {
        setup();
        method.Kind = CodeMethodKind.Deserializer;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("GetStringValue", result);
        Assert.Contains("GetCollectionOfPrimitiveValues", result);
        Assert.Contains("GetCollectionOfObjectValues", result);
        Assert.Contains("GetEnumValue", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{ \"DummyUCaseProp", result);
        Assert.Contains("{ \"dummyProp", result);
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
        Assert.Contains("base.Serialize(writer)", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.DoesNotContain("base.Serialize(writer)", result);
        Assert.Contains("ComplexType1Value != null", result);
        Assert.Contains("writer.WriteObjectValue<ComplexType1>(null, ComplexType1Value)", result);
        Assert.Contains("StringValue != null", result);
        Assert.Contains("writer.WriteStringValue(null, StringValue)", result);
        Assert.Contains("ComplexType2Value != null", result);
        Assert.Contains("writer.WriteCollectionOfObjectValues<ComplexType2>(null, ComplexType2Value)", result);
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
        Assert.DoesNotContain("base.Serialize(writer)", result);
        Assert.DoesNotContain("ComplexType1Value != null", result);
        Assert.Contains("writer.WriteObjectValue<ComplexType1>(null, ComplexType1Value, ComplexType3Value)", result);
        Assert.Contains("StringValue != null", result);
        Assert.Contains("writer.WriteStringValue(null, StringValue)", result);
        Assert.Contains("ComplexType2Value != null", result);
        Assert.Contains("writer.WriteCollectionOfObjectValues<ComplexType2>(null, ComplexType2Value)", result);
        AssertExtensions.Before("writer.WriteStringValue(null, StringValue)", "writer.WriteObjectValue<ComplexType1>(null, ComplexType1Value, ComplexType3Value)", result);
        AssertExtensions.Before("writer.WriteCollectionOfObjectValues<ComplexType2>(null, ComplexType2Value)", "writer.WriteObjectValue<ComplexType1>(null, ComplexType1Value, ComplexType3Value)", result);
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
        Assert.Contains("WriteCollectionOfPrimitiveValues", result);
        Assert.Contains("WriteCollectionOfObjectValues", result);
        Assert.Contains("WriteEnumValue", result);
        Assert.Contains("WriteAdditionalData(additionalData);", result);
        Assert.Contains("WriteStringValue(\"dummyProp\"", result);
        Assert.Contains("WriteStringValue(\"DummyUCaseProp\"", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesMethodAsyncDescription()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
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
        Assert.DoesNotContain("@returns a Promise of", result);
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
        Assert.Contains("<see href=", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive()
    {
        setup();
        var codeMethodWriter = new CodeMethodWriter(new CSharpConventionService());
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
    private const string TaskPrefix = "Task<";
    private const string AsyncKeyword = "async";
    [Fact]
    public void WritesReturnType()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"{AsyncKeyword} {TaskPrefix}{ReturnTypeName}> {MethodName.ToFirstCharacterUpperCase()}", result); // async default
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType()
    {
        setup();
        method.ReturnType.IsNullable = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("?", result);
    }
    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    {
        setup();
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain(TaskPrefix, result);
        Assert.DoesNotContain(AsyncKeyword, result);
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
        Assert.Contains("RequestAdapter", result);
        Assert.Contains("PathParameters", result);
        Assert.Contains("pathParam", result);
        Assert.Contains("return new", result);
    }
    [Fact]
    public void DoesNotWriteDeprecatedBodyParameterRequestBuilder()
    {
        setup();
        method.Kind = CodeMethodKind.RequestBuilderWithParameters;
        method.AddParameter(new CodeParameter
        {
            Name = "pathParam",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType
            {
                Name = "string"
            },
            Deprecation = new DeprecationInformation("parameter")
        });
        method.Deprecation = new DeprecationInformation("method");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("[Obsolete(\"method\")]", result);
        Assert.DoesNotContain("[Obsolete(\"parameter\")]", result);
    }
    [Fact]
    public void WritesConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        method.Documentation.DescriptionTemplate = "Initializes a new instance of the {TypeName} class";
        method.Documentation.TypeReferences.TryAdd("TypeName", new CodeType { TypeDefinition = parentClass, IsExternal = false });
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            },
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
        var defaultValueEscapedLineBreak = "\"someVal\n\"";
        var expectedValueEscapedLineBreak = @"""someVal\n""";
        var escapedLineBreakPropName = "propWithDefaultEscapedLineBreak";
        parentClass.AddProperty(new CodeProperty
        {
            Name = escapedLineBreakPropName,
            DefaultValue = defaultValueEscapedLineBreak,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            },
        });
        var defaultValueEscapedQuotes = "\"this is \"surrounded by quotes\"\"";
        var expectedValueEscapedQuotes = @"""this is \""surrounded by quotes\""""";
        var escapedQuotesPropName = "propWithDefaultEscapedQuotes";
        parentClass.AddProperty(new CodeProperty
        {
            Name = escapedQuotesPropName,
            DefaultValue = defaultValueEscapedQuotes,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            },
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("<summary>", result);
        Assert.Contains("<see cref=", result);
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"{propName.ToFirstCharacterUpperCase()} = {defaultValue}", result);
        Assert.Contains($"{nullPropName.ToFirstCharacterUpperCase()} = {defaultValueNull.TrimQuotes()}", result);
        Assert.Contains($"{boolPropName.ToFirstCharacterUpperCase()} = {defaultValueBool.TrimQuotes()}", result);
        Assert.Contains($"{escapedLineBreakPropName.ToFirstCharacterUpperCase()} = {expectedValueEscapedLineBreak}", result);
        Assert.Contains($"{escapedQuotesPropName.ToFirstCharacterUpperCase()} = {expectedValueEscapedQuotes}", result);
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
        Assert.Contains($"return new {parentClass.Name.ToFirstCharacterUpperCase()}", result);
    }
    [Fact]
    public void WritesConstructorWithEnumValue()
    {
        setup();
        var modelsNamespace = root.AddNamespace("models");
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "1024x1024";
        var propName = "size";
        var codeEnum = modelsNamespace.AddEnum(new CodeEnum
        {
            Name = "pictureSize"
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains($"{propName.ToFirstCharacterUpperCase()} = global::{modelsNamespace.Name}.{codeEnum.Name.ToFirstCharacterUpperCase()}.{defaultValue.CleanupSymbolName()}", result);//ensure symbol is cleaned up
    }
    [Fact]
    public void WritesConstructorAndIncludesSanitizedEnumValue()
    {
        setup();
        var modelsNamespace = root.AddNamespace("models");
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "/";
        var propName = "size";
        var codeEnum = modelsNamespace.AddEnum(new CodeEnum
        {
            Name = "pictureSize"
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("PictureSize.Slash", result);//ensure symbol is cleaned up
        Assert.Contains($"{propName.ToFirstCharacterUpperCase()} = global::{modelsNamespace.Name}.{codeEnum.Name.ToFirstCharacterUpperCase()}.{defaultValue.CleanupSymbolName()}", result);//ensure symbol is cleaned up
    }
    [Fact]
    public void WritesConstructorAndDisambiguatesEnumType()
    {
        setup();
        var modelsNamespace = root.AddNamespace("models");
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "first";
        var propName = "testEnum";
        var codeEnum = modelsNamespace.AddEnum(new CodeEnum
        {
            Name = "testEnum"// same type name as property definition
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("global::models.TestEnum.First;", result);//ensure enum is fully qualified due to conflicting property name
        Assert.Contains($"{propName.ToFirstCharacterUpperCase()} = global::models.TestEnum.First;", result);//ensure enum is fully qualified due to conflicting property name
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
        Assert.DoesNotContain($"{propName.ToFirstCharacterUpperCase()}.{unionTypeWrapper.OriginalComposedType.AllTypes.First().Name.ToFirstCharacterUpperCase()} = {defaultValue}", result);//ensure the composed type is not referenced
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
                Name = "RequestAdapter",
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
        Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
        Assert.Contains("RegisterDefaultSerializer", result);
        Assert.Contains("RegisterDefaultDeserializer", result);
        Assert.Contains($"TryAdd(\"baseurl\", Core.BaseUrl)", result);
        Assert.Contains($"BaseUrl = \"{method.BaseUrl}\"", result);
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
                Name = "RequestAdapter",
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
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("EnableBackingStore", result);
    }
    [Fact]
    public void ThrowsOnGetter()
    {
        setup();
        method.Kind = CodeMethodKind.Getter;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void ThrowsOnSetter()
    {
        setup();
        method.Kind = CodeMethodKind.Setter;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
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
    public void WritesNullableMethodPrototypeForValueType()
    {
        setup();
        method.ReturnType = new CodeType
        {
            Name = "void",
            IsExternal = true
        };
        method.Kind = CodeMethodKind.Constructor;
        method.AddParameter(new CodeParameter
        {
            Name = "ra",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "RequestAdapter",
                IsExternal = true,
                IsNullable = false
            },
            Optional = false
        });
        method.AddParameter(new CodeParameter
        {
            Name = "sampleParam",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "integer",
                IsExternal = true,
                IsNullable = true
            },
            Optional = true
        });
        parentClass.Kind = CodeClassKind.RequestBuilder;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain(expectedSubstring: "RequestAdapter? ra", result);
        Assert.Contains(expectedSubstring: "RequestAdapter ra", result);
        Assert.DoesNotContain("int sampleParam", result);
        Assert.Contains("int? sampleParam", result);
        Assert.DoesNotContain("#nullable enable", result);
        Assert.DoesNotContain("#nullable restore", result);
        Assert.Contains("if(ReferenceEquals(ra, null)) throw new ArgumentNullException(nameof(ra));", result);
    }

    [Fact]
    public void ConstructorCallsCliBaseClass()
    {
        setup();
        method.ReturnType = new CodeType
        {
            Name = "void",
            IsExternal = true
        };
        method.Kind = CodeMethodKind.Constructor;
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseCliRequestBuilder",
            IsExternal = true
        };
        parentClass.AddProperty(new CodeProperty
        {
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
            Kind = CodePropertyKind.UrlTemplate,
            DefaultValue = "\"test\"",
            Name = "urlTpl"
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(": base(\"test\")", result);
    }

    [Fact]
    public void WritesMethodWithEmptyStringAsDefaultValueIfNotNullableAndOptional()
    {
        setup();
        method.ReturnType = new CodeType
        {
            Name = "void",
            IsExternal = true
        };
        method.Kind = CodeMethodKind.Constructor;
        method.AddParameter(new CodeParameter
        {
            Name = "ra",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "RequestAdapter",
                IsExternal = true,
                IsNullable = false
            },
            Optional = false
        });
        method.AddParameter(new CodeParameter
        {
            Name = "sampleParam",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true,
                IsNullable = false
            },
            Optional = true
        });
        parentClass.Kind = CodeClassKind.RequestBuilder;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("RequestAdapter? ra", result);
        Assert.Contains("RequestAdapter ra", result);
        Assert.Contains("string sampleParam = \"\"", result);
        Assert.DoesNotContain("string? sampleParam = \"\"", result);
        Assert.DoesNotContain("#nullable enable", result);
        Assert.DoesNotContain("#nullable restore", result);
        Assert.Contains("if(ReferenceEquals(ra, null)) throw new ArgumentNullException(nameof(ra));", result);
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
        Assert.Contains("[Obsolete", result);
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
