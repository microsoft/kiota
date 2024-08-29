﻿using System;
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
        await service.UpdateDescriptionAsync("clientName", stream);
        using var result = await service.GetDescriptionAsync("clientName");
        Assert.NotNull(result);
    }
    [Fact]
    public async Task DeletesAStoredDescriptionAsync()
    {
        var service = new DescriptionStorageService(tempPath);
        using var stream = new MemoryStream();
        stream.WriteByte(0x1);
        await service.UpdateDescriptionAsync("clientNameA", stream);
        service.RemoveDescription("clientNameA");
        var result = await service.GetDescriptionAsync("clientNameA");
        Assert.Null(result);
    }
    [Fact]
    public async Task ReturnsNothingIfNoDescriptionIsPresentAsync()
    {
        var service = new DescriptionStorageService(tempPath);
        var result = await service.GetDescriptionAsync("clientNameB");
        Assert.Null(result);
    }
    [Fact]
    public async Task DefensiveAsync()
    {
        Assert.Throws<ArgumentException>(() => new DescriptionStorageService(string.Empty));
        var service = new DescriptionStorageService(tempPath);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDescriptionAsync(null, Stream.Null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDescriptionAsync("foo", null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetDescriptionAsync(null));
    }
}
