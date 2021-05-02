using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.tests {
    public class CodeBlockTests {
        [Fact]
        public void RemovesElements() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var elements = child.AddClass(new CodeClass(child) { Name = "class1"},
                            new CodeClass(child) { Name = "class2"});
            child.RemoveChildElement(elements.First());
            Assert.Single(child.GetChildElements(true));

            child.RemoveChildElement<CodeClass>(null); // doesn't fail when passing null collection
        }
        [Fact]
        public void AddsUsing() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            child.AddUsing(new CodeUsing(child) {
                Name = "someNS"
            });
            Assert.Single(child.StartBlock.Usings);
        }
        [Fact]
        public void ThrowsWhenInsertingDuplicatedElements() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            Assert.Throws<InvalidOperationException>(() => {
                child.AddClass(new CodeClass(child) {
                    Name = "class1"
                }, new CodeClass(child) {
                    Name = "class1"
                });
            });
        }
        [Fact]
        [InlineData(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)]
        public void DoesntThrowWhenAddingOVerloads() {
            //TODO research how to access the data when online
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            var method = new CodeMethod(codeClass) {
                Name = "method",
                MethodKind = CodeMethodKind.RequestExecutor,
            };
            var overload = method.Clone() as CodeMethod;
            overload.Parameters.Add(new CodeParameter(overload) {
                Name = "param1"
            });
            codeClass.AddMethod(method, overload);
        }
        [Fact]
        public void DoesntThrowWhenAddingIndexersWIthPropName() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            var property = new CodeProperty(codeClass) {
                Name = "method",
                PropertyKind = CodePropertyKind.RequestBuilder,
            };
            var indexer = new CodeMethod(child) {
                Name = "method",
                MethodKind = CodeMethodKind.IndexerBackwardCompatibility
            };
            codeClass.AddProperty(property);
            codeClass.AddMethod(indexer);
        }
        [Fact]
        public void FindChildByNameThrowsOnEmptyNames() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            Assert.Throws<ArgumentNullException>(() => {
                child.FindChildByName<CodeClass>(string.Empty);
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.FindChildrenByName<CodeClass>(string.Empty);
            });
        }
        [Fact]
        public void FindsChildByNameInSubnamespace() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var className = "class1";
            var class1 = child.AddClass(new CodeClass(child) {
                Name = className
            }).First();
            Assert.Equal(class1, child.FindChildByName<CodeClass>(className));
            Assert.Null(child.FindChildByName<CodeClass>("class2"));
            Assert.Null(child.FindChildByName<CodeEnum>(className));
        }
        [Fact]
        public void FindsChildrenByName() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var className = "class1";
            child.AddClass(new CodeClass(child) {
                Name = className
            });
            var subchild = child.AddNamespace($"{child.Name}.four");
            subchild.AddClass(new CodeClass(subchild) {
                Name = className
            });
            Assert.Equal(2, root.FindChildrenByName<CodeClass>(className).Count());
        }
    }
}
