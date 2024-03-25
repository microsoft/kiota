using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Plugin;

internal class GenerateHandler : BaseKiotaCommandHandler
{
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
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        bool refresh = context.ParseResult.GetValueForOption(RefreshOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
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
                    var generationConfiguration = new GenerationConfiguration();
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
                        DisplaySuccess($"Update of {clientEntry.Key} client completed");
                        var manifestPath = $"{GetAbsolutePath(Path.Combine(WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName))}#{clientEntry.Key}";
                        DisplayInfoHint(generationConfiguration.Language, string.Empty, manifestPath);
                    }
                    else
                    {
                        DisplayWarning($"Update of {clientEntry.Key} skipped, no changes detected");
                        DisplayCleanHint("client generate", "--refresh");
                    }
                }
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
        throw new NotImplementedException();
    }
}
