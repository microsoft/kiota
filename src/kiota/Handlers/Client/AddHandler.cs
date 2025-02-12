using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.WorkspaceManagement;
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
        string? output0 = context.ParseResult.GetValueForOption(OutputOption);
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        AccessModifier typeAccessModifier = context.ParseResult.GetValueForOption(TypeAccessModifierOption);
        string? openapi0 = context.ParseResult.GetValueForOption(DescriptionOption);
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool excludeBackwardCompatible = context.ParseResult.GetValueForOption(ExcludeBackwardCompatibleOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        bool skipGeneration = context.ParseResult.GetValueForOption(SkipGenerationOption);
        string? className0 = context.ParseResult.GetValueForOption(ClassOption);
        string? namespaceName0 = context.ParseResult.GetValueForOption(NamespaceOption);
        List<string>? includePatterns0 = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string>? excludePatterns0 = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        List<string>? disabledValidationRules0 = context.ParseResult.GetValueForOption(DisabledValidationRulesOption);
        List<string>? structuredMimeTypes0 = context.ParseResult.GetValueForOption(StructuredMimeTypesOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        var tc = context.BindingContext.GetService(typeof(TelemetryComponents)) as TelemetryComponents;

        CreateTelemetryTags(tc, language, backingStore, excludeBackwardCompatible, skipGeneration, output0,
            namespaceName0, includePatterns0, excludePatterns0, structuredMimeTypes0, out var tags);
        // Start span
        using var invokeActivity = tc?.ActivitySource.StartActivity(
            TelemetryLabels.SpanAddClientCommand, ActivityKind.Internal,
            startTime: startTime, parentContext: default,
            tags: _commonTags.ConcatNullable(tags));
        // Create command duration meter
        var meterRuntime = tc?.CreateCommandDurationHistogram(tags);
        if (meterRuntime is null) stopwatch = null;

        string output = output0 ?? string.Empty;
        string openapi = openapi0 ?? string.Empty;
        string className = className0 ?? string.Empty;
        string namespaceName = namespaceName0 ?? string.Empty;
        List<string> includePatterns = includePatterns0 ?? [];
        List<string> excludePatterns = excludePatterns0 ?? [];
        List<string> disabledValidationRules = disabledValidationRules0 ?? [];
        List<string> structuredMimeTypes = structuredMimeTypes0 ?? [];
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
                using var genActivity = !skipGeneration
                    ? tc?.ActivitySource.StartActivity(TelemetryLabels.SpanGenerateClientAction, ActivityKind.Internal, invokeActivity?.Context ?? default, tags)
                    : null;
                var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient, true);
                var result = await builder.GenerateClientAsync(cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    genActivity?.SetTag(TelemetryLabels.TagGeneratorLanguage, Configuration.Generation.Language.ToString("G"));
                    DisplaySuccess("Generation completed successfully");
                    DisplayUrlInformation(Configuration.Generation.ApiRootUrl);
                    genActivity?.SetStatus(ActivityStatusCode.Ok);
                }
                else if (skipGeneration)
                {
                    DisplaySuccess("Generation skipped as --skip-generation was passed");
                    DisplayGenerateCommandHint();
                } // else we get an error because we're adding a client that already exists
                genActivity?.Stop();
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
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, _commonTags);
            }
        }
    }

    private static void CreateTelemetryTags(TelemetryComponents? tc, GenerationLanguage language, bool backingStore,
        bool excludeBackwardCompatible, bool skipGeneration, string? output, string? namespaceName,
        List<string>? includePatterns, List<string>? excludePatterns, List<string>? structuredMimeTypes,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = tc?.ActivitySource.HasListeners() == true ? new List<KeyValuePair<string, object?>>(16)
            {
                new(TelemetryLabels.TagGeneratorLanguage, language.ToString("G")),
                // new($"{TelemetryLabels.TagCommandParams}.type_access_modifier", typeAccessModifier.ToString("G")),
                new($"{TelemetryLabels.TagCommandParams}.backing_store", backingStore),
                new($"{TelemetryLabels.TagCommandParams}.exclude_backward_compatible", excludeBackwardCompatible),
                // new($"{TelemetryLabels.TagCommandParams}.include_additional_data", includeAdditionalData),
                new($"{TelemetryLabels.TagCommandParams}.skip_generation", skipGeneration),
            } : null;
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        if (output is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.output", redacted));
        if (namespaceName is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.namespace", redacted));
        // if (className is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.client_name", redacted));
        // if (openapi is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.openapi", redacted));
        if (includePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.include_path", redacted));
        if (excludePatterns is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.exclude_path", redacted));
        // if (disabledValidationRules is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.disable_validation_rules", disabledValidationRules0));
        if (structuredMimeTypes is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.structured_media_types", structuredMimeTypes.ToArray()));
    }
}
