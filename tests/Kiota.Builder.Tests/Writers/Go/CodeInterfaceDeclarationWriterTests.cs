using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public sealed class CodeInterfaceDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeInterfaceDeclarationWriter codeElementWriter;
    private readonly CodeInterface parentInterface;
    private readonly CodeNamespace root;

    public CodeInterfaceDeclarationWriterTests()
    {
        codeElementWriter = new CodeInterfaceDeclarationWriter(new GoConventionService());
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentInterface = new()
        {
            Name = "parentClass",
            OriginalClass = new CodeClass() { Name = "parentClass" }
        };
        root.AddInterface(parentInterface);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesSimpleDeclaration()
    {
        codeElementWriter.WriteCodeElement(parentInterface.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("DO NOT EDIT", result);
        Assert.Contains("type", result);
        Assert.Contains("interface", result);
        Assert.DoesNotContain("struct", result);
        Assert.Contains("package", result);
    }
    [Fact]
    public void WritesInheritance()
    {
        var declaration = parentInterface.StartBlock;
        declaration.AddImplements(new CodeType
        {
            Name = "someParent"
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("SomeParent", result);
    }
    [Fact]
    public void WritesImports()
    {
        var declaration = parentInterface.StartBlock;
        declaration.AddUsings(new()
        {
            Name = "Objects",
            Declaration = new()
            {
                Name = "Go.util",
                IsExternal = true,
            }
        },
        new()
        {
            Name = "project.graph",
            Declaration = new()
            {
                Name = "Message",
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("import (", result);
        Assert.Contains("ib040b76fd8c2f056725723d35361c053f34e28d2ff1828ce830f3e00e807a59b \"project/graph\"", result);
        Assert.Contains("i5bda920e7ace9d5445a4ab998b248741f78d1219c76fbec57ddf13651f485ee4 \"Go.util\"", result);
    }
}
