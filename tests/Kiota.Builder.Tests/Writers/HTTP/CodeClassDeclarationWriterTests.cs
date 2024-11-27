using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Tests.OpenApiSampleFiles;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static Kiota.Builder.Refiners.HttpRefiner;

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
    public async Task TestWriteTypeDeclaration()
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
            DefaultValue = "\"https://example.com/{id}\"",
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true
            },
        };
        codeClass.AddProperty(urlTemplateProperty);

        // Add a new property named BaseUrl and set its value to the baseUrl string
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

        root.AddClass(codeClass);

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.HTTP }, root);

        writer.Write(codeClass.StartBlock);
        var result = tw.ToString();

        Assert.Contains("# Base url for the server/host", result);
        Assert.Contains("@url = https://example.com", result);
    }
}
