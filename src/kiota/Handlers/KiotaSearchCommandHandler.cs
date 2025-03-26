using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Diagnostics;
using System.Text.Json;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.SearchProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaSearchCommandHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "search"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Argument<string> SearchTermArgument
    {
        get; init;
    }
    public required Option<bool> ClearCacheOption
    {
        get; init;
    }
    public required Option<string> VersionOption
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
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, version0, clearCache, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanSearchCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        string version = version0.OrEmpty();
        Configuration.Search.ClearCache = clearCache;


        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaSearcher>(context);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));

            try
            {
                var searcher = await GetKiotaSearcherAsync(loggerFactory, cancellationToken).ConfigureAwait(false);
                var results = await searcher.SearchAsync(searchTerm, version, cancellationToken);
                await DisplayResultsAsync(searchTerm, version, results, logger, cancellationToken);
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error searching for a description: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error searching for a description: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }
    private async Task DisplayResultsAsync(string searchTerm, string version, IDictionary<string, SearchResult> results, ILogger logger, CancellationToken cancellationToken)
    {
        if (results.Any() && !string.IsNullOrEmpty(searchTerm) && searchTerm.Contains(KiotaSearcher.ProviderSeparator) && results.ContainsKey(searchTerm))
        {
            var result = results.First();
            DisplayInfo($"Key: {result.Key}");
            DisplayInfo($"Title: {result.Value.Title}");
            DisplayInfo($"Description: {result.Value.Description}");
            DisplayInfo($"Service: {result.Value.ServiceUrl}");
            DisplayInfo($"OpenAPI: {result.Value.DescriptionUrl}");
            DisplayDownloadHint(searchTerm, version);
            DisplayShowHint(searchTerm, version);
        }
        else
        {
            var view = new TableView<KeyValuePair<string, SearchResult>>()
            {
                Items = results.OrderBy(static x => x.Key).Select(static x => x).ToList(),
            };
            view.AddColumn(static x => x.Key, "Key");
            view.AddColumn(static x => x.Value.Title, "Title");
            view.AddColumn(static x => ShortenDescription(x.Value.Description), "Description");
            view.AddColumn(static x => string.Join(", ", x.Value.VersionLabels), "Versions");
            var console = new SystemConsole();
            using var terminal = new SystemConsoleTerminal(console);
            var layout = new StackLayoutView { view };
            console.Append(layout);
            DisplaySearchHint(results.Keys.FirstOrDefault(), version);
            await DisplayLoginHintAsync(logger, cancellationToken);
            DisplaySearchAddHint();
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, string? version, bool clearCache,
        LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(5)
        {
            new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
            // Search term is required
            new($"{TelemetryLabels.TagCommandParams}.search_term", redacted),
            new($"{TelemetryLabels.TagCommandParams}.clear_cache", clearCache),
        } : null;

        if (version is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.version", redacted));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }

    private const int MaxDescriptionLength = 70;
    private static string ShortenDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;
        if (description.Length > MaxDescriptionLength)
            return description[..MaxDescriptionLength] + "...";
        return description;
    }
}
