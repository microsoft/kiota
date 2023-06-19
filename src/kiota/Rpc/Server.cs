using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Kiota.Builder.Logging;
using Kiota.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;

namespace kiota.Rpc;
internal class Server : IServer
{
    protected KiotaConfiguration Configuration
    {
        get => (KiotaConfiguration)ConfigurationFactory.Value.Clone();
    }
    private readonly Lazy<KiotaConfiguration> ConfigurationFactory = new(() =>
    {
        var builder = new ConfigurationBuilder();
        using var defaultStream = new MemoryStream(Kiota.Generated.KiotaAppSettings.Default());
        var configuration = builder.AddJsonStream(defaultStream)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables(prefix: "KIOTA_")
                .Build();
        var configObject = new KiotaConfiguration();
        configObject.BindConfiguration(configuration);
        return configObject;
    });
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
                var config = Configuration.Generation;
                x.lockInfo?.UpdateGenerationConfigurationFromLock(config);
                config.OutputPath = x.lockDirectoryPath;
                return config;
            }).ToArray();
            _ = await Task.WhenAll(configurations
                                    .Select(x => GenerateClientAsync(x, logger, cancellationToken)));
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
    private static async Task<bool> GenerateClientAsync(GenerationConfiguration config, ILogger<KiotaBuilder> globalLogger, CancellationToken cancellationToken)
    {
        using var fileLogger = new FileLogLogger<KiotaBuilder>(config.OutputPath, LogLevel.Warning);
        var logger = new AggregateLogger<KiotaBuilder>(globalLogger, fileLogger);
        return await new KiotaBuilder(logger, config, httpClient).GenerateClientAsync(cancellationToken);
    }
    public async Task<SearchOperationResult> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaSearcher>();
        var configuration = Configuration.Search;
        var searchService = new KiotaSearcher(logger, configuration, httpClient, null, (_) => Task.FromResult(false));
        var results = await searchService.SearchAsync(searchTerm, string.Empty, cancellationToken);
        return new(logger.LogEntries, results);
    }
    public async Task<ShowResult> ShowAsync(string descriptionPath, string[] includeFilters, string[] excludeFilters, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.IncludePatterns = includeFilters.ToHashSet();
        configuration.ExcludePatterns = excludeFilters.ToHashSet();
        configuration.OpenAPIFilePath = GetAbsolutePath(descriptionPath);
        var urlTreeNode = await new KiotaBuilder(logger, configuration, httpClient).GetUrlTreeNodeAsync(cancellationToken);
        var rootNode = urlTreeNode != null ? ConvertOpenApiUrlTreeNodeToPathItem(urlTreeNode) : null;
        return new ShowResult(logger.LogEntries, rootNode);
    }
    public async Task<List<LogEntry>> GenerateAsync(string descriptionPath, string output, GenerationLanguage language, string[] includeFilters, string[] excludeFilters, string clientClassName, string clientNamespaceName, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.IncludePatterns = includeFilters.ToHashSet();
        configuration.ExcludePatterns = excludeFilters.ToHashSet();
        configuration.OpenAPIFilePath = GetAbsolutePath(descriptionPath);
        configuration.OutputPath = GetAbsolutePath(output);
        configuration.Language = language;
        if (!string.IsNullOrEmpty(clientClassName))
            configuration.ClientClassName = clientClassName;
        if (!string.IsNullOrEmpty(clientNamespaceName))
            configuration.ClientNamespaceName = clientNamespaceName;
        try
        {
            var result = await GenerateClientAsync(configuration, logger, cancellationToken);
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
    public LanguagesInformation Info()
    {
        return Configuration.Languages;
    }
    public Task<LanguagesInformation> InfoAsync(GenerationLanguage language, string descriptionPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(descriptionPath);
        return InfoInternalAsync(language, descriptionPath, cancellationToken);
    }
    private async Task<LanguagesInformation> InfoInternalAsync(GenerationLanguage language, string descriptionPath, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.OpenAPIFilePath = GetAbsolutePath(descriptionPath);
        configuration.Language = language;
        var builder = new KiotaBuilder(logger, configuration, httpClient);
        var result = await builder.GetLanguagesInformationAsync(cancellationToken);
        if (result is not null) return result;
        return Configuration.Languages;
    }
    private static PathItem ConvertOpenApiUrlTreeNodeToPathItem(OpenApiUrlTreeNode node)
    {
        return new PathItem(node.Path, node.Segment, node.Children
                                                        .Select(static x => ConvertOpenApiUrlTreeNodeToPathItem(x.Value))
                                                        .Union(node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var openApiPathItems) ?
                                                                    openApiPathItems.Operations.Select(x => new PathItem(
                                                                        $"{node.Path}#{x.Key.ToString().ToUpperInvariant()}",
                                                                        x.Key.ToString().ToUpperInvariant(),
                                                                        Array.Empty<PathItem>(),
                                                                        true,
                                                                        x.Value.ExternalDocs?.Url)) :
                                                                    Enumerable.Empty<PathItem>())
                                                        .OrderByDescending(static x => x.isOperation)
                                                        .ThenBy(static x => x.segment, StringComparer.OrdinalIgnoreCase)
                                                        .ToArray());
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
