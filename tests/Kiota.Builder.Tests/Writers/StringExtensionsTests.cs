using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers;

public class StringExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Null(StringExtensions.StripArraySuffix(null));
        Assert.Empty(string.Empty.StripArraySuffix());
    }
    [Fact]
    public void StripsSuffix()
    {
        Assert.Equal("foo", "foo[]".StripArraySuffix());
        Assert.Equal("[]foo", "[]foo".StripArraySuffix());
    }
    [Fact]
    public void SanitizesDoubleQuotedLiterals()
    {
        const string input = "line1\"\\\n\r\t\0";
        Assert.Equal("line1\\\"\\\\\\n\\r\\t\\0", input.SanitizeDoubleQuote());
    }
    [Fact]
    public void SanitizesSingleQuotedLiterals()
    {
        const string input = "line1'\\\n\r\t\0";
        Assert.Equal("line1\\'\\\\\\n\\r\\t\\0", input.SanitizeSingleQuote());
    }
    [Fact]
    public void SanitizesQuotedStringLiteral()
    {
        const string input = "\"line1\\\nline2\"";
        Assert.Equal("\"line1\\\\\\nline2\"", input.SanitizeQuotedStringLiteral());
    }
}
