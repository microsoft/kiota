using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Python;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;
public class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly CodeNamespace root;
    private readonly CodeNamespace ns;
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeClass parentClass;

    public CodeClassDeclarationWriterTests() {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        codeElementWriter = new CodeClassDeclarationWriter(new PythonConventionService());
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        ns = root.AddNamespace("graphtests.models");
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
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("class", result);
    }
    [Fact]
    public void WritesImplementation() {
        var declaration = parentClass.StartBlock;
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
        var declaration = parentClass.StartBlock;
        declaration.Inherits = new () {
            Name = "someInterface"
        };
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("(some_interface.SomeInterface):", result);
    }
    [Fact]
    public void WritesExternalImports() {
        var declaration = parentClass.StartBlock;
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
    [Fact]
    public void WritesInternalImportsSubNamespace() {
        var declaration = parentClass.StartBlock;
        var subNS = ns.AddNamespace($"{ns.Name}.messages");
        var messageClassDef = new CodeClass {
            Name = "Message",
        };
        subNS.AddClass(messageClassDef);
        var nUsing = new CodeUsing {
            Name = messageClassDef.Name,
            Declaration = new() {
                Name = messageClassDef.Name,
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("message = lazy_import('graphtests.models.messages.message')", result);
    }

    [Fact]
    public void WritesInternalImportsSameNamespace() {
        var declaration = parentClass.StartBlock;
        var messageClassDef = new CodeClass {
            Name = "Message",
        };
        ns.AddClass(messageClassDef);
        var nUsing = new CodeUsing {
            Name = "graph",
            Declaration = new() {
                Name = "Message",
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("message = lazy_import('graphtests.models.message')", result);
    }
}
