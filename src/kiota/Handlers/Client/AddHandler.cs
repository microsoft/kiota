using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Client;

internal class AddHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagGenerationOutputType, "client"),
        new(TelemetryLabels.TagCommandName, "add"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<bool> BackingStoreOption
    {
        get; init;
    }
    public required Option<string> OutputOption
    {
        get; init;
    }
    public required Option<GenerationLanguage> LanguageOption
    {
        get; init;
    }
    public required Option<AccessModifier> TypeAccessModifierOption
    {
        get; init;
    }
    public required Option<string> DescriptionOption
    {
        get; init;
    }
    public required Option<string> NamespaceOption
    {
        get; init;
    }
    public required Option<bool> AdditionalDataOption
    {
        get; init;
    }
    public required Option<List<string>> DisabledValidationRulesOption
    {
        get; init;
    }
    public required Option<List<string>> StructuredMimeTypesOption
    {
        get; init;
    }
    public required Option<bool> ExcludeBackwardCompatibleOption
    {
        get;
        set;
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
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        AccessModifier typeAccessModifier = context.ParseResult.GetValueForOption(TypeAccessModifierOption);
        string? openapi = context.ParseResult.GetValueForOption(DescriptionOption);
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool excludeBackwardCompatible = context.ParseResult.GetValueForOption(ExcludeBackwardCompatibleOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        bool skipGeneration = context.ParseResult.GetValueForOption(SkipGenerationOption);
        string? className = context.ParseResult.GetValueForOption(ClassOption);
        string? namespaceName = context.ParseResult.GetValueForOption(NamespaceOption);
        List<string>? includePatterns0 = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string>? excludePatterns0 = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        List<string>? disabledValidationRules0 = context.ParseResult.GetValueForOption(DisabledValidationRulesOption);
        List<string>? structuredMimeTypes0 = context.ParseResult.GetValueForOption(StructuredMimeTypesOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, language, backingStore, excludeBackwardCompatible, skipGeneration, output,
            namespaceName, includePatterns0, excludePatterns0, structuredMimeTypes0, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanAddClientCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        List<string> includePatterns = includePatterns0.OrEmpty();
        List<string> excludePatterns = excludePatterns0.OrEmpty();
        List<string> disabledValidationRules = disabledValidationRules0.OrEmpty();
        List<string> structuredMimeTypes = structuredMimeTypes0.OrEmpty();
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
        Configuration.Generation.UsesBackingStore = backingStore;
        Configuration.Generation.ExcludeBackwardCompatible = excludeBackwardCompatible;
        Configuration.Generation.IncludeAdditionalData = includeAdditionalData;
        Configuration.Generation.Language = language;
        WarnUsingPreviewLanguage(language);
        Configuration.Generation.TypeAccessModifier = typeAccessModifier;
        Configuration.Generation.SkipGeneration = skipGeneration;
        Configuration.Generation.Operation = ConsumerOperation.Add;
        if (includePatterns.Count != 0)
            Configuration.Generation.IncludePatterns = includePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (excludePatterns.Count != 0)
            Configuration.Generation.ExcludePatterns = excludePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (disabledValidationRules.Count != 0)
            Configuration.Generation.DisabledValidationRules = disabledValidationRules
                                                                    .Select(static x => x.TrimQuotes())
                                                                    .SelectMany(static x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (structuredMimeTypes.Count != 0)
            Configuration.Generation.StructuredMimeTypes = new(structuredMimeTypes.SelectMany(static x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                                                            .Select(static x => x.TrimQuotes()));

        Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(Configuration.Generation.OpenAPIFilePath);
        Configuration.Generation.OutputPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.OutputPath));
        Configuration.Generation.ApiManifestPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.ApiManifestPath));

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));

            try
            {
                var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient, true);
                var result = await builder.GenerateClientAsync(cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    DisplaySuccess("Generation completed successfully");
                    DisplayUrlInformation(Configuration.Generation.ApiRootUrl);
                    var genCounter = instrumentation?.CreateClientGenerationCounter();
                    var meterTags = new TagList(_commonTags.AsSpan())
                    {
                        new KeyValuePair<string, object?>(
                            TelemetryLabels.TagGeneratorLanguage,
                            Configuration.Generation.Language.ToString("G"))
                    };
                    genCounter?.Add(1, meterTags);
                }
                else if (skipGeneration)
                {
                    DisplaySuccess("Generation skipped as --skip-generation was passed");
                    DisplayGenerateCommandHint();
                } // else we get an error because we're adding a client that already exists
                var manifestPath = $"{GetAbsolutePath(Path.Combine(WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName))}#{Configuration.Generation.ClientClassName}";
                DisplayInfoHint(language, string.Empty, manifestPath);
                DisplayGenerateAdvancedHint(includePatterns, excludePatterns, string.Empty, manifestPath, "client add");
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
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, GenerationLanguage language, bool backingStore,
        bool excludeBackwardCompatible, bool skipGeneration, string? output, string? namespaceName,
        List<string>? includePatterns, List<string>? excludePatterns, List<string>? structuredMimeTypes, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(11)
            {
                new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
                new(TelemetryLabels.TagGeneratorLanguage, language.ToString("G")),
                new($"{TelemetryLabels.TagCommandParams}.backing_store", backingStore),
                new($"{TelemetryLabels.TagCommandParams}.exclude_backward_compatible", excludeBackwardCompatible),
                new($"{TelemetryLabels.TagCommandParams}.skip_generation", skipGeneration),
            } : null;
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        if (output is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.output", redacted));
        if (namespaceName is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.namespace", redacted));
        if (includePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.include_path", redacted));
        if (excludePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.exclude_path", redacted));
        if (structuredMimeTypes is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.structured_media_types", structuredMimeTypes.ToArray()));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
