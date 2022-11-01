using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.Lock;
using Xunit;

namespace Kiota.Builder.Tests.Lock;

public class LockManagementServiceTests {
    [Fact]
    public async Task DefensivePrograming() {
        var lockManagementService = new LockManagementService();
        Assert.Throws<ArgumentNullException>(() => lockManagementService.GetDirectoriesContainingLockFile(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromDirectoryAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromStreamAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync(null, new KiotaLock()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync("path", null));
    }
    [Fact]
    public async Task Identity() {
        var lockManagementService = new LockManagementService();
        var lockFile = new KiotaLock {
            DescriptionLocation = "description",
        };
        var path = Path.GetTempPath();
        await lockManagementService.WriteLockFileAsync(path, lockFile);
        var result = await lockManagementService.GetLockFromDirectoryAsync(path);
        Assert.Equal(lockFile, result, new KiotaLockComparer());
    }
}
