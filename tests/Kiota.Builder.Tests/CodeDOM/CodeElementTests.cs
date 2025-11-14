using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeElementTests
{
    [Fact]
    public void GetImmediateParentOfType()
    {
        var root = CodeNamespace.InitRootNamespace();
        var childClass = root.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        var method = childClass.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        Assert.Equal(root, childClass.GetImmediateParentOfType<CodeNamespace>());
        Assert.Equal(childClass, childClass.GetImmediateParentOfType<CodeClass>());
        Assert.Throws<InvalidOperationException>(() =>
        {
            childClass.GetImmediateParentOfType<CodeClass>(root);
        });
        Assert.Equal(root, method.GetImmediateParentOfType<CodeNamespace>());
    }
    [Fact]
    public void IsChildOf()
    {
        var root = CodeNamespace.InitRootNamespace();
        var childClass = root.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        var method = childClass.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "void"
            }
        }).First();
        Assert.True(method.IsChildOf(childClass));
        Assert.True(method.IsChildOf(root));
        Assert.Throws<ArgumentNullException>(() =>
        {
            method.IsChildOf(null);
        });
        Assert.False(method.IsChildOf(root, true));
        Assert.False(root.IsChildOf(method));
    }
}
