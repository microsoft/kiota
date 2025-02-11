using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using kiota.Telemetry;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Client;

internal class GenerateHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagGenerationOutputType, "client"),
        new(TelemetryLabels.TagCommandName, "generate"),
        new(TelemetryLabels.TagCommandRevision, 2)
    ];
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<bool> RefreshOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        // Get options
        string? className0 = context.ParseResult.GetValueForOption(ClassOption);
        bool refresh = context.ParseResult.GetValueForOption(RefreshOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        var tc = context.BindingContext.GetService(typeof(TelemetryComponents)) as TelemetryComponents;

        // set up telemetry tags
        var tags = tc?.ActivitySource.HasListeners() == true ? new List<KeyValuePair<string, object?>>(5)
        {
            new($"{TelemetryLabels.TagCommandParams}.refresh", refresh),
        } : null;
        if (className0 is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.client_name", TelemetryLabels.RedactedValuePlaceholder));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));

        using var invokeActivity = tc?.ActivitySource.StartActivity(
            TelemetryLabels.SpanGenerateClientCommand, ActivityKind.Internal,
            startTime: startTime, parentContext: default,
            tags: _commonTags.ConcatNullable(tags));
        var meterRuntime = tc?.Meter.CreateHistogram<double>(name: TelemetryLabels.InstrumentCommandDurationName, unit: "s",
            description: "Duration of the command", tags: tags);
        if (meterRuntime is null) stopwatch = null;

        var className = className0 ?? string.Empty;
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));
            try
            {
                var workspaceStorageService = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
                var (config, manifest) = await workspaceStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
                if (config == null)
                {
                    DisplayError("The workspace configuration is missing, please run the init command first.");
                    return 1;
                }
                var clientNameWasNotProvided = string.IsNullOrEmpty(className);
                var clientEntries = config
                                            .Clients
                                            .Where(x => clientNameWasNotProvided || x.Key.Equals(className, StringComparison.OrdinalIgnoreCase))
                                            .ToArray();
                if (clientEntries.Length == 0 && !clientNameWasNotProvided)
                {
                    DisplayError($"No client found with the provided name {className}");
                    return 1;
                }
                foreach (var clientEntry in clientEntries)
                {
                    using var genActivity = tc?.ActivitySource.StartActivity(TelemetryLabels.SpanGenerateClientAction,
                        ActivityKind.Internal, invokeActivity?.Context ?? default,
                        tags: _commonTags.ConcatNullable(tags));
                    var generationConfiguration = new GenerationConfiguration();
                    var requests = !refresh && manifest is not null && manifest.ApiDependencies.TryGetValue(clientEntry.Key, out var value) ? value.Requests : [];
                    clientEntry.Value.UpdateGenerationConfigurationFromApiClientConfiguration(generationConfiguration, clientEntry.Key, requests);
                    generationConfiguration.ClearCache = refresh;
                    generationConfiguration.CleanOutput = refresh;
                    generationConfiguration.Operation = ConsumerOperation.Generate;
                    genActivity?.SetTag(
                        TelemetryLabels.TagGeneratorLanguage,
                        generationConfiguration.Language.ToString("G"));
                    var builder = new KiotaBuilder(logger, generationConfiguration, httpClient, true);
                    var result = await builder.GenerateClientAsync(cancellationToken).ConfigureAwait(false);
                    if (result)
                    {
                        DisplaySuccess($"Update of {clientEntry.Key} client completed");
                        DisplayUrlInformation(generationConfiguration.ApiRootUrl);
                        var manifestPath = $"{GetAbsolutePath(Path.Combine(WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName))}#{clientEntry.Key}";
                        DisplayInfoHint(generationConfiguration.Language, string.Empty, manifestPath);
                        genActivity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    else
                    {
                        DisplayWarning($"Update of {clientEntry.Key} skipped, no changes detected");
                        DisplayCleanHint("client generate", "--refresh");
                    }
                    genActivity?.Stop();
                }

                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error adding the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error adding the client: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, _commonTags);
            }
        }
    }
}
