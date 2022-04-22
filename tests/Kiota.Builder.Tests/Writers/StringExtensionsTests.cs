using Xunit;

namespace Kiota.Builder.Writers.Tests {
    public class StringExtensionsTests {
        [Fact]
        public void Defensive() {
            Assert.Null(StringExtensions.StripArraySuffix(null));
            Assert.Empty(StringExtensions.StripArraySuffix(string.Empty));
        }
        [Fact]
        public void StripsSuffix() {
            Assert.Equal("foo", StringExtensions.StripArraySuffix("foo[]"));
            Assert.Equal("[]foo", StringExtensions.StripArraySuffix("[]foo"));
        }
    }
}
