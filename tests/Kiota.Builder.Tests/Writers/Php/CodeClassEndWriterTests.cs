using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

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
        codeElementWriter = new CodeBlockEndWriter();
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
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
    public void ClosesNonNestedClasses()
    {
        codeElementWriter.WriteCodeElement(parentClass.EndBlock, writer);
        var result = tw.ToString();
        Assert.Equal(1, result.Count(x => x == '}'));
    }
}
