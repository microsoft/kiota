using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;

namespace Kiota.Builder.WorkspaceManagement;

public class DescriptionStorageService
{
    public const string KiotaDirectorySegment = ".kiota";
    internal const string DescriptionsSubDirectoryRelativePath = $"{KiotaDirectorySegment}/documents";
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
    private string GetDescriptionFilePath(string clientName, string extension)
    {
        ValidateConsumerName(clientName);
        ValidateExtension(extension);
        var documentsDirectory = Path.Join(TargetDirectory, DescriptionsSubDirectoryRelativePath);
        var descriptionFilePath = Path.GetFullPath(Path.Combine(documentsDirectory, clientName, $"openapi.{extension}"));
        var documentsFullPath = Path.GetFullPath(documentsDirectory);
        var documentsFullPathWithSeparator = Path.EndsInDirectorySeparator(documentsFullPath) ? documentsFullPath : documentsFullPath + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!descriptionFilePath.StartsWith(documentsFullPathWithSeparator, comparison))
            throw new InvalidOperationException($"The consumer name '{clientName}' resolves to a path outside of the documents directory.");
        return descriptionFilePath;
    }
    internal static void ValidateConsumerName(string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new InvalidOperationException("The consumer name must not be empty or whitespace.");
        if (Path.IsPathRooted(clientName) ||
            clientName.Contains('/', StringComparison.Ordinal) ||
            clientName.Contains('\\', StringComparison.Ordinal) ||
            clientName.Split('/', '\\').Contains("..", StringComparer.Ordinal) ||
            clientName is "." or "..")
            throw new InvalidOperationException($"The consumer name '{clientName}' is not a valid single path segment and cannot navigate the file system.");
    }
    private static void ValidateExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("The description file extension must not be empty or whitespace.");
        if (Path.IsPathRooted(extension) ||
            extension.Contains('/', StringComparison.Ordinal) ||
            extension.Contains('\\', StringComparison.Ordinal))
            throw new InvalidOperationException($"The description file extension '{extension}' must not contain path separators or be rooted.");
    }
    public async Task UpdateDescriptionAsync(string clientName, Stream description, string extension = "yml", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(extension);
        var descriptionFilePath = GetDescriptionFilePath(clientName, extension);
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
        var descriptionFilePath = GetDescriptionFilePath(clientName, extension);
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
    public void RemoveDescription(string clientName, string extension = "yml")
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(extension);
        var descriptionFilePath = GetDescriptionFilePath(clientName, extension);
        if (File.Exists(descriptionFilePath))
            File.Delete(descriptionFilePath);
    }
    public void Clean()
    {
        var kiotaDirectoryPath = Path.Join(TargetDirectory, DescriptionsSubDirectoryRelativePath);
        if (Path.Exists(kiotaDirectoryPath))
            Directory.Delete(kiotaDirectoryPath, true);
    }
}
