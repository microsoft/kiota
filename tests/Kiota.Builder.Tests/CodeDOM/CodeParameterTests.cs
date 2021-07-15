using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeParameterTests {
        [Fact]
        public void Defensive() {
            var root = CodeNamespace.InitRootNamespace();
            var parameter = new CodeParameter(root) {
                Name = "class",
            };
            Assert.False(parameter.IsOfKind((CodeParameterKind[])null));
            Assert.False(parameter.IsOfKind(new CodeParameterKind[] { }));
        }
        [Fact]
        public void IsOfKind() {
            var root = CodeNamespace.InitRootNamespace();
            var parameter = new CodeParameter(root) {
                Name = "class",
            };
            Assert.False(parameter.IsOfKind(CodeParameterKind.Headers));
            parameter.ParameterKind = CodeParameterKind.HttpCore;
            Assert.True(parameter.IsOfKind(CodeParameterKind.HttpCore));
            Assert.True(parameter.IsOfKind(CodeParameterKind.HttpCore, CodeParameterKind.Headers));
            Assert.False(parameter.IsOfKind(CodeParameterKind.Headers));
        }
    }
}
