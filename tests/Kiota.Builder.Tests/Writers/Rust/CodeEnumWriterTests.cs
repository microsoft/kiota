using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public sealed class CodeEnumWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    public CodeEnumWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Rust, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        currentEnum = root.AddEnum(new CodeEnum
        {
            Name = EnumName,
        }).First();
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesEnum()
    {
        currentEnum.AddOption(new CodeEnumOption { Name = "option1" });
        currentEnum.AddOption(new CodeEnumOption { Name = "option2" });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("pub enum SomeEnum {", result);
        Assert.Contains("Option1,", result);
        Assert.Contains("Option2,", result);
        Assert.Contains("#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]", result);
        // Display impl
        Assert.Contains("impl std::fmt::Display for SomeEnum {", result);
        Assert.Contains("fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {", result);
        Assert.Contains("Self::Option1 => write!(f, \"option1\")", result);
        Assert.Contains("Self::Option2 => write!(f, \"option2\")", result);
        // Parse method
        Assert.Contains("impl SomeEnum {", result);
        Assert.Contains("pub fn parse(s: &str) -> Option<Self> {", result);
        Assert.Contains("\"option1\" => Some(Self::Option1)", result);
        Assert.Contains("\"option2\" => Some(Self::Option2)", result);
        Assert.Contains("_ => None,", result);
    }
    [Fact]
    public void WritesEnumWithDocComments()
    {
        currentEnum.Documentation = new()
        {
            DescriptionTemplate = "Represents the available choices",
        };
        currentEnum.AddOption(new CodeEnumOption
        {
            Name = "active",
            Documentation = new()
            {
                DescriptionTemplate = "The active state",
            },
        });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("/// Represents the available choices", result);
        Assert.Contains("/// The active state", result);
        Assert.Contains("pub enum SomeEnum {", result);
        Assert.Contains("Active,", result);
    }
    [Fact]
    public void WritesFlagEnumWithoutCopy()
    {
        var root = CodeNamespace.InitRootNamespace();
        var flagEnum = root.AddEnum(new CodeEnum
        {
            Name = "permissions",
            Flags = true,
        }).First();
        flagEnum.AddOption(new CodeEnumOption { Name = "read" });
        flagEnum.AddOption(new CodeEnumOption { Name = "write" });
        writer.Write(flagEnum);
        var result = tw.ToString();
        Assert.Contains("#[derive(Debug, Clone, PartialEq, Eq, Hash)]", result);
        Assert.DoesNotContain("Copy", result);
    }
    [Fact]
    public void WritesGeneratedCodeComment()
    {
        currentEnum.AddOption(new CodeEnumOption { Name = "val" });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("DO NOT EDIT", result);
    }
}
