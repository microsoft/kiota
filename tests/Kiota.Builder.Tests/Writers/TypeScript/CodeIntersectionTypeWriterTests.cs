using System;
using System.IO;
using Kiota.Builder.CodeDOM;
using Moq;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests;

public sealed class CodeIntersectionTypeWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeIntersectionTypeWriter codeElementWriter;

    public CodeIntersectionTypeWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        codeElementWriter = new CodeIntersectionTypeWriter(new TypeScriptConventionService());
        tw = new StringWriter();
        writer.SetTextWriter(tw);
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WriteCodeElement_ShouldThrowArgumentNullException_WhenCodeElementIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => codeElementWriter.WriteCodeElement(null, writer));
    }

    [Fact]
    public void WriteCodeElement_ShouldThrowArgumentNullException_WhenWriterIsNull()
    {
        var composedType = new Mock<CodeIntersectionType>();
        Assert.Throws<ArgumentNullException>(() => codeElementWriter.WriteCodeElement(composedType.Object, null));
    }

    [Fact]
    public void WriteCodeElement_ShouldThrowInvalidOperationException_WhenTypesIsEmpty()
    {
        var composedType = new Mock<CodeIntersectionType>();
        Assert.Throws<InvalidOperationException>(() => codeElementWriter.WriteCodeElement(composedType.Object, writer));
    }

    [Fact]
    public void WriteCodeElement_ShouldWriteCorrectOutput_WhenTypesIsNotEmpty()
    {
        CodeIntersectionType composedType = new CodeIntersectionType() { Name = "Test" };
        composedType.AddType(new CodeType { Name = "Type1" });
        composedType.AddType(new CodeType { Name = "Type2" });

        var root = CodeNamespace.InitRootNamespace();
        var ns = root.AddNamespace("graphtests.models");
        ns.TryAddCodeFile(DefaultPath, composedType);

        codeElementWriter.WriteCodeElement(composedType, writer);

        var result = tw.ToString();
        Assert.Contains("export type Test = Type1 | Type2;", result);
    }
}
