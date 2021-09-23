using System;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodePropertyTests {
        [Fact]
        public void Defensive() {
            var root = CodeNamespace.InitRootNamespace();
            var property = new CodeProperty(root) {
                Name = "prop",
            };
            Assert.False(property.IsOfKind((CodePropertyKind[])null));
            Assert.False(property.IsOfKind(Array.Empty<CodePropertyKind>()));
        }
        [Fact]
        public void IsOfKind() {
            var root = CodeNamespace.InitRootNamespace();
            var property = new CodeProperty(root) {
                Name = "prop",
            };
            Assert.False(property.IsOfKind(CodePropertyKind.BackingStore));
            property.PropertyKind = CodePropertyKind.RequestBuilder;
            Assert.True(property.IsOfKind(CodePropertyKind.RequestBuilder));
            Assert.True(property.IsOfKind(CodePropertyKind.RequestBuilder, CodePropertyKind.BackingStore));
            Assert.False(property.IsOfKind(CodePropertyKind.BackingStore));
        }
    }
}
