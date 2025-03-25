using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaUpdateCommandHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagGenerationOutputType, "client"),
        new(TelemetryLabels.TagCommandName, "update"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Option<string> OutputOption
    {
        get; init;
    }
    public required Option<bool> CleanOutputOption
    {
        get; init;
    }
    public required Option<bool> ClearCacheOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        WarnShouldUseKiotaConfigClientsCommands();
        // Get options
        string? output = context.ParseResult.GetValueForOption(OutputOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, output, clearCache, cleanOutput,
            logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanUpdateCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        var searchPath = GetAbsolutePath(output.OrEmpty());
        var lockService = new LockManagementService();
        var lockFileDirectoryPaths = lockService.GetDirectoriesContainingLockFile(searchPath);
        if (!lockFileDirectoryPaths.Any())
        {
            DisplayError("No lock file found. Please run the generation command first.");
            return 1;
        }
        Configuration.Generation.ClearCache = clearCache;
        Configuration.Generation.CleanOutput = cleanOutput;
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            try
            {
                var locks = await Task.WhenAll(lockFileDirectoryPaths.Select(x => lockService.GetLockFromDirectoryAsync(x, cancellationToken)
                                                                                .ContinueWith(
                                                                                    (t, _) => (lockInfo: t.Result, lockDirectoryPath: x),
                                                                                    null,
                                                                                    cancellationToken,
                                                                                    TaskContinuationOptions.None,
                                                                                    TaskScheduler.Default)));
                var configurations = locks.Select(x =>
                {
                    var config = (GenerationConfiguration)Configuration.Generation.Clone();
                    x.lockInfo?.UpdateGenerationConfigurationFromLock(config);
                    config.OutputPath = x.lockDirectoryPath;
                    return config;
                }).ToArray();
                var genCounter = instrumentation?.CreateClientGenerationCounter();
                var results = await Task.WhenAll(configurations
                                                .Select(async x =>
                                                {
                                                    var meterTags = new TagList(_commonTags.AsSpan())
                                                    {
                                                        new KeyValuePair<string, object?>(
                                                            TelemetryLabels.TagGeneratorLanguage,
                                                            x.Language.ToString("G"))
                                                    };
                                                    var result = await GenerateClientAsync(context, x, cancellationToken);
                                                    genCounter?.Add(1, meterTags);
                                                    return result;
                                                }));
                foreach (var (lockInfo, lockDirectoryPath) in locks)
                {
                    DisplaySuccess($"Update of {lockInfo?.ClientClassName} client for {lockInfo?.Language} at {lockDirectoryPath} completed");
                    DisplayUrlInformation(configurations.FirstOrDefault(x => lockDirectoryPath.Equals(x.OutputPath, StringComparison.OrdinalIgnoreCase))?.ApiRootUrl);
                }
                DisplaySuccess($"Update of {locks.Length} clients completed successfully");
                foreach (var configuration in configurations)
                    DisplayInfoHint(configuration.Language, configuration.OpenAPIFilePath, string.Empty);
                if (Array.Exists(results, static x => x) && !cleanOutput)
                    DisplayCleanHint("update");
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error updating the client: {ExceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error updating the client: {ExceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }
    private async Task<bool> GenerateClientAsync(InvocationContext context, GenerationConfiguration config, CancellationToken cancellationToken)
    {
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, config.OutputPath);
        using (loggerFactory)
        {
            return await new KiotaBuilder(logger, config, httpClient).GenerateClientAsync(cancellationToken);
        }
    }
    private static void CreateTelemetryTags(ActivitySource? activitySource, string? output, bool clearCache,
        bool cleanOutput, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(5)
            {
                new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
                new($"{TelemetryLabels.TagCommandParams}.clear_cache", clearCache),
                new($"{TelemetryLabels.TagCommandParams}.clean_output", cleanOutput),
            } : null;
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        if (output is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.output", redacted));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
