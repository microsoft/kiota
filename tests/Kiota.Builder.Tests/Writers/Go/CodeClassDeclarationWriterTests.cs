using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public sealed class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;

    public CodeClassDeclarationWriterTests()
    {
        codeElementWriter = new CodeClassDeclarationWriter(new GoConventionService());
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new()
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
    public void WritesSimpleDeclaration()
    {
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("DO NOT EDIT", result);
        Assert.Contains("type", result);
        Assert.Contains("struct", result);
        Assert.Contains("package", result);
    }
    [Fact]
    public void WritesInheritance()
    {
        var declaration = parentClass.StartBlock;
        declaration.Inherits = new()
        {
            Name = "someParent"
        };
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("SomeParent", result);
    }
    [Fact]
    public void WritesImports()
    {
        var declaration = parentClass.StartBlock;
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
    [Fact]
    public void SeparatesTheDeclarationFromTheHeaderWithASingleBlankLine()
    {
        // gofmt: exactly one blank line between top-level declarations
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains($"package {GoTestConstants.LineFeed}{GoTestConstants.LineFeed}type ParentClass struct {{", result);
    }
    [Fact]
    public void AlignsCommentLessFieldsIntoColumns()
    {
        // gofmt column-aligns consecutive undocumented fields (typical query parameter struct)
        parentClass.AddProperty(new CodeProperty
        {
            Name = "active",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "activeParameter",
            Access = AccessModifier.Public,
            Type = new CodeType { Name = "boolean" },
        },
        new CodeProperty
        {
            Name = "keyword",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "keywordParameter",
            Access = AccessModifier.Public,
            Type = new CodeType { Name = "string" },
        });
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("\tActive  *bool   \"uriparametername:\\\"activeParameter\\\"\"", result);
        Assert.Contains("\tKeyword *string \"uriparametername:\\\"keywordParameter\\\"\"", result);
    }
    [Fact]
    public void DoesNotAlignFieldsSeparatedByComments()
    {
        // a field documented by its own comment is a group of one and never gets padded
        parentClass.AddProperty(new CodeProperty
        {
            Name = "id",
            Kind = CodePropertyKind.Custom,
            Access = AccessModifier.Private,
            Type = new CodeType { Name = "string" },
            Documentation = new() { DescriptionTemplate = "The id property" },
        },
        new CodeProperty
        {
            Name = "displayName",
            Kind = CodePropertyKind.Custom,
            Access = AccessModifier.Private,
            Type = new CodeType { Name = "string" },
            Documentation = new() { DescriptionTemplate = "The displayName property" },
        });
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains($"\t// The displayName property{GoTestConstants.LineFeed}\tdisplayName *string", result);
        Assert.Contains($"\t// The id property{GoTestConstants.LineFeed}\tid *string", result);
    }
}
