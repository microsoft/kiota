using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests;

public sealed class CodeInterfaceDeclaraterWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeInterface parentInterface;

    public CodeInterfaceDeclaraterWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        var ns = root.AddNamespace("graphtests.models");
        var originalClass = ns.AddClass(new CodeClass() { Name = "originalParentClass" }).First();
        parentInterface = new CodeInterface()
        {
            Name = "parent",
            OriginalClass = originalClass
        };
        ns.AddInterface(parentInterface);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesAutoGenerationStart()
    {
        writer.Write(parentInterface.StartBlock);
        var result = tw.ToString();
        Assert.Contains("/* eslint-disable */", result);
        Assert.Contains("/* tslint:disable */", result);
    }
    [Fact]
    public void WritesSimpleDeclaration()
    {
        writer.Write(parentInterface.StartBlock);
        var result = tw.ToString();
        Assert.Contains("export interface", result);
    }

    [Fact]
    public void WritesInheritance()
    {

        parentInterface.StartBlock.AddImplements(new CodeType()
        {
            Name = "someInterface"
        });
        writer.Write(parentInterface.StartBlock);
        var result = tw.ToString();
        Assert.Contains("extends", result);
    }
    [Fact]
    public void WritesImports()
    {
        parentInterface.StartBlock.AddUsings(new CodeUsing
        {
            Name = "Objects",
            Declaration = new()
            {
                Name = "util",
                IsExternal = true,
            }
        });
        writer.Write(parentInterface.StartBlock);
        var result = tw.ToString();
        Assert.Contains("import", result);
        Assert.Contains("from", result);
        Assert.Contains("'util'", result);
    }
}
