using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.Lock;

/// <summary>
/// A service that manages the lock file for a Kiota project implemented using the file system.
/// </summary>
public class LockManagementService : ILockManagementService {
    private const string LockFileName = "kiota-lock.json";
    /// <inheritdoc/>
    public IEnumerable<string> GetDirectoriesContainingLockFile(string searchDirectory) {
        ArgumentException.ThrowIfNullOrEmpty(searchDirectory);
        var files = Directory.GetFiles(searchDirectory, LockFileName, SearchOption.AllDirectories);
        return files.Select(Path.GetDirectoryName).Where(x => !string.IsNullOrEmpty(x)).OfType<string>();
    }
    /// <inheritdoc/>
    public Task<KiotaLock?> GetLockFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return GetLockFromDirectoryInternalAsync(directoryPath, cancellationToken);
    }
    private static async Task<KiotaLock?> GetLockFromDirectoryInternalAsync(string directoryPath, CancellationToken cancellationToken) {
        var lockFile = Path.Combine(directoryPath, LockFileName);
        if(File.Exists(lockFile)) {
            await using var fileStream = File.OpenRead(lockFile);
            return await GetLockFromStreamInternalAsync(fileStream, cancellationToken);
        }
        return null;
    }
    /// <inheritdoc/>
    public Task<KiotaLock?> GetLockFromStreamAsync(Stream stream, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(stream);
        return GetLockFromStreamInternalAsync(stream, cancellationToken);
    }
    private static async Task<KiotaLock?> GetLockFromStreamInternalAsync(Stream stream, CancellationToken cancellationToken) {
        return await JsonSerializer.DeserializeAsync<KiotaLock>(stream, options, cancellationToken);
    }
    /// <inheritdoc/>
    public Task WriteLockFileAsync(string directoryPath, KiotaLock lockInfo, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(lockInfo);
        return WriteLockFileInternalAsync(directoryPath, lockInfo, cancellationToken);
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static async Task WriteLockFileInternalAsync(string directoryPath, KiotaLock lockInfo, CancellationToken cancellationToken) {
        var lockFilePath = Path.Combine(directoryPath, LockFileName);
        await using var fileStream = File.Open(lockFilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(fileStream, lockInfo, options, cancellationToken);
    }
}
