using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("#category", "category")]
    [InlineData("https://example.com/schema#itemType", "itemType")]
    [InlineData("itemType", "itemType")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ExtractAnchorNameReturnsExpectedValue(string dynamicRef, string expected)
    {
        Assert.Equal(expected, KiotaBuilder.ExtractAnchorName(dynamicRef));
    }
}
