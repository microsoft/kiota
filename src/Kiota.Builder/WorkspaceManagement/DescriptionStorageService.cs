using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;

namespace Kiota.Builder.WorkspaceManagement;

public class DescriptionStorageService
{
    private const string DescriptionsSubDirectoryRelativePath = ".kiota/clients";
    private readonly string TargetDirectory;
    public DescriptionStorageService(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetDirectory);
        TargetDirectory = targetDirectory;
    }
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    public async Task UpdateDescriptionAsync(string clientName, Stream description, string extension = "yml", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(extension);
        var descriptionFilePath = Path.Combine(TargetDirectory, DescriptionsSubDirectoryRelativePath, $"{clientName}.{extension}");
        using (await localFilesLock.LockAsync(descriptionFilePath, cancellationToken).ConfigureAwait(false))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(descriptionFilePath) ?? throw new InvalidOperationException("The target path is invalid"));
            using var fs = new FileStream(descriptionFilePath, FileMode.Create);
            description.Seek(0, SeekOrigin.Begin);
            await description.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
    }
    public async Task<Stream?> GetDescriptionAsync(string clientName, string extension = "yml", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(extension);
        var descriptionFilePath = Path.Combine(TargetDirectory, DescriptionsSubDirectoryRelativePath, $"{clientName}.{extension}");
        if (!File.Exists(descriptionFilePath))
            return null;
        using (await localFilesLock.LockAsync(descriptionFilePath, cancellationToken).ConfigureAwait(false))
        {
            using var fs = new FileStream(descriptionFilePath, FileMode.Open);
            var ms = new MemoryStream();
            await fs.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
