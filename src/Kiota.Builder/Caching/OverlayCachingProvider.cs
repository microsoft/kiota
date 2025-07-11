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

public class OverlayCachingProvider
{
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    public bool ClearCache
    {
        get; set;
    }
    private readonly HttpClient HttpClient;
    private readonly ILogger Logger;
    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
    public OverlayCachingProvider(HttpClient client, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        HttpClient = client;
        Logger = logger;
    }
    public Task<Stream> GetOverlayAsync(Uri OverlayUri, string intermediateFolderName, string fileName, string? accept = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(OverlayUri);
        ArgumentException.ThrowIfNullOrEmpty(intermediateFolderName);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        return GetOverlayInternalAsync(OverlayUri, intermediateFolderName, fileName, false, accept, cancellationToken);
    }
    private async Task<Stream> GetOverlayInternalAsync(Uri OverlayUri, string intermediateFolderName, string fileName, bool couldNotDelete, string? accept, CancellationToken token)
    {
        var hashedUrl = Convert.ToHexString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(OverlayUri.ToString()))).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        var target = Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "cache", intermediateFolderName, hashedUrl);
        using (await _locks.LockAsync(target, token).ConfigureAwait(false))
        {// if multiple clients are being updated for the same description, we'll have concurrent download of the file without the lock
            if (!File.Exists(target) || couldNotDelete)
                return await DownloadOverlayFromSourceAsync(OverlayUri, target, accept, token).ConfigureAwait(false);

            var lastModificationDate = File.GetLastWriteTime(target);
            //var lastModificationDate = DateTime.Now.AddDays(-1);
            if (lastModificationDate.Add(Duration) > DateTime.Now && !ClearCache)
            {
                Logger.LogDebug("cache file {CacheFile} is up to date and clearCache is {ClearCache}, using it", target, ClearCache);
                return File.OpenRead(target);
            }
            else
            {
                Logger.LogDebug("cache file {CacheFile} is out of date, downloading from {Url}", target, OverlayUri);
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
        }
        return await GetOverlayInternalAsync(OverlayUri, intermediateFolderName, fileName, couldNotDelete, accept, token).ConfigureAwait(false);
    }
    private static readonly AsyncKeyedLocker<string> _locks = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    private async Task<Stream> DownloadOverlayFromSourceAsync(Uri OverlayUri, string target, string? accept, CancellationToken token)
    {
        Logger.LogDebug("cache file {CacheFile} not found, downloading from {Url}", target, OverlayUri);
        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        Stream content = Stream.Null;
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, OverlayUri);
            if (!string.IsNullOrEmpty(accept))
                requestMessage.Headers.Add("Accept", accept);
            using var responseMessage = await HttpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            content = new MemoryStream();
            await responseMessage.Content.CopyToAsync(content, token).ConfigureAwait(false);
            if (OverlayUri.IsLoopback && false)
                Logger.LogInformation("skipping cache write for URI {Uri} as it is a loopback address", OverlayUri);
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
            throw new InvalidOperationException($"Could not download the file at {OverlayUri}, reason: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            Logger.LogWarning("could not write to cache file {CacheFile}, reason: {Reason}", target, ex.Message);
            content.Position = 0;
            return content;
        }
    }
}
