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
    public Argument<string> SearchTermArgument { get; set; }
    public Option<string> VersionOption { get; set; }
    public Option<string> OutputPathOption { get; set; }
    public Option<bool> ClearCacheOption { get; set; }
    public Option<bool> CleanOutputOption { get; set; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string searchTerm = context.ParseResult.GetValueForArgument(SearchTermArgument);
        string version = context.ParseResult.GetValueForOption(VersionOption);
        string outputPath = context.ParseResult.GetValueForOption(OutputPathOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));

        Configuration.Download.SearchTerm = searchTerm;
        Configuration.Download.Version = version;
        Configuration.Download.ClearCache = clearCache;
        Configuration.Download.CleanOutput = cleanOutput;
        Configuration.Download.OutputPath = NormalizeSlashesInPath(outputPath);

        Configuration.Search.SearchTerm = Configuration.Download.SearchTerm;
        Configuration.Search.Version = Configuration.Download.Version;
        Configuration.Search.ClearCache = Configuration.Download.ClearCache;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaSearcher>(context);
        using (loggerFactory) {
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

            try {
                var results = await new KiotaSearcher(logger, Configuration.Search).SearchAsync(cancellationToken);
                return await SaveResultsAsync(results, logger, cancellationToken);
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
    private async Task<int> SaveResultsAsync(IDictionary<string, SearchResult> results, ILogger logger, CancellationToken cancellationToken){
        var searchTerm = Configuration.Download.SearchTerm;
        if (!results.Any())
            DisplayError("No matching result found, use the search command to find the right key");
        else if (results.Any() && !string.IsNullOrEmpty(searchTerm) && searchTerm.Contains(KiotaSearcher.ProviderSeparator) && results.ContainsKey(searchTerm)) {
            var (path, statusCode) = await SaveResultAsync(results.First(), logger, cancellationToken);
            DisplaySuccess($"File successfully downloaded to {path}");
            DisplayShowHint(Configuration.Search.SearchTerm, Configuration.Search.Version, path);
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
        if(!Directory.Exists(Path.GetDirectoryName(path)))
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        
        using var client = new HttpClient();
        var cacheProvider = new DocumentCachingProvider(client, logger) {
            ClearCache = true,
        };
        using var document = await cacheProvider.GetDocumentAsync(result.Value.DescriptionUrl, "download", Path.GetFileName(path), cancellationToken);
        using var fileStream = File.Create(path);
        await document.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);
        return (path, 0);
    }
}
