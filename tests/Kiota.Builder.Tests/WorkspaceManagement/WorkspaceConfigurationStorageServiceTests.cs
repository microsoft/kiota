using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;
public sealed class WorkspaceConfigurationStorageServiceTests : IDisposable
{
    [Fact]
    public async Task DefensiveProgramming()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceConfigurationStorageService(string.Empty));
        var service = new WorkspaceConfigurationStorageService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateWorkspaceConfigurationAsync(null, null));
    }
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task InitializesAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync();
        Assert.True(File.Exists(Path.Combine(tempPath, "kiota-config.json")));
    }
    [Fact]
    public async Task FailsOnDoubleInit()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
    }
    [Fact]
    public async Task ReturnsNullOnNonInitialized()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        var (config, manifest) = await service.GetWorkspaceConfigurationAsync();
        Assert.Null(config);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsConfigurationWhenInitialized()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync();
        var (result, manifest) = await service.GetWorkspaceConfigurationAsync();
        Assert.NotNull(result);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsIsInitialized()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync();
        var result = await service.IsInitializedAsync();
        Assert.True(result);
    }
    [Fact]
    public async Task DoesNotReturnIsInitialized()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        var result = await service.IsInitializedAsync();
        Assert.False(result);
    }
    public void Dispose()
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        GC.SuppressFinalize(this);
    }
}
