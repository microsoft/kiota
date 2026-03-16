using System.CommandLine;
using System.Diagnostics;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Workspace;

internal class InitHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "workspace-init"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;

        // Get options
        var logLevel = parseResult.GetResult(LogLevelOption)?.GetValueOrDefault<LogLevel>() as LogLevel?;
        var instrumentation = ServiceProvider?.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(TelemetryLabels.SpanInitWorkspaceCommand,
            ActivityKind.Internal, startTime: startTime, parentContext: default,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        var workspaceStorageService = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
        var (loggerFactory, logger) = GetLoggerAndFactory<WorkspaceConfigurationStorageService>(parseResult, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            try
            {
                await workspaceStorageService.InitializeAsync(cancellationToken).ConfigureAwait(false);
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
                logger.LogCritical(ex, "error initializing the workspace configuration");
                return 1;
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(2)
        {
            new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue)
        } : null;
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
