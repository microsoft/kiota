using Kiota.Builder.Writers.CSharp;
using Xunit;
using Moq;
using System;


namespace Kiota.Builder.Tests.Writers {
    public class CommonLanguageConventionServiceTests {
        [Fact]
        public void TranslatesType() {
            var service = new CSharpConventionService();
            var root = CodeNamespace.InitRootNamespace();
            var unknownTypeMock = new Mock<CodeTypeBase>(root);
            unknownTypeMock.Setup(x => x.Name).Returns("unkownType");
            Assert.Throws<InvalidOperationException>(() => service.TranslateType(unknownTypeMock.Object));
            var stringType = new CodeType(root) {
                Name = "string"
            };
            Assert.Equal("string", service.TranslateType(stringType));
            var unionStringType = new CodeUnionType(root) {
                Name = "unionString"
            };
            unionStringType.Types.Add(stringType);
            Assert.Equal("string", service.TranslateType(unionStringType));
        }
    }
}
