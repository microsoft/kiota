using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Python.Tests;
public class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeClass parentClass;

    public CodeClassDeclarationWriterTests() {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        codeElementWriter = new CodeClassDeclarationWriter(new PythonConventionService(writer), "graphtests");
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        var ns = root.AddNamespace("graphtests.models");
        parentClass = new () {
            Name = "parentClass"
        };
        ns.AddClass(parentClass);
    }
    public void Dispose() {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesSimpleDeclaration() {
        codeElementWriter.WriteCodeElement(parentClass.StartBlock as ClassDeclaration, writer);
        var result = tw.ToString();
        Assert.Contains("class", result);
    }
    [Fact]
    public void WritesImplementation() {
        var declaration = parentClass.StartBlock as ClassDeclaration;
        declaration.AddImplements(new CodeType {
            Name = "someInterface"
        });
        declaration.AddImplements(new CodeType {
            Name = "secondInterface"
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("(SecondInterface, SomeInterface):", result);
    }
    [Fact]
    public void WritesInheritance() {
        var declaration = parentClass.StartBlock as ClassDeclaration;
        declaration.Inherits = new () {
            Name = "someInterface"
        };
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("(some_interface.SomeInterface):", result);
    }
    [Fact]
    public void WritesImports() {
        var declaration = parentClass.StartBlock as ClassDeclaration;
        declaration.AddUsings(new CodeUsing {
            Name = "Objects",
            Declaration = new () {
                Name = "util",
                IsExternal = true,
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("from util import Objects", result);
    }
}
