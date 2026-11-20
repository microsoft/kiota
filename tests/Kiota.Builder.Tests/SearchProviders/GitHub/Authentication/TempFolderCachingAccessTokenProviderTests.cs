using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.SearchProviders.GitHub.Authentication;

public class TempFolderCachingAccessTokenProviderTests
{
    [Fact]
    public async Task CachesTokenAsync()
    {
        string cachedToken = null;
        var storage = new Mock<ITokenStorageService>();
        storage.Setup(x => x.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => cachedToken);
        storage.Setup(x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((value, _) => cachedToken = value)
            .Returns(Task.CompletedTask);
        var concreteProvider = new Mock<IAccessTokenProvider>();
        concreteProvider.Setup(x => x.GetAuthorizationTokenAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync("foo");
        var cachingProvider = new TempFolderCachingAccessTokenProvider
        {
            Logger = new Mock<ILogger>().Object,
            ApiBaseUrl = new("https://api.github.com"),
            AppId = "test-app",
            Concrete = concreteProvider.Object,
            TokenStorageService = new(() => storage.Object),
        };
        Assert.Equal("foo", await cachingProvider.GetAuthorizationTokenAsync(new("https://api.github.com"), new(), TestContext.Current.CancellationToken));
        await cachingProvider.GetAuthorizationTokenAsync(new("https://api.github.com"), new(), TestContext.Current.CancellationToken);
        concreteProvider.Verify(x => x.GetAuthorizationTokenAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DoesNotReturnTokenWhenPersistenceFailsAsync()
    {
        var storage = new Mock<ITokenStorageService>();
        storage.Setup(x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.IO.IOException("Storage unavailable."));
        var concrete = new Mock<IAccessTokenProvider>();
        concrete.Setup(x => x.GetAuthorizationTokenAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("dummy-token");
        var provider = new TempFolderCachingAccessTokenProvider
        {
            Logger = new Mock<ILogger>().Object,
            ApiBaseUrl = new("https://api.github.com"),
            AppId = "test-app",
            Concrete = concrete.Object,
            TokenStorageService = new(() => storage.Object),
        };
        await Assert.ThrowsAsync<System.IO.IOException>(() => provider.GetAuthorizationTokenAsync(new("https://api.github.com"), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DoesNotPersistEmptyAuthorizationResultAsync()
    {
        var storage = new Mock<ITokenStorageService>();
        var concrete = new Mock<IAccessTokenProvider>();
        concrete.Setup(x => x.GetAuthorizationTokenAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        var provider = new TempFolderCachingAccessTokenProvider
        {
            Logger = new Mock<ILogger>().Object,
            ApiBaseUrl = new("https://api.github.com"),
            AppId = "test-app",
            Concrete = concrete.Object,
            TokenStorageService = new(() => storage.Object),
        };
        Assert.Empty(await provider.GetAuthorizationTokenAsync(new("https://api.github.com"), cancellationToken: TestContext.Current.CancellationToken));
        storage.Verify(x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
