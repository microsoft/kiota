using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.Lock;

/// <summary>
/// A service that manages the lock file for a Kiota project implemented using the file system.
/// </summary>
public class LockManagementService : ILockManagementService
{
    private const string LockFileName = "kiota-lock.json";
    /// <inheritdoc/>
    public IEnumerable<string> GetDirectoriesContainingLockFile(string searchDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(searchDirectory);
        var files = Directory.GetFiles(searchDirectory, LockFileName, SearchOption.AllDirectories);
        return files.Select(Path.GetDirectoryName).Where(x => !string.IsNullOrEmpty(x)).OfType<string>();
    }
    /// <inheritdoc/>
    public Task<KiotaLock?> GetLockFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return GetLockFromDirectoryInternalAsync(directoryPath, cancellationToken);
    }
    private static async Task<KiotaLock?> GetLockFromDirectoryInternalAsync(string directoryPath, CancellationToken cancellationToken)
    {
        var lockFile = Path.Combine(directoryPath, LockFileName);
        if (File.Exists(lockFile))
        {
            await using var fileStream = File.OpenRead(lockFile);
            return await GetLockFromStreamInternalAsync(fileStream, cancellationToken);
        }
        return null;
    }
    /// <inheritdoc/>
    public Task<KiotaLock?> GetLockFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return GetLockFromStreamInternalAsync(stream, cancellationToken);
    }
    private static async Task<KiotaLock?> GetLockFromStreamInternalAsync(Stream stream, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(stream, context.KiotaLock, cancellationToken);
    }
    /// <inheritdoc/>
    public Task WriteLockFileAsync(string directoryPath, KiotaLock lockInfo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(lockInfo);
        return WriteLockFileInternalAsync(directoryPath, lockInfo, cancellationToken);
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly KiotaLockGenerationContext context = new(options);
    private static async Task WriteLockFileInternalAsync(string directoryPath, KiotaLock lockInfo, CancellationToken cancellationToken)
    {
        var lockFilePath = Path.Combine(directoryPath, LockFileName);
        await using var fileStream = File.Open(lockFilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(fileStream, lockInfo, context.KiotaLock, cancellationToken);
    }
    /// <inheritdoc/>
    public static Task BackupLockFileAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return BackupLockFileInternalAsync(directoryPath);
    }
    private static Task BackupLockFileInternalAsync(string directoryPath)
    {
        var lockFilePath = Path.Combine(directoryPath, LockFileName);
        if (File.Exists(lockFilePath))
        {
            var backupFilePath = GetBackupFilePath(directoryPath);
            var targetDirectory = Path.GetDirectoryName(backupFilePath);
            if (string.IsNullOrEmpty(targetDirectory)) return Task.CompletedTask;
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            File.Copy(lockFilePath, backupFilePath, true);
        }
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public Task RestoreLockFileAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return RestoreLockFileInternalAsync(directoryPath);
    }
    private static Task RestoreLockFileInternalAsync(string directoryPath)
    {
        var lockFilePath = Path.Combine(directoryPath, LockFileName);
        var targetDirectory = Path.GetDirectoryName(lockFilePath);
        if (string.IsNullOrEmpty(targetDirectory)) return Task.CompletedTask;
        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);
        var backupFilePath = GetBackupFilePath(directoryPath);
        if (File.Exists(backupFilePath))
        {
            File.Copy(backupFilePath, lockFilePath, true);
        }
        return Task.CompletedTask;
    }
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private static string GetBackupFilePath(string outputPath)
    {
        var hashedPath = BitConverter.ToString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(outputPath))).Replace("-", string.Empty);
        return Path.Combine(Path.GetTempPath(), "kiota", "backup", hashedPath, LockFileName);
    }
}
