using System;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public class ApiPluginConfigurationComparerTests
{
    private readonly ApiPluginConfigurationComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }

    [Fact]
    public void TestEquals()
    {
        Assert.True(_comparer.Equals(new(), new()));
        Assert.True(_comparer.Equals(new() { Types = ["a", "b", "c"] }, new() { Types = ["a", "b", "c"] }));
        Assert.False(_comparer.Equals(
            new() { AllowedExternalOrigins = ["https://contoso.com/*"] },
            new() { AllowedExternalOrigins = ["https://example.com/*"] }));
    }
    [Fact]
    public void GetsHashCode()
    {
        Assert.NotEqual(_comparer.GetHashCode(new() { Types = ["OpenAI"] }), _comparer.GetHashCode(new() { Types = ["APIManifest"] }));
    }
}
