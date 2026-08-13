
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.SearchProviders.GitHub.Authentication;

public class TempFolderTokenStorageServiceTests
{
    [Fact]
    public async Task DeletesAsync()
    {
        var storageService = new TempFolderTokenStorageService
        {
            Logger = new Mock<ILogger>().Object,
            FileName = Path.GetRandomFileName()
        };
        Assert.False(await storageService.IsTokenPresentAsync(new()));
        Assert.False(await storageService.DeleteTokenAsync(new()));
        await storageService.SetTokenAsync("foo", new());
        Assert.True(await storageService.IsTokenPresentAsync(new()));
        Assert.True(await storageService.DeleteTokenAsync(new()));
    }
    [Fact]
    public async Task GetsAsync()
    {
        var storageService = new TempFolderTokenStorageService
        {
            Logger = new Mock<ILogger>().Object,
            FileName = Path.GetRandomFileName()
        };
        Assert.Null(await storageService.GetTokenAsync(new()));
        await storageService.SetTokenAsync("foo", new());
        Assert.Equal("foo", await storageService.GetTokenAsync(new()));
    }
}
