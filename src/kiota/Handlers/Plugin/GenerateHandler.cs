﻿using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace kiota.Handlers.Plugin;

internal class GenerateHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagGenerationOutputType, "plugin"),
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

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, refresh, className0, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(TelemetryLabels.SpanGeneratePluginCommand,
            ActivityKind.Internal, startTime: startTime, parentContext: default,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        var className = className0.OrEmpty();
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, $"./{DescriptionStorageService.KiotaDirectorySegment}");
        using (loggerFactory)
        {
            var httpClient = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
            var configuration = host.Services.GetRequiredService<IOptions<KiotaConfiguration>>().Value;
            var generationConfiguration = host.Services.GetRequiredKeyedService<GenerationConfiguration>(ServiceConstants.ServiceKeys.Default);
            await CheckForNewVersionAsync(configuration, httpClient, logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));
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
                                            .Plugins
                                            .Where(x => clientNameWasNotProvided || x.Key.Equals(className, StringComparison.OrdinalIgnoreCase))
                                            .ToArray();
                if (clientEntries.Length == 0 && !clientNameWasNotProvided)
                {
                    DisplayError($"No client found with the provided name {className}");
                    return 1;
                }
                foreach (var clientEntry in clientEntries)
                {
                    var requests = !refresh && manifest is not null && manifest.ApiDependencies.TryGetValue(clientEntry.Key, out var value) ? value.Requests : [];
                    clientEntry.Value.UpdateGenerationConfigurationFromApiPluginConfiguration(generationConfiguration, clientEntry.Key, requests);
                    DefaultSerializersAndDeserializers(generationConfiguration);
                    generationConfiguration.ClearCache = refresh;
                    generationConfiguration.CleanOutput = refresh;
                    generationConfiguration.Operation = ConsumerOperation.Generate;
                    var builder = new KiotaBuilder(logger, generationConfiguration, httpClient, true);
                    var result = await builder.GeneratePluginAsync(cancellationToken).ConfigureAwait(false);
                    if (result)
                    {
                        DisplaySuccess($"Update of {clientEntry.Key} plugin completed");
                        var genCounter = instrumentation?.CreatePluginGenerationCounter();
                        var meterTags = new TagList(_commonTags.AsSpan())
                        {
                            new KeyValuePair<string, object?>(
                                TelemetryLabels.TagGeneratorPluginTypes,
                                generationConfiguration.PluginTypes.Select(static x=> x.ToString("G").ToLowerInvariant()).ToArray())
                        };
                        genCounter?.Add(1, meterTags);
                    }
                    else
                    {
                        DisplayWarning($"Update of {clientEntry.Key} skipped, no changes detected");
                        DisplayCleanHint("client generate", "--refresh");
                    }
                }

                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error generating the plugin: {ExceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error generating the plugin: {ExceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, bool refresh, string? className, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(3)
        {
            new($"{TelemetryLabels.TagCommandParams}.refresh", refresh),
        } : null;
        if (className is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.client_name", TelemetryLabels.RedactedValuePlaceholder));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
