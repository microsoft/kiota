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

    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("./Victim")]
    [InlineData(".")]
    [InlineData(" ")]
    public async Task UpdateDescriptionRejectsTraversalNamesAsync(string clientName)
    {
        var service = new DescriptionStorageService(tempPath);
        using var stream = new MemoryStream();
        stream.WriteByte(0x1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDescriptionAsync(clientName, stream, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateDescriptionTraversalDoesNotEscapeConsumerNamespaceAsync()
    {
        var service = new DescriptionStorageService(tempPath);
        using var victimStream = new MemoryStream();
        victimStream.WriteByte(0x2);
        await service.UpdateDescriptionAsync("Victim", victimStream, cancellationToken: TestContext.Current.CancellationToken);

        using var maliciousStream = new MemoryStream();
        maliciousStream.WriteByte(0x9);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDescriptionAsync("junk/../Victim", maliciousStream, cancellationToken: TestContext.Current.CancellationToken));

        // The victim's cached description must remain untouched (single byte 0x2 written above).
        var victimFilePath = Path.Combine(tempPath, DescriptionStorageService.DescriptionsSubDirectoryRelativePath, "Victim", "openapi.yml");
        Assert.True(File.Exists(victimFilePath));
        var contents = await File.ReadAllBytesAsync(victimFilePath, TestContext.Current.CancellationToken);
        Assert.Equal([0x2], contents);
    }

    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task GetDescriptionRejectsTraversalNamesAsync(string clientName)
    {
        var service = new DescriptionStorageService(tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDescriptionAsync(clientName, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void RemoveDescriptionRejectsTraversalNames(string clientName)
    {
        var service = new DescriptionStorageService(tempPath);
        Assert.Throws<InvalidOperationException>(() => service.RemoveDescription(clientName));
    }
}
