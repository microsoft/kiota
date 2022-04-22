using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
    public class NamespaceExtensionsTests {
        [Fact]
        public void Defensive() {
            Assert.Equal(GoNamespaceExtensions.GetNamespaceImportSymbol((CodeNamespace)null), string.Empty);
            Assert.Equal(GoNamespaceExtensions.GetLastNamespaceSegment((string)null), string.Empty);
            Assert.Equal(GoNamespaceExtensions.GetInternalNamespaceImport((CodeNamespace)null), string.Empty);
        }
        [Fact]
        public void GetLastNamespaceSegment() {
            Assert.Equal("something", GoNamespaceExtensions.GetLastNamespaceSegment("github.com/microsoft/kiota.something"));
        }
        [Fact]
        public void GetNamespaceImportSymbol() {
            var root = CodeNamespace.InitRootNamespace();
            var main = root.AddNamespace("github.com/something");
            Assert.Equal("i749ccebf37b522f21de9a46471b0aeb8823a49292ca8740fc820cf9bd340c846", GoNamespaceExtensions.GetNamespaceImportSymbol(main));
        }
    }
}
