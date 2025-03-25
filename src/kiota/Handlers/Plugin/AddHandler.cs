using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace kiota.Handlers.Plugin;

internal class AddHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagGenerationOutputType, "plugin"),
        new(TelemetryLabels.TagCommandName, "add"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<string> OutputOption
    {
        get; init;
    }
    public required Option<List<PluginType>> PluginTypesOption
    {
        get; init;
    }

    public required Option<SecuritySchemeType> PluginAuthTypeOption
    {
        get; init;
    }

    public required Option<string> PluginAuthRefIdOption
    {
        get; init;
    }
    public required Option<string> DescriptionOption
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
    public required Option<bool> SkipGenerationOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        // Get options
        string? output = context.ParseResult.GetValueForOption(OutputOption);
        List<PluginType>? pluginTypes = context.ParseResult.GetValueForOption(PluginTypesOption);
        SecuritySchemeType? pluginAuthType = context.ParseResult.GetValueForOption(PluginAuthTypeOption);
        string? pluginAuthRefId = context.ParseResult.GetValueForOption(PluginAuthRefIdOption);
        string? openapi = context.ParseResult.GetValueForOption(DescriptionOption);
        bool skipGeneration = context.ParseResult.GetValueForOption(SkipGenerationOption);
        string? className = context.ParseResult.GetValueForOption(ClassOption);
        List<string>? includePatterns0 = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string>? excludePatterns0 = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, pluginTypes, pluginAuthType, pluginAuthRefId, skipGeneration, output, includePatterns0, excludePatterns0,
            logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanAddPluginCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        Configuration.Generation.SkipGeneration = skipGeneration;
        Configuration.Generation.Operation = ConsumerOperation.Add;
        if (pluginTypes is { Count: > 0 })
            Configuration.Generation.PluginTypes = pluginTypes.ToHashSet();
        if (pluginAuthType.HasValue && !string.IsNullOrWhiteSpace(pluginAuthRefId))
            Configuration.Generation.PluginAuthInformation = PluginAuthConfiguration.FromParameters(pluginAuthType, pluginAuthRefId);
        if (includePatterns0 is { Count: > 0 })
            Configuration.Generation.IncludePatterns = includePatterns0.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (excludePatterns0 is { Count: > 0 })
            Configuration.Generation.ExcludePatterns = excludePatterns0.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(Configuration.Generation.OpenAPIFilePath);
        Configuration.Generation.OutputPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.OutputPath));
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));
            try
            {
                var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient, true);
                var result = await builder.GeneratePluginAsync(cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    DisplaySuccess("Generation completed successfully");
                    DisplayUrlInformation(Configuration.Generation.ApiRootUrl, true);
                    var genCounter = instrumentation?.CreatePluginGenerationCounter();
                    var meterTags = new TagList(_commonTags.AsSpan())
                    {
                        new KeyValuePair<string, object?>(
                            TelemetryLabels.TagGeneratorPluginTypes,
                            Configuration.Generation.PluginTypes.Select(static x=> x.ToString("G").ToLowerInvariant()).ToArray())
                    };
                    genCounter?.Add(1, meterTags);
                }
                else if (skipGeneration)
                {
                    DisplaySuccess("Generation skipped as --skip-generation was passed");
                    DisplayGenerateCommandHint();
                } // else we get an error because we're adding a client that already exists
                var manifestPath = $"{GetAbsolutePath(Path.Combine(WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName))}#{Configuration.Generation.ClientClassName}";
                DisplayGenerateAdvancedHint(includePatterns0.OrEmpty(), excludePatterns0.OrEmpty(), string.Empty, manifestPath, "plugin add");
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error adding the plugin: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error adding the plugin: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, List<PluginType>? pluginTypes,
        SecuritySchemeType? pluginAuthType, string? pluginAuthRefId, bool skipGeneration, string? output,
        List<string>? includePatterns, List<string>? excludePatterns, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(9)
            {
                new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
                new($"{TelemetryLabels.TagCommandParams}.skip_generation", skipGeneration),
            } : null;
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        if (output is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.output", redacted));
        if (pluginTypes is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.plugin_types", pluginTypes.Select(static x => x.ToString("G").ToLowerInvariant()).ToArray()));
        if (pluginAuthType is { } at) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.auth_type", at.ToString("G")));
        if (pluginAuthRefId is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.auth_ref_id", redacted));
        if (includePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.include_path", redacted));
        if (excludePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.exclude_path", redacted));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
