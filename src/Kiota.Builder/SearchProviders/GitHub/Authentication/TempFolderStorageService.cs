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
    private string GetTokenCacheFilePath()
    {
        if (string.IsNullOrWhiteSpace(FileName) || FileName is "." or ".." ||
            FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            FileName.Contains('/', StringComparison.Ordinal) || FileName.Contains('\\', StringComparison.Ordinal) ||
            FileName.EndsWith('.') || FileName.EndsWith(' '))
            throw new ArgumentException("The credential cache key must be a safe filename.", nameof(FileName));

        var root = applicationDataPath ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(root) || !Path.IsPathFullyQualified(root))
            throw new IOException("A private per-user application-data directory is required for credential storage.");

        if (applicationDataPath is null)
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var relative = Path.GetRelativePath(profile, root);
            if (string.IsNullOrEmpty(profile) || !Path.IsPathFullyQualified(profile) ||
                Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                throw new IOException("Credential storage must be within the current user's profile.");
            TokenCacheSecurity.EnsureDirectory(profile, false);
            var current = profile;
            foreach (var segment in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                TokenCacheSecurity.EnsureDirectory(current, false);
            }
        }
        else
            TokenCacheSecurity.EnsureDirectory(root, true);

        var cacheDirectory = Path.Combine(root, Constants.TempDirectoryName);
        TokenCacheSecurity.EnsureDirectory(cacheDirectory, true);
        cacheDirectory = Path.Combine(cacheDirectory, "auth");
        TokenCacheSecurity.EnsureDirectory(cacheDirectory, true);
        return Path.Combine(cacheDirectory, $"{FileName}.txt");
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
        if (!ValidateExistingFile(target))
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
        var target = GetTokenCacheFilePath();
        ValidateExistingFile(target);
        var temporaryPath = Path.Combine(Path.GetDirectoryName(target)!, Path.GetRandomFileName());
        var created = false;
        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough,
            };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = TokenCacheSecurity.FileMode;
            var stream = new FileStream(temporaryPath, options);
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
        if (!ValidateExistingFile(target))
            return Task.FromResult(false);
        File.Delete(target);
        return Task.FromResult(true);
    }
}
