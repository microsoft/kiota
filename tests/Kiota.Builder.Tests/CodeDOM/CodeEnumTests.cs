using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeEnumTests {
        [Fact]
        public void EnumInits() {
            var root = CodeNamespace.InitRootNamespace();
            var codeEnum = root.AddEnum(new CodeEnum(root) {
                Name = "Enum",
                Description = "some description",
                Flags = true,
            }).First();
            codeEnum.Options.Add("option1");
        }
    }
}
