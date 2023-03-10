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
    private readonly CodeMethod method;
    private readonly CodeClass parentClass;
    private readonly CodeClass childClass;
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
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
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
                Name = "string",
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
            },
            OriginalPropertyFromBaseType = new CodeProperty
            {
                Name = "definedInParent",
                Type = new CodeType
                {
                    Name = "string"
                }
            }
        });
    }
    private void AddInheritanceClass()
    {
        parentClass.StartBlock.Inherits = new CodeType
        {
            Name = "someParentClass"
        };
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
        method.AddParameter(new CodeParameter
        {
            Name = "r",
            Kind = CodeParameterKind.ResponseHandler,
            Type = stringType,
        });
    }
    [Fact]
    public void WritesRequestBuilder()
    {
        method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesRequestBodiesThrowOnNullHttpMethod()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.Kind = CodeMethodKind.RequestGenerator;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesRequestExecutorBody()
    {
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
        Assert.Contains("from . import error4_x_x, error401, error5_x_x", result);
        Assert.Contains("error_mapping: Dict[str, ParsableFactory] =", result);
        Assert.Contains("\"4XX\": error4_x_x.Error4XX", result);
        Assert.Contains("\"5XX\": error5_x_x.Error5XX", result);
        Assert.Contains("\"401\": error401.Error401", result);
        Assert.Contains("from . import somecustomtype", result);
        Assert.Contains("send_async", result);
        Assert.Contains("raise Exception", result);
    }
    [Fact]
    public void DoesntCreateDictionaryOnEmptyErrorMapping()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("error_mapping: Dict[str, ParsableFactory]", result);
        Assert.Contains("cannot be undefined", result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForCollections()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("from . import somecustomtype", result);
        Assert.Contains("send_collection_async", result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForScalar()
    {
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
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("from . import somecustomtype", result);
        Assert.Contains("super_fields = super()", result);
        Assert.Contains("fields.update(super_fields)", result);
        Assert.Contains("return fields", result);
    }
    [Fact]
    public void WritesDeSerializerBody()
    {
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("from . import somecustomtype", result);
        Assert.Contains("get_str_value", result);
        Assert.Contains("get_int_value", result);
        Assert.Contains("get_float_value", result);
        Assert.Contains("get_bool_value", result);
        Assert.Contains("get_bytes_value", result);
        Assert.Contains("get_object_value", result);
        Assert.Contains("get_collection_of_primitive_values", result);
        Assert.Contains("get_collection_of_object_values", result);
        Assert.Contains("get_enum_value", result);
        Assert.DoesNotContain("defined_in_parent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void WritesInheritedSerializerBody()
    {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("super().serialize", result);
    }
    [Fact]
    public void WritesSerializerBody()
    {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("write_str_value", result);
        Assert.Contains("write_collection_of_primitive_values", result);
        Assert.Contains("write_collection_of_object_values", result);
        Assert.Contains("write_enum_value", result);
        Assert.Contains("write_additional_data_value(self.additional_data)", result);
        Assert.DoesNotContain("defined_in_parent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void WritesMethodAsyncDescription()
    {

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
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("Returns:", result);
        Assert.Contains("await", result);
    }
    [Fact]
    public void WritesMethodSyncDescription()
    {

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
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result);
        Assert.DoesNotContain("await", result);
    }
    [Fact]
    public void Defensive()
    {
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
        method.Kind = CodeMethodKind.RawUrlConstructor;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void ThrowsIfParentIsNotClass()
    {
        method.Parent = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesReturnType()
    {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName, result);
        Assert.Contains(ReturnTypeName, result);
        Assert.Contains("Optional[", result);// nullable default
    }
    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType()
    {
        method.ReturnType.IsNullable = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("Optional[", result);
    }
    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    {
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("async", result);
    }
    [Fact]
    public void WritesFactoryMethods()
    {
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
    public void WritesModelFactoryBody()
    {
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
        Assert.Contains("mapping_value_node = parse_node.get_child_node(\"@odata.type\")", result);
        Assert.Contains("if mapping_value_node:", result);
        Assert.Contains("mapping_value = mapping_value_node.get_str_value()", result);
        Assert.Contains("if mapping_value == \"ns.childclass\"", result);
        Assert.Contains("from . import child_class", result);
        Assert.Contains("return child_class.ChildClass()", result);
        Assert.Contains("return ParentClass()", result);
    }
    [Fact]
    public void DoesntWriteFactoryConditionalsOnMissingParameter()
    {
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
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain($"_{MethodName}", result); ;// public default
    }
    [Fact]
    public void WritesProtectedMethod()
    {
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"_{MethodName}", result);
    }
    [Fact]
    public void WritesIndexer()
    {
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
        Assert.Contains("from . import somecustomtype", result);
        Assert.Contains("self.request_adapter", result);
        Assert.Contains("self.path_parameters", result);
        Assert.Contains("path_param", result);
        Assert.Contains("return", result);
    }
    [Fact]
    public void WritesGetterToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
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
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("return self.some_property", result);
    }
    [Fact]
    public void WritesGetterToBackingStoreWithNonnullProperty()
    {
        method.AddAccessedProperty();
        parentClass.AddBackingStoreProperty();
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
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.some_property = value", result);
    }
    [Fact]
    public void WritesSetterToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.backing_store[\"some_property\"] = value", result);
    }
    [Fact]
    public void WritesGetterToField()
    {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.some_property", result);
    }
    [Fact]
    public void WritesSetterToField()
    {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("self.some_property = value", result);
    }
    [Fact]
    public void WritesConstructor()
    {
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
        Assert.DoesNotContain("super().__init__()", result);
        Assert.Contains("has a description", result);
        Assert.Contains($"self.{propName}: Optional[str] = {defaultValue}", result);
        Assert.Contains("get_path_parameters", result);
    }
    [Fact]
    public void DoesNotWriteConstructorWithDefaultFromComposedType()
    {
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
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var propName = "prop_with_no_default_value";
        parentClass.Kind = CodeClassKind.Model;
        AddInheritanceClass();
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
        Assert.Contains("has a description", result);
        Assert.Contains($"self.{prop2Name}: Optional[str] = {defaultValue}", result);
        Assert.Contains($"self.{propName}: Optional[str] = None", result);
    }
    [Fact]
    public void WritesApiConstructor()
    {
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
    public void MapperMethodFailsIfNoQueryParametersMapperParameter()
    {
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
        method.Kind = CodeMethodKind.Serializer;
        AddSerializationProperties();
        AddInheritanceClass();
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
