using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Caching;

public partial class DocumentCachingProvider
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
        var hashedUrl = Convert.ToHexString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(documentUri.ToString()))).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        var target = Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "cache", intermediateFolderName, hashedUrl, fileName);
        using (await _locks.LockAsync(target, token).ConfigureAwait(false))
        {// if multiple clients are being updated for the same description, we'll have concurrent download of the file without the lock
            if (!File.Exists(target) || couldNotDelete)
                return await DownloadDocumentFromSourceAsync(documentUri, target, accept, token).ConfigureAwait(false);

            var lastModificationDate = File.GetLastWriteTime(target);
            if (lastModificationDate.Add(Duration) > DateTime.Now && !ClearCache)
            {
                LogCacheFileUpToDate(target, ClearCache);
                return File.OpenRead(target);
            }
            else
            {
                LogCacheFileOutOfDate(target, documentUri);
                try
                {
                    File.Delete(target);
                }
                catch (IOException ex)
                {
                    couldNotDelete = true;
                    LogCouldNotDeleteCache(target, ex.Message);
                }
            }
        }
        return await GetDocumentInternalAsync(documentUri, intermediateFolderName, fileName, couldNotDelete, accept, token).ConfigureAwait(false);
    }
    private static readonly AsyncKeyedLocker<string> _locks = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    private async Task<Stream> DownloadDocumentFromSourceAsync(Uri documentUri, string target, string? accept, CancellationToken token)
    {
        LogCacheFileNotFound(target, documentUri);
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
            if (documentUri.IsLoopback)
            {
                LogSkippingCacheWrite(documentUri);
            }
            else
            {
#pragma warning disable CA2007
                await using var fileStream = File.Create(target);
#pragma warning restore CA2007
                content.Position = 0;
                await content.CopyToAsync(fileStream, token).ConfigureAwait(false);
                await fileStream.FlushAsync(token).ConfigureAwait(false);
            }
            content.Position = 0;
            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not download the file at {documentUri}, reason: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            LogCouldNotWriteCache(target, ex.Message);
            content.Position = 0;
            return content;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "cache file {CacheFile} is up to date and clearCache is {ClearCache}, using it")]
    private partial void LogCacheFileUpToDate(string cacheFile, bool clearCache);

    [LoggerMessage(Level = LogLevel.Debug, Message = "cache file {CacheFile} is out of date, downloading from {Url}")]
    private partial void LogCacheFileOutOfDate(string cacheFile, Uri url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "could not delete cache file {CacheFile}, reason: {Reason}")]
    private partial void LogCouldNotDeleteCache(string cacheFile, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "cache file {CacheFile} not found, downloading from {Url}")]
    private partial void LogCacheFileNotFound(string cacheFile, Uri url);

    [LoggerMessage(Level = LogLevel.Information, Message = "skipping cache write for URI {Uri} as it is a loopback address")]
    private partial void LogSkippingCacheWrite(Uri uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "could not write to cache file {CacheFile}, reason: {Reason}")]
    private partial void LogCouldNotWriteCache(string cacheFile, string reason);
}
