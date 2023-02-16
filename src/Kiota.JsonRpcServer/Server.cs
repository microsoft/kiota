using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Kiota.Generated;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;

namespace Kiota.JsonRpcServer;
internal class Server
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
                                                                            .ContinueWith(t => (lockInfo: t.Result, lockDirectoryPath: x), cancellationToken)));
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
                logger.LogInformation($"Update of {lockInfo?.ClientClassName} client for {lockInfo?.Language} at {lockDirectoryPath} completed");
            logger.LogInformation($"Update of {locks.Length} clients completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogCritical("error updating the client: {exceptionMessage}", ex.Message);
        }
        return logger.LogEntries;
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
