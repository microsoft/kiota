using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Caching;
using Kiota.Builder.SearchProviders;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaDownloadCommandHandler : BaseKiotaCommandHandler
{
    public required Argument<string> SearchTermArgument { get; init; }
    public required Option<string> VersionOption { get; init; }
    public required Option<string> OutputPathOption { get; init; }
    public required Option<bool> ClearCacheOption { get; init; }
    public required Option<bool> CleanOutputOption { get; init; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string searchTerm = context.ParseResult.GetValueForArgument(SearchTermArgument);
        string version = context.ParseResult.GetValueForOption(VersionOption) ?? string.Empty;
        string outputPath = context.ParseResult.GetValueForOption(OutputPathOption) ?? string.Empty;
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        Configuration.Download.ClearCache = clearCache;
        Configuration.Download.CleanOutput = cleanOutput;
        Configuration.Download.OutputPath = NormalizeSlashesInPath(outputPath);

        Configuration.Search.ClearCache = Configuration.Download.ClearCache;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaSearcher>(context);
        using (loggerFactory) {
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

            try {
                var searcher = await GetKiotaSearcher(loggerFactory, cancellationToken).ConfigureAwait(false);
                var results = await searcher.SearchAsync(searchTerm, version, cancellationToken).ConfigureAwait(false);
                return await SaveResultsAsync(searchTerm, version, results, logger, cancellationToken);
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error downloading a description: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error downloading a description: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }
    private async Task<int> SaveResultsAsync(string searchTerm, string version, IDictionary<string, SearchResult> results, ILogger logger, CancellationToken cancellationToken){
        if (!results.Any())
            DisplayError("No matching result found, use the search command to find the right key");
        else if (results.Any() && !string.IsNullOrEmpty(searchTerm) && searchTerm.Contains(KiotaSearcher.ProviderSeparator) && results.ContainsKey(searchTerm)) {
            var (path, statusCode) = await SaveResultAsync(results.First(), logger, cancellationToken);
            DisplaySuccess($"File successfully downloaded to {path}");
            DisplayShowHint(searchTerm, version, path);
            DisplayGenerateHint(path, Enumerable.Empty<string>(), Enumerable.Empty<string>());
            return statusCode;
        }  else 
            DisplayError("Multiple matches found, use the key to select a specific description. You can find the key by using the search command.");

        return 0;
    }
    private async Task<(string, int)> SaveResultAsync(KeyValuePair<string, SearchResult> result, ILogger logger, CancellationToken cancellationToken) {
        string path;
        try {
            if (Path.IsPathFullyQualified(Configuration.Download.OutputPath))
                path = Configuration.Download.OutputPath;
            else
                path = Path.GetFullPath(Configuration.Download.OutputPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path))) {
                logger.LogCritical("The output path does not contain a file name: {path}", path);
                return (path, 1);
            }
        } catch (Exception) {
            logger.LogCritical("Invalid output path: {path}", Configuration.Download.OutputPath);
            return (string.Empty, 1);
        }
        if (File.Exists(path)) {
            if(Configuration.Download.CleanOutput)
                File.Delete(path);
            else {
                logger.LogCritical("Output path already exists and the clean output option was not specified: {path}", path);
                return (path, 1);
            }
        }
        if(Path.GetDirectoryName(path) is string directoryName && !Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);
        
        var cacheProvider = new DocumentCachingProvider(httpClient, logger) {
            ClearCache = true,
        };
        await using var document = await cacheProvider.GetDocumentAsync(result.Value.DescriptionUrl, "download", Path.GetFileName(path), cancellationToken: cancellationToken);
        await using var fileStream = File.Create(path);
        await document.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);
        return (path, 0);
    }
}
