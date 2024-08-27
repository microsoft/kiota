using Kiota.Builder.Manifest;
using Microsoft.OpenApi.ApiManifest;
using Xunit;

namespace Kiota.Builder.Tests.Manifest;

public sealed class ApiDependencyComparerTests
{
    private readonly ApiDependencyComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(0, _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }
    [Fact]
    public void Equal()
    {
        Assert.False(_comparer.Equals(new ApiDependency { ApiDescriptionUrl = "https://foo" }, new ApiDependency { ApiDescriptionUrl = "https://bar" }));
        Assert.True(_comparer.Equals(new ApiDependency { ApiDescriptionUrl = "https://foo" }, new ApiDependency { ApiDescriptionUrl = "https://foo" }));
    }
}
