using System.Text.RegularExpressions;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Lock;
using Kiota.Builder.Logging;
using Kiota.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace kiota.Rpc;
internal partial class Server : IServer
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
    private static readonly Lazy<bool> IsConfigPreviewEnabled = new(() => bool.TryParse(Environment.GetEnvironmentVariable("KIOTA_CONFIG_PREVIEW"), out var isPreviewEnabled) && isPreviewEnabled);
    public string GetVersion()
    {
        return KiotaVersion.Current();
    }

    public async Task<List<LogEntry>> UpdateAsync(string output, bool cleanOutput, bool clearCache, CancellationToken cancellationToken)
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
                var config = (GenerationConfiguration)Configuration.Generation.Clone();
                x.lockInfo?.UpdateGenerationConfigurationFromLock(config);
                config.OutputPath = x.lockDirectoryPath;
                config.ClearCache = clearCache;
                config.CleanOutput = cleanOutput;
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
        return await new KiotaBuilder(logger, config, httpClient, IsConfigPreviewEnabled.Value).GenerateClientAsync(cancellationToken);
    }
    public async Task<SearchOperationResult> SearchAsync(string searchTerm, bool clearCache, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaSearcher>();
        var configuration = Configuration.Search;
        configuration.ClearCache = clearCache;
        var searchService = new KiotaSearcher(logger, configuration, httpClient, null, (_) => Task.FromResult(false));
        var results = await searchService.SearchAsync(searchTerm, string.Empty, cancellationToken);
        return new(logger.LogEntries, results);
    }
    public async Task<ManifestResult> GetManifestDetailsAsync(string manifestPath, string apiIdentifier, bool clearCache, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.ClearCache = clearCache;
        configuration.ApiManifestPath = $"{manifestPath}#{apiIdentifier}";
        var builder = new KiotaBuilder(logger, configuration, httpClient, IsConfigPreviewEnabled.Value);
        var manifestResult = await builder.GetApiManifestDetailsAsync(cancellationToken: cancellationToken);
        return new ManifestResult(logger.LogEntries,
                            manifestResult?.Item1,
                            manifestResult?.Item2.ToArray());
    }
    public async Task<ShowResult> ShowAsync(string descriptionPath, string[] includeFilters, string[] excludeFilters, bool clearCache, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.ClearCache = clearCache;
        configuration.OpenAPIFilePath = GetAbsolutePath(descriptionPath);
        var builder = new KiotaBuilder(logger, configuration, httpClient, IsConfigPreviewEnabled.Value);
        var fullUrlTreeNode = await builder.GetUrlTreeNodeAsync(cancellationToken);
        configuration.IncludePatterns = includeFilters.ToHashSet(StringComparer.Ordinal);
        configuration.ExcludePatterns = excludeFilters.ToHashSet(StringComparer.Ordinal);
        var filteredTreeNode = configuration.IncludePatterns.Count != 0 || configuration.ExcludePatterns.Count != 0 ?
                            await new KiotaBuilder(new NoopLogger<KiotaBuilder>(), configuration, httpClient, IsConfigPreviewEnabled.Value).GetUrlTreeNodeAsync(cancellationToken) : // openapi.net seems to have side effects between tree node and the document, we need to drop all references
                            default;
        var filteredPaths = filteredTreeNode is null ? new HashSet<string>() : GetOperationsFromTreeNode(filteredTreeNode).ToHashSet(StringComparer.Ordinal);
        var rootNode = fullUrlTreeNode != null ? ConvertOpenApiUrlTreeNodeToPathItem(fullUrlTreeNode, filteredPaths) : null;
        return new ShowResult(logger.LogEntries, rootNode, builder.OpenApiDocument?.Info?.Title);
    }
    private static IEnumerable<string> GetOperationsFromTreeNode(OpenApiUrlTreeNode node)
    {
        return (node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItems) ?
                                    pathItems.Operations.Select(x => NormalizeOperationNodePath(node, x.Key, true)) :
                                    Enumerable.Empty<string>())
                                    .Union(node.Children.SelectMany(static x => GetOperationsFromTreeNode(x.Value)));
    }
    [GeneratedRegex(@"{\w+}", RegexOptions.Singleline, 500)]
    private static partial Regex indexingNormalizationRegex();
    private static string NormalizeOperationNodePath(OpenApiUrlTreeNode node, OperationType operationType, bool forIndexing = false)
    {
        var name = $"{node.Path}#{operationType.ToString().ToUpperInvariant()}";
        if (forIndexing)
            return indexingNormalizationRegex().Replace(name, "{}");
        return name;
    }
    public async Task<List<LogEntry>> GenerateAsync(string openAPIFilePath, string outputPath, GenerationLanguage language, string[] includePatterns, string[] excludePatterns, string clientClassName, string clientNamespaceName, bool usesBackingStore, bool cleanOutput, bool clearCache, bool excludeBackwardCompatible, string[] disabledValidationRules, string[] serializers, string[] deserializers, string[] structuredMimeTypes, bool includeAdditionalData, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.IncludePatterns = includePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        configuration.ExcludePatterns = excludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        configuration.OpenAPIFilePath = GetAbsolutePath(openAPIFilePath);
        configuration.OutputPath = GetAbsolutePath(outputPath);
        configuration.Language = language;
        configuration.UsesBackingStore = usesBackingStore;
        configuration.CleanOutput = cleanOutput;
        configuration.ClearCache = clearCache;
        configuration.ExcludeBackwardCompatible = excludeBackwardCompatible;
        configuration.IncludeAdditionalData = includeAdditionalData;
        if (disabledValidationRules is not null && disabledValidationRules.Length != 0)
            configuration.DisabledValidationRules = disabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (serializers is not null && serializers.Length != 0)
            configuration.Serializers = serializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (deserializers is not null && deserializers.Length != 0)
            configuration.Deserializers = deserializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (structuredMimeTypes is not null && structuredMimeTypes.Length != 0)
            configuration.StructuredMimeTypes = new(structuredMimeTypes);
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
    public Task<LanguagesInformation> InfoForDescriptionAsync(string descriptionPath, bool clearCache, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(descriptionPath);
        return InfoInternalAsync(descriptionPath, clearCache, cancellationToken);
    }
    private async Task<LanguagesInformation> InfoInternalAsync(string descriptionPath, bool clearCache, CancellationToken cancellationToken)
    {
        var logger = new ForwardedLogger<KiotaBuilder>();
        var configuration = Configuration.Generation;
        configuration.ClearCache = clearCache;
        configuration.OpenAPIFilePath = GetAbsolutePath(descriptionPath);
        var builder = new KiotaBuilder(logger, configuration, httpClient, IsConfigPreviewEnabled.Value);
        var result = await builder.GetLanguagesInformationAsync(cancellationToken);
        if (result is not null) return result;
        return Configuration.Languages;
    }
    private static PathItem ConvertOpenApiUrlTreeNodeToPathItem(OpenApiUrlTreeNode node, HashSet<string> filteredPaths)
    {
        var children = node.Children
                            .Select(x => ConvertOpenApiUrlTreeNodeToPathItem(x.Value, filteredPaths))
                            .Union(node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var openApiPathItems) ?
                                        openApiPathItems.Operations.Select(x => new PathItem(
                                            NormalizeOperationNodePath(node, x.Key),
                                            x.Key.ToString().ToUpperInvariant(),
                                            Array.Empty<PathItem>(),
                                            filteredPaths.Count == 0 || filteredPaths.Contains(NormalizeOperationNodePath(node, x.Key, true)),
                                            true,
                                            x.Value.ExternalDocs?.Url)) :
                                        Enumerable.Empty<PathItem>())
                            .OrderByDescending(static x => x.isOperation)
                            .ThenBy(static x => x.segment, StringComparer.OrdinalIgnoreCase)
                            .ToArray();
        return new PathItem(node.Path, node.DeduplicatedSegment(), children, filteredPaths.Count == 0 || Array.Exists(children, static x => x.isOperation) && children.Where(static x => x.isOperation).All(static x => x.selected));
    }
    private static string GetAbsolutePath(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        return Path.IsPathRooted(source) || source.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
    }
    private static string NormalizeSlashesInPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            return path.Replace('/', '\\');
        return path.Replace('\\', '/');
    }
}
