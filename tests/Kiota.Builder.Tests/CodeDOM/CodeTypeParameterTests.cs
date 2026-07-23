using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeTypeParameterTests
{
    [Fact]
    public void AddTypeParameterMakesClassGeneric()
    {
        var root = CodeNamespace.InitRootNamespace();
        var codeClass = root.AddClass(new CodeClass { Name = "PaginatedTemplate" }).First();

        Assert.False(codeClass.IsGeneric);
        Assert.Empty(codeClass.TypeParameters);

        codeClass.StartBlock.AddTypeParameter(new CodeTypeParameter { Name = "TItemType" });

        Assert.True(codeClass.IsGeneric);
        var parameter = Assert.Single(codeClass.TypeParameters);
        Assert.Equal("TItemType", parameter.Name);
    }

    [Fact]
    public void AddTypeParameterIsIdempotentByName()
    {
        var root = CodeNamespace.InitRootNamespace();
        var codeClass = root.AddClass(new CodeClass { Name = "PaginatedTemplate" }).First();

        codeClass.StartBlock.AddTypeParameter(new CodeTypeParameter { Name = "T" });
        codeClass.StartBlock.AddTypeParameter(new CodeTypeParameter { Name = "T" });

        Assert.Single(codeClass.TypeParameters);
    }

    [Fact]
    public void AddTypeParameterRejectsNullEntries()
    {
        var root = CodeNamespace.InitRootNamespace();
        var codeClass = root.AddClass(new CodeClass { Name = "PaginatedTemplate" }).First();

        Assert.Throws<ArgumentNullException>(() => codeClass.StartBlock.AddTypeParameter(null!));
        Assert.Throws<ArgumentNullException>(() => codeClass.StartBlock.AddTypeParameter(new CodeTypeParameter { Name = "T" }, null!));
    }
}
