using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
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
    private const string EnumName = "SomeEnum";
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
        const string optionName = "Option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName, SerializationName = "option1" });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("#[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]", result);
        Assert.Contains("pub enum SomeEnum", result);
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
            Name = "Option1",
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("///", result);
        Assert.Contains(option.Documentation.DescriptionTemplate, result);
    }
    [Fact]
    public void WritesEnumSerializationValue()
    {
        var optionName = "Plus1";
        var serializationValue = "+1";
        var option = new CodeEnumOption
        {
            Name = optionName,
            SerializationName = serializationValue
        };
        currentEnum.AddOption(option);
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"#[serde(rename = \"{serializationValue}\")]", result);
        Assert.Contains(optionName, result);
    }
    [Fact]
    public void WritesEnumDescription()
    {
        currentEnum.Documentation.DescriptionTemplate = "Some enum description";
        currentEnum.AddOption(new CodeEnumOption { Name = "Option1" });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("/// Some enum description", result);
    }
}
