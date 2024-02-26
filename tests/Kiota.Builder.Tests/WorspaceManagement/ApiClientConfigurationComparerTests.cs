using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;
public sealed class ApiClientConfigurationComparerTests
{
    private readonly ApiClientConfigurationComparer _comparer = new();
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
        Assert.Equal(13, _comparer.GetHashCode(new() { UsesBackingStore = true }));
    }
}
