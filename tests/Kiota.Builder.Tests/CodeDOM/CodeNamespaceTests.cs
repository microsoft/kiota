using System;
using System.IO;
using Kiota.Builder.Writers.CSharp;
using Xunit;

namespace Kiota.Builder.Tests
{
    public class CodeNamespaceTests
    {
        [Fact]
        public void CreateClassAndRender()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var myNamespace = rootNamespace.AddNamespace("foo");
            var myClass = new CodeClass(myNamespace) { Name = "bar"};
            myNamespace.AddClass(myClass);

            var outputCode = CodeRenderer.RenderCodeAsString(new CSharpWriter(Path.GetRandomFileName(), "foo", false),myNamespace);

            Assert.Equal(@"namespace foo {
    public class Bar {
    }
}
", outputCode);
            
        }
        public const string childName = "one.two.three";
        [Fact]
        public void DoesntThrowOnRootInitialization() {
            var root = CodeNamespace.InitRootNamespace();
            Assert.NotNull(root);
            Assert.Null(root.Parent);
            Assert.NotNull(root.StartBlock);
            Assert.NotNull(root.EndBlock);
        }
        [Fact]
        public void SegmentsNamespaceNames() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(childName);
            Assert.NotNull(child);
            Assert.Equal(childName, child.Name);
            var two = child.Parent as CodeNamespace;
            Assert.NotNull(two);
            Assert.Equal("one.two", two.Name);
            var one = two.Parent as CodeNamespace;
            Assert.NotNull(one);
            Assert.Equal("one", one.Name);
            Assert.Equal(root, one.Parent);
        }
        [Fact]
        public void AddsASingleItemNamespace() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(childName);
            var item = child.EnsureItemNamespace();
            Assert.NotNull(item);
            Assert.True(item.IsItemNamespace);
            Assert.Contains(".item", item.Name);
            Assert.Equal(child, item.Parent);
            var subitem = item.EnsureItemNamespace();
            Assert.Equal(item, subitem);
        }
        [Fact]
        public void ThrowsWhenAddingItemToRoot() {
            Assert.Throws<InvalidOperationException>(() => {
                var root = CodeNamespace.InitRootNamespace();
                var item = root.EnsureItemNamespace();
            });
        }
        [Fact]
        public void ThrowsWhenAddingANamespaceWithEmptyName() {
            var root = CodeNamespace.InitRootNamespace();
            Assert.Throws<ArgumentNullException>(() => {
                root.AddNamespace(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                root.AddNamespace(string.Empty);
            });
        }
        [Fact]
        public void FindsNamespaceByName() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(childName);
            var result = root.FindNamespaceByName(childName);
            Assert.Equal(child, result);
        }
        [Fact]
        public void ThrowsOnAddingEmptyCollections() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(childName);
            Assert.Throws<ArgumentNullException>(() => {
                child.AddClass(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                child.AddClass(new CodeClass[] {});
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.AddClass(new CodeClass[] {null});
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.AddEnum(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                child.AddEnum(new CodeEnum[] {});
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.AddEnum(new CodeEnum[] {null});
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.AddUsing(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                child.AddUsing(new CodeUsing[] {});
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.AddUsing(new CodeUsing[] {null});
            });
        }
    }
}
