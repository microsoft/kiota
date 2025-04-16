using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaGenerateCommandHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagGenerationOutputType, "client"),
        new(TelemetryLabels.TagCommandName, "generate"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public required Option<string> DescriptionOption
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
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<AccessModifier> TypeAccessModifierOption
    {
        get; init;
    }
    public required Option<string> NamespaceOption
    {
        get; init;
    }
    public required Option<bool> BackingStoreOption
    {
        get; init;
    }
    public required Option<bool> AdditionalDataOption
    {
        get; init;
    }
    public required Option<List<string>> SerializerOption
    {
        get; init;
    }
    public required Option<List<string>> DeserializerOption
    {
        get; init;
    }
    public required Option<List<string>> DisabledValidationRulesOption
    {
        get; init;
    }
    public required Option<bool> CleanOutputOption
    {
        get; init;
    }
    public required Option<List<string>> StructuredMimeTypesOption
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
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        string? openapi = context.ParseResult.GetValueForOption(DescriptionOption);
        string? manifest = context.ParseResult.GetValueForOption(ManifestOption);
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool excludeBackwardCompatible = context.ParseResult.GetValueForOption(ExcludeBackwardCompatibleOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool disableSSLValidation = context.ParseResult.GetValueForOption(DisableSSLValidationOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        string? className = context.ParseResult.GetValueForOption(ClassOption);
        AccessModifier typeAccessModifier = context.ParseResult.GetValueForOption(TypeAccessModifierOption);
        string? namespaceName = context.ParseResult.GetValueForOption(NamespaceOption);
        List<string> serializer = context.ParseResult.GetValueForOption(SerializerOption).OrEmpty();
        List<string> deserializer = context.ParseResult.GetValueForOption(DeserializerOption).OrEmpty();
        List<string>? includePatterns0 = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string>? excludePatterns0 = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        List<string>? disabledValidationRules0 = context.ParseResult.GetValueForOption(DisabledValidationRulesOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        List<string>? structuredMimeTypes0 = context.ParseResult.GetValueForOption(StructuredMimeTypesOption);
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, language, backingStore, excludeBackwardCompatible, clearCache, disableSSLValidation, cleanOutput, output,
            namespaceName, includePatterns0, excludePatterns0, structuredMimeTypes0, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanGenerateClientCommand,
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
        AssignIfNotNullOrEmpty(manifest, (c, s) => c.ApiManifestPath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
        Configuration.Generation.TypeAccessModifier = typeAccessModifier;
        Configuration.Generation.UsesBackingStore = backingStore;
        Configuration.Generation.ExcludeBackwardCompatible = excludeBackwardCompatible;
        Configuration.Generation.IncludeAdditionalData = includeAdditionalData;
        Configuration.Generation.Language = language;
        WarnUsingPreviewLanguage(language);
        if (serializer.Count != 0)
            Configuration.Generation.Serializers = serializer.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (deserializer.Count != 0)
            Configuration.Generation.Deserializers = deserializer.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        Configuration.Generation.CleanOutput = cleanOutput;
        Configuration.Generation.ClearCache = clearCache;
        Configuration.Generation.DisableSSLValidation = disableSSLValidation;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));

            try
            {
                var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient);
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
                else
                {
                    DisplaySuccess("Generation skipped as no changes were detected");
                    if (!cleanOutput)
                        DisplayCleanHint("generate");
                }
                var manifestResult = await builder.GetApiManifestDetailsAsync(true, cancellationToken).ConfigureAwait(false);
                var manifestPath = manifestResult is null ? string.Empty : Configuration.Generation.ApiManifestPath;
                DisplayInfoHint(language, Configuration.Generation.OpenAPIFilePath, manifestPath);
                DisplayGenerateAdvancedHint(includePatterns, excludePatterns, Configuration.Generation.OpenAPIFilePath, manifestPath);
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return 0;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error generating the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error generating the client: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
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
    public required Option<bool> ExcludeBackwardCompatibleOption
    {
        get;
        set;
    }
    public required Option<bool> DisableSSLValidationOption
    {
        get; init;
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, GenerationLanguage language, bool backingStore,
        bool excludeBackwardCompatible, bool clearCache, bool disableSslValidation, bool cleanOutput, string? output,
        string? namespaceName, List<string>? includePatterns, List<string>? excludePatterns,
        List<string>? structuredMimeTypes, LogLevel? logLevel, out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(13)
            {
                new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
                new(TelemetryLabels.TagGeneratorLanguage, language.ToString("G")),
                new($"{TelemetryLabels.TagCommandParams}.backing_store", backingStore),
                new($"{TelemetryLabels.TagCommandParams}.exclude_backward_compatible", excludeBackwardCompatible),
                new($"{TelemetryLabels.TagCommandParams}.clear_cache", clearCache),
                new($"{TelemetryLabels.TagCommandParams}.disable_ssl_validation", disableSslValidation),
                new($"{TelemetryLabels.TagCommandParams}.clean_output", cleanOutput),
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
