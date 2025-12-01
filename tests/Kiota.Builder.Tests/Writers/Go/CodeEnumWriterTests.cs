using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
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
    public void WritesSingleValueEnum()
    {
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"type {EnumName.ToFirstCharacterUpperCase()} int", result);
        Assert.Contains("const (", result);
        Assert.Contains($"{EnumName.ToFirstCharacterUpperCase()} = iota", result);
        Assert.Contains("func (i", result);
        Assert.Contains("String() string {", result);
        Assert.Contains("return []string{\"option1\"}[i]", result);
        Assert.Contains("result = OPTION1_SOMEENUM", result);
        Assert.Contains("[i]", result);
        Assert.Contains("func Parse", result);
        Assert.Contains("(v string) (any, error)", result);
        Assert.Contains("switch v", result);
        Assert.Contains("default", result);
        Assert.Contains("result :=", result);
        Assert.Contains("return &result, nil", result);
        Assert.Contains("return nil, nil", result);
        AssertExtensions.CurlyBracesAreClosed(result);
        Assert.Contains(optionName.ToUpperInvariant(), result);
        Assert.Contains("func (i SomeEnum) isMultiValue() bool {", result);
        Assert.Contains("return false", result);
    }
    [Fact]
    public void WritesMultiValueEnum()
    {
        var root = CodeNamespace.InitRootNamespace();
        var myEnum = root.AddEnum(new CodeEnum
        {
            Name = "MultiValueEnum",
            Flags = true
        }).First();
        const string optionName = "option1";
        myEnum.AddOption(new CodeEnumOption { Name = optionName }, new CodeEnumOption { Name = "option2" }, new CodeEnumOption { Name = "option3" });
        writer.Write(myEnum);

        var result = tw.ToString();
        Assert.Contains($"type MultiValueEnum int", result);
        Assert.Contains("const (", result);
        Assert.Contains("OPTION1_MULTIVALUEENUM = 1", result);
        Assert.Contains("OPTION2_MULTIVALUEENUM = 2", result);
        Assert.Contains("OPTION3_MULTIVALUEENUM = 4", result);
        Assert.Contains("func (i", result);
        Assert.Contains("String() string {", result);
        Assert.Contains("options := []string{\"option1\", \"option2\", \"option3\"}", result);
        Assert.Contains("for p := 0; p < 3; p++ {", result);
        Assert.Contains("mantis := MultiValueEnum(int(math.Pow(2, float64(p))))", result);
        Assert.Contains("if i&mantis == mantis {", result);
        Assert.Contains("values = append(values, options[p])", result);
        Assert.Contains("for _, str := range values {", result);
        Assert.Contains("strings.Join(values", result);
        Assert.Contains("result |= OPTION1_MULTIVALUEENUM", result);
        Assert.Contains("[i]", result);
        Assert.Contains("func Parse", result);
        Assert.Contains("(v string) (any, error)", result);
        Assert.Contains("switch str", result);
        Assert.Contains("default", result);
        Assert.Contains("result :=", result);
        Assert.Contains("return &result, nil", result);
        Assert.Contains("return nil, nil", result);
        Assert.Contains("func (i MultiValueEnum) isMultiValue() bool {", result);
        Assert.Contains("return true", result);
        AssertExtensions.CurlyBracesAreClosed(result);
        Assert.Contains(optionName.ToUpperInvariant(), result);
    }
    [Fact]
    public void DoesntWriteAnythingOnNoOption()
    {
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Empty(result);
    }
    [Fact]
    public void WritesUsing()
    {
        currentEnum.AddUsing(new CodeUsing
        {
            Name = "using1",
        });
        currentEnum.AddOption(new CodeEnumOption { Name = "o" });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("using1", result);
    }
    [Fact]
    public void WritesGenerateCodeComment()
    {
        var option = new CodeEnumOption
        {
            Documentation = new()
            {
            },
            Name = "generated1",
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains(Environment.NewLine, result);
        var firstline = result[0..^result.IndexOf(Environment.NewLine)];
        Assert.Contains("DO NOT EDIT", firstline);
    }
    [Fact]
    public void WritesEnumOptionDescription()
    {
        var option = new CodeEnumOption
        {
            Documentation = new()
            {
                DescriptionTemplate = "Some option description",
            },
            Name = "option1",
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"// {option.Documentation.DescriptionTemplate}", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesNotWriteImportOnEmptyImports()
    {
        var option = new CodeEnumOption
        {
            Documentation = new()
            {
                DescriptionTemplate = "Some option description",
            },
            Name = "option1",
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.DoesNotContain("import", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
}
