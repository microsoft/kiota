using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Python.Tests;
public class CodeEnumWriterTests :IDisposable {
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    public CodeEnumWriterTests(){
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        currentEnum = root.AddEnum(new CodeEnum {
            Name = EnumName,
        }).First();
    }
    public void Dispose(){
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesEnum() {
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName});
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains($"(Enum):", result);
        Assert.Contains(optionName, result);
    }
    [Fact]
    public void DoesntWriteAnythingOnNoOption() {
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Empty(result);
    }
}
