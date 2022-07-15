using System;
using Kiota.Builder.Writers.Go;
using Moq;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
    public class GoConventionServiceTests {
        private readonly GoConventionService instance = new();
        [Fact]
        public void ThrowsOnInvalidOverloads() {
            var root = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
        }
    }
}
