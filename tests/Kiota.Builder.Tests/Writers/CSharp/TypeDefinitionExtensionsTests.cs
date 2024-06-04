using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.CSharp;

using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public sealed class TypeDefinitionExtensionsTests
{
    [Fact]
    public void ReturnsFullNameForTypeWithoutNamespace()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();
        var myClass = new CodeClass
        {
            Name = "myClass"
        };
        rootNamespace.AddClass(myClass);

        var fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.Equal("MyClass", fullName);
    }

    [Fact]
    public void ReturnsFullNameForTypeInNamespace()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();

        var myNamespace = rootNamespace.AddNamespace("MyNamespace");
        var myClass = new CodeClass
        {
            Name = "myClass",
        };
        myNamespace.AddClass(myClass);

        var fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.Equal("MyNamespace.MyClass", fullName);
    }

    [Fact]
    public void ReturnsFullNameForNestedTypes()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();

        var myNamespace = rootNamespace.AddNamespace("MyNamespace");

        var myParentClass = new CodeClass
        {
            Name = "myParentClass"
        };
        myNamespace.AddClass(myParentClass);

        var myNestedClass = new CodeClass
        {
            Name = "myNestedClass",
        };
        myParentClass.AddInnerClass(myNestedClass);

        var parentClassFullName = TypeDefinitionExtensions.GetFullName(myParentClass);
        var nestedClassFullName = TypeDefinitionExtensions.GetFullName(myNestedClass);

        Assert.Equal("MyNamespace.MyParentClass", parentClassFullName);
        Assert.Equal("MyNamespace.MyParentClass.MyNestedClass", nestedClassFullName);
    }

    [Fact]
    public void ThrowsIfTypeIsNull()
    {
        Assert.Throws<ArgumentNullException>("typeDefinition", () => TypeDefinitionExtensions.GetFullName(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ThrowsIfTypeDoesNotHaveAName(string typeName)
    {
        var myClass = new CodeClass
        {
            Name = typeName
        };

        Assert.Throws<ArgumentException>("typeDefinition", () => TypeDefinitionExtensions.GetFullName(myClass));
    }

    [Fact]
    public void ThrowsIfTypesParentIsInvalid()
    {
        var myClass = new CodeClass
        {
            Name = "myClass",
            Parent = new CodeConstant()
        };

        Assert.Throws<InvalidOperationException>(() => TypeDefinitionExtensions.GetFullName(myClass));
    }

    [Fact]
    public void CapitalizesTypeNamesInTypeHierarchyButNotTheNamespace()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();
        var myNamespace = rootNamespace.AddNamespace("myNamespace");

        var myParentClass = new CodeClass
        {
            Name = "myParentClass"
        };
        myNamespace.AddClass(myParentClass);

        var myNestedClass = new CodeClass
        {
            Name = "myNestedClass",
        };
        myParentClass.AddInnerClass(myNestedClass);

        var nestedClassFullName = TypeDefinitionExtensions.GetFullName(myNestedClass);

        Assert.Equal("myNamespace.MyParentClass.MyNestedClass", nestedClassFullName);
    }

    [Fact]
    public void DoesNotAppendNamespaceSegmentIfNamespaceNameIsEmpty()
    {
        var rootNamespace = CodeNamespace.InitRootNamespace();
        var myNamespace = rootNamespace.AddNamespace("ThisWillBeEmpty");
        myNamespace.Name = "";

        var myClass = new CodeClass
        {
            Name = "myClass"
        };
        myNamespace.AddClass(myClass);

        var fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.Equal("MyClass", fullName);
    }
}
