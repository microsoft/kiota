using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public sealed class CodeEnumWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "SomeEnum";
    public CodeEnumWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, DefaultPath, DefaultName);
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
        currentEnum.AddOption(new CodeEnumOption { Name = optionName, SerializationName = optionName });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("enum", result);
        Assert.Contains(EnumName, result);
        Assert.Contains($"{optionName}('{optionName}')", result);
        Assert.Contains($"const {EnumName}(this.value);", result);
        Assert.Contains("final String value;", result);
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
        Console.WriteLine(result);
        Assert.Contains($"///  {option.Documentation.DescriptionTemplate}", result);
    }
    [Fact]
    public void WritesEnumSerializationValue()
    {
        var OptionName = "plus1";
        var SerializationValue = "+1";
        var option = new CodeEnumOption
        {
            Name = OptionName,
            SerializationName = SerializationValue
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"{OptionName}('{SerializationValue}')", result);
    }
}
