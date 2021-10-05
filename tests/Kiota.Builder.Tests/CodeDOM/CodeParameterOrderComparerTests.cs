using Xunit;
using Moq;

namespace Kiota.Builder.Tests {
    public class CodeParameterOrderComparerTests {
        [Fact]
        public void DefensiveProgramming() {
            var comparer = new CodeParameterOrderComparer();
            Assert.NotNull(comparer);
            var root = CodeNamespace.InitRootNamespace();
            var mockParameter = new Mock<CodeParameter>().Object;
            Assert.Equal(0, comparer.Compare(null, null));
            Assert.Equal(-1, comparer.Compare(null, mockParameter));
            Assert.Equal(1, comparer.Compare(mockParameter, null));
        }
    }
}
