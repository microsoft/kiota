﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

using Kiota.Builder;
using Kiota.Builder.Extensions;

using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaGenerateCommandHandler : BaseKiotaCommandHandler
{
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
        WarnShouldUseKiotaConfigClientsCommands();
        string output = context.ParseResult.GetValueForOption(OutputOption) ?? string.Empty;
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption) ?? string.Empty;
        string manifest = context.ParseResult.GetValueForOption(ManifestOption) ?? string.Empty;
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool excludeBackwardCompatible = context.ParseResult.GetValueForOption(ExcludeBackwardCompatibleOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool disableSSLValidation = context.ParseResult.GetValueForOption(DisableSSLValidationOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        string namespaceName = context.ParseResult.GetValueForOption(NamespaceOption) ?? string.Empty;
        List<string> serializer = context.ParseResult.GetValueForOption(SerializerOption) ?? [];
        List<string> deserializer = context.ParseResult.GetValueForOption(DeserializerOption) ?? [];
        List<string> includePatterns = context.ParseResult.GetValueForOption(IncludePatternsOption) ?? [];
        List<string> excludePatterns = context.ParseResult.GetValueForOption(ExcludePatternsOption) ?? [];
        List<string> disabledValidationRules = context.ParseResult.GetValueForOption(DisabledValidationRulesOption) ?? [];
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        List<string> structuredMimeTypes = context.ParseResult.GetValueForOption(StructuredMimeTypesOption) ?? [];
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(manifest, (c, s) => c.ApiManifestPath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
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
                }
                else
                {
                    DisplaySuccess("Generation skipped as no changes were detected");
                    DisplayCleanHint("generate");
                }
                var manifestResult = await builder.GetApiManifestDetailsAsync(true, cancellationToken).ConfigureAwait(false);
                var manifestPath = manifestResult is null ? string.Empty : Configuration.Generation.ApiManifestPath;
                DisplayInfoHint(language, Configuration.Generation.OpenAPIFilePath, manifestPath);
                DisplayGenerateAdvancedHint(includePatterns, excludePatterns, Configuration.Generation.OpenAPIFilePath, manifestPath);
                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                logger.LogCritical(ex, "error generating the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error generating the client: {exceptionMessage}", ex.Message);
                return 1;
#endif
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
}
