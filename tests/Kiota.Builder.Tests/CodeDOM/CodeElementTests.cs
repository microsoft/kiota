using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.tests {
    public class CodeElementTests {
        [Fact]
        public void ThrowsOnEmptyParent() {
            Assert.Throws<ArgumentNullException>(() => {
                var element = new CodeClass(null);
            });
        }
        [Fact]
        public void GetImmediateParentOfType() {
            var root = CodeNamespace.InitRootNamespace();
            var childClass = root.AddClass(new CodeClass(root) {
                Name = "class1"
            }).First();
            var method = childClass.AddMethod(new CodeMethod(childClass) {
                Name = "method"
            }).First();
            Assert.Equal(root, childClass.GetImmediateParentOfType<CodeNamespace>());
            Assert.Equal(childClass, childClass.GetImmediateParentOfType<CodeClass>());
            Assert.Throws<InvalidOperationException>(() => {
                childClass.GetImmediateParentOfType<CodeClass>(root);
            });
            Assert.Equal(root, method.GetImmediateParentOfType<CodeNamespace>());
        }
        [Fact]
        public void IsChildOf() {
            var root = CodeNamespace.InitRootNamespace();
            var childClass = root.AddClass(new CodeClass(root) {
                Name = "class1"
            }).First();
            var method = childClass.AddMethod(new CodeMethod(childClass) {
                Name = "method"
            }).First();
            Assert.True(method.IsChildOf(childClass));
            Assert.True(method.IsChildOf(root));
            Assert.Throws<ArgumentNullException>(() => { 
                method.IsChildOf(null);
            });
            Assert.False(method.IsChildOf(root, true));
            Assert.False(root.IsChildOf(method));
        }
    }
}
