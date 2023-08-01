using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public class CodeMethodWriterTests : IDisposable
{
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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass",
            Kind = CodeClassKind.RequestBuilder
        };
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
        parentClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
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
                TypeDefinition = TestHelper.CreateModelClass(root, "SomeComplexTypeForRequestBody"),
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
        }).First();
        var error5XX = root.AddClass(new CodeClass
        {
            Name = "Error5XX",
        }).First();
        var error403 = root.AddClass(new CodeClass
        {
            Name = "Error403",
        }).First();
        method.AddErrorMapping("4XX", new CodeType { Name = "Error4XX", TypeDefinition = error4XX });
        method.AddErrorMapping("5XX", new CodeType { Name = "Error5XX", TypeDefinition = error5XX });
        method.AddErrorMapping("403", new CodeType { Name = "Error403", TypeDefinition = error403 });
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("const requestInfo", result);
        Assert.Contains("const errorMapping", result);
        Assert.Contains("\"4XX\": createError4XXFromDiscriminatorValue,", result);
        Assert.Contains("\"5XX\": createError5XXFromDiscriminatorValue,", result);
        Assert.Contains("\"403\": createError403FromDiscriminatorValue,", result);
        Assert.Contains("as Record<string, ParsableFactory<Parsable>>", result);
        Assert.Contains("sendAsync", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesModelFactoryBodyThrowsIfMethodAndNotFactory()
    {
        var parentModel = TestHelper.CreateModelClass(root, "parentModel");
        var childModel = TestHelper.CreateModelClass(root, "childModel");
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
        Assert.Throws<InvalidOperationException>(() => writer.Write(factoryMethod));
    }
    [Fact]
    public void DoesntCreateDictionaryOnEmptyErrorMapping()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestBodyParameters();
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("const errorMapping: Record<string, new () => Parsable> =", result);
        Assert.Contains("undefined", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForMultipart()
    {
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
    public void WritesRequestGeneratorBodyForScalar()
    {
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.AcceptedResponseTypes.Add("application/json");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("const requestInfo = new RequestInformation()", result);
        Assert.Contains("requestInfo.httpMethod = HttpMethod", result);
        Assert.Contains("requestInfo.urlTemplate = ", result);
        Assert.Contains("requestInfo.pathParameters = ", result);
        Assert.Contains("requestInfo.headers[\"Accept\"] = [\"application/json\"]", result);
        Assert.Contains("if (c)", result);
        Assert.Contains("requestInfo.addRequestHeaders", result);
        Assert.Contains("requestInfo.addRequestOptions", result);
        Assert.Contains("requestInfo.setQueryStringParametersFromRawObject", result);
        Assert.Contains("setContentFromScalar", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesRequestGeneratorBodyForParsable()
    {
        method.Kind = CodeMethodKind.RequestGenerator;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        method.AcceptedResponseTypes.Add("application/json");
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("const requestInfo = new RequestInformation()", result);
        Assert.Contains("requestInfo.httpMethod = HttpMethod", result);
        Assert.Contains("requestInfo.urlTemplate = ", result);
        Assert.Contains("requestInfo.pathParameters = ", result);
        Assert.Contains("requestInfo.headers[\"Accept\"] = [\"application/json\"]", result);
        Assert.Contains("if (c)", result);
        Assert.Contains("requestInfo.addRequestHeaders", result);
        Assert.Contains("requestInfo.addRequestOptions", result);
        Assert.Contains("requestInfo.setQueryStringParametersFromRawObject", result);
        Assert.Contains("setContentFromParsable", result);
        Assert.Contains("return requestInfo;", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
        Assert.Contains("/**", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("@param ", result);
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("@returns a Promise of", result);
        Assert.Contains("*/", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
        Assert.DoesNotContain("@returns a Promise of", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesMethodDescriptionLink()
    {
        method.Documentation.Description = MethodDescription;
        method.Documentation.DocumentationLabel = "see more";
        method.Documentation.DocumentationLink = new("https://foo.org/docs");
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
        Assert.Contains("@see {@link", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive()
    {
        var codeMethodWriter = new CodeMethodWriter(new TypeScriptConventionService(writer), false);
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
        Assert.Contains("Promise<", result);// async default
        Assert.Contains("| undefined", result);// nullable default
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType()
    {
        method.ReturnType.IsNullable = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("| undefined", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    {
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("Promise<", result);
        Assert.DoesNotContain("async", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesPublicMethodByDefault()
    {
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("public ", result);// public default
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesPrivateMethod()
    {
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("private ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesProtectedMethod()
    {
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("protected ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
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
            },
            IndexParameterName = "id",
        };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.requestAdapter", result);
        Assert.Contains("this.pathParameters", result);
        Assert.Contains("id", result);
        Assert.Contains("return new", result);
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
        Assert.Contains("this.requestAdapter", result);
        Assert.Contains("this.pathParameters", result);
        Assert.Contains("pathParam", result);
        Assert.Contains("return new", result);
    }
    [Fact]
    public void WritesGetterToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.backingStore.get(\"someProperty\")", result);
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
        Assert.Contains("if(!value)", result);
        Assert.Contains(defaultValue, result);
    }
    [Fact]
    public void WritesSetterToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.backingStore.set(\"someProperty\", value)", result);
    }
    [Fact]
    public void WritesGetterToField()
    {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty", result);
    }
    [Fact]
    public void WritesSetterToField()
    {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Setter;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("this.someProperty = value", result);
    }
    [Fact]
    public void WritesConstructor()
    {
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
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
            },
        });
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = "Map<string,string>"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"this.{propName} = {defaultValue}", result);
    }
    [Fact]
    public void WritesConstructorWithEnumValue()
    {
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "1024x1024";
        var propName = "size";
        var codeEnum = new CodeEnum
        {
            Name = "pictureSize"
        };
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"this.{propName.ToFirstCharacterLowerCase()} = {codeEnum.Name.ToFirstCharacterUpperCase()}.{defaultValue.CleanupSymbolName()}", result);//ensure symbol is cleaned up
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
        Assert.Contains("constructor", result);
        Assert.DoesNotContain(defaultValue, result);//ensure the composed type is not referenced
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
        Assert.Contains("constructor", result);
        Assert.Contains("registerDefaultSerializer", result);
        Assert.Contains("registerDefaultDeserializer", result);
        Assert.Contains($"[\"baseurl\"] = core.baseUrl", result);
        Assert.Contains($"baseUrl = \"{method.BaseUrl}\"", result);
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
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("enableBackingStore", result);
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
            }
        },
        new CodeProperty
        {
            Name = "expand",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24expand",
            Type = new CodeType
            {
                Name = "string",
            }
        },
        new CodeProperty
        {
            Name = "filter",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24filter",
            Type = new CodeType
            {
                Name = "string",
            }
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
        Assert.Contains("originalName", result);
        Assert.Contains("switch", result);
        Assert.Contains("case \"select\": return \"%24select\"", result);
        Assert.Contains("case \"expand\": return \"%24expand\"", result);
        Assert.Contains("case \"filter\": return \"%24filter\"", result);
        Assert.Contains("default: return originalName", result);
    }
    [Fact]
    public void WritesDeprecationInformation()
    {
        method.Deprecation = new("This method is deprecated", DateTimeOffset.Parse("2020-01-01T00:00:00Z"), DateTimeOffset.Parse("2021-01-01T00:00:00Z"), "v2.0");
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("This method is deprecated", result);
        Assert.Contains("2020-01-01", result);
        Assert.Contains("2021-01-01", result);
        Assert.Contains("v2.0", result);
        Assert.Contains("@deprecated", result);
    }
}
