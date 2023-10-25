using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.Lock;
using Xunit;

namespace Kiota.Builder.Tests.Lock;

public class LockManagementServiceTests
{
    [Fact]
    public async Task DefensiveProgramming()
    {
        var lockManagementService = new LockManagementService();
        Assert.Throws<ArgumentNullException>(() => lockManagementService.GetDirectoriesContainingLockFile(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromDirectoryAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromStreamAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync(null, new KiotaLock()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync("path", null));
    }
    [Fact]
    public async Task Identity()
    {
        var lockManagementService = new LockManagementService();
        var lockFile = new KiotaLock
        {
            DescriptionLocation = "D:/description.yml",
        };
        var path = Path.GetTempPath();
        await lockManagementService.WriteLockFileAsync(path, lockFile);
        var result = await lockManagementService.GetLockFromDirectoryAsync(path);
        Assert.Equal(lockFile, result, new KiotaLockComparer());
    }
    [Fact]
    public async Task UsesRelativePaths()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), "tests", "kiota");
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
        await lockManagementService.WriteLockFileAsync(outputDirectory, lockFile);
        Assert.Equal("../information/description.yml", lockFile.DescriptionLocation, StringComparer.OrdinalIgnoreCase);
    }
}
