using System;
using System.IO;
using System.Linq;
using Kiota.Builder;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public class CodeEnumObjectWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnumObjectWriter codeEnumObjectWriter;
    public CodeEnumObjectWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        codeEnumObjectWriter = new CodeEnumObjectWriter(new());
        tw = new StringWriter();
        writer.SetTextWriter(tw);
    }

    [Fact]
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenCodeElementIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => codeEnumObjectWriter.WriteCodeElement(null, writer));
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenWriterIsNull()
    {
        var codeElement = new CodeEnumObject();
        Assert.Throws<ArgumentNullException>(() => codeEnumObjectWriter.WriteCodeElement(codeElement, null));
    }

    [Fact]
    public void WriteCodeElement_WritesExpectedOutput_WhenCodeElementAndWriterAreNotNull()
    {
        var codeElement = new CodeEnumObject { Name = "TestEnum", Parent = new CodeClass { Name = "TestClass" } };

        codeEnumObjectWriter.WriteCodeElement(codeElement, writer);

        var expectedOutput = "export type TestClass = (typeof TestEnum)[keyof typeof TestEnum];";

        var result = tw.ToString();
        Assert.Contains(expectedOutput, result);
    }
}
