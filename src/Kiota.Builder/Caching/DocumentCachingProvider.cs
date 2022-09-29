using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Caching;

public class DocumentCachingProvider {
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(() => SHA256.Create());
    public bool ClearCache { get; init; }
    public HttpClient HttpClient { get; init; }
    public ILogger Logger { get; init; }
    public async Task<Stream> GetDocumentAsync(Uri documentUri, string intermediateFolderName, string fileName, CancellationToken token) {
        ArgumentNullException.ThrowIfNull(documentUri);
        if(string.IsNullOrEmpty(intermediateFolderName)) throw new ArgumentNullException(nameof(intermediateFolderName));
        if(string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
        var hashedUrl = BitConverter.ToString(HashAlgorithm.Value.ComputeHash(Encoding.UTF8.GetBytes(documentUri.ToString()))).Replace("-", string.Empty);
        var target = Path.Combine(Path.GetTempPath(), "kiota", "cache", intermediateFolderName, hashedUrl, fileName);
        if(!File.Exists(target)) {
            Logger?.LogDebug("cache file {cacheFile} not found, downloading from {url}", target, documentUri);
            var directory = Path.GetDirectoryName(target);
            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            Stream content = null;
            try {
                using var httpContent = await HttpClient.GetStreamAsync(documentUri, token);
                content = new MemoryStream();
                await httpContent.CopyToAsync(content, token);
                using var fileStream = File.Create(target);
                content.Position = 0;
                await content.CopyToAsync(fileStream, token);
                await fileStream.FlushAsync(token);
                content.Position = 0;
                return content;
            } catch (HttpRequestException ex) {
                throw new InvalidOperationException($"Could not download the file at {documentUri}, reason: {ex.Message}", ex);
            } catch (IOException ex) {
                Logger?.LogWarning("could not write to cache file {cacheFile}, reason: {reason}", target, ex.Message);
                content.Position = 0;
                return content;
            }
        }

        var lastModificationDate = File.GetLastWriteTime(target);
        if (lastModificationDate.AddHours(1) > DateTime.Now && !ClearCache) {
            Logger?.LogDebug("cache file {cacheFile} is up to date and clearCache is {clearCache}, using it", target, ClearCache);
            return File.OpenRead(target);
        } else {
            Logger?.LogDebug("cache file {cacheFile} is out of date, downloading from {url}", target, documentUri);
            File.Delete(target);
        }
        return await GetDocumentAsync(documentUri, intermediateFolderName, fileName, token);
    }
}
