using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

public class CodeNameSpaceWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;

    public CodeNameSpaceWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WritesBlankLine()
    {
        var root = CodeNamespace.InitRootNamespace();
        var requestbuilder = new CodeClass
        {
            Kind = CodeClassKind.RequestBuilder,
            Name = "TestRequestBuilder",
        };
        var model = new CodeClass
        {
            Kind = CodeClassKind.Model,
            Name = "TestModel",
        };
        root.AddClass(requestbuilder);
        root.AddClass(model);
        writer.Write(root);
        var result = tw.ToString();
        Assert.True(string.IsNullOrWhiteSpace(result));// single blank line written in namespace files
        Assert.NotNull(result);
    }
}
