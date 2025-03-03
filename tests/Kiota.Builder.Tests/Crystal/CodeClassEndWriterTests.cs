using System;
using System.IO;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Crystal;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Crystal;

public sealed class CodeClassEndWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassEndWriter codeClassEndWriter;
    private readonly CodeClass parentClass;
    private readonly BlockEnd blockEnd;

    public CodeClassEndWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Crystal, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var conventionService = new CrystalConventionService();
        codeClassEndWriter = new CodeClassEndWriter(conventionService);
        parentClass = new CodeClass
        {
            Name = "TestClass",
            Kind = CodeClassKind.Model
        };
        blockEnd = new BlockEnd
        {
            Parent = parentClass
        };
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WritesClassEnd()
    {
        codeClassEndWriter.WriteCodeElement(blockEnd, writer);
        var result = tw.ToString();
        Assert.Contains("end", result);
    }

    [Fact]
    public void WritesNamespaceEnd()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();
        var childNamespace = rootNamespace.AddNamespace("TestNamespace");
        childNamespace.AddClass(parentClass);
        codeClassEndWriter.WriteCodeElement(blockEnd, writer);
        var result = tw.ToString();
        Assert.Contains("end", result);
    }
}

