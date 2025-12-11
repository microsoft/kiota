using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Java;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Java;

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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
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
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("public enum", result);
        Assert.Contains("implements ValuedEnum", result);
        Assert.Contains("public final String value", result);
        Assert.Contains("this.value = value", result);
        Assert.Contains("public String getValue()", result);
        Assert.Contains("return this.value", result);
        Assert.Contains("@jakarta.annotation.Nonnull", result);
        Assert.Contains("@jakarta.annotation.Nullable", result);
        Assert.Contains("forValue(@jakarta.annotation.Nonnull final String searchValue)", result);
        Assert.Contains("default: return null;", result);
        Assert.Contains(JavaConventionService.AutoGenerationHeader, result);
        AssertExtensions.CurlyBracesAreClosed(result);
        Assert.Contains(optionName, result);
    }
    [Fact]
    public void DoesntWriteAnythingOnNoOption()
    {
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Empty(result);
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
        Assert.Contains($"/** {option.Documentation.DescriptionTemplate} */", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesEnumSerializationValue()
    {
        var option = new CodeEnumOption
        {
            Name = "plus_1",
            SerializationName = "+1"
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"plus_1(\"+1\")", result);
    }
}
