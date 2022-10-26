using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.Lock;

public class LockManagementService {
    private const string LockFileName = "kiota.lock";
    public IEnumerable<string> GetDirectoriesContainingLockFile(string searchDirectory) {
        if(string.IsNullOrEmpty(searchDirectory))
            throw new ArgumentNullException(nameof(searchDirectory));
        var files = Directory.GetFiles(searchDirectory, LockFileName, SearchOption.AllDirectories);
        return files.Select(x => Path.GetDirectoryName(x));
    }
    public Task<KiotaLock> GetLockFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default) {
        if(string.IsNullOrEmpty(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        return GetLockFromDirectoryInternalAsync(directoryPath, cancellationToken);
    }
    private async Task<KiotaLock> GetLockFromDirectoryInternalAsync(string directoryPath, CancellationToken cancellationToken) {
        var lockFile = Path.Combine(directoryPath, LockFileName);
        if(File.Exists(lockFile)) {
            await using var fileStream = File.OpenRead(lockFile);
            var result = await JsonSerializer.DeserializeAsync<KiotaLock>(fileStream, cancellationToken: cancellationToken);
            return result;
        }
        return null;
    }
    public Task WriteLockFileAsync(string directoryPath, KiotaLock lockInfo, CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        ArgumentNullException.ThrowIfNull(lockInfo);
        return WriteLockFileInternalAsync(directoryPath, lockInfo, cancellationToken);
    }
    private static async Task WriteLockFileInternalAsync(string directoryPath, KiotaLock lockInfo, CancellationToken cancellationToken) {
        var lockFilePath = Path.Combine(directoryPath, LockFileName);
        await using var fileStream = File.Open(lockFilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(fileStream, lockInfo, cancellationToken: cancellationToken);
    }
}
