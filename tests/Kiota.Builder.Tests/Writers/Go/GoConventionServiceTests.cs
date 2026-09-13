using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public class GoConventionServiceTests
{
    private readonly GoConventionService instance = new();
    [Theory]
    [InlineData("``quoted''", "“quoted”")]
    [InlineData("`code`", "`code`")]
    [InlineData("```", "```")]
    [InlineData("```go", "```go")]
    [InlineData("````", "````")]
    [InlineData("`````", "`````")]
    [InlineData("```code``` and ``quoted''", "```code``` and “quoted”")]
    public void NormalizesOnlyDoubleBacktickRuns(string description, string expected)
    {
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, "./", "name");
        using var textWriter = new StringWriter();
        writer.SetTextWriter(textWriter);

        instance.WriteDescriptionItem(description, writer);

        Assert.Equal($"// {expected}{textWriter.NewLine}", textWriter.ToString());
    }
    [Fact]
    public void ThrowsOnInvalidOverloads()
    {
        var root = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
    }
    [Fact]
    public void SanitizesLineBreaksInDocumentationComments()
    {
        var codeClass = new CodeClass
        {
            Name = "testClass",
            Documentation = new()
            {
                DescriptionTemplate = "line1\r\nline2\tline3",
            },
        };
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, "./", "name");
        using var textWriter = new StringWriter();
        writer.SetTextWriter(textWriter);

        instance.WriteShortDescription(codeClass, writer);
        var result = textWriter.ToString();

        Assert.Contains("// line1line2 line3", result);
        Assert.DoesNotContain($"{GoTestConstants.LineFeed}line2", result);
    }
}
