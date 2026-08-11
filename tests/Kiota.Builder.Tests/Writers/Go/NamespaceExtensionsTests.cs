using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public class NamespaceExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Equal(GoNamespaceExtensions.GetNamespaceImportSymbol(null), string.Empty);
        Assert.Equal(GoNamespaceExtensions.GetLastNamespaceSegment(null), string.Empty);
        Assert.Equal(GoNamespaceExtensions.GetInternalNamespaceImport(null), string.Empty);
    }
    [Fact]
    public void GetLastNamespaceSegment()
    {
        Assert.Equal("something", "github.com/microsoft/kiota.something".GetLastNamespaceSegment());
    }
    [Fact]
    public void GetNamespaceImportSymbol()
    {
        var root = CodeNamespace.InitRootNamespace();
        var main = root.AddNamespace("github.com/something");
        Assert.Equal("i749ccebf37b522f21de9a46471b0aeb8823a49292ca8740fc820cf9bd340c846", main.GetNamespaceImportSymbol());
    }
}
