using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Caching;

public class DocumentCachingProvider
{
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(() => SHA256.Create());
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
        var hashedUrl = BitConverter.ToString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(documentUri.ToString()))).Replace("-", string.Empty);
        var target = Path.Combine(Path.GetTempPath(), "kiota", "cache", intermediateFolderName, hashedUrl, fileName);
        if (!File.Exists(target) || couldNotDelete)
            return await DownloadDocumentFromSourceAsync(documentUri, target, accept, token);

        var lastModificationDate = File.GetLastWriteTime(target);
        if (lastModificationDate.Add(Duration) > DateTime.Now && !ClearCache)
        {
            Logger.LogDebug("cache file {cacheFile} is up to date and clearCache is {clearCache}, using it", target, ClearCache);
            return File.OpenRead(target);
        }
        else
        {
            Logger.LogDebug("cache file {cacheFile} is out of date, downloading from {url}", target, documentUri);
            try
            {
                File.Delete(target);
            }
            catch (IOException ex)
            {
                couldNotDelete = true;
                Logger.LogWarning("could not delete cache file {cacheFile}, reason: {reason}", target, ex.Message);
            }
        }
        return await GetDocumentInternalAsync(documentUri, intermediateFolderName, fileName, couldNotDelete, accept, token);
    }
    private async Task<Stream> DownloadDocumentFromSourceAsync(Uri documentUri, string target, string? accept, CancellationToken token)
    {
        Logger.LogDebug("cache file {cacheFile} not found, downloading from {url}", target, documentUri);
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
            await responseMessage.Content.CopyToAsync(content, token);
            await using var fileStream = File.Create(target);
            content.Position = 0;
            await content.CopyToAsync(fileStream, token);
            await fileStream.FlushAsync(token);
            content.Position = 0;
            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not download the file at {documentUri}, reason: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            Logger.LogWarning("could not write to cache file {cacheFile}, reason: {reason}", target, ex.Message);
            content.Position = 0;
            return content;
        }
    }
}
