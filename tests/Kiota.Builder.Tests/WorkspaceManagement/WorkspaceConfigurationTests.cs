using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.Manifest;

public sealed class WorkspaceConfigurationTests
{
    [Fact]
    public void Clones()
    {
        var source = new WorkspaceConfiguration
        {
            Clients = { { "GraphClient", new ApiClientConfiguration { ClientNamespaceName = "foo" } } },
        };
        var cloned = (WorkspaceConfiguration)source.Clone();
        Assert.NotNull(cloned);
        Assert.Equal(source.Clients.Count, cloned.Clients.Count);
    }
}
