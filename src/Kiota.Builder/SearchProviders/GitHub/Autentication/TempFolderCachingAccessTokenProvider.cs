using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class TempFolderCachingAccessTokenProvider : IAccessTokenProvider
{
    public required IAccessTokenProvider Concrete
    {
        get; init;
    }
    public required ILogger Logger
    {
        get; init;
    }
    public required Uri ApiBaseUrl { get; init; }
    public required string AppId { get; init; }
    public AllowedHostsValidator AllowedHostsValidator => Concrete.AllowedHostsValidator;
    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var result = await GetTokenFromCacheAsync(uri, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result))
        {
            Logger.LogInformation("Token not found in cache, requesting a new one");
            result = await Concrete.GetAuthorizationTokenAsync(uri, additionalAuthenticationContext, cancellationToken).ConfigureAwait(false);
            await CacheTokenAsync(uri, result, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }
    private string GetTokenCacheFilePath() => GetTokenCacheFilePath(ApiBaseUrl);
    private string GetTokenCacheFilePath(Uri uri) => Path.Combine(Path.GetTempPath(), "kiota", "auth", $"{AppId}-{uri.Host}.txt");
    private async Task CacheTokenAsync(Uri uri, string result, CancellationToken cancellationToken)
    {
        try
        {
            var target = GetTokenCacheFilePath(uri);
            var directory = Path.GetDirectoryName(target);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(target, result, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error while writing token to cache.");
        }
    }
    private async Task<string> GetTokenFromCacheAsync(Uri uri, CancellationToken cancellationToken)
    {
        try {
            var target = GetTokenCacheFilePath(uri);
            if(!IsCachedTokenPresent())
                return null;
            var result = await File.ReadAllTextAsync(target, cancellationToken).ConfigureAwait(false);
            return result;
        } catch (Exception ex) {
            Logger.LogWarning(ex, "Error while reading token from cache.");
            return null;
        }
    }
    public bool Logout() {
        //no try-catch as we want the exception to bubble up to the command
        var target = GetTokenCacheFilePath();
        if(!IsCachedTokenPresent())
            return false;
        File.Delete(target);
        return true;
    }
    public bool IsCachedTokenPresent() {
        try {
            var target = GetTokenCacheFilePath();
            if (!File.Exists(target))
                return false;
            var fileDate = File.GetLastWriteTime(target);
            if(fileDate > DateTime.Now.AddMonths(6)) {
                Logout();
                return false;
            }
            return true;
        } catch (Exception ex) {
            Logger.LogWarning(ex, "Error while reading token from cache.");
            return false;
        }
    }
}
