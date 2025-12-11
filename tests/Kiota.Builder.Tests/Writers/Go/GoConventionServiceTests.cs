using System;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public class GoConventionServiceTests
{
    private readonly GoConventionService instance = new();
    [Fact]
    public void ThrowsOnInvalidOverloads()
    {
        var root = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
    }
}
