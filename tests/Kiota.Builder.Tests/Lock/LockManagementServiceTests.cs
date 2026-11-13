using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.Lock;
using Xunit;

namespace Kiota.Builder.Tests.Lock;

public sealed class LockManagementServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    public LockManagementServiceTests()
    {
        Directory.CreateDirectory(_testDirectory);
    }
    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }
    [Fact]
    public async Task DefensiveProgrammingAsync()
    {
        var lockManagementService = new LockManagementService();
        Assert.Throws<ArgumentNullException>(() => lockManagementService.GetDirectoriesContainingLockFile(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromDirectoryAsync(null, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromStreamAsync(null, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync(null, new KiotaLock(), cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync("path", null, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task IdentityAsync()
    {
        var lockManagementService = new LockManagementService();
        var descriptionPath = Path.Combine(_testDirectory, "description.yml");
        var lockFile = new KiotaLock
        {
            ClientClassName = "foo",
            ClientNamespaceName = "bar",
            DescriptionLocation = descriptionPath,
            AllowedExternalOrigins = ["https://contoso.com/*"],
        };
        var path = _testDirectory;
        await lockManagementService.WriteLockFileAsync(path, lockFile, cancellationToken: TestContext.Current.CancellationToken);
        lockFile.DescriptionLocation = Path.GetFullPath(descriptionPath); // expected since we write the relative path but read to the full path
        var result = await lockManagementService.GetLockFromDirectoryAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(lockFile, result, new KiotaLockComparer());
    }
    [Fact]
    public async Task UsesRelativePathsAsync()
    {
        var tmpPath = _testDirectory;
        var lockManagementService = new LockManagementService();
        var descriptionPath = Path.Combine(tmpPath, "information", "description.yml");
        var descriptionDirectory = Path.GetDirectoryName(descriptionPath);
        Directory.CreateDirectory(descriptionDirectory);
        var lockFile = new KiotaLock
        {
            DescriptionLocation = descriptionPath,
        };
        var outputDirectory = Path.Combine(tmpPath, "output");
        Directory.CreateDirectory(outputDirectory);
        await lockManagementService.WriteLockFileAsync(outputDirectory, lockFile, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("../information/description.yml", lockFile.DescriptionLocation, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task DeletesALockAsync()
    {
        var lockManagementService = new LockManagementService();
        var descriptionPath = Path.Combine(_testDirectory, "description.yml");
        var lockFile = new KiotaLock
        {
            ClientClassName = "foo",
            ClientNamespaceName = "bar",
            DescriptionLocation = descriptionPath,
        };
        var path = _testDirectory;
        await lockManagementService.WriteLockFileAsync(path, lockFile, cancellationToken: TestContext.Current.CancellationToken);
        lockManagementService.DeleteLockFile(path);
        Assert.Null(await lockManagementService.GetLockFromDirectoryAsync(path, cancellationToken: TestContext.Current.CancellationToken));
    }
}
