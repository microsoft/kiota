using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;

namespace Kiota.Builder.Caching;

public class DocumentCachingProvider
{
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    public bool ClearCache
    {
        get; set;
    }
    private readonly HttpClient HttpClient;
    private readonly ILogger Logger;
    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
    public DocumentCachingProvider(HttpClient client, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        HttpClient = client;
        Logger = logger;
    }
    public Task<Stream> GetDocumentAsync(Uri documentUri, string intermediateFolderName, string fileName, string? accept = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentUri);
        ArgumentException.ThrowIfNullOrEmpty(intermediateFolderName);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        return GetDocumentInternalAsync(documentUri, intermediateFolderName, fileName, false, accept, cancellationToken);
    }
    private async Task<Stream> GetDocumentInternalAsync(Uri documentUri, string intermediateFolderName, string fileName, bool couldNotDelete, string? accept, CancellationToken token)
    {
        var hashedUrl = BitConverter.ToString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(documentUri.ToString()))).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        var target = Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "cache", intermediateFolderName, hashedUrl, fileName);
        var currentLock = _locks.GetOrAdd(target, _ => new AsyncLock());
        using (await currentLock.LockAsync(token).ConfigureAwait(false))
        {// if multiple clients are being updated for the same description, we'll have concurrent download of the file without the lock
            if (!File.Exists(target) || couldNotDelete)
                return await DownloadDocumentFromSourceAsync(documentUri, target, accept, token).ConfigureAwait(false);

            var lastModificationDate = File.GetLastWriteTime(target);
            if (lastModificationDate.Add(Duration) > DateTime.Now && !ClearCache)
            {
                Logger.LogDebug("cache file {CacheFile} is up to date and clearCache is {ClearCache}, using it", target, ClearCache);
                return File.OpenRead(target);
            }
            else
            {
                Logger.LogDebug("cache file {CacheFile} is out of date, downloading from {Url}", target, documentUri);
                try
                {
                    File.Delete(target);
                }
                catch (IOException ex)
                {
                    couldNotDelete = true;
                    Logger.LogWarning("could not delete cache file {CacheFile}, reason: {Reason}", target, ex.Message);
                }
            }
            return await GetDocumentInternalAsync(documentUri, intermediateFolderName, fileName, couldNotDelete, accept, token).ConfigureAwait(false);
        }
    }
    private static readonly ConcurrentDictionary<string, AsyncLock> _locks = new(StringComparer.OrdinalIgnoreCase);
    private async Task<Stream> DownloadDocumentFromSourceAsync(Uri documentUri, string target, string? accept, CancellationToken token)
    {
        Logger.LogDebug("cache file {CacheFile} not found, downloading from {Url}", target, documentUri);
        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        Stream content = Stream.Null;
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, documentUri);
            if (!string.IsNullOrEmpty(accept))
                requestMessage.Headers.Add("Accept", accept);
            using var responseMessage = await HttpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            content = new MemoryStream();
            await responseMessage.Content.CopyToAsync(content, token).ConfigureAwait(false);
#pragma warning disable CA2007
            await using var fileStream = File.Create(target);
#pragma warning restore CA2007
            content.Position = 0;
            await content.CopyToAsync(fileStream, token).ConfigureAwait(false);
            await fileStream.FlushAsync(token).ConfigureAwait(false);
            content.Position = 0;
            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not download the file at {documentUri}, reason: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            Logger.LogWarning("could not write to cache file {CacheFile}, reason: {Reason}", target, ex.Message);
            content.Position = 0;
            return content;
        }
    }
}
