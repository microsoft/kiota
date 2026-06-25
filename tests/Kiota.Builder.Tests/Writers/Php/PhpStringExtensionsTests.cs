using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

public class PhpStringExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Null(PhpStringExtensions.EscapePhpDoubleQuote(null));
        Assert.Empty(string.Empty.EscapePhpDoubleQuote());
    }
    [Fact]
    public void EscapesDollarSignToPreventInterpolation()
    {
        Assert.Equal("\\$foo", "$foo".EscapePhpDoubleQuote());
        Assert.Equal("\\${env('HOME')}", "${env('HOME')}".EscapePhpDoubleQuote());
        Assert.Equal("{\\$obj->method()}", "{$obj->method()}".EscapePhpDoubleQuote());
    }
    [Fact]
    public void StillEscapesStandardDoubleQuoteCharacters()
    {
        const string input = "line1\"\\\n\r\t\0";
        Assert.Equal("line1\\\"\\\\\\n\\r\\t\\0", input.EscapePhpDoubleQuote());
    }
    [Fact]
    public void EscapesDollarSignAfterBackslashEscaping()
    {
        // A literal backslash followed by a dollar sign must remain a literal "\$" once PHP parses it.
        Assert.Equal("\\\\\\$foo", "\\$foo".EscapePhpDoubleQuote());
    }
}
