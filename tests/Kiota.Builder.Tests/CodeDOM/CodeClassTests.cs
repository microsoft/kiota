using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;
public class CodeClassTests {
    [Fact]
    public void Defensive() {
        var root = CodeNamespace.InitRootNamespace();
        var codeClass = new CodeClass {
            Name = "class",
        };
        root.AddClass(codeClass);
        Assert.False(codeClass.IsOfKind(null));
        Assert.False(codeClass.IsOfKind(Array.Empty<CodeClassKind>()));
        Assert.Throws<ArgumentNullException>(() => codeClass.DiscriminatorInformation.AddDiscriminatorMapping(null, new CodeType{Name = "class"}));
        Assert.Throws<ArgumentNullException>(() => codeClass.DiscriminatorInformation.AddDiscriminatorMapping("oin", null));
        Assert.Throws<ArgumentNullException>(() => codeClass.DiscriminatorInformation.GetDiscriminatorMappingValue(null));
        Assert.Null(codeClass.DiscriminatorInformation.GetDiscriminatorMappingValue("oin"));

        Assert.Null(codeClass.GetParentClass());
    }
    [Fact]
    public void IsOfKind() {
        var root = CodeNamespace.InitRootNamespace();
        var codeClass = new CodeClass {
            Name = "class",
        };
        root.AddClass(codeClass);
        Assert.False(codeClass.IsOfKind(CodeClassKind.Model));
        codeClass.Kind = CodeClassKind.RequestBuilder;
        Assert.True(codeClass.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.True(codeClass.IsOfKind(CodeClassKind.RequestBuilder, CodeClassKind.QueryParameters));
        Assert.False(codeClass.IsOfKind(CodeClassKind.QueryParameters));
    }
    [Fact]
    public void SetsIndexer() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var codeClass = child.AddClass(new CodeClass {
            Name = "class1"
        }).First();
        codeClass.SetIndexer(new CodeIndexer {
            Name = "idx",
            SerializationName = "idx_smth"
        });
        Assert.Single(codeClass.GetChildElements(true).OfType<CodeIndexer>());
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.SetIndexer(null);
        });
        codeClass.SetIndexer(new CodeIndexer {
            Name = "idx2",
            SerializationName = "idx-2"
        });
        Assert.Empty(codeClass.GetChildElements(true).OfType<CodeIndexer>());
        var methods = codeClass.GetChildElements(true).OfType<CodeMethod>().Where(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)).ToArray();
        Assert.Equal(2, methods.Length);
        Assert.Equal("ByIdx_smth", methods.FirstOrDefault(x => x.OriginalIndexer.Name.Equals("idx")).Name);
        Assert.Equal("ByIdx2", methods.FirstOrDefault(x => x.OriginalIndexer.Name.Equals("idx2")).Name);
    }
    [Fact]
    public void ThrowsOnAddingEmptyCollections() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var codeClass = child.AddClass(new CodeClass {
            Name = "class1"
        }).First();
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddMethod(null);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            codeClass.AddMethod(Array.Empty<CodeMethod>());
        });
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddMethod(new CodeMethod[] {null});
        });
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddProperty(null);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            codeClass.AddProperty(Array.Empty<CodeProperty>());
        });
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddProperty(new CodeProperty[] {null});
        });
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddInnerClass(null);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            codeClass.AddInnerClass(Array.Empty<CodeClass>());
        });
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddInnerClass(new CodeClass[] {null});
        });
    }
    [Fact]
    public void AddsInnerElements() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var codeClass = child.AddClass(new CodeClass {
            Name = "class1"
        }).First();
        codeClass.AddInnerClass(new CodeClass {
            Name = "subclass"
        });
        Assert.Single(codeClass.GetChildElements(true));
        codeClass.AddMethod(new CodeMethod {
            Name = "submethod"
        });
        Assert.Equal(2, codeClass.GetChildElements(true).Count());
        codeClass.AddProperty(new CodeProperty {
            Name = "subprop"
        });
        Assert.Equal(3, codeClass.GetChildElements(true).Count());
    }
    [Fact]
    public void AddsInnerInterface() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var codeClass = child.AddClass(new CodeClass {
            Name = "class1"
        }).First();
        codeClass.AddInnerInterface(new CodeInterface {
            Name = "subinterface"
        });
        Assert.Single(codeClass.GetChildElements(true).OfType<CodeInterface>());
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddInnerInterface(null);
        });
        Assert.Throws<ArgumentNullException>(() => {
            codeClass.AddInnerInterface(new CodeInterface[] {null});
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            codeClass.AddInnerInterface();
        });
    }
    [Fact]
    public void GetsParentAndGrandParent() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var grandParent = child.AddClass(new CodeClass {
            Name = "class1"
        }).First();
        var parent = child.AddClass(new CodeClass {
            Name = "parent"
        }).First();
        var childClass = child.AddClass(new CodeClass {
            Name = "child"
        }).First();
        childClass.StartBlock.Inherits = new CodeType {
            TypeDefinition = parent,
        };
        parent.StartBlock.Inherits = new CodeType {
            TypeDefinition = grandParent,
        };
        Assert.Equal(grandParent, parent.GetParentClass());
        Assert.Equal(parent, childClass.GetParentClass());
        Assert.Equal(grandParent, childClass.GetGreatestGrandparent());
    }
    [Fact]
    public void ContainsMember() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var codeClass = child.AddClass(new CodeClass {
            Name = "class1"
        }).First();
        codeClass.AddInnerClass(new CodeClass {
            Name = "subclass"
        });
        Assert.True(codeClass.ContainsMember("subclass"));
    }
    [Fact]
    public void InheritsFrom() {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(CodeNamespaceTests.ChildName);
        var baseClass = child.AddClass(new CodeClass {
            Name = "baseClass"
        }).First();
        var baseClass2 = child.AddClass(new CodeClass {
            Name = "baseClass2"
        }).First();
        baseClass2.StartBlock.Inherits = new CodeType {
            TypeDefinition = baseClass,
        };
        var subClass = child.AddClass(new CodeClass {
            Name = "subclass"
        }).First();
        var unrelatedClass = child.AddClass(new CodeClass {
            Name = "unrelatedClass"
        }).First();
        subClass.StartBlock.Inherits = new CodeType {
            TypeDefinition = baseClass2,
        };
        Assert.True(baseClass2.StartBlock.InheritsFrom(baseClass));
        Assert.True(subClass.StartBlock.InheritsFrom(baseClass));
        Assert.True(subClass.StartBlock.InheritsFrom(baseClass2));
        Assert.False(subClass.StartBlock.InheritsFrom(unrelatedClass));
        Assert.False(baseClass.StartBlock.InheritsFrom(baseClass2));
    }
}
