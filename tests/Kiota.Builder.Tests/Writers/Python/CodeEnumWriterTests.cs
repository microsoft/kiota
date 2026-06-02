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
        Assert.Contains("Option1 = \"option1\"", result);
        // Verify no injected code appears as executable Python (on its own line outside a comment)
        var lines = result.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            // Every line must be a comment, import, class declaration, or assignment — never bare injected code
            Assert.True(
                trimmed.StartsWith('#') || trimmed.StartsWith("from ") || trimmed.StartsWith("class ") || trimmed.Contains('=') || trimmed == "pass",
                $"Unexpected executable line in output: {trimmed}");
        }
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
        Assert.Contains("Option1 = \"option1\"", result);
        // Verify no injected code appears as executable Python
        var lines = result.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            Assert.True(
                trimmed.StartsWith('#') || trimmed.StartsWith("from ") || trimmed.StartsWith("class ") || trimmed.Contains('=') || trimmed == "pass",
                $"Unexpected executable line in output: {trimmed}");
        }
        // Verify no unescaped triple quotes that could break out of a docstring
        Assert.DoesNotContain("\"\"\"\nimport", result);
    }
    [Theory]
    [InlineData("line1\nimport os\nline3", "line1 import os line3")]
    [InlineData("line1\r\nimport os\r\nline3", "line1  import os  line3")]
    [InlineData("before\"\"\"\nimport os\nafter", "before\\\"\\\"\\\" import os after")]
    [InlineData("normal description", "normal description")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void RemoveInvalidDescriptionCharactersHandlesInjection(string input, string expected)
    {
        var result = Kiota.Builder.Writers.Python.PythonConventionService.RemoveInvalidDescriptionCharacters(input!);
        Assert.Equal(expected, result);
    }
}
