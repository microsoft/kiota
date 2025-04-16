using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Workspace;

internal class MigrateHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "workspace-migrate"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Option<string> LockDirectoryOption
    {
        get;
        init;
    }
    public required Option<string> ClassOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;

        // Get options
        var workingDirectory = NormalizeSlashesInPath(Directory.GetCurrentDirectory());
        string? lockDirectory0 = context.ParseResult.GetValueForOption(LockDirectoryOption);
        string? clientName0 = context.ParseResult.GetValueForOption(ClassOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, lockDirectory0, clientName0, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(TelemetryLabels.SpanMigrateWorkspaceCommand,
            ActivityKind.Internal, startTime: startTime, parentContext: default,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        var lockDirectory = NormalizeSlashesInPath(lockDirectory0 ?? workingDirectory);
        var clientName = clientName0.OrEmpty();
        var (loggerFactory, logger) = GetLoggerAndFactory<WorkspaceManagementService>(context, $"./{DescriptionStorageService.KiotaDirectorySegment}");
        using (loggerFactory)
        {
            try
            {
                var workspaceManagementService = new WorkspaceManagementService(logger, httpClient, true, workingDirectory);
                var clientNames = await workspaceManagementService.MigrateFromLockFileAsync(clientName, lockDirectory, cancellationToken).ConfigureAwait(false);
                if (!clientNames.Any())
                {
                    DisplayWarning("no client configuration was migrated");
                    return 1;
                }
                DisplaySuccess($"Client configurations migrated successfully: {string.Join(", ", clientNames)}");
                DisplayGenerateAfterMigrateHint();
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
                logger.LogCritical(ex, "error migrating the workspace configuration");
                return 1;
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, string? lockDirectory, string? className,
        LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(4)
        {
            new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
        } : null;
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        if (lockDirectory is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.lock_directory", redacted));
        if (className is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.client_name", redacted));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
