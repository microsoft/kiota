using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Python;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

public class CodeUsingWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly LanguageWriter writer;
    private readonly StringWriter tw;
    private readonly CodeNamespace root;
    public CodeUsingWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    [Fact]
    public void WritesAliasedSymbol()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Alias = "baz",
            Declaration = new CodeType
            {
                Name = "bar",
                TypeDefinition = codeClass,
            },
        };
        codeClass.AddUsing(us);
        usingWriter.WriteInternalImports(codeClass, writer);
        var result = tw.ToString();
        Assert.Contains("from . import bar as baz", result);
    }
    [Fact]
    public void DoesntAliasRegularSymbols()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",

        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = "bar",
                TypeDefinition = codeClass,
            },
        };
        codeClass.AddUsing(us);
        usingWriter.WriteInternalImports(codeClass, writer);
        var result = tw.ToString();
        Assert.Contains("from . import bar", result);
    }
}
