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
}
