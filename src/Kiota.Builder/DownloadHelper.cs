using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Kiota.Builder.Caching;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.SearchProviders.APIsGuru;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder;
internal static class DownloadHelper
{
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    internal static async Task<(Stream, bool)> LoadStream(string inputPath, HttpClient httpClient, ILogger logger, GenerationConfiguration config, WorkspaceManagementService? workspaceManagementService = default, bool useKiotaConfig = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        inputPath = inputPath.Trim();

        Stream input;
        var isDescriptionFromWorkspaceCopy = false;
        if (useKiotaConfig &&
            config.Operation is ClientOperation.Edit or ClientOperation.Add &&
            workspaceManagementService is not null &&
            await workspaceManagementService.GetDescriptionCopyAsync(config.ClientClassName, inputPath, cancellationToken).ConfigureAwait(false) is { } descriptionStream)
        {
            logger.LogInformation("loaded description from the workspace copy");
            input = descriptionStream;
            isDescriptionFromWorkspaceCopy = true;
        }
        else if (inputPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            try
            {
                var cachingProvider = new DocumentCachingProvider(httpClient, logger)
                {
                    ClearCache = config.ClearCache,
                };
                var targetUri = APIsGuruSearchProvider.ChangeSourceUrlToGitHub(new Uri(inputPath)); // so updating existing clients doesn't break
                var fileName = targetUri.GetFileName() is string name && !string.IsNullOrEmpty(name) ? name : "description.yml";
                input = await cachingProvider.GetDocumentAsync(targetUri, "generation", fileName, cancellationToken: cancellationToken).ConfigureAwait(false);
                logger.LogInformation("loaded description from remote source");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Could not download the file at {inputPath}, reason: {ex.Message}", ex);
            }
        else
            try
            {
                var inMemoryStream = new MemoryStream();
                using (await localFilesLock.LockAsync(inputPath, cancellationToken).ConfigureAwait(false))
                {// To avoid deadlocking on update with multiple clients for the same local description
                    using var fileStream = new FileStream(inputPath, FileMode.Open);
                    await fileStream.CopyToAsync(inMemoryStream, cancellationToken).ConfigureAwait(false);
                }
                inMemoryStream.Position = 0;
                input = inMemoryStream;
                logger.LogInformation("loaded description from local source");
            }
            catch (Exception ex) when (ex is FileNotFoundException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is NotSupportedException)
            {
                throw new InvalidOperationException($"Could not open the file at {inputPath}, reason: {ex.Message}", ex);
            }
        stopwatch.Stop();
        logger.LogTrace("{Timestamp}ms: Read OpenAPI file {File}", stopwatch.ElapsedMilliseconds, inputPath);
        return (input, isDescriptionFromWorkspaceCopy);
    }
}
