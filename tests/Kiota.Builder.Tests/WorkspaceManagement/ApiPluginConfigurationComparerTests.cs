using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public class ApiPluginConfigurationComparerTests
{
    private readonly ApiPluginConfigurationComparer _comparer = new();
    [Fact]
    public void GetsHashCode()
    {
        Assert.NotEqual(_comparer.GetHashCode(new() { Types = ["OpenAI"] }), _comparer.GetHashCode(new() { Types = ["APIManifest"] }));
    }
}
