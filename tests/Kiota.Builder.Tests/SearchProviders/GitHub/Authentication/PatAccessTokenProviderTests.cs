
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.SearchProviders.GitHub.Authentication;

public class PatAccessTokenProviderTests
{
    [Fact]
    public async Task GetsTokenAsync()
    {
        var storageMock = new Mock<ITokenStorageService>();
        storageMock.Setup(x => x.GetTokenAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync("foo");
        var provider = new PatAccessTokenProvider
        {
            StorageService = storageMock.Object
        };
        Assert.Equal("foo", await provider.GetAuthorizationTokenAsync(new("https://api.github.com"), new(), new()));
        Assert.Empty(await provider.GetAuthorizationTokenAsync(new("http://api.github.com"), new(), new()));
        provider.AllowedHostsValidator = new(new[] { "web.github.com" });
        Assert.Empty(await provider.GetAuthorizationTokenAsync(new("https://api.github.com"), new(), new()));
    }
}
