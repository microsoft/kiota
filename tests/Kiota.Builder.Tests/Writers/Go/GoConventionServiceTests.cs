using System;
using Kiota.Builder.Writers.Go;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Go {
    public class GoConventionServiceTests {
        private readonly GoConventionService instance = new GoConventionService();
        [Fact]
        public void ThrowsOnInvalidOverloads() {
            var root = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
            Assert.Throws<InvalidOperationException>(() => instance.GetParameterSignature(new Mock<CodeParameter>(root).Object));
            Assert.Throws<InvalidOperationException>(() => instance.GetTypeString(new Mock<CodeType>(root).Object));
            Assert.Throws<InvalidOperationException>(() => instance.GetTypeString(new Mock<CodeUnionType>(root).Object, new Mock<CodeClass>(root).Object));
            Assert.Throws<InvalidOperationException>(() => instance.GetTypeString(new Mock<CodeTypeBase>(root).Object, new Mock<CodeClass>(root).Object));
        }
    }
}
