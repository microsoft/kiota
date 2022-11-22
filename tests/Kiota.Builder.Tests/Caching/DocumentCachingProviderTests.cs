using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Caching.Tests;

public class DocumentCachingProviderTests {
    [Fact]
    public async Task DefensivePrograming() {
        using var client = new HttpClient();
        var mockLogger = new Mock<ILogger>().Object;

        Assert.Throws<ArgumentNullException>(() => new DocumentCachingProvider(null, null));
        Assert.Throws<ArgumentNullException>(() => new DocumentCachingProvider(client, null));
        Assert.Throws<ArgumentNullException>(() => new DocumentCachingProvider(null, mockLogger));

        var provider = new DocumentCachingProvider(client, mockLogger);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await provider.GetDocumentAsync(null, null, null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await provider.GetDocumentAsync(new Uri("https://localhost"), null, null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await provider.GetDocumentAsync(new Uri("https://localhost"), "foo", null));
    }
    [Fact]
    public async Task GetsCached() {
        using var client = new HttpClient();
        var mockLogger = new Mock<ILogger>().Object;
        var provider = new DocumentCachingProvider(client, mockLogger);
        await using var result = await provider.GetDocumentAsync(new Uri("https://httpbin.org/headers"), "foo", "bar.json");
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Length);
    }
    [Fact]
    public async Task GetsWhenCacheIsOutdated() {
        using var client = new HttpClient();
        var mockLogger = new Mock<ILogger>().Object;
        var provider = new DocumentCachingProvider(client, mockLogger);
        await using var result1 = await provider.GetDocumentAsync(new Uri("https://httpbin.org/headers"), "foo", "bar.json");
        provider.Duration = TimeSpan.FromMilliseconds(-1);
        await using var result2 = await provider.GetDocumentAsync(new Uri("https://httpbin.org/headers"), "foo", "bar.json");
        Assert.NotNull(result2);
        Assert.NotEqual(0, result2.Length);
    }
    [Fact]
    public async Task GetsWhenCacheIsCleared() {
        using var client = new HttpClient();
        var mockLogger = new Mock<ILogger>().Object;
        var provider = new DocumentCachingProvider(client, mockLogger);
        await using var result1 = await provider.GetDocumentAsync(new Uri("https://httpbin.org/headers"), "foo", "bar.json");
        provider.ClearCache = true;
        await using var result2 = await provider.GetDocumentAsync(new Uri("https://httpbin.org/headers"), "foo", "bar.json");
        Assert.NotNull(result2);
        Assert.NotEqual(0, result2.Length);
    }
}
