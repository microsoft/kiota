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
    [Fact]
    public void EscapesEnumWireValues()
    {
        currentEnum.AddOption(new CodeEnumOption
        {
            Name = "Option1",
            SerializationName = "line1\"\nline2",
        });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("Option1 = \"line1\\\"\\nline2\",", result);
    }
    [Fact]
    public void SanitizesEnumOptionDescriptionWithNewlines()
    {
        currentEnum.AddOption(new CodeEnumOption
        {
            Name = "Option1",
            SerializationName = "option1",
            Documentation = new()
            {
                DescriptionTemplate = "line1\nimport os; os.system('evil')\r\nline3",
            },
        });
        writer.Write(currentEnum);
        var result = tw.ToString();
        // Newlines replaced with spaces: entire payload is on one comment line, not executable code
        Assert.Contains("# line1 import os; os.system('evil') line3", result);
        // Verify no raw newlines exist within the comment content itself
        var commentLine = result.Split('\n').First(l => l.TrimStart().StartsWith("# line1")).TrimEnd('\r');
        Assert.DoesNotContain("\n", commentLine);
        Assert.DoesNotContain("\r", commentLine);
    }
    [Fact]
    public void SanitizesEnumOptionDescriptionWithTripleQuotes()
    {
        currentEnum.AddOption(new CodeEnumOption
        {
            Name = "Option1",
            SerializationName = "option1",
            Documentation = new()
            {
                DescriptionTemplate = "before\"\"\"\nimport os\nafter",
            },
        });
        writer.Write(currentEnum);
        var result = tw.ToString();
        // Triple quotes escaped, newlines replaced with spaces — all on one comment line
        Assert.Contains("# before\\\"\\\"\\\" import os after", result);
        var commentLine = result.Split('\n').First(l => l.TrimStart().StartsWith("# before"));
        Assert.DoesNotContain("\"\"\"", commentLine);
    }
}
