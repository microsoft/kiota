using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Kiota.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;

namespace Kiota.JsonRpcServer;
internal class Server : IServer
{
    private static readonly HttpClient httpClient = new();
    public string GetVersion()
    {
        return KiotaVersion.Current();
    }

    public async Task<List<LogEntry>> UpdateAsync(string output, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var searchPath = GetAbsolutePath(output);
        var lockService = new LockManagementService();
        var lockFileDirectoryPaths = lockService.GetDirectoriesContainingLockFile(searchPath);
        var defaultConfiguration = new GenerationConfiguration();
        if (!lockFileDirectoryPaths.Any())
        {
            logger.LogCritical("No lock file found. Please run the generation command first.");
            return logger.LogEntries;
        }
        try
        {
            var locks = await Task.WhenAll(lockFileDirectoryPaths.Select(x => lockService.GetLockFromDirectoryAsync(x, cancellationToken)
                                                                            .ContinueWith(
                                                                                (t, _) => (lockInfo: t.Result, lockDirectoryPath: x),
                                                                                null,
                                                                                cancellationToken,
                                                                                TaskContinuationOptions.None,
                                                                                TaskScheduler.Default)));
            var configurations = locks.Select(x =>
            {
                var config = (GenerationConfiguration)defaultConfiguration.Clone();
                x.lockInfo?.UpdateGenerationConfigurationFromLock(config);
                config.OutputPath = x.lockDirectoryPath;
                return config;
            }).ToArray();
            var results = await Task.WhenAll(configurations
                                    .Select(x => new KiotaBuilder(logger, x, httpClient)
                                                .GenerateClientAsync(cancellationToken)));
            foreach (var (lockInfo, lockDirectoryPath) in locks)
                logger.LogInformation("Update of {clientClassName} client for {language} at {lockDirectoryPath} completed", lockInfo?.ClientClassName, lockInfo?.Language, lockDirectoryPath);
            logger.LogInformation("Update of {length} clients completed successfully", locks.Length);
        }
        catch (Exception ex)
        {
            logger.LogCritical("error updating the client: {exceptionMessage}", ex.Message);
        }
        return logger.LogEntries;
    }
    public async Task<SearchOperationResult> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaSearcher>();
        var configuration = new SearchConfiguration { };
        var searchService = new KiotaSearcher(logger, configuration, httpClient, null, (_) => Task.FromResult(false));
        var results = await searchService.SearchAsync(searchTerm, string.Empty, cancellationToken);
        return new(logger.LogEntries, results);
    }
    public async Task<ShowResult> ShowAsync(string descriptionPath, string[] includeFilters, string[] excludeFilters, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = new GenerationConfiguration
        {
            IncludePatterns = includeFilters.ToHashSet(),
            ExcludePatterns = excludeFilters.ToHashSet(),
            OpenAPIFilePath = GetAbsolutePath(descriptionPath),
        };
        var urlTreeNode = await new KiotaBuilder(logger, configuration, httpClient).GetUrlTreeNodeAsync(cancellationToken);
        var rootNode = urlTreeNode != null ? ConvertOpenApiUrlTreeNodeToPathItem(urlTreeNode) : null;
        return new ShowResult(logger.LogEntries, rootNode);
    }
    public async Task<List<LogEntry>> GenerateAsync(string descriptionPath, string output, GenerationLanguage language, string[] includeFilters, string[] excludeFilters, string clientClassName, string clientNamespaceName, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = new GenerationConfiguration
        {
            IncludePatterns = includeFilters.ToHashSet(),
            ExcludePatterns = excludeFilters.ToHashSet(),
            OpenAPIFilePath = GetAbsolutePath(descriptionPath),
            OutputPath = GetAbsolutePath(output),
            Language = language,
        };
        if (!string.IsNullOrEmpty(clientClassName))
            configuration.ClientClassName = clientClassName;
        if (!string.IsNullOrEmpty(clientNamespaceName))
            configuration.ClientNamespaceName = clientNamespaceName;
        try
        {
            var result = await new KiotaBuilder(logger, configuration, httpClient).GenerateClientAsync(cancellationToken);
            if (result)
                logger.LogInformation("Generation of {clientClassName} client for {language} at {outputPath} completed", configuration.ClientClassName, configuration.Language, configuration.OutputPath);
            else
                logger.LogInformation("Client generation skipped, client is up to date");
        }
        catch (Exception ex)
        {
            logger.LogCritical("error generating the client: {exceptionMessage}", ex.Message);
        }
        return logger.LogEntries;
    }
    private static PathItem ConvertOpenApiUrlTreeNodeToPathItem(OpenApiUrlTreeNode node)
    {
        return new PathItem(node.Path, node.Segment, node.Children.Select(x => ConvertOpenApiUrlTreeNodeToPathItem(x.Value)).ToArray());
    }
    protected static string GetAbsolutePath(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        return Path.IsPathRooted(source) || source.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
    }
    protected static string NormalizeSlashesInPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            return path.Replace('/', '\\');
        return path.Replace('\\', '/');
    }
}
