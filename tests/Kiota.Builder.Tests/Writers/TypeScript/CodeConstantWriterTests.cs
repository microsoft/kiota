using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public sealed class CodeConstantWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    private readonly CodeConstantWriter codeConstantWriter;
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    private readonly CodeClass parentClass;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";
    public CodeConstantWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        codeConstantWriter = new CodeConstantWriter(new());
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        parentClass = root.AddClass(new CodeClass
        {
            Name = "parentClass",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        method = parentClass.AddMethod(new CodeMethod
        {
            Name = MethodName,
            ReturnType = new CodeType
            {
                Name = ReturnTypeName
            }
        }).First();
    }
    private void AddCodeEnum()
    {
        currentEnum = root.AddEnum(new CodeEnum
        {
            Name = EnumName,
        }).First();
        if (CodeConstant.FromCodeEnum(currentEnum) is CodeConstant constant)
        {
            currentEnum.CodeEnumObject = constant;
            root.AddConstant(constant);
        }
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenCodeElementIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => codeConstantWriter.WriteCodeElement(null, writer));
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenWriterIsNull()
    {
        var codeElement = new CodeConstant();
        Assert.Throws<ArgumentNullException>(() => codeConstantWriter.WriteCodeElement(codeElement, null));
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenOriginalCodeElementIsNull()
    {
        var codeElement = new CodeConstant();
        Assert.Throws<InvalidOperationException>(() => codeConstantWriter.WriteCodeElement(codeElement, writer));
    }

    [Fact]
    public void WritesEnumOptionDescription()
    {
        AddCodeEnum();
        var option = new CodeEnumOption
        {
            Documentation = new()
            {
                DescriptionTemplate = "Some option description",
            },
            Name = "option1",
        };
        currentEnum.AddOption(option);
        codeConstantWriter.WriteCodeElement(currentEnum.CodeEnumObject, writer);
        var result = tw.ToString();
        Assert.Contains($"/** {option.Documentation.DescriptionTemplate} */", result);
        AssertExtensions.CurlyBracesAreClosed(result, 0);
    }

    [Fact]
    public void WritesEnum()
    {
        AddCodeEnum();
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        codeConstantWriter.WriteCodeElement(currentEnum.CodeEnumObject, writer);
        var result = tw.ToString();
        Assert.Contains("export const SomeEnumObject = {", result);
        Assert.Contains("Option1: \"option1\"", result);
        Assert.Contains("as const;", result);
        Assert.Contains(optionName, result);
        AssertExtensions.CurlyBracesAreClosed(result, 0);
    }

    [Fact]
    public void DoesntWriteAnythingOnNoOption()
    {
        AddCodeEnum();
        codeConstantWriter.WriteCodeElement(currentEnum.CodeEnumObject, writer);
        var result = tw.ToString();
        Assert.Empty(result);
    }
    private readonly CodeMethod method;
    [Fact]
    public void DoesNotCreateDictionaryOnEmptyErrorMapping()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;

        AddRequestBodyParameters();
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = parentClass.GetImmediateParentOfType<CodeNamespace>().TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.DoesNotContain("errorMappings", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForMultipart()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters();
        method.Parameters.First(static x => x.IsOfKind(CodeParameterKind.RequestBody)).Type = new CodeType { Name = "MultipartBody", IsExternal = true };
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = root.TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("uriTemplate:", result);
        Assert.Contains("setContentFromParsable", result);
        Assert.Contains("serializeMultipartBody", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBodyForCollections()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
        AddRequestBodyParameters();
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = parentClass.GetImmediateParentOfType<CodeNamespace>().TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("sendCollection", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorForEnum()
    {
        AddCodeEnum();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType
        {
            Name = "SomeComplexTypeForRequestBody",
            TypeDefinition = currentEnum,
        };
        AddRequestBodyParameters();
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = parentClass.GetImmediateParentOfType<CodeNamespace>().TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("sendEnum", result);
        Assert.Contains("enumObject: " + EnumName.ToFirstCharacterUpperCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorForEnumCollection()
    {
        AddCodeEnum();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType
        {
            Name = "SomeComplexTypeForRequestBody",
            TypeDefinition = currentEnum,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
        };
        AddRequestBodyParameters();
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = parentClass.GetImmediateParentOfType<CodeNamespace>().TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("sendCollectionOfEnum", result);
        Assert.Contains("enumObject: " + EnumName.ToFirstCharacterUpperCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorForPrimitive()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType
        {
            Name = "string",
        };
        AddRequestBodyParameters();
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = parentClass.GetImmediateParentOfType<CodeNamespace>().TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("sendPrimitive", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorForPrimitiveCollection()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        method.ReturnType = new CodeType
        {
            Name = "string",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
        };
        AddRequestBodyParameters();
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = parentClass.GetImmediateParentOfType<CodeNamespace>().TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("sendCollectionOfPrimitive", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForScalar()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters();
        var generatorMethod = parentClass.AddMethod(new CodeMethod
        {
            Name = "toGetRequestInformation",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType
            {
                Name = "RequestInformation",
                IsExternal = true,
            },
        }).First();
        generatorMethod.AcceptedResponseTypes.Add("application/json");
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = root.TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("export const", result);
        Assert.Contains("RequestsMetadata: RequestsMetadata = {", result);
        Assert.Contains("responseBodyContentType: \"application/json\"", result);
        Assert.Contains("requestInformationContentSetMethod: \"setContentFromScalar", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyForParsable()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        AddRequestBodyParameters(true);
        var generatorMethod = parentClass.AddMethod(new CodeMethod
        {
            Name = "toGetRequestInformation",
            HttpMethod = HttpMethod.Get,
            Kind = CodeMethodKind.RequestGenerator,
            ReturnType = new CodeType
            {
                Name = "RequestInformation",
                IsExternal = true,
            },
        }).First();
        var bodyParameter = method.Parameters.First(static x => x.Kind is CodeParameterKind.RequestBody);
        bodyParameter.Type = new CodeType
        {
            Name = "SomeComplexTypeForRequestBody",
            TypeDefinition = new CodeInterface
            {
                Name = "SomeComplexTypeForRequestBody",
                Kind = CodeInterfaceKind.Model,
                OriginalClass = new CodeClass() { Name = "SomeComplexTypeForRequestBody" }
            },
        };
        generatorMethod.AcceptedResponseTypes.Add("application/json");
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = root.TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("export const", result);
        Assert.Contains("RequestsMetadata: RequestsMetadata = {", result);
        Assert.Contains("responseBodyContentType: \"application/json\"", result);
        Assert.Contains("requestInformationContentSetMethod: \"setContentFromParsable", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyKnownRequestBodyType()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = new TypeScriptConventionService().StreamTypeName,
            IsExternal = true,
        };
        method.RequestBodyContentType = "application/json";
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = root.TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("export const", result);
        Assert.Contains("RequestsMetadata: RequestsMetadata = {", result);
        Assert.Contains("requestBodyContentType: \"application/json\"", result);
        Assert.Contains("requestInformationContentSetMethod: \"setStreamContent", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestGeneratorBodyUnknownRequestBodyType()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
        method.Kind = CodeMethodKind.RequestExecutor;
        method.HttpMethod = HttpMethod.Post;
        AddRequestProperties();
        AddRequestBodyParameters(false);
        method.Parameters.OfKind(CodeParameterKind.RequestBody).Type = new CodeType
        {
            Name = new TypeScriptConventionService().StreamTypeName,
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
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = root.TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("export const", result);
        Assert.Contains("RequestsMetadata: RequestsMetadata = {", result);
        Assert.DoesNotContain("requestBodyContentType: \"application/json\"", result);
        Assert.Contains("requestInformationContentSetMethod: \"setStreamContent", result);
        Assert.Contains("setStreamContent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesRequestExecutorBody()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
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
        var constant = CodeConstant.FromRequestBuilderToRequestsMetadata(parentClass);
        var codeFile = root.TryAddCodeFile("foo", constant);
        codeFile.AddElements(new CodeConstant
        {
            Name = "UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = "{baseurl+}/foo/bar"
        });
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("errorMappings: {", result);
        Assert.Contains("_4XX: createError4XXFromDiscriminatorValue as ParsableFactory<Parsable>", result);
        Assert.Contains("_5XX: createError5XXFromDiscriminatorValue as ParsableFactory<Parsable>", result);
        Assert.Contains("403: createError403FromDiscriminatorValue as ParsableFactory<Parsable>", result);
    }
    [Fact]
    public void WritesIndexer()
    {
        AddRequestProperties();
        method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
        parentClass.Kind = CodeClassKind.RequestBuilder;
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
                SerializationName = "id",
                Type = new CodeType
                {
                    Name = "string",
                    IsNullable = true,
                },
            }
        };
        var parentInterface = new CodeInterface
        {
            Name = "parentClass",
            Kind = CodeInterfaceKind.RequestBuilder,
            OriginalClass = new CodeClass() { Name = "parentClass" }
        };
        method.AddParameter(new CodeParameter
        {
            Name = "id",
            Type = new CodeType
            {
                Name = "string",
                IsNullable = true,
            },
            SerializationName = "foo-id",
            Kind = CodeParameterKind.Path
        });
        var parentNS = parentClass.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var childNS = parentNS.AddNamespace($"{parentNS.Name}.childNS");
        childNS.TryAddCodeFile("foo",
            new CodeConstant
            {
                Name = "SomecustomtypeUriTemplate",
                Kind = CodeConstantKind.UriTemplate,
            },
            new CodeConstant
            {
                Name = "SomecustomtypeNavigationMetadata",
                Kind = CodeConstantKind.NavigationMetadata,
            },
            new CodeConstant
            {
                Name = "SomecustomtypeRequestsMetadata",
                Kind = CodeConstantKind.RequestsMetadata,
            });
        parentInterface.AddMethod(method);
        var constant = CodeConstant.FromRequestBuilderToNavigationMetadata(parentClass);
        Assert.NotNull(constant);
        parentNS.TryAddCodeFile("foo", constant, parentInterface);
        writer.Write(constant);
        var result = tw.ToString();
        Assert.Contains("export const ParentClassNavigationMetadata: Record<Exclude<keyof ParentClass, KeysToExcludeForNavigationMetadata>, NavigationMetadata> = {", result);
        Assert.Contains("methodName", result);
        Assert.Contains("requestsMetadata: SomecustomtypeRequestsMetadata", result);
        Assert.Contains("navigationMetadata: SomecustomtypeNavigationMetadata", result);
        Assert.Contains("pathParametersMappings: [\"foo-id\"]", result);
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
            },
            Optional = true,
        });
    }
}
