using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Kiota.Builder.Caching.Tests;

public class DocumentCachingProviderTests
{
    [Fact]
    public async Task DefensivePrograming()
    {
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
    private const string ResponsePayload = @"{
  ""headers"": {
    ""Accept"": ""text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"", 
    ""Accept-Encoding"": ""gzip, deflate, br"", 
    ""Accept-Language"": ""en-US,en;q=0.9,fr;q=0.8"", 
    ""Host"": ""httpbin.org"", 
    ""Sec-Ch-Ua"": ""\""Not.A/Brand\"";v=\""8\"", \""Chromium\"";v=\""114\"", \""Microsoft Edge\"";v=\""114\"""", 
    ""Sec-Ch-Ua-Mobile"": ""?0"", 
    ""Sec-Ch-Ua-Platform"": ""\""Windows\"""", 
    ""Sec-Fetch-Dest"": ""document"", 
    ""Sec-Fetch-Mode"": ""navigate"", 
    ""Sec-Fetch-Site"": ""none"", 
    ""Sec-Fetch-User"": ""?1"", 
    ""Upgrade-Insecure-Requests"": ""1"", 
    ""User-Agent"": ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.51"", 
    ""X-Amzn-Trace-Id"": ""Root=1-64944685-218f4c485cb4eae87e33ab7e""
  }
}";
    private static readonly Lazy<HttpClient> HttpClientInstance = new(() =>
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
        .Protected()
        // Setup the PROTECTED method to mock
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        // prepare the expected response of the mocked http call
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(ResponsePayload, new MediaTypeHeaderValue("application/json")),
        })
        .Verifiable();
        return new HttpClient(handlerMock.Object);
    });
    [Fact]
    public async Task GetsCached()
    {
        var client = HttpClientInstance.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var provider = new DocumentCachingProvider(client, mockLogger);
        await using var result = await provider.GetDocumentAsync(new Uri("https://localhost/foo.json"), "foo", "bar.json");
        Assert.NotNull(result);
        Assert.Equal(ResponsePayload.Length, result.Length);
    }
    [Fact]
    public async Task GetsWhenCacheIsOutdated()
    {
        var client = HttpClientInstance.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var provider = new DocumentCachingProvider(client, mockLogger);
        await using var result1 = await provider.GetDocumentAsync(new Uri("https://localhost/foo.json"), "foo", "bar.json");
        provider.Duration = TimeSpan.FromMilliseconds(-1);
        await using var result2 = await provider.GetDocumentAsync(new Uri("https://localhost/foo.json"), "foo", "bar.json");
        Assert.NotNull(result2);
        Assert.Equal(ResponsePayload.Length, result2.Length);
    }
    [Fact]
    public async Task GetsWhenCacheIsCleared()
    {
        var client = HttpClientInstance.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var provider = new DocumentCachingProvider(client, mockLogger);
        await using var result1 = await provider.GetDocumentAsync(new Uri("https://localhost/foo.json"), "foo", "bar.json");
        provider.ClearCache = true;
        await using var result2 = await provider.GetDocumentAsync(new Uri("https://localhost/foo.json"), "foo", "bar.json");
        Assert.NotNull(result2);
        Assert.Equal(ResponsePayload.Length, result2.Length);
    }
}
