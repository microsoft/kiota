using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
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
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("from enum import Enum", result);
        Assert.Contains("(str, Enum):", result);
        Assert.Contains($"Option1 = \"{optionName}\"", result);
        Assert.DoesNotContain("pass", result);
    }
    [Fact]
    public void WritesNullStatementOnNoOption()
    {
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("from enum import Enum", result);
        Assert.Contains("(str, Enum):", result);
        Assert.Contains("pass", result);
    }
}
