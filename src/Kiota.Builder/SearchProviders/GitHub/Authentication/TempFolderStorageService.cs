using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class TempFolderTokenStorageService : ITokenStorageService
{
    private readonly string? applicationDataPath;

    public TempFolderTokenStorageService()
    {
    }

    internal TempFolderTokenStorageService(string applicationDataPath)
    {
        this.applicationDataPath = applicationDataPath;
    }

    public required ILogger Logger
    {
        get; init;
    }
    public required string FileName
    {
        get; init;
    }
    private string? GetTokenCacheFilePath(bool createDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(FileName) || FileName is "." or ".." ||
            FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            FileName.Contains('/', StringComparison.Ordinal) || FileName.Contains('\\', StringComparison.Ordinal) ||
            FileName.EndsWith('.') || FileName.EndsWith(' '))
            throw new ArgumentException("The credential cache key (FileName) must be a safe filename.");

        var root = applicationDataPath ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(root) || !Path.IsPathFullyQualified(root))
            throw new IOException("A private per-user application-data directory is required for credential storage.");

        if (applicationDataPath is null)
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!EnsureApplicationDataDirectory(root, profile, createDirectory))
                return null;
        }
        else if (!TokenCacheSecurity.EnsureDirectory(root, true, createDirectory))
            return null;

        var cacheDirectory = Path.Combine(root, Constants.TempDirectoryName);
        if (!TokenCacheSecurity.EnsureDirectory(cacheDirectory, true, createDirectory))
            return null;
        cacheDirectory = Path.Combine(cacheDirectory, "auth");
        if (!TokenCacheSecurity.EnsureDirectory(cacheDirectory, true, createDirectory))
            return null;
        return Path.Combine(cacheDirectory, $"{FileName}.txt");
    }

    internal static bool EnsureApplicationDataDirectory(string root, string profile, bool createDirectory)
    {
        if (string.IsNullOrEmpty(profile) || !Path.IsPathFullyQualified(profile))
            throw new IOException("A user profile is required for credential storage.");
        var relative = Path.GetRelativePath(profile, root);
        var withinProfile = !Path.IsPathRooted(relative) && relative != ".." &&
            !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
        var current = withinProfile ? profile : Path.GetPathRoot(root)!;
        if (!TokenCacheSecurity.EnsureDirectory(current, false, false))
            return false;
        relative = Path.GetRelativePath(current, root);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;
            current = Path.Combine(current, segment);
            if (!TokenCacheSecurity.EnsureDirectory(current, false, createDirectory))
                return false;
        }
        return true;
    }

    private static bool ValidateExistingFile(string path)
    {
        try
        {
            TokenCacheSecurity.ValidateFile(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target = GetTokenCacheFilePath();
        if (target is null || !ValidateExistingFile(target))
            return null;
        if (File.GetLastWriteTimeUtc(target).AddMonths(6) < DateTime.UtcNow)
        {
            File.Delete(target);
            return null;
        }
        using var stream = TokenCacheSecurity.OpenRead(target);
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public async Task SetTokenAsync(string value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        cancellationToken.ThrowIfCancellationRequested();
        var target = GetTokenCacheFilePath(true)!;
        ValidateExistingFile(target);
        var temporaryPath = Path.Combine(Path.GetDirectoryName(target)!, Path.GetRandomFileName());
        var created = false;
        try
        {
            var stream = TokenCacheSecurity.CreateFile(temporaryPath);
            created = true;
            await using (stream.ConfigureAwait(false))
            {
                var writer = new StreamWriter(stream);
                await using (writer.ConfigureAwait(false))
                {
                    await writer.WriteAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            TokenCacheSecurity.ValidateFile(temporaryPath);
            cancellationToken.ThrowIfCancellationRequested();
            ValidateExistingFile(target);
            File.Move(temporaryPath, target, true);
        }
        finally
        {
            if (created)
                File.Delete(temporaryPath);
        }
    }

    public async Task<bool> IsTokenPresentAsync(CancellationToken cancellationToken) =>
        !string.IsNullOrEmpty(await GetTokenAsync(cancellationToken).ConfigureAwait(false));

    public Task<bool> DeleteTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target = GetTokenCacheFilePath();
        if (target is null || !ValidateExistingFile(target))
            return Task.FromResult(false);
        File.Delete(target);
        return Task.FromResult(true);
    }
}
