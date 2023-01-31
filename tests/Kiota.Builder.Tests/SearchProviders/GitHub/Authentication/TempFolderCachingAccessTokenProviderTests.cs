using System;
using System.Collections.Generic;
using System.IO;
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
    public async Task CachesToken()
    {
        var concreteProvider = new Mock<IAccessTokenProvider>();
        concreteProvider.Setup(x => x.GetAuthorizationTokenAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync("foo");
        var cachingProvider = new TempFolderCachingAccessTokenProvider
        {
            Logger = new Mock<ILogger>().Object,
            ApiBaseUrl = new("https://api.github.com"),
            AppId = Path.GetRandomFileName(),
            Concrete = concreteProvider.Object,
        };
        Assert.Equal("foo", await cachingProvider.GetAuthorizationTokenAsync(new("https://api.github.com"), new(), new()));
        await cachingProvider.GetAuthorizationTokenAsync(new("https://api.github.com"), new(), new());
        concreteProvider.Verify(x => x.GetAuthorizationTokenAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
