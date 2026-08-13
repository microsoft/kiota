using System;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeNamespaceTests
{
    public const string ChildName = "one.two.three";
    [Fact]
    public void DoesntThrowOnRootInitialization()
    {
        var root = CodeNamespace.InitRootNamespace();
        Assert.NotNull(root);
        Assert.Null(root.Parent);
        Assert.NotNull(root.StartBlock);
        Assert.NotNull(root.EndBlock);
    }
    [Fact]
    public void SegmentsNamespaceNames()
    {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(ChildName);
        Assert.NotNull(child);
        Assert.Equal(ChildName, child.Name);
        var two = child.Parent as CodeNamespace;
        Assert.NotNull(two);
        Assert.Equal("one.two", two.Name);
        var one = two.Parent as CodeNamespace;
        Assert.NotNull(one);
        Assert.Equal("one", one.Name);
        Assert.Equal(root, one.Parent);
    }
    [Fact]
    public void AddsASingleItemNamespace()
    {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(ChildName);
        var item = child.EnsureItemNamespace();
        Assert.NotNull(item);
        Assert.True(item.IsItemNamespace);
        Assert.Contains(".item", item.Name);
        Assert.Equal(child, item.Parent);
        var subitem = item.EnsureItemNamespace();
        Assert.Equal(item, subitem);
    }
    [Fact]
    public void ThrowsWhenAddingItemToRoot()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var root = CodeNamespace.InitRootNamespace();
            var item = root.EnsureItemNamespace();
        });
    }
    [Fact]
    public void ThrowsWhenAddingANamespaceWithEmptyName()
    {
        var root = CodeNamespace.InitRootNamespace();
        Assert.Throws<ArgumentNullException>(() =>
        {
            root.AddNamespace(null);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            root.AddNamespace(string.Empty);
        });
    }
    [Fact]
    public void FindsNamespaceByName()
    {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(ChildName);
        var result = root.FindNamespaceByName(ChildName);
        Assert.Equal(child, result);
    }
    [Fact]
    public void ThrowsOnAddingEmptyCollections()
    {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(ChildName);
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddClass(null);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            child.AddClass(Array.Empty<CodeClass>());
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddClass(new CodeClass[] { null });
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddEnum(null);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            child.AddEnum(Array.Empty<CodeEnum>());
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddEnum(new CodeEnum[] { null });
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddUsing(null);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddUsing(new CodeUsing[] { null });
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddFunction(null);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddFunction(new CodeFunction[] { null });
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddInterface(null);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddInterface(new CodeInterface[] { null });
        });
    }
    [Fact]
    public void IsParentOf()
    {
        var root = CodeNamespace.InitRootNamespace();
        var child = root.AddNamespace(ChildName);
        var grandchild = child.AddNamespace(ChildName + ".four");
        Assert.True(child.IsParentOf(grandchild));
        Assert.False(grandchild.IsParentOf(child));
    }

}
