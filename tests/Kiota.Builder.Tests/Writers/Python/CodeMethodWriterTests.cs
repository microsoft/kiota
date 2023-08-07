using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Python;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;
public class CodeMethodWriterTests : IDisposable
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
    private const string ParamName = "paramName";
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
        childClass = new CodeClass
        {
            Name = "childClass"
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
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "requestAdapter"
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
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyString",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyInteger",
            Type = new CodeType
            {
                Name = "integer"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyBoolean",
            Type = new CodeType
            {
                Name = "boolean"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyFloat",
            Type = new CodeType
            {
                Name = "decimal"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyTimespan",
            Type = new CodeType
            {
                Name = "datetime.timedelta"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyDateTime",
            Type = new CodeType
            {
                Name = "datetime.datetime"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyTime",
            Type = new CodeType
            {
                Name = "datetime.time"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyDate",
            Type = new CodeType
            {
                Name = "datetime.date"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyGuid",
            Type = new CodeType
            {
                Name = "UUID"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyClass",
            Type = new CodeType
            {
                Name = "dummyClass"
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyStream",
            Type = new CodeType
            {
                Name = "binary"
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyColl",
            Type = new CodeType
            {
                Name = "guid",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
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
        Assert.Contains("error_mapping: Dict[str, ParsableFactory] =", result);
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
        Assert.DoesNotContain("error_mapping: Dict[str, ParsableFactory]", result);
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
        Assert.Contains("request_info = RequestInformation()", result);
        Assert.Contains("request_info.http_method = Method", result);
        Assert.Contains("request_info.url_template = ", result);
        Assert.Contains("request_info.path_parameters = ", result);
        Assert.Contains("request_info.headers[\"Accept\"] = [\"application/json, text/plain\"]", result);
        Assert.Contains("if c:", result);
        Assert.Contains("request_info.add_request_headers", result);
        Assert.Contains("request_info.add_request_options", result);
        Assert.Contains("request_info.set_query_string_parameters_from_raw_object", result);
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
        Assert.Contains("request_info = RequestInformation()", result);
        Assert.Contains("request_info.http_method = Method", result);
        Assert.Contains("request_info.url_template = ", result);
        Assert.Contains("request_info.path_parameters = ", result);
        Assert.Contains("request_info.headers[\"Accept\"] = [\"application/json, text/plain\"]", result);
        Assert.Contains("if c:", result);
        Assert.Contains("request_info.add_request_headers", result);
        Assert.Contains("request_info.add_request_options", result);
        Assert.Contains("request_info.set_query_string_parameters_from_raw_object", result);
        Assert.Contains("set_content_from_parsable", result);
        Assert.Contains("return request_info", result);
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
            Name = "GetFieldDeserializers",
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "Dict[str, Callable[[ParseNode], None]]",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("super_fields = super()", result);
        Assert.DoesNotContain("return fields", result);
        Assert.Contains("if self.complex_type1_value:", result);
        Assert.Contains("return self.complex_type1_value.get_field_deserializers()", result);
        Assert.Contains("return {}", result);
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
                Name = "Dict[str, Callable[[ParseNode], None]]",
            },
        }).First();
        writer.Write(deserializationMethod);
        var result = tw.ToString();
        Assert.DoesNotContain("super_fields = super()", result);
        Assert.DoesNotContain("return fields", result);
        Assert.Contains("from .complex_type1 import ComplexType1", result);
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
        Assert.Contains("fields: Dict[str, Callable[[Any], None]] =", result);
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
        Assert.Contains("get_collection_of_object_values(Complex)", result);
        Assert.Contains("get_enum_value(SomeEnum)", result);
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
        method.Documentation.Description = MethodDescription;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                Description = ParamDescription,
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
        Assert.Contains("Args:", result);
        Assert.Contains("param_name", result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("Returns:", result);
        Assert.Contains("await", result);
    }
    [Fact]
    public void WritesMethodSyncDescription()
    {
        setup();
        method.Documentation.Description = MethodDescription;
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                Description = ParamDescription,
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
        Assert.Contains("Args:", result);
        Assert.Contains("param_name", result);
        Assert.Contains(ParamDescription, result);
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("try:", result);
        Assert.Contains("mapping_value = parse_node.get_child_node(\"@odata.type\").get_str_value()", result);
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
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode"
            }
        });
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Assert.Contains("try:", result);
        Assert.Contains("mapping_value = parse_node.get_child_node(\"@odata.type\").get_str_value()", result);
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
            Name = "parseNode",
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
            SerializationName = "id",
            IndexType = new CodeType
            {
                Name = "string",
                IsNullable = true,
            },
            ReturnType = new CodeType
            {
                Name = "string",
            },
            IndexParameterName = "id",
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
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("@property", result);
        Assert.Contains("return self.backing_store.get(\"some_property\")", result);
    }
    [Fact]
    public void WritesGetterNullBackingStore()
    {
        setup();
        method.AddAccessedProperty();
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
        Assert.Contains("self.backing_store[\"some_property\"] = value", result);
    }
    [Fact]
    public void WritesGetterToField()
    {
        setup();
        method.AddAccessedProperty();
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
                Description = "This property has a description",
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
                Name = "Union[Dict[str, Any], str]",
                IsNullable = true,
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("super().__init__(self)", result);
        Assert.DoesNotContain("This property has a description", result);
        Assert.DoesNotContain($"self.{propName}: Optional[str] = {defaultValue}", result);
        Assert.DoesNotContain("get_path_parameters(", result);
    }
    [Fact]
    public void DoesntWriteConstructorForModelClasses()
    {
        setup();
        method.AddAccessedProperty();
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
                Description = "This property has a description",
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
                Description = "This property has a description",
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
                Description = "This property has a description",
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
                Name = "Union[Dict[str, Any], str]",
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
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "Dict[str, str]",
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
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("enable_backing_store", result);
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
            Name = "select-from",
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
            Name = "originalName",
            Type = new CodeType
            {
                Name = "string",
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("if not original_name:", result);
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
            Name = "startDateTime",
            Kind = CodePropertyKind.QueryParameter,
            Type = new CodeType
            {
                Name = "datetime",
            },
        });
        method.AddParameter(new CodeParameter
        {
            Kind = CodeParameterKind.QueryParametersMapperParameter,
            Name = "originalName",
            Type = new CodeType
            {
                Name = "string",
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("if not original_name:", result);
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
}
