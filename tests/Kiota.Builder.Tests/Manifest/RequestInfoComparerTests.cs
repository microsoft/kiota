using Kiota.Builder.Manifest;
using Microsoft.OpenApi.ApiManifest;
using Xunit;

namespace Kiota.Builder.Tests.Manifest;

public sealed class RequestInfoComparerTests
{
    private readonly RequestInfoComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(0, _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }
    [Fact]
    public void GetsHashCode()
    {
        Assert.Equal(0, _comparer.GetHashCode(new()));
    }
    [Fact]
    public void Compares()
    {
        var requestInfo = new RequestInfo
        {
            Method = "get",
            UriTemplate = "https://graph.microsoft.com/v1.0/users"
        };
        var requestInfo2 = new RequestInfo
        {
            Method = "get",
            UriTemplate = "https://graph.microsoft.com/v1.0/me"
        };
        Assert.False(_comparer.Equals(requestInfo, requestInfo2));
    }
}
