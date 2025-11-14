using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Python;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

public sealed class CodeMethodWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private CodeMethod method;
    private CodeClass parentClass;
    private CodeClass childClass;
    private readonly CodeNamespace root;
    private const string ClientNamespaceName = "graph";
    private const string MethodName = "method_name";
    private const string ReturnTypeName = "Somecustomtype";
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "param_name";


    public CodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
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
                Name = "defined_in_parent",
                Type = new CodeType
                {
                    Name = "string"
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
        childClass = new CodeClass
        {
            Name = "ChildClass"
        };
        root.AddClass(childClass);
        var returnTypeClassDef = new CodeClass
        {
            Name = ReturnTypeName,
        };
        root.AddClass(returnTypeClassDef);
        var nUsing = new CodeUsing
        {
            Name = returnTypeClassDef.Name,
            Declaration = new()
            {
                Name = returnTypeClassDef.Name,
                TypeDefinition = returnTypeClassDef,
            }
        };
        parentClass.StartBlock.AddUsings(nUsing);
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
        parentClass.AddProperty(new CodeProperty
        {
            Name = "request_adapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "path_parameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "string"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "url_template",
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
            Name = "additional_data",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = "string"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_string",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_integer",
            Type = new CodeType
            {
                Name = "integer"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_boolean",
            Type = new CodeType
            {
                Name = "boolean"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_float",
            Type = new CodeType
            {
                Name = "decimal"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_timespan",
            Type = new CodeType
            {
                Name = "datetime.timedelta"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_date_time",
            Type = new CodeType
            {
                Name = "datetime.datetime"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_time",
            Type = new CodeType
            {
                Name = "datetime.time"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_date",
            Type = new CodeType
            {
                Name = "datetime.date"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_guid",
            Type = new CodeType
            {
                Name = "UUID"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_class",
            Type = new CodeType
            {
                Name = "DummyClass"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_stream",
            Type = new CodeType
            {
                Name = "binary"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_coll",
            Type = new CodeType
            {
                Name = "guid",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_complex_coll",
            Type = new CodeType
            {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = new CodeClass
                {
                    Name = "SomeComplexType"
                }
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummy_enum_collection",
            Type = new CodeType
            {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum
                {
                    Name = "EnumType"
                }
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "defined_in_parent",
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
        var nUsingComplexType1 = new CodeUsing
        {
            Name = complexType1.Name,
            Declaration = new()
            {
                Name = complexType1.Name,
                TypeDefinition = complexType1,
            }
        };
        var nUsingComplexType2 = new CodeUsing
        {
            Name = complexType2.Name,
            Declaration = new()
            {
                Name = complexType2.Name,
                TypeDefinition = complexType2,
            }
        };
        unionTypeWrapper.StartBlock.AddUsings(nUsingComplexType1, nUsingComplexType2);
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
            Name = "complex_type1_value",
            Type = cType1,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "complex_type2_value",
            Type = cType2,
            Kind = CodePropertyKind.Custom
        });
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "string_value",
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
        var nUsingComplexType1 = new CodeUsing
        {
            Name = complexType1.Name,
            Declaration = new()
            {
                Name = complexType1.Name,
                TypeDefinition = complexType1,
            }
        };
        var nUsingComplexType2 = new CodeUsing
        {
            Name = complexType2.Name,
            Declaration = new()
            {
                Name = complexType2.Name,
                TypeDefinition = complexType2,
            }
        };
        intersectionTypeWrapper.StartBlock.AddUsings(nUsingComplexType1, nUsingComplexType2);
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
            Name = "complex_type1_value",
            Type = cType1,
            Kind = CodePropertyKind.Custom
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "complex_type2_value",
            Type = cType2,
            Kind = CodePropertyKind.Custom
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "complex_type3_value",
            Type = cType3,
            Kind = CodePropertyKind.Custom
        });
        intersectionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "string_value",
            Type = sType,
            Kind = CodePropertyKind.Custom
        });
        return intersectionTypeWrapper;
    }
    private void AddCodeUsings()
    {
        var nUsing = new CodeUsing
        {
            Name = childClass.Name,
            Declaration = new()
            {
                Name = childClass.Name,
                TypeDefinition = childClass,
            }
        };
        parentClass.StartBlock.AddUsings(nUsing);
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
            IsErrorDefinition = true
        }).First();
        var error5XX = root.AddClass(new CodeClass
        {
            Name = "Error5XX",
            IsErrorDefinition = true
        }).First();
        var error401 = root.AddClass(new CodeClass
        {
            Name = "Error401",
            IsErrorDefinition = true
        }).First();
        parentClass.StartBlock.AddUsings(new()
        {
            Name = error401.Name,
            Declaration = new()
            {
                Name = error401.Name,
                TypeDefinition = error401,
            }
        },
        new()
        {
            Name = error5XX.Name,
            Declaration = new()
            {
                Name = error5XX.Name,
                TypeDefinition = error5XX,
            }
        },
        new()
        {
            Name = error4XX.Name,
            Declaration = new()
            {
                Name = error4XX.Name,
                TypeDefinition = error4XX,
            }
        });
        method.AddErrorMapping("4XX", new CodeType { Name = "Error4XX", TypeDefinition = error4XX });
        method.AddErrorMapping("5XX", new CodeType { Name = "Error5XX", TypeDefinition = error5XX });
        method.AddErrorMapping("401", new CodeType { Name = "Error401", TypeDefinition = error401 });
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("request_info", result);
        Assert.Contains("from .error401 import Error401", result);
        Assert.Contains("from .error4_x_x import Error4XX", result);
        Assert.Contains("from .error5_x_x import Error5XX", result);
        Assert.Contains("error_mapping: dict[str, type[ParsableFactory]] =", result);
        Assert.Contains("\"4XX\": Error4XX", result);
        Assert.Contains("\"5XX\": Error5XX", result);
        Assert.Contains("\"401\": Error401", result);
        Assert.Contains("from .somecustomtype import Somecustomtype", result);
        Assert.Contains("send_async", result);
        Assert.Contains("raise Exception", result);
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
        Assert.DoesNotContain("error_mapping: dict[str, ParsableFactory]", result);
        Assert.Contains("cannot be null", result);
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
        Assert.Contains("from .somecustomtype import Somecustomtype", result);
        Assert.Contains("send_collection_async", result);
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
        method.AcceptedResponseTypes.Add("text/plain");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("request_info = RequestInformation(Method.GET, self.url_template, self.path_parameters)", result);
        Assert.Contains("request_info.headers.try_add(\"Accept\", \"application/json, text/plain\")", result);
        Assert.Contains("request_info.configure(c)", result);
        Assert.Contains("set_content_from_scalar", result);
        Assert.Contains("return request_info", result);
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
        method.AcceptedResponseTypes.Add("text/plain");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("request_info = RequestInformation(Method.GET, self.url_template, self.path_parameters", result);
        Assert.Contains("request_info.headers.try_add(\"Accept\", \"application/json, text/plain\")", result);
        Assert.Contains("request_info.configure(c)", result);
        Assert.Contains("set_content_from_parsable", result);
        Assert.Contains("return request_info", result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForMultipart()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = "MultipartBody",
            IsExternal = true,
        };
        method.RequestBodyContentType = "multipart/form-data";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("set_content_from_parsable", result);
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
        method.AcceptedResponseTypes.Add("text/plain");
        method.UrlTemplateOverride = "{baseurl+}/foo/bar";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("request_info = RequestInformation(Method.GET, '{baseurl+}/foo/bar', self.path_parameters", result);
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
            Name = new PythonConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.RequestBodyContentType = "application/json";
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("set_stream_content", result, StringComparison.OrdinalIgnoreCase);
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
            Name = new PythonConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.AddParameter(new CodeParameter
        {
            Name = "request_content_type",
            Type = new CodeType()
            {
                Name = "string",
                IsExternal = true,
            },
            Kind = CodeParameterKind.RequestBodyContentType,
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("set_stream_content", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("application/json", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(", request_content_type", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("from .somecustomtype import Somecustomtype", result);
        Assert.Contains("super_fields = super()", result);
        Assert.Contains("fields.update(super_fields)", result);
        Assert.Contains("return fields", result);
        Assert.DoesNotContain("defined_in_parent", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WritesUnionDeSerializerBody()
    {
        setup();
        var wrapper = AddUnionTypeWrapper();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "get_field_deserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "dict[str, Callable[[ParseNode], None]]",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("super_fields = super()", result);
        Assert.DoesNotContain("return fields", result);
        Assert.DoesNotContain("elif", result);
        Assert.Contains("if self.complex_type1_value:", result);
        Assert.Contains("return self.complex_type1_value.get_field_deserializers()", result);
        Assert.Contains("return {}", result);
    }
    [Theory]
    [InlineData(true, false, false, false, "string", "")]
    [InlineData(false, true, false, false, "Stream", " \"Stream\",")]
    [InlineData(false, false, true, false, "SomeEnum", " \"SomeEnum\",")]
    [InlineData(false, false, false, true, "int", " int,")]
    [InlineData(false, false, false, false, "int", " \"int\",")]
    [InlineData(false, false, false, false, "CustomType", " CustomType,")]
    public void GetTypeFactory_ReturnsCorrectString(bool isVoid, bool isStream, bool isEnum, bool isCollection, string returnType, string expected)
    {
        var mockConventionService = new Mock<PythonConventionService>();

        var codeMethodWriter = new CodeMethodWriter(
            mockConventionService.Object,
            "TestNamespace",
            false // usesBackingStore
        );

        var result = codeMethodWriter.GetTypeFactory(isVoid, isStream, isEnum, returnType, isCollection);
        Assert.Equal(expected, result);
    }
    [Fact]
    public void WritesIntersectionDeSerializerBody()
    {
        setup();
        var wrapper = AddIntersectionTypeWrapper();
        var deserializationMethod = wrapper.AddMethod(new CodeMethod
        {
            Name = "get_field_deserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "dict[str, Callable[[ParseNode], None]]",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("super_fields = super()", result);
        Assert.DoesNotContain("return fields", result);
        Assert.Contains("from .complex_type1 import ComplexType1", result);
        Assert.DoesNotContain("elif", result);
        Assert.Contains("if self.complex_type1_value or self.complex_type3_value", result);
        Assert.Contains("return ParseNodeHelper.merge_deserializers_for_intersection_wrapper(self.complex_type1_value, self.complex_type3_value)", result);
        Assert.Contains("return {}", result);
        AssertExtensions.Before("return ParseNodeHelper.merge_deserializers_for_intersection_wrapper(self.complex_type1_value, self.complex_type3_value)", "return {}", result);
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
        Assert.Contains("from .somecustomtype import Somecustomtype", result);
        Assert.Contains("fields: dict[str, Callable[[Any], None]] =", result);
        Assert.Contains("get_str_value()", result);
        Assert.Contains("get_int_value()", result);
        Assert.Contains("get_float_value()", result);
        Assert.Contains("get_bool_value()", result);
        Assert.Contains("get_bytes_value()", result);
        Assert.Contains("get_date_value()", result);
        Assert.Contains("get_time_value()", result);
        Assert.Contains("get_timedelta_value()", result);
        Assert.Contains("get_datetime_value()", result);
        Assert.Contains("get_uuid_value()", result);
        Assert.Contains("get_object_value(DummyClass)", result);
        Assert.Contains("get_collection_of_primitive_values(UUID)", result);
        Assert.Contains("get_collection_of_object_values(SomeComplexType)", result);
        Assert.Contains("get_enum_value(EnumType)", result);
        Assert.Contains("defined_in_parent", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("super().serialize", result);
        Assert.DoesNotContain("defined_in_parent", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.DoesNotContain("super().serialize", result);
        Assert.Contains("if self.complex_type1_value:", result);
        Assert.Contains("writer.write_object_value(None, self.complex_type1_value)", result);
        Assert.Contains("if self.string_value:", result);
        Assert.Contains("writer.write_str_value(None, self.string_value)", result);
        Assert.Contains("if self.complex_type2_value:", result);
        Assert.Contains("writer.write_collection_of_object_values(None, self.complex_type2_value)", result);
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
        Assert.DoesNotContain("super().serialize", result);
        Assert.DoesNotContain("if self.complex_type1_value:", result);
        Assert.Contains("writer.write_object_value(None, self.complex_type1_value, self.complex_type3_value)", result);
        Assert.Contains("if self.string_value:", result);
        Assert.Contains("writer.write_str_value(None, self.string_value)", result);
        Assert.Contains("if self.complex_type2_value:", result);
        Assert.Contains("writer.write_collection_of_object_values(None, self.complex_type2_value)", result);
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
        Assert.Contains("write_str_value", result);
        Assert.Contains("write_bool_value", result);
        Assert.Contains("write_int_value", result);
        Assert.Contains("write_float_value", result);
        Assert.Contains("write_datetime_value", result);
        Assert.Contains("write_timedelta_value", result);
        Assert.Contains("write_date_value", result);
        Assert.Contains("write_time_value", result);
        Assert.Contains("write_uuid_value", result);
        Assert.Contains("write_object_value", result);
        Assert.Contains("write_collection_of_primitive_values", result);
        Assert.Contains("write_collection_of_object_values", result);
        Assert.Contains("write_enum_value", result);
        Assert.Contains("write_additional_data_value(self.additional_data)", result);
        Assert.Contains("defined_in_parent", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("()", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void WritesMethodAsyncDescription()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.Documentation.DocumentationLabel = "see more";
        method.Documentation.DocumentationLink = new("https://example.org/docs");
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
        Assert.Contains("\"\"\"", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("param param_name:", result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("see more:", result);
        Assert.Contains("https://example.org/docs", result);
        Assert.Contains("Returns: Optional[Somecustomtype]", result);
        Assert.Contains("await", result);
    }
    [Fact]
    public void WritesMethodSyncDescription()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.Documentation.DocumentationLabel = "see more";
        method.Documentation.DocumentationLink = new("https://example.org/docs");
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
        Assert.Contains("\"\"\"", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("param param_name:", result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("see more:", result);
        Assert.Contains("https://example.org/docs", result);
        Assert.Contains("Returns: Optional[Somecustomtype]", result);
        Assert.DoesNotContain("await", result);
    }
    [Fact]
    public void Defensive()
    {
        setup();
        var codeMethodWriter = new CodeMethodWriter(new PythonConventionService(), ClientNamespaceName, false);
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
        var originalParent = method.Parent;
        method.Parent = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
        method.Parent = originalParent;
    }
    [Fact]
    public void ThrowsIfMethodIsRawUrlConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.RawUrlConstructor;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
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
        Assert.Contains(MethodName, result);
        Assert.Contains(ReturnTypeName, result);
        Assert.Contains("Optional[", result);// nullable default
    }
    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType()
    {
        setup();
        method.ReturnType.IsNullable = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("Optional[", result);
    }
    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    {
        setup();
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("async", result);
    }
    [Fact]
    public void WritesFactoryMethods()
    {
        setup();
        method.Kind = CodeMethodKind.Factory;
        method.AddParameter(new CodeParameter
        {
            Name = "parse_node",
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("@staticmethod", result);
        Assert.DoesNotContain("self", result);
    }
    [Fact]
    public void WritesModelFactoryBodyForInheritedModels()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        childClass.Kind = CodeClassKind.Model;
        childClass.StartBlock.Inherits = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.Kind = CodeMethodKind.Factory;
        method.ReturnType = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.IsStatic = true;
        parentClass.DiscriminatorInformation.AddDiscriminatorMapping("ns.childclass", new CodeType
        {
            Name = "childClass",
            TypeDefinition = childClass,
        });
        parentClass.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        AddCodeUsings();
        method.AddParameter(new CodeParameter
        {
            Name = "parse_node",
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("try:", result);
        Assert.Contains("child_node = parse_node.get_child_node(\"@odata.type\")", result);
        Assert.Contains("mapping_value = child_node.get_str_value() if child_node else None", result);
        Assert.Contains("except AttributeError:", result);
        Assert.Contains("mapping_value = None", result);
        Assert.Contains("if mapping_value and mapping_value.casefold() == \"ns.childclass\".casefold()", result);
        Assert.Contains("from .child_class import ChildClass", result);
        Assert.Contains("return ChildClass()", result);
        Assert.Contains("return ParentClass()", result);
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
            Name = "parse_node",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode"
            }
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("try:", result);
        Assert.Contains("child_node = parse_node.get_child_node(\"@odata.type\")", result);
        Assert.Contains("mapping_value = child_node.get_str_value() if child_node else None", result);
        Assert.Contains("except AttributeError:", result);
        Assert.Contains("mapping_value = None", result);
        Assert.Contains("result = UnionTypeWrapper()", result);
        Assert.Contains("if mapping_value and mapping_value.casefold() == \"#kiota.complexType1\".casefold():", result);
        Assert.Contains("from .complex_type1 import ComplexType1", result);
        Assert.Contains("result.complex_type1_value = ComplexType1()", result);
        Assert.Contains("elif string_value_value := parse_node.get_str_value():", result);
        Assert.Contains("result.string_value = string_value_value", result);
        Assert.Contains("elif complex_type2_value_value := parse_node.get_collection_of_object_values(ComplexType2):", result);
        Assert.Contains("result.complex_type2_value = complex_type2_value_value", result);
        Assert.Contains("return result", result);
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
            Name = "parse_node",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode"
            }
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("try:", result);
        Assert.DoesNotContain("mapping_value = parse_node.get_child_node(\"@odata.type\").get_str_value()", result);
        Assert.Contains("result = IntersectionTypeWrapper()", result);
        Assert.DoesNotContain("if mapping_value and mapping_value.casefold() == \"#kiota.complexType1\".casefold():", result);
        Assert.Contains("if string_value_value := parse_node.get_str_value():", result);
        Assert.Contains("result.string_value = string_value_value", result);
        Assert.Contains("elif complex_type2_value_value := parse_node.get_collection_of_object_values(ComplexType2):", result);
        Assert.Contains("result.complex_type2_value = complex_type2_value_value", result);
        Assert.Contains("else:", result);
        Assert.Contains("from .complex_type1 import ComplexType1", result);
        Assert.Contains("result.complex_type1_value = ComplexType1()", result);
        Assert.Contains("return result", result);
    }
    [Fact]
    public void DoesntWriteFactoryConditionalsOnMissingParameter()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        childClass.Kind = CodeClassKind.Model;
        childClass.StartBlock.Inherits = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.Kind = CodeMethodKind.Factory;
        method.ReturnType = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.IsStatic = true;
        parentClass.DiscriminatorInformation.AddDiscriminatorMapping("ns.childclass", new CodeType
        {
            Name = "childClass",
            TypeDefinition = childClass,
        });
        parentClass.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void DoesntWriteFactoryConditionalsOnEmptyPropertyName()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        childClass.Kind = CodeClassKind.Model;
        childClass.StartBlock.Inherits = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.Kind = CodeMethodKind.Factory;
        method.ReturnType = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.IsStatic = true;
        parentClass.DiscriminatorInformation.AddDiscriminatorMapping("ns.childclass", new CodeType
        {
            Name = "childClass",
            TypeDefinition = childClass,
        });
        parentClass.DiscriminatorInformation.DiscriminatorPropertyName = string.Empty;
        method.AddParameter(new CodeParameter
        {
            Name = "parse_node",
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("mapping_value_node = parse_node.get_child_node(\"@odata.type\")", result);
        Assert.DoesNotContain("if mapping_value_node:", result);
        Assert.DoesNotContain("mapping_value = mapping_value_node.get_str_value", result);
        Assert.DoesNotContain("if mapping_value == \"ns.childclass\"", result);
        Assert.Contains("return ParentClass()", result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyMappings()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        method.Kind = CodeMethodKind.Factory;
        method.ReturnType = new CodeType
        {
            Name = "parentClass",
            TypeDefinition = parentClass,
        };
        method.IsStatic = true;
        parentClass.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        method.AddParameter(new CodeParameter
        {
            Name = "parse_node",
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("mapping_value_node = parse_node.get_child_node(\"@odata.type\")", result);
        Assert.DoesNotContain("if mapping_value_node:", result);
        Assert.DoesNotContain("mapping_value = mapping_value_node.get_str_value", result);
        Assert.DoesNotContain("if mapping_value == \"ns.childclass\"", result);
        Assert.Contains("return ParentClass()", result);
    }
    [Fact]
    public void WritesPublicMethodByDefault()
    {
        setup();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain($"_{MethodName}", result); ;// public default
    }
    [Fact]
    public void WritesProtectedMethod()
    {
        setup();
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"_{MethodName}", result);
    }
    [Fact]
    public void WritesIndexer()
    {
        setup();
        AddRequestProperties();
        method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
        method.OriginalIndexer = new()
        {
            Name = "indx",
            ReturnType = new CodeType
            {
                Name = "string",
            },
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string",
                    IsNullable = true,
                },
                SerializationName = "id",
            }
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.request_adapter", result);
        Assert.Contains("self.path_parameters", result);
        Assert.Contains("id", result);
        Assert.Contains("return", result);
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
        Assert.Contains("from .somecustomtype import Somecustomtype", result);
        Assert.Contains("self.request_adapter", result);
        Assert.Contains("self.path_parameters", result);
        Assert.Contains("path_param", result);
        Assert.Contains("return", result);
    }
    [Fact]
    public void WritesGetterToBackingStore()
    {
        setup();
        parentClass.GetGreatestGrandparent().AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("@property", result);
        Assert.Contains("return self.backingStore.get(\"some_property\")", result);
    }
    [Fact]
    public void WritesGetterNullBackingStore()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("return self.some_property", result);
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
        Assert.Contains("if not value:", result);
        Assert.Contains(defaultValue, result);
    }
    [Fact]
    public void WritesSetterNullBackingStore()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.some_property = value", result);
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
        Assert.Contains("self.backingStore[\"someProperty\"] = value", result);
    }
    [Fact]
    public void WritesGetterToField()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.some_property", result);
    }
    [Fact]
    public void DoesntWriteGetterToFieldForModelClasses()
    {
        setup();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        parentClass.Kind = CodeClassKind.Model;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Empty(result);
    }
    [Fact]
    public void WritesSetterToField()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.some_property = value", result);
    }
    [Fact]
    public void DoesntWritesSetterToFieldForModelClasses()
    {
        setup();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        parentClass.Kind = CodeClassKind.Model;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Empty(result);
    }
    [Fact]
    public void WritesConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var propName = "prop_without_default_value";
        parentClass.Kind = CodeClassKind.Custom;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            Kind = CodePropertyKind.Custom,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("def __init__(self,)", result);
        Assert.Contains("This property has a description", result);
        Assert.Contains($"self.{propName}: Optional[str] = None", result);
        Assert.DoesNotContain("get_path_parameters(", result);
    }
    [Fact]
    public void EscapesCommentCharactersInDescription()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        parentClass.Kind = CodeClassKind.Custom;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "prop_without_default_value",
            Kind = CodePropertyKind.Custom,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description with comments \"\"\".",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("This property has a description with comments \\\"\\\"\\\".", result);
    }
    [Fact]
    public void WritesWithUrl()
    {
        setup();
        method.Kind = CodeMethodKind.RawUrlBuilder;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.AddParameter(new CodeParameter
        {
            Name = "raw_url",
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
        Assert.Contains($"return {parentClass.Name}", result);
        Assert.Contains("request_adapter, raw_url", result);
    }
    [Fact]
    public void WritesConstructorForRequestBuilder()
    {
        setup(true);
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var defaultValue = "someVal";
        var propName = "prop_with_default_value";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("def __init__(self,)", result);
        Assert.DoesNotContain("This property has a description", result);
        Assert.DoesNotContain($"self.{propName}: Optional[str] = {defaultValue}", result);
        Assert.DoesNotContain("get_path_parameters(", result);
        Assert.Contains("super().__init__()", result);
    }
    [Fact]
    public void WritesConstructorForRequestBuilderWithRequestAdapter()
    {
        setup(true);
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var defaultValue = "someVal";
        var propName = "prop_with_default_value";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        // AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "request_adapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter"
            },
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("super().__init__(self)", result);
        Assert.Contains("def __init__(self,request_adapter: RequestAdapter)", result);
        Assert.DoesNotContain("This property has a description", result);
        Assert.DoesNotContain($"self.{propName}: Optional[str] = {defaultValue}", result);
        Assert.DoesNotContain("get_path_parameters(", result);
        Assert.Contains("super().__init__(request_adapter, someVal, None)", result);
    }
    [Fact]
    public void WritesConstructorForRequestBuilderWithRequestAdapterAndPathParameters()
    {
        setup(true);
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var defaultValue = "someVal";
        var propName = "prop_with_default_value";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        // AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "request_adapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter"
            },
        });
        method.AddParameter(new CodeParameter
        {
            Name = "path_parameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = "Union[dict[str, Any], str]",
                IsNullable = true,
            },
        });
        method.AddParameter(new CodeParameter
        {
            Kind = CodeParameterKind.Path,
            Name = "username",
            Optional = true,
            Type = new CodeType
            {
                Name = "string",
                IsNullable = true
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("super().__init__(self)", result);
        Assert.Contains("def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[dict[str, Any], str],", result);
        Assert.Contains("username: Optional[str] = None", result);
        Assert.Contains("if isinstance(path_parameters, dict):", result);
        Assert.Contains("path_parameters['username'] = username", result);
        Assert.DoesNotContain("This property has a description", result);
        Assert.DoesNotContain($"self.{propName}: Optional[str] = {defaultValue}", result);
        Assert.DoesNotContain("get_path_parameters(", result);
        Assert.Contains("super().__init__(request_adapter, someVal, path_parameters)", result);
    }
    [Fact]
    public void DoesntWriteConstructorForModelClasses()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var defaultValue = "someVal";
        var propName = "prop_with_default_value";
        parentClass.Kind = CodeClassKind.Model;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.AdditionalData,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("def __init__()", result);
        Assert.DoesNotContain("super().__init__()", result);
        Assert.Contains("has a description", result);
        Assert.Contains($"{propName}: Optional[str] = {defaultValue}", result);
        Assert.Contains($"some_property: Optional[str] = None", result);
    }
    [Fact]
    public void WritesModelClasses()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Constructor;
        parentClass.Kind = CodeClassKind.Model;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("def __init__()", result);
        Assert.DoesNotContain("super().__init__()", result);
        Assert.Contains($"some_property: Optional[str] = None", result);
    }
    [Fact]
    public void WritesModelClassesWithDefaultEnumValue()
    {
        setup();
        method.AddAccessedProperty();
        method.AccessedProperty.Name = "some_property";
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "1024x1024";
        var propName = "size";
        var codeEnum = new CodeEnum
        {
            Name = "PictureSize"
        };
        root.AddEnum(codeEnum);
        parentClass.Kind = CodeClassKind.Model;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = codeEnum.Name,
                TypeDefinition = codeEnum

            }
        });
        var nUsing = new CodeUsing
        {
            Name = codeEnum.Name,
            Declaration = new()
            {
                Name = codeEnum.Name,
                TypeDefinition = codeEnum,
            }
        };
        parentClass.StartBlock.AddUsings(nUsing);
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("def __init__()", result);
        Assert.DoesNotContain("super().__init__()", result);
        Assert.Contains($"from .{codeEnum.Name.ToSnakeCase()} import {codeEnum.Name.ToFirstCharacterUpperCase()}", result);
        Assert.Contains("has a description", result);
        Assert.Contains($"{propName}: Optional[{codeEnum.Name}] = {codeEnum.Name}({defaultValue})", result);
        Assert.Contains($"some_property: Optional[str] = None", result);
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
        Assert.Contains("__init__", result);
        Assert.DoesNotContain(defaultValue, result);//ensure the composed type is not referenced
    }
    [Fact]
    public void WritesConstructorWithInheritance()
    {
        setup(true);
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var propName = "prop_with_no_default_value";
        parentClass.Kind = CodeClassKind.Model;
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            Kind = CodePropertyKind.Custom,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        var defaultValue = "someVal";
        var prop2Name = "prop_with_default_value";
        parentClass.Kind = CodeClassKind.RequestBuilder;
        parentClass.AddProperty(new CodeProperty
        {
            Name = prop2Name,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.UrlTemplate,
            Documentation = new()
            {
                DescriptionTemplate = "This property has a description",
            },
            Type = new CodeType
            {
                Name = "string"
            }
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = "Union[dict[str, Any], str]",
                IsNullable = true,
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("super().__init__()", result);
        Assert.DoesNotContain("has a description", result);
        Assert.DoesNotContain($"self.{prop2Name}: Optional[str] = {defaultValue}", result);
        Assert.DoesNotContain($"self.{propName}: Optional[str] = None", result);
    }
    [Fact]
    public void WritesApiConstructor()
    {
        setup();
        method.Kind = CodeMethodKind.ClientConstructor;
        method.IsAsync = false;
        method.BaseUrl = "https://graph.microsoft.com/v1.0";
        parentClass.AddProperty(new CodeProperty
        {
            Name = "path_parameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "dict[str, str]",
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
            },
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
        Assert.Contains("__init__(", result);
        Assert.Contains("register_default_serializer", result);
        Assert.Contains("register_default_deserializer", result);
        Assert.Contains("self.core.base_url = \"https://graph.microsoft.com/v1.0\"", result);
        Assert.Contains("self.path_parameters[\"base_url\"] = self.core.base_url", result);
    }
    [Fact]
    public void WritesBackedModelConstructor()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        method.Kind = CodeMethodKind.Constructor;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "backing_store",
            Kind = CodePropertyKind.BackingStore,
            Access = AccessModifier.Public,
            DefaultValue = "field(default_factory=BackingStoreFactorySingleton(backing_store_factory=None).backing_store_factory.create_backing_store, repr=False)",
            Type = new CodeType
            {
                Name = "BackingStore",
                IsExternal = true,
                IsNullable = false,
            }
        });
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("backing_store: BackingStore = field(default_factory=BackingStoreFactorySingleton(backing_store_factory=None).backing_store_factory.create_backing_store, repr=False)", result);
    }
    [Fact]
    public void WritesApiConstructorWithBackingStore()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        method.Kind = CodeMethodKind.ClientConstructor;
        var requestAdapterProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "request_adapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "request_adapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = requestAdapterProp.Type,
        });
        var backingStoreParam = new CodeParameter
        {
            Name = "backing_store",
            Kind = CodeParameterKind.BackingStore,
            Type = new CodeType
            {
                Name = "BackingStoreFactory",
                IsExternal = true,
                IsNullable = true,
            }
        };
        method.AddParameter(backingStoreParam);
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("backing_store: BackingStoreFactory)", result);
        Assert.Contains("self.request_adapter.enable_backing_store(backing_store)", result);
    }
    [Fact]
    public void WritesNameMapperMethod()
    {
        setup();
        method.Kind = CodeMethodKind.QueryParametersMapper;
        method.IsAsync = false;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "select",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string",
            },
        },
        new CodeProperty
        {
            Name = "expand",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24expand",
            Type = new CodeType
            {
                Name = "string",
            },
        },
        new CodeProperty
        {
            Name = "select_from",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "select%2Dfrom",
            Type = new CodeType
            {
                Name = "string",
            },
        },
        new CodeProperty
        {
            Name = "filter",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24filter",
            Type = new CodeType
            {
                Name = "string",
            },
        });
        method.AddParameter(new CodeParameter
        {
            Kind = CodeParameterKind.QueryParametersMapperParameter,
            Name = "original_name",
            Type = new CodeType
            {
                Name = "string",
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("if original_name is None:", result);
        Assert.Contains("if original_name == \"select\":", result);
        Assert.Contains("return \"%24select\"", result);
        Assert.Contains("if original_name == \"expand\":", result);
        Assert.Contains("return \"%24expand\"", result);
        Assert.Contains("if original_name == \"select_from\":", result);
        Assert.Contains("return \"select%2Dfrom\"", result);
        Assert.Contains("if original_name == \"filter\":", result);
        Assert.Contains("return \"%24filter\"", result);
        Assert.Contains("return original_name", result);
    }
    [Fact]
    public void WritesNameMapperMethodWithUnescapedProperties()
    {
        setup();
        method.Kind = CodeMethodKind.QueryParametersMapper;
        method.IsAsync = false;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "start_date_time",
            SerializationName = "startDateTime",
            Kind = CodePropertyKind.QueryParameter,
            Type = new CodeType
            {
                Name = "datetime",
            },
        });
        method.AddParameter(new CodeParameter
        {
            Kind = CodeParameterKind.QueryParametersMapperParameter,
            Name = "original_name",
            Type = new CodeType
            {
                Name = "string",
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("if original_name is None:", result);
        Assert.Contains("if original_name == \"start_date_time\":", result);
        Assert.Contains("return \"startDateTime\"", result);
        Assert.Contains("return original_name", result);
    }
    [Fact]
    public void MapperMethodFailsIfNoQueryParametersMapperParameter()
    {
        setup();
        method.Kind = CodeMethodKind.QueryParametersMapper;
        method.IsAsync = false;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "select",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string",
            },
        },
        new CodeProperty
        {
            Name = "expand",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24expand",
            Type = new CodeType
            {
                Name = "string",
            },
        },
        new CodeProperty
        {
            Name = "filter",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24filter",
            Type = new CodeType
            {
                Name = "string",
            },
        });
        method.AddParameter(new CodeParameter
        {
            Kind = CodeParameterKind.RawUrl,
            Name = "originalName",
            Type = new CodeType
            {
                Name = "string",
            }
        });
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
        Assert.Contains("warn(", result);
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
        Assert.Contains("request_info.headers.try_add(\"Accept\", \"application/json; profile=\\\"CamelCase\\\"\")", result);
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
