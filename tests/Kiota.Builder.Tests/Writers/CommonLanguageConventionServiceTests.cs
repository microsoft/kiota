using Kiota.Builder.Writers.CSharp;
using Xunit;
using Moq;
using System;


namespace Kiota.Builder.Writers.Tests {
    public class CommonLanguageConventionServiceTests {
        [Fact]
        public void TranslatesType() {
            var service = new CSharpConventionService();
            var root = CodeNamespace.InitRootNamespace();
            var unknownTypeMock = new Mock<CodeTypeBase>();
            unknownTypeMock.Setup(x => x.Name).Returns("unkownType");
            Assert.Throws<InvalidOperationException>(() => service.TranslateType(unknownTypeMock.Object));
            var stringType = new CodeType {
                Name = "string"
            };
            Assert.Equal("string", service.TranslateType(stringType));
            var unionStringType = new CodeUnionType {
                Name = "unionString"
            };
            unionStringType.AddType(stringType);
            Assert.Equal("string", service.TranslateType(unionStringType));
        }
    }
}
