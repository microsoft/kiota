using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.SearchProviders.APIsGuru;

public class CachedGuruSearchProvider : APIsGuruSearchProvider
{
    private static HashAlgorithm HashAlgorithm { get; } = SHA256.Create();
    public bool ClearCache { get; set; }

    protected override async Task<string> GetAPIsList(CancellationToken token) {
        ArgumentNullException.ThrowIfNull(SearchUri);
        var hashedUrl = BitConverter.ToString(HashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(SearchUri.ToString()))).Replace("-", string.Empty);
        var target = Path.Combine(Path.GetTempPath(), "kiota", "search", "cache", hashedUrl, "apisgurulist.json");
        if(!File.Exists(target)) {
            Logger?.LogDebug("cache file {cacheFile} not found, downloading from {url}", target, SearchUri);
            var directory = Path.GetDirectoryName(target);
            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var content = await base.GetAPIsList(token);
            await File.WriteAllTextAsync(target, content, token);
            return content;
        }

        var lastModificationDate = File.GetLastWriteTime(target);
        if (lastModificationDate.AddHours(1) > DateTime.Now && !ClearCache) {
            Logger?.LogDebug("cache file {cacheFile} is up to date and {clearCache} is false, using it", target, ClearCache);
            return await File.ReadAllTextAsync(target, token);
        }
        Logger?.LogDebug("cache file {cacheFile} is out of date, downloading from {url}", target, SearchUri);
        File.Delete(target);
        return await GetAPIsList(token);
    }
}
