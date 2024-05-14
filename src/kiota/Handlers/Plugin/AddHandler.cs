using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Plugin;

internal class AddHandler : BaseKiotaCommandHandler
{
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<string> OutputOption
    {
        get; init;
    }
    public required Option<List<PluginType>> PluginTypesOption
    {
        get; init;
    }
    public required Option<string> DescriptionOption
    {
        get; init;
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
        List<PluginType> pluginTypes = context.ParseResult.GetValueForOption(PluginTypesOption) ?? [];
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption) ?? string.Empty;
        bool skipGeneration = context.ParseResult.GetValueForOption(SkipGenerationOption);
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        List<string> includePatterns = context.ParseResult.GetValueForOption(IncludePatternsOption) ?? [];
        List<string> excludePatterns = context.ParseResult.GetValueForOption(ExcludePatternsOption) ?? [];
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        Configuration.Generation.SkipGeneration = skipGeneration;
        Configuration.Generation.Operation = ConsumerOperation.Add;
        if (pluginTypes.Count != 0)
            Configuration.Generation.PluginTypes = pluginTypes.ToHashSet();
        if (includePatterns.Count != 0)
            Configuration.Generation.IncludePatterns = includePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (excludePatterns.Count != 0)
            Configuration.Generation.ExcludePatterns = excludePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(Configuration.Generation.OpenAPIFilePath);
        Configuration.Generation.OutputPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.OutputPath));
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            logger.AppendInternalTracing();
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, KiotaConfigurationJsonContext.Default.KiotaConfiguration));
            try
            {
                var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient, true);
                var result = await builder.GeneratePluginAsync(cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    DisplaySuccess("Generation completed successfully");
                    DisplayUrlInformation(Configuration.Generation.ApiRootUrl);
                }
                else if (skipGeneration)
                {
                    DisplaySuccess("Generation skipped as --skip-generation was passed");
                    DisplayGenerateCommandHint();
                } // else we get an error because we're adding a client that already exists
                var manifestPath = $"{GetAbsolutePath(Path.Combine(WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName))}#{Configuration.Generation.ClientClassName}";
                DisplayGenerateAdvancedHint(includePatterns, excludePatterns, string.Empty, manifestPath, "plugin add");
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
