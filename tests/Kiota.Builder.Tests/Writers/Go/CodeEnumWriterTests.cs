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
        Assert.Contains("return 0, errors.New(\"Unknown ", result);
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
        myEnum.AddOption(new CodeEnumOption { Name = optionName });
        writer.Write(myEnum);

        var result = tw.ToString();
        Assert.Contains($"type MultiValueEnum int", result);
        Assert.Contains("const (", result);
        Assert.Contains("OPTION1_MULTIVALUEENUM MultiValueEnum = iota", result);
        Assert.Contains("func (i", result);
        Assert.Contains("String() string {", result);
        Assert.Contains("for p := MultiValueEnum(1); p <= OPTION1_MULTIVALUEENUM; p <<= 1 {", result);
        Assert.Contains("if i&p == p {", result);
        Assert.Contains("values = append(values, []string{\"option1\"}[p])", result);
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
        Assert.Contains("return 0, errors.New(\"Unknown ", result);
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
    public void WritesEnumOptionDescription()
    {
        var option = new CodeEnumOption
        {
            Documentation = new()
            {
                Description = "Some option description",
            },
            Name = "option1",
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"// {option.Documentation.Description}", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
}
