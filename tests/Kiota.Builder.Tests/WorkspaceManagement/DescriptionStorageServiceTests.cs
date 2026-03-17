using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class DescriptionStorageServiceTests
{
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task StoresADescriptionAsync()
    {
        var service = new DescriptionStorageService(tempPath);
        using var stream = new MemoryStream();
        stream.WriteByte(0x1);
        await service.UpdateDescriptionAsync("clientName", stream, cancellationToken: TestContext.Current.CancellationToken);
        using var result = await service.GetDescriptionAsync("clientName", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }
    [Fact]
    public async Task DeletesAStoredDescriptionAsync()
    {
        var service = new DescriptionStorageService(tempPath);
        using var stream = new MemoryStream();
        stream.WriteByte(0x1);
        await service.UpdateDescriptionAsync("clientNameA", stream, cancellationToken: TestContext.Current.CancellationToken);
        service.RemoveDescription("clientNameA");
        var result = await service.GetDescriptionAsync("clientNameA", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(result);
    }
    [Fact]
    public async Task ReturnsNothingIfNoDescriptionIsPresentAsync()
    {
        var service = new DescriptionStorageService(tempPath);
        var result = await service.GetDescriptionAsync("clientNameB", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(result);
    }
    [Fact]
    public async Task DefensiveAsync()
    {
        Assert.Throws<ArgumentException>(() => new DescriptionStorageService(string.Empty));
        var service = new DescriptionStorageService(tempPath);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDescriptionAsync(null, Stream.Null, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDescriptionAsync("foo", null, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetDescriptionAsync(null, cancellationToken: TestContext.Current.CancellationToken));
    }
}
