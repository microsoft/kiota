using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceConfigurationStorageServiceTests : IDisposable
{
    [Fact]
    public async Task DefensiveProgrammingAsync()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceConfigurationStorageService(string.Empty));
        var service = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateWorkspaceConfigurationAsync(null, null, cancellationToken: TestContext.Current.CancellationToken));
    }
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task InitializesAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName)));
    }
    [Fact]
    public async Task FailsOnDoubleInitAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task ReturnsNullOnNonInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        var (config, manifest) = await service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(config);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsConfigurationWhenInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        var (result, manifest) = await service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsIsInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        var result = await service.IsInitializedAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);
    }
    [Fact]
    public async Task DoesNotReturnIsInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        var result = await service.IsInitializedAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }
    [Fact]
    public async Task BackupsAndRestoresAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await service.BackupConfigAsync(cancellationToken: TestContext.Current.CancellationToken);
        var targetConfigFile = Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName);
        File.Delete(targetConfigFile);
        Assert.False(File.Exists(targetConfigFile));
        await service.RestoreConfigAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(File.Exists(targetConfigFile));
    }
    public void Dispose()
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        GC.SuppressFinalize(this);
    }
}
