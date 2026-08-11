using System;

using Kiota.Builder.Writers.Java;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Java;

public class JavaWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new JavaWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new JavaWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new JavaWriter("./", null));
    }
    [Theory]
    [InlineData("**//", "** //")] // deletion would re-form "*/"; replacement must not
    [InlineData("**\\/", "** //")] // backslash normalization must not re-form "*/"
    [InlineData("*\u00e9/", "* /")] // non-ASCII strip must run before delimiter neutralization
    [InlineData("*/", "* /")]
    [InlineData("/*", "//*")]
    [InlineData("normal description", "normal description")]
    [InlineData("", "")]
    public void RemoveInvalidDescriptionCharactersNeutralizesCommentBreakout(string input, string expected)
    {
        var result = JavaConventionService.RemoveInvalidDescriptionCharacters(input);
        Assert.Equal(expected, result);
        Assert.DoesNotContain("*/", result, StringComparison.Ordinal);
    }
}
