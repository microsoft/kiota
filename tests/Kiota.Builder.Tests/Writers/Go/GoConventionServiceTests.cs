using System;
using Kiota.Builder.Writers.Go;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Go {
    public class GoConventionServiceTests {
        private readonly GoConventionService instance = new();
        [Fact]
        public void ThrowsOnInvalidOverloads() {
            var root = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
            Assert.Throws<InvalidOperationException>(() => instance.GetParameterSignature(new Mock<CodeParameter>().Object));
            Assert.Throws<InvalidOperationException>(() => instance.GetTypeString(new Mock<CodeType>().Object));
            Assert.Throws<InvalidOperationException>(() => instance.GetTypeString(new Mock<CodeUnionType>().Object, new Mock<CodeClass>().Object));
            Assert.Throws<InvalidOperationException>(() => instance.GetTypeString(new Mock<CodeTypeBase>().Object, new Mock<CodeClass>().Object));
        }
    }
}
