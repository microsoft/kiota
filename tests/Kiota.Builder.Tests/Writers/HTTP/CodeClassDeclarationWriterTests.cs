using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Http;

public sealed class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeNamespace root;

    public CodeClassDeclarationWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.HTTP, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WritesRequestExecutorMethods()
    {
        var codeClass = new CodeClass
        {
            Name = "TestClass",
            Kind = CodeClassKind.RequestBuilder
        };
        var urlTemplateProperty = new CodeProperty
        {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            DefaultValue = "\"{+baseurl}/posts\"",
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
            Documentation = new CodeDocumentation
            {
                DescriptionTemplate = "The URL template for the request."
            }
        };
        codeClass.AddProperty(urlTemplateProperty);

        // Add base url property
        var baseUrlProperty = new CodeProperty
        {
            Name = "BaseUrl",
            Kind = CodePropertyKind.Custom,
            Access = AccessModifier.Private,
            DefaultValue = "https://example.com",
            Type = new CodeType { Name = "string", IsExternal = true }
        };
        codeClass.AddProperty(baseUrlProperty);

        var method = new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            Documentation = new CodeDocumentation { DescriptionTemplate = "GET method" },
            ReturnType = new CodeType { Name = "void" }
        };
        codeClass.AddMethod(method);

        var postMethod = new CodeMethod
        {
            Name = "post",
            Kind = CodeMethodKind.RequestExecutor,
            Documentation = new CodeDocumentation { DescriptionTemplate = "Post method" },
            ReturnType = new CodeType { Name = "void" },
            RequestBodyContentType = "application/json"
        };


        var typeDefinition = new CodeClass
        {
            Name = "PostParameter",
        };

        var properties = new List<CodeProperty>
        {
            new() {
                Name = "body",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { Name = "string", IsExternal = true }
            },
            new() {
                Name = "id",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { Name = "int", IsExternal = true }
            },
            new() {
                Name = "title",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { Name = "string", IsExternal = true }
            },
            new() {
                Name = "userId",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { Name = "int", IsExternal = true }
            }
        };

        typeDefinition.AddProperty(properties.ToArray());

        // Define the parameter with the specified properties
        var postParameter = new CodeParameter
        {
            Name = "postParameter",
            Kind = CodeParameterKind.RequestBody,
            Type = new CodeType
            {
                Name = "PostParameter",
                TypeDefinition = typeDefinition
            }
        };

        // Add the parameter to the post method
        postMethod.AddParameter(postParameter);

        codeClass.AddMethod(postMethod);

        var patchMethod = new CodeMethod
        {
            Name = "patch",
            Kind = CodeMethodKind.RequestExecutor,
            Documentation = new CodeDocumentation { DescriptionTemplate = "Patch method" },
            ReturnType = new CodeType { Name = "void" }
        };
        codeClass.AddMethod(patchMethod);

        root.AddClass(codeClass);

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.HTTP }, root);

        writer.Write(codeClass.StartBlock);
        var result = tw.ToString();

        // Check HTTP operations 
        Assert.Contains("GET {{hostAddress}}/posts HTTP/1.1", result);
        Assert.Contains("PATCH {{hostAddress}}/posts HTTP/1.1", result);
        Assert.Contains("POST {{hostAddress}}/posts HTTP/1.1", result);

        // Check content type
        Assert.Contains("Content-Type: application/json", result);

        // check the request body
        Assert.Contains("\"body\": \"string\"", result);
        Assert.Contains("\"id\": 0", result);
        Assert.Contains("\"title\": \"string\"", result);
        Assert.Contains("\"userId\": 0", result);
    }

    [Fact]
    public async Task WritesRequestExecutorsWithoutCrashing()
    {
        var codeClass = new CodeClass
        {
            Name = "TestClass",
            Kind = CodeClassKind.RequestBuilder
        };
        var urlTemplateProperty = new CodeProperty
        {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            DefaultValue = "\"{+baseurl}/posts\"",
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
            Documentation = new CodeDocumentation
            {
                DescriptionTemplate = "The URL template for the request."
            }
        };
        codeClass.AddProperty(urlTemplateProperty);

        // Add base url property
        var baseUrlProperty = new CodeProperty
        {
            Name = "BaseUrl",
            Kind = CodePropertyKind.Custom,
            Access = AccessModifier.Private,
            DefaultValue = "https://example.com",
            Type = new CodeType { Name = "string", IsExternal = true }
        };
        codeClass.AddProperty(baseUrlProperty);

        var method = new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            Documentation = new CodeDocumentation { DescriptionTemplate = "GET method" },
            ReturnType = new CodeType { Name = "void" }
        };

        var postMethod = new CodeMethod
        {
            Name = "post",
            Kind = CodeMethodKind.RequestExecutor,
            Documentation = new CodeDocumentation { DescriptionTemplate = "Post method" },
            ReturnType = new CodeType { Name = "void" },
            RequestBodyContentType = "application/json"
        };


        var typeDefinition = new CodeClass
        {
            Name = "PostParameter",
            Kind = CodeClassKind.QueryParameters
        };

        var properties = new List<CodeProperty>
        {
            new() {
                Name = "body",
                Kind = CodePropertyKind.QueryParameter,
                Type = new CodeType { Name = "string", IsExternal = true }
            }
        };

        typeDefinition.AddProperty([.. properties]);

        // Define the parameter with the specified properties
        var postParameter = new CodeParameter
        {
            Name = "postParameter",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "PostParameter",
                TypeDefinition = typeDefinition
            }
        };

        // Add the parameter to the post and get methods
        method.AddParameter(postParameter);
        postMethod.AddParameter(postParameter);
        codeClass.AddMethod(postMethod);
        codeClass.AddMethod(method);


        root.AddClass(codeClass);

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.HTTP }, root);

        writer.Write(codeClass.StartBlock);
        var result = tw.ToString();

        // Check HTTP operations 
        Assert.Contains("GET {{hostAddress}}/posts HTTP/1.1", result);
    }
}
