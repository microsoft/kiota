using System.Linq;
using Xunit;

namespace Kiota.Builder.tests {
    public class CodeUnionTypeTests {
        [Fact]
        public void ClonesProperly() {
            var root = CodeNamespace.InitRootNamespace();
            var type = new CodeUnionType(root) {
                Name = "type1",
            };
            type.AddType(new CodeType(type) {
                Name = "subtype"
            });
            var clone = type.Clone() as CodeUnionType;
            Assert.NotNull(clone);
            Assert.Single(clone.AllTypes);
            Assert.Single(clone.Types);
            Assert.Equal(type.AllTypes.First().Name, clone.AllTypes.First().Name);
        }
    }
    
}
