using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Crystal;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Crystal;

public sealed class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeClassDeclarationWriter;
    private readonly CodeClass parentClass;
    private readonly ClassDeclaration classDeclaration;

    public CodeClassDeclarationWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Crystal, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var conventionService = new CrystalConventionService();
        codeClassDeclarationWriter = new CodeClassDeclarationWriter(conventionService);
        parentClass = new CodeClass
        {
            Name = "TestClass",
            Kind = CodeClassKind.Model
        };
        classDeclaration = new ClassDeclaration
        {
            Name = "TestClassDeclaration",
            Parent = parentClass
        };
        parentClass.StartBlock = classDeclaration;
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WritesClassDeclaration()
    {
        codeClassDeclarationWriter.WriteCodeElement(classDeclaration, writer);
        var result = tw.ToString();
        Assert.Contains("class TestClassDeclaration", result);
    }

    [Fact]
    public void WritesInheritance()
    {
        classDeclaration.Inherits = new CodeType
        {
            Name = "BaseClass"
        };
        codeClassDeclarationWriter.WriteCodeElement(classDeclaration, writer);
        var result = tw.ToString();
        Assert.Contains("class TestClassDeclaration < BaseClass", result);
    }

    [Fact]
    public void WritesIncludes()
    {
        var module1 = new CodeType { Name = "Module1" };
        var module2 = new CodeType { Name = "Module2" };
        
        // Create a mock implementation for testing
        var implementsList = parentClass.StartBlock.Implements as IList<CodeType>;
        implementsList.Add(module1);
        implementsList.Add(module2);
        codeClassDeclarationWriter.WriteCodeElement(classDeclaration, writer);
        var result = tw.ToString();
        Assert.Contains("include Module1", result);
        Assert.Contains("include Module2", result);
    }

    [Fact]
    public void WritesNamespace()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();
        var childNamespace = rootNamespace.AddNamespace("TestNamespace");
        childNamespace.AddClass(parentClass);
        codeClassDeclarationWriter.WriteCodeElement(classDeclaration, writer);
        var result = tw.ToString();
        Assert.Contains("module TestNamespace", result);
    }

    [Fact]
    public void WritesUsings()
    {
        parentClass.AddUsing(new CodeUsing
        {
            Name = "System",
            Declaration = new CodeType
            {
                Name = "System",
                IsExternal = true
            }
        });
        codeClassDeclarationWriter.WriteCodeElement(classDeclaration, writer);
        var result = tw.ToString();
        Assert.Contains("require \"system\"", result);
    }
}
