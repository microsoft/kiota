using Xunit;

namespace Kiota.Builder.tests {
    public class CodeUsingComparerTests {
        [Fact]
        public void ComparesWithDeclaration() {
            var root = CodeNamespace.InitRootNamespace();
            var cUsing = new CodeUsing(root) {
                Name = "using1",
            };
            cUsing.Declaration = new CodeType(cUsing) {
                Name = "type1"
            };

            var cUsing2 = new CodeUsing(root) {
                Name = "using2",
            };
            cUsing2.Declaration = new CodeType(cUsing2) {
                Name = "type2"
            };
            var comparer = new CodeUsingComparer(true);
            Assert.False(comparer.Equals(cUsing, cUsing2));
            Assert.NotEqual(0, comparer.GetHashCode(cUsing));
        }
    }
    
}
