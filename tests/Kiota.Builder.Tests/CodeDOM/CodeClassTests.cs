using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeClassTests {
        [Fact]
        public void Defensive() {
            var root = CodeNamespace.InitRootNamespace();
            var codeClass = new CodeClass(root) {
                Name = "class",
            };
            Assert.False(codeClass.IsOfKind((CodeClassKind[])null));
            Assert.False(codeClass.IsOfKind(new CodeClassKind[] { }));

            codeClass.StartBlock = new CodeBlock.BlockDeclaration(codeClass);
            Assert.Null(codeClass.GetParentClass());
        }
        [Fact]
        public void IsOfKind() {
            var root = CodeNamespace.InitRootNamespace();
            var codeClass = new CodeClass(root) {
                Name = "class",
            };
            Assert.False(codeClass.IsOfKind(CodeClassKind.Model));
            codeClass.ClassKind = CodeClassKind.RequestBuilder;
            Assert.True(codeClass.IsOfKind(CodeClassKind.RequestBuilder));
            Assert.True(codeClass.IsOfKind(CodeClassKind.RequestBuilder, CodeClassKind.QueryParameters));
            Assert.False(codeClass.IsOfKind(CodeClassKind.QueryParameters));
        }
        [Fact]
        public void SetsIndexer() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            codeClass.SetIndexer(new CodeIndexer(codeClass) {
                Name = "idx"
            });
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.SetIndexer(null);
            });
            Assert.Throws<InvalidOperationException>(() => {
                codeClass.SetIndexer(new CodeIndexer(codeClass) {
                    Name = "idx2"
                });
            });
        }
        [Fact]
        public void ThrowsOnAddingEmptyCollections() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.AddMethod(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                codeClass.AddMethod(new CodeMethod[] {});
            });
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.AddMethod(new CodeMethod[] {null});
            });
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.AddProperty(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                codeClass.AddProperty(new CodeProperty[] {});
            });
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.AddProperty(new CodeProperty[] {null});
            });
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.AddInnerClass(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                codeClass.AddInnerClass(new CodeClass[] {});
            });
            Assert.Throws<ArgumentNullException>(() => {
                codeClass.AddInnerClass(new CodeClass[] {null});
            });
        }
        [Fact]
        public void AddsInnerElements() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            codeClass.AddInnerClass(new CodeClass(codeClass) {
                Name = "subclass"
            });
            Assert.Single(codeClass.GetChildElements(true));
            codeClass.AddMethod(new CodeMethod(codeClass) {
                Name = "submethod"
            });
            Assert.Equal(2, codeClass.GetChildElements(true).Count());
            codeClass.AddProperty(new CodeProperty(codeClass) {
                Name = "subprop"
            });
            Assert.Equal(3, codeClass.GetChildElements(true).Count());
        }
        [Fact]
        public void GetsParentAndGrandParent() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var grandParent = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            var parent = child.AddClass(new CodeClass(child) {
                Name = "parent"
            }).First();
            var childClass = child.AddClass(new CodeClass(child) {
                Name = "child"
            }).First();
            (childClass.StartBlock as CodeClass.Declaration).Inherits = new CodeType(childClass) {
                TypeDefinition = parent,
            };
            (parent.StartBlock as CodeClass.Declaration).Inherits = new CodeType(parent) {
                TypeDefinition = grandParent,
            };
            Assert.Equal(grandParent, parent.GetParentClass());
            Assert.Equal(parent, childClass.GetParentClass());
            Assert.Equal(grandParent, childClass.GetGreatestGrandparent());
        }
        [Fact]
        public void ContainsMember() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            codeClass.AddInnerClass(new CodeClass(codeClass) {
                Name = "subclass"
            });
            Assert.True(codeClass.ContainsMember("subclass"));
        }
    }
}
