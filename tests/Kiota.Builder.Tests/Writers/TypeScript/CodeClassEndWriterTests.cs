using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public sealed class CodeClassEndWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeBlockEndWriter codeElementWriter;
    private readonly CodeClass parentClass;
    public CodeClassEndWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        codeElementWriter = new CodeBlockEndWriter(new());
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void ClosesNestedClasses()
    {
        var child = parentClass.AddInnerClass(new CodeClass
        {
            Name = "child"
        }).First();
        codeElementWriter.WriteCodeElement(child.EndBlock, writer);
        var result = tw.ToString();
        Assert.Equal(1, result.Count(x => x == '}'));
        Assert.DoesNotContain("/* eslint-enable */", result);
        Assert.DoesNotContain("/* tslint:enable */", result);
    }
    [Fact]
    public void ClosesNonNestedClasses()
    {
        codeElementWriter.WriteCodeElement(parentClass.EndBlock, writer);
        var result = tw.ToString();
        Assert.Contains("/* eslint-enable */", result);
        Assert.Contains("/* tslint:enable */", result);
        Assert.Equal(1, result.Count(x => x == '}'));
    }
}
