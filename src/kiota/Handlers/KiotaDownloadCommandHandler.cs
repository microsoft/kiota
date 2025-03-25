using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.Caching;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaDownloadCommandHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "download"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Argument<string> SearchTermArgument
    {
        get; init;
    }
    public required Option<string> VersionOption
    {
        get; init;
    }
    public required Option<string> OutputPathOption
    {
        get; init;
    }
    public required Option<bool> ClearCacheOption
    {
        get; init;
    }
    public required Option<bool> CleanOutputOption
    {
        get; init;
    }
    public required Option<bool> DisableSSLValidationOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        // Get options
        string searchTerm = context.ParseResult.GetValueForArgument(SearchTermArgument);
        string? version0 = context.ParseResult.GetValueForOption(VersionOption);
        string? outputPath0 = context.ParseResult.GetValueForOption(OutputPathOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool disableSSLValidation = context.ParseResult.GetValueForOption(DisableSSLValidationOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, version0, outputPath0, cleanOutput, clearCache, disableSSLValidation,
            logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanDownloadCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        string outputPath = outputPath0.OrEmpty();
        string version = version0.OrEmpty();
        Configuration.Download.ClearCache = clearCache;
        Configuration.Download.DisableSSLValidation = disableSSLValidation;
        Configuration.Download.CleanOutput = cleanOutput;
        Configuration.Download.OutputPath = NormalizeSlashesInPath(outputPath);

        Configuration.Search.ClearCache = Configuration.Download.ClearCache;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaSearcher>(context);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));

            try
            {
                var searcher = await GetKiotaSearcherAsync(loggerFactory, cancellationToken).ConfigureAwait(false);
                var results = await searcher.SearchAsync(searchTerm, version, cancellationToken).ConfigureAwait(false);
                var result = await SaveResultsAsync(searchTerm, version, results, logger, cancellationToken);
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error downloading a description: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error downloading a description: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }
    private async Task<int> SaveResultsAsync(string searchTerm, string version, IDictionary<string, SearchResult> results, ILogger logger, CancellationToken cancellationToken)
    {
        if (!results.Any())
            DisplayError("No matching result found, use the search command to find the right key");
        else if (results.Any() && !string.IsNullOrEmpty(searchTerm) && searchTerm.Contains(KiotaSearcher.ProviderSeparator) && results.ContainsKey(searchTerm))
        {
            var (path, statusCode) = await SaveResultAsync(results.First(), logger, cancellationToken);
            if (statusCode == 0)
            {
                DisplaySuccess($"File successfully downloaded to {path}");
                DisplayShowHint(searchTerm, version, path);
                DisplayGenerateHint(path, string.Empty, Enumerable.Empty<string>(), Enumerable.Empty<string>());
            }
            return statusCode;
        }
        else
            DisplayError("Multiple matches found, use the key to select a specific description. You can find the key by using the search command.");

        return 0;
    }
    private async Task<(string, int)> SaveResultAsync(KeyValuePair<string, SearchResult> result, ILogger logger, CancellationToken cancellationToken)
    {
        string path;
        if (result.Value.DescriptionUrl is null)
        {
            logger.LogCritical("The description could not be found");
            return (string.Empty, 1);
        }
        try
        {
            Console.WriteLine($"output path: {Configuration.Download.OutputPath}");
            var defaultOutputPath = new DownloadConfiguration().OutputPath.Replace('/', Path.DirectorySeparatorChar);
            var defaultExtension = Path.GetExtension(defaultOutputPath)[1..];
            var fileExtension = Path.GetExtension(result.Value.DescriptionUrl.ToString())[1..];
            if (Configuration.Download.OutputPath.Equals(defaultOutputPath, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(fileExtension) &&
                !fileExtension.Equals(defaultExtension, StringComparison.OrdinalIgnoreCase))
                Configuration.Download.OutputPath = Configuration.Download.OutputPath[..^defaultExtension.Length] + fileExtension;
            if (Path.IsPathFullyQualified(Configuration.Download.OutputPath))
                path = Configuration.Download.OutputPath;
            else
                path = Path.GetFullPath(Configuration.Download.OutputPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)))
            {
                logger.LogCritical("The output path does not contain a file name: {path}", path);
                return (path, 1);
            }
        }
        catch (Exception)
        {
            logger.LogCritical("Invalid output path: {path}", Configuration.Download.OutputPath);
            return (string.Empty, 1);
        }
        if (File.Exists(path))
        {
            if (Configuration.Download.CleanOutput)
                File.Delete(path);
            else
            {
                logger.LogCritical("Output path already exists and the clean output option was not specified: {path}", path);
                return (path, 1);
            }
        }
        if (Path.GetDirectoryName(path) is string directoryName && !Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);

        var cacheProvider = new DocumentCachingProvider(httpClient, logger)
        {
            ClearCache = true,
        };
        await using var document = await cacheProvider.GetDocumentAsync(result.Value.DescriptionUrl, "download", Path.GetFileName(path), cancellationToken: cancellationToken);
        await using var fileStream = File.Create(path);
        await document.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);
        return (path, 0);
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, string? version,
        string? outputPath, bool cleanOutput, bool clearCache, bool disableSslValidation, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(8)
            {
                new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
                // search term is required, so it's always available
                new($"{TelemetryLabels.TagCommandParams}.search_term", redacted),
                new($"{TelemetryLabels.TagCommandParams}.clean_output", cleanOutput),
                new($"{TelemetryLabels.TagCommandParams}.clear_cache", clearCache),
                new($"{TelemetryLabels.TagCommandParams}.disable_ssl_validation", disableSslValidation),
            } : null;
        if (outputPath is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.output", redacted));
        if (version is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.version", redacted));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
