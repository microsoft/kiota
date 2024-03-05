using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Client;

internal class EditHandler : BaseKiotaCommandHandler
{
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<bool?> BackingStoreOption
    {
        get; init;
    }
    public required Option<string> OutputOption
    {
        get; init;
    }
    public required Option<GenerationLanguage?> LanguageOption
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
    public required Option<bool?> AdditionalDataOption
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
    public required Option<bool?> ExcludeBackwardCompatibleOption
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
        string output = context.ParseResult.GetValueForOption(OutputOption) ?? string.Empty;
        GenerationLanguage? language = context.ParseResult.GetValueForOption(LanguageOption);
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption) ?? string.Empty;
        bool? backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool? excludeBackwardCompatible = context.ParseResult.GetValueForOption(ExcludeBackwardCompatibleOption);
        bool? includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        bool skipGeneration = context.ParseResult.GetValueForOption(SkipGenerationOption);
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        string namespaceName = context.ParseResult.GetValueForOption(NamespaceOption) ?? string.Empty;
        List<string>? includePatterns = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string>? excludePatterns = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        List<string>? disabledValidationRules = context.ParseResult.GetValueForOption(DisabledValidationRulesOption);
        List<string>? structuredMimeTypes = context.ParseResult.GetValueForOption(StructuredMimeTypesOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        Configuration.Generation.SkipGeneration = skipGeneration;
        Configuration.Generation.Operation = ClientOperation.Edit;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));

            try
            {
                var workspaceStorageService = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
                var (config, _) = await workspaceStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
                if (config == null)
                {
                    DisplayError("The workspace configuration is missing, please run the init command first.");
                    return 1;
                }
                if (!config.Clients.TryGetValue(className, out var clientConfiguration))
                {
                    DisplayError($"No client found with the provided name {className}");
                    return 1;
                }
                clientConfiguration.UpdateGenerationConfigurationFromApiClientConfiguration(Configuration.Generation, className);
                if (language.HasValue)
                    Configuration.Generation.Language = language.Value;
                if (backingStore.HasValue)
                    Configuration.Generation.UsesBackingStore = backingStore.Value;
                if (excludeBackwardCompatible.HasValue)
                    Configuration.Generation.ExcludeBackwardCompatible = excludeBackwardCompatible.Value;
                if (includeAdditionalData.HasValue)
                    Configuration.Generation.IncludeAdditionalData = includeAdditionalData.Value;
                AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
                AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
                AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
                AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
                //TODO probably not needed anymore
                // Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(Configuration.Generation.OpenAPIFilePath);
                // Configuration.Generation.OutputPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.OutputPath));
                // Configuration.Generation.ApiManifestPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.ApiManifestPath));
                if (includePatterns is { Count: > 0 })
                    Configuration.Generation.IncludePatterns = includePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (excludePatterns is { Count: > 0 })
                    Configuration.Generation.ExcludePatterns = excludePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (disabledValidationRules is { Count: > 0 })
                    Configuration.Generation.DisabledValidationRules = disabledValidationRules
                                                                            .Select(static x => x.TrimQuotes())
                                                                            .SelectMany(static x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (structuredMimeTypes is { Count: > 0 })
                    Configuration.Generation.StructuredMimeTypes = new(structuredMimeTypes.SelectMany(static x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                                                                    .Select(static x => x.TrimQuotes()));

                var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient, true);
                var result = await builder.GenerateClientAsync(cancellationToken).ConfigureAwait(false);
                if (result)
                    DisplaySuccess("Generation completed successfully");
                else if (skipGeneration)
                {
                    DisplaySuccess("Generation skipped as --skip-generation was passed");
                    DisplayGenerateCommandHint();
                } // else we get an error because we're adding a client that already exists
                var manifestPath = $"{GetAbsolutePath(WorkspaceConfigurationStorageService.ManifestFileName)}#{Configuration.Generation.ClientClassName}";
                DisplayInfoHint(Configuration.Generation.Language, string.Empty, manifestPath);
                DisplayGenerateAdvancedHint(includePatterns ?? [], excludePatterns ?? [], string.Empty, manifestPath, "client edit");
                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                logger.LogCritical(ex, "error adding the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error adding the client: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
        }
    }
}
