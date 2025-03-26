using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;

namespace kiota.Handlers;
internal class KiotaShowCommandHandler : KiotaSearchBasedCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "show"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Option<string> DescriptionOption
    {
        get; init;
    }
    public required Option<string> SearchTermOption
    {
        get; init;
    }
    public required Option<string> VersionOption
    {
        get; init;
    }
    public required Option<uint> MaxDepthOption
    {
        get; init;
    }
    public required Option<List<string>> IncludePatternsOption
    {
        get; init;
    }
    public required Option<List<string>> ExcludePatternsOption
    {
        get; init;
    }
    public required Option<bool> ClearCacheOption
    {
        get; init;
    }
    public required Option<string> ManifestOption
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
        string? openapi0 = context.ParseResult.GetValueForOption(DescriptionOption);
        string? manifest0 = context.ParseResult.GetValueForOption(ManifestOption);
        string? searchTerm0 = context.ParseResult.GetValueForOption(SearchTermOption);
        string? version0 = context.ParseResult.GetValueForOption(VersionOption);
        uint maxDepth = context.ParseResult.GetValueForOption(MaxDepthOption);
        List<string>? includePatterns0 = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string>? excludePatterns0 = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool disableSSLValidation = context.ParseResult.GetValueForOption(DisableSSLValidationOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, searchTerm0, version0, clearCache, includePatterns0, excludePatterns0, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanShowCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        string openapi = openapi0.OrEmpty();
        string manifest = manifest0.OrEmpty();
        string searchTerm = searchTerm0.OrEmpty();
        string version = version0.OrEmpty();
        var includePatterns = includePatterns0.OrEmpty();
        var excludePatterns = excludePatterns0.OrEmpty();
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);

        Configuration.Search.ClearCache = clearCache;
        Configuration.Generation.DisableSSLValidation = disableSSLValidation;
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            var descriptionProvided = (!string.IsNullOrEmpty(openapi) || !string.IsNullOrEmpty(manifest)) && string.IsNullOrEmpty(searchTerm);
            var (searchResultDescription, statusCode) = await GetDescriptionFromSearchAsync(openapi, manifest, searchTerm, version, loggerFactory, logger, cancellationToken);
            if (statusCode.HasValue)
            {
                return statusCode.Value;
            }
            if (!string.IsNullOrEmpty(searchResultDescription))
            {
                openapi = searchResultDescription;
            }
            if (string.IsNullOrEmpty(openapi) && string.IsNullOrEmpty(manifest))
            {
                logger.LogError("no description provided");
                return 1;
            }
            Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(openapi);
            Configuration.Generation.ApiManifestPath = manifest;
            Configuration.Generation.IncludePatterns = [.. includePatterns];
            Configuration.Generation.ExcludePatterns = [.. excludePatterns];
            Configuration.Generation.ClearCache = clearCache;
            try
            {
                var urlTreeNode = await new KiotaBuilder(logger, Configuration.Generation, httpClient).GetUrlTreeNodeAsync(cancellationToken).ConfigureAwait(false);

                var builder = new StringBuilder();
                if (urlTreeNode != null)
                    RenderNode(urlTreeNode, maxDepth, builder);
                var tree = builder.ToString();
                Console.Write(tree);
                if (descriptionProvided)
                    DisplayShowAdvancedHint(string.Empty, string.Empty, includePatterns, excludePatterns, openapi, manifest);
                else
                    DisplayShowAdvancedHint(searchTerm, version, includePatterns, excludePatterns, openapi);
                DisplayGenerateHint(openapi, manifest, includePatterns, excludePatterns);
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error showing the description: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error showing the description: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }

        }
        invokeActivity?.SetStatus(ActivityStatusCode.Ok);
        return 0;
    }
    private const string Cross = " ├─";
    private const string Corner = " └─";
    private const string Vertical = " │ ";
    private const string Space = "   ";
    private static void RenderNode(OpenApiUrlTreeNode node, uint maxDepth, StringBuilder builder, string indent = "", int nodeDepth = 0)
    {
        builder.AppendLine(node.DeduplicatedSegment());

        var children = node.Children;
        var numberOfChildren = children.Count;
        for (var i = 0; i < numberOfChildren; i++)
        {
            var child = children.ElementAt(i);
            var isLast = i == (numberOfChildren - 1);
            RenderChildNode(child.Value, maxDepth, builder, indent, isLast, nodeDepth);
        }
    }

    private static void RenderChildNode(OpenApiUrlTreeNode node, uint maxDepth, StringBuilder builder, string indent, bool isLast, int nodeDepth = 0)
    {
        if (nodeDepth >= maxDepth && maxDepth != 0)
            return;
        builder.Append(indent);

        if (isLast)
        {
            builder.Append(Corner);
            indent += Space;
        }
        else
        {
            builder.Append(Cross);
            indent += Vertical;
        }

        RenderNode(node, maxDepth, builder, indent, nodeDepth + 1);
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, string? searchTerm, string? version,
        bool clearCache, List<string>? includePatterns, List<string>? excludePatterns, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(9)
        {
            new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
            new($"{TelemetryLabels.TagCommandParams}.openapi", redacted),
            new($"{TelemetryLabels.TagCommandParams}.clear_cache", clearCache),
            new($"{TelemetryLabels.TagCommandParams}.max_depth", redacted),
        } : null;

        if (searchTerm is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.search_key", redacted));
        if (version is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.version", redacted));
        if (includePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.include_path", redacted));
        if (excludePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.exclude_path", redacted));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
