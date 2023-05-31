using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Python;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;
public class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";

    private const string ClientNamespaceName = "graph";
    private readonly CodeNamespace root;
    private readonly CodeNamespace ns;
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeClass parentClass;

    public CodeClassDeclarationWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        codeElementWriter = new CodeClassDeclarationWriter(new PythonConventionService(), ClientNamespaceName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        ns = root.AddNamespace("graphtests.models");
        parentClass = new()
        {
            Name = "parentClass"
        };
        ns.AddClass(parentClass);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void Defensive()
    {
        var codeClassDeclarationWriter = new CodeClassDeclarationWriter(new PythonConventionService(), ClientNamespaceName);
        Assert.Throws<ArgumentNullException>(() => codeClassDeclarationWriter.WriteCodeElement(null, writer));
        var declaration = parentClass.StartBlock;
        Assert.Throws<ArgumentNullException>(() => codeClassDeclarationWriter.WriteCodeElement(declaration, null));
    }
    [Fact]
    public void WritesSimpleDeclaration()
    {
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.DoesNotContain("@dataclass", result);
        Assert.Contains("class ParentClass()", result);
    }
    [Fact]
    public void WritesModelClassDeclaration()
    {
        parentClass.Kind = CodeClassKind.Model;
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("@dataclass", result);
        Assert.Contains("class ParentClass()", result);
    }
    [Fact]
    public void WritesRequestBuilderClassDeclaration()
    {
        parentClass.Kind = CodeClassKind.RequestBuilder;
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.DoesNotContain("@dataclass", result);
        Assert.Contains("class ParentClass()", result);
    }
    [Fact]
    public void WritesImplementation()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddImplements(new CodeType
        {
            Name = "someInterface"
        });
        declaration.AddImplements(new CodeType
        {
            Name = "secondInterface"
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.DoesNotContain("()", result);
        Assert.Contains("(SecondInterface, SomeInterface):", result);
    }
    [Fact]
    public void WritesInheritance()
    {
        var declaration = parentClass.StartBlock;
        var interfaceDef = new CodeInterface
        {
            Name = "someInterface",
        };
        ns.AddInterface(interfaceDef);
        var nUsing = new CodeUsing
        {
            Name = "graph",
            Declaration = new()
            {
                Name = "someInterface",
                TypeDefinition = interfaceDef,
            }
        };
        declaration.AddUsings(nUsing);
        declaration.Inherits = new()
        {
            Name = "someInterface"
        };
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("if TYPE_CHECKING:", result);
        Assert.Contains("from . import some_interface", result);
        Assert.Contains("(some_interface.SomeInterface):", result);
    }
    [Fact]
    public void WritesInnerClasses()
    {
        parentClass.AddInnerClass(new CodeClass
        {
            Name = "InnerClass"
        });
        var declaration = parentClass.InnerClasses.First().StartBlock;
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("@dataclass", result);
    }
    [Fact]
    public void WritesExternalImports()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddUsings(new CodeUsing
        {
            Name = "Objects",
            Declaration = new()
            {
                Name = "util",
                IsExternal = true,
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("from util import Objects", result);
    }
    [Fact]
    public void WritesExternalImportsWithoutPath()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddUsings(new CodeUsing
        {
            Name = "Objects",
            Declaration = new()
            {
                Name = "-",
                IsExternal = true,
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("import Objects", result);
    }
    [Fact]
    public void WritesConditionalInternalImportsSubNamespace()
    {
        var declaration = parentClass.StartBlock;
        var subNS = ns.AddNamespace($"{ns.Name}.messages");
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        subNS.AddClass(messageClassDef);
        var nUsing = new CodeUsing
        {
            Name = messageClassDef.Name,
            Declaration = new()
            {
                Name = messageClassDef.Name,
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("if TYPE_CHECKING:", result);
        Assert.Contains("from .messages import message", result);
    }

    [Fact]
    public void WritesConditionalInternalImportsSameNamespace()
    {
        var declaration = parentClass.StartBlock;
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        ns.AddClass(messageClassDef);
        var nUsing = new CodeUsing
        {
            Name = "graph",
            Declaration = new()
            {
                Name = "Message",
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("if TYPE_CHECKING:", result);
        Assert.Contains("from . import message", result);
    }
    [Fact]
    public void WritesInternalImportsNoTypeDef()
    {
        var declaration = parentClass.StartBlock;
        var nUsing = new CodeUsing
        {
            Name = "graph",
            Declaration = new()
            {
                Name = "Message"
            }
        };
        declaration.AddUsings(nUsing);
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.DoesNotContain("from . import message", result);
    }
}
