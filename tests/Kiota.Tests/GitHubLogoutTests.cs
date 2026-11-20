using System.IO;
using System.Threading;
using System.Threading.Tasks;
using kiota.Handlers;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Moq;
using Xunit;

namespace Kiota.Tests;

public class GitHubLogoutTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClearsOtherCredentialWhenOneStoreFailsAsync(bool deviceFails)
    {
        var device = new Mock<ITokenStorageService>();
        var pat = new Mock<ITokenStorageService>();
        var failing = deviceFails ? device : pat;
        var working = deviceFails ? pat : device;
        failing.Setup(x => x.DeleteTokenAsync(It.IsAny<CancellationToken>())).Throws(new IOException("Unsafe cache."));
        working.Setup(x => x.DeleteTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<IOException>(() => KiotaGitHubLogoutCommandHandler.DeleteTokensAsync(device.Object, pat.Object, TestContext.Current.CancellationToken));
        device.Verify(x => x.DeleteTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        pat.Verify(x => x.DeleteTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public async Task ReportsWhetherAnyCredentialWasClearedAsync(bool deviceDeleted, bool patDeleted, bool expected)
    {
        var device = new Mock<ITokenStorageService>();
        var pat = new Mock<ITokenStorageService>();
        device.Setup(x => x.DeleteTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(deviceDeleted);
        pat.Setup(x => x.DeleteTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patDeleted);
        Assert.Equal(expected, await KiotaGitHubLogoutCommandHandler.DeleteTokensAsync(device.Object, pat.Object, TestContext.Current.CancellationToken));
    }
}
