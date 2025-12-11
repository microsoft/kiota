using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Globbing;
using Kiota.Builder.Caching;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;
using Kiota.Builder.EqualityComparers;
using Kiota.Builder.Exceptions;
using Kiota.Builder.Export;
using Kiota.Builder.Extensions;
using Kiota.Builder.Logging;
using Kiota.Builder.Manifest;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.Plugins;
using Kiota.Builder.Refiners;
using Kiota.Builder.Settings;
using Kiota.Builder.WorkspaceManagement;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Reader;
using DomHttpMethod = Kiota.Builder.CodeDOM.HttpMethod;
using NetHttpMethod = System.Net.Http.HttpMethod;
[assembly: InternalsVisibleTo("Kiota.Builder.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100957cb48387b2a5f54f5ce39255f18f26d32a39990db27cf48737afc6bc62759ba996b8a2bfb675d4e39f3d06ecb55a178b1b4031dcb2a767e29977d88cce864a0d16bfc1b3bebb0edf9fe285f10fffc0a85f93d664fa05af07faa3aad2e545182dbf787e3fd32b56aca95df1a3c4e75dec164a3f1a4c653d971b01ffc39eb3c4")]

namespace Kiota.Builder;

public partial class KiotaBuilder
{
    private readonly ILogger<KiotaBuilder> logger;
    private readonly GenerationConfiguration config;
    private readonly ParallelOptions parallelOptions;
    private readonly HttpClient httpClient;
    private OpenApiDocument? openApiDocument;
    private readonly ISettingsManagementService settingsFileManagementService;
    internal void SetOpenApiDocument(OpenApiDocument document) => openApiDocument = document ?? throw new ArgumentNullException(nameof(document));

    public KiotaBuilder(ILogger<KiotaBuilder> logger, GenerationConfiguration config, HttpClient client, bool useKiotaConfig = false, ISettingsManagementService? settingsManagementService = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(client);
        this.logger = logger;
        this.config = config;
        httpClient = client;
        parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = config.MaxDegreeOfParallelism,
        };
        var workingDirectory = Directory.GetCurrentDirectory();
        workspaceManagementService = new WorkspaceManagementService(logger, client, useKiotaConfig, workingDirectory);
        this.useKiotaConfig = useKiotaConfig;
        openApiDocumentDownloadService = new OpenApiDocumentDownloadService(client, logger);
        settingsFileManagementService = settingsManagementService ?? new SettingsFileManagementService();
    }
    private readonly OpenApiDocumentDownloadService openApiDocumentDownloadService;
    private readonly bool useKiotaConfig;
    private async Task CleanOutputDirectoryAsync(CancellationToken cancellationToken)
    {
        if (config.CleanOutput && Directory.Exists(config.OutputPath))
        {
            logger.LogInformation("Cleaning output directory {Path}", config.OutputPath);
            // not using Directory.Delete on the main directory because it's locked when mapped in a container
            foreach (var subDir in Directory.EnumerateDirectories(config.OutputPath))
                Directory.Delete(subDir, true);
            if (!config.NoWorkspace)
            {
                await workspaceManagementService.BackupStateAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
            }
            foreach (var subFile in Directory.EnumerateFiles(config.OutputPath)
                                            .Where(static x => !x.EndsWith(FileLogLogger.LogFileName, StringComparison.OrdinalIgnoreCase)))
                File.Delete(subFile);
        }
    }
    public async Task<(OpenApiUrlTreeNode?, OpenApiDiagnostic?)> GetUrlTreeNodeAsync(CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        var inputPath = config.OpenAPIFilePath;
        var (_, openApiTree, _, openApiDiagnostic) = await GetTreeNodeInternalAsync(inputPath, false, sw, cancellationToken).ConfigureAwait(false);
        return (openApiTree, openApiDiagnostic);
    }
    public OpenApiDocument? OpenApiDocument => openApiDocument;
    private static string NormalizeApiManifestPath(RequestInfo request, string? baseUrl)
    {
        var rawValue = $"{request.UriTemplate}{(request.Method is null ? string.Empty : "#")}{request.Method?.ToUpperInvariant()}";
        if (!string.IsNullOrEmpty(baseUrl) && rawValue.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            rawValue = rawValue[baseUrl.Length..];
        if (!rawValue.StartsWith('/'))
            rawValue = '/' + rawValue;
        return rawValue.Split('?', StringSplitOptions.RemoveEmptyEntries)[0];
    }
    public async Task<Tuple<string, IEnumerable<string>>?> GetApiManifestDetailsAsync(bool skipErrorLog = false, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Api manifest path: {ApiManifestPath}", config.ApiManifestPath);
            var pathParts = config.ApiManifestPath.Split(manifestPathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var manifestPath = pathParts[0];
            var apiIdentifier = pathParts.Length > 1 ? pathParts[1] : string.Empty;
            var manifestManagementService = new ManifestManagementService();
            var documentCachingProvider = new DocumentCachingProvider(httpClient, logger);
#pragma warning disable CA2000
            using var manifestFileContent = manifestPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) switch
            {
                false => File.OpenRead(manifestPath),
                true => await documentCachingProvider.GetDocumentAsync(new Uri(manifestPath), "manifests", "manifest.json", cancellationToken: cancellationToken).ConfigureAwait(false)
            };
#pragma warning restore CA2000
            var manifest = await manifestManagementService.DeserializeManifestDocumentAsync(manifestFileContent).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("The manifest could not be decoded");

            var apiDependency = (manifest.ApiDependencies.Count, string.IsNullOrEmpty(apiIdentifier)) switch
            {
                (0, _) => throw new InvalidOperationException("The manifest contains no APIs"),
                (1, _) => manifest.ApiDependencies.First().Value,
                (_, true) => throw new InvalidOperationException("The manifest contains multiple APIs, please specify the API identifier"),
                (_, false) => manifest.ApiDependencies.TryGetValue(apiIdentifier, out var apiDep) ? apiDep : throw new InvalidOperationException($"The manifest does not contain the API {apiIdentifier}")
            };

            if (apiDependency.ApiDescriptionUrl is null)
                throw new InvalidOperationException("The manifest does not contain an API description URL");

            return new Tuple<string, IEnumerable<string>>(apiDependency.ApiDescriptionUrl,
                                apiDependency.Requests.Select(x => NormalizeApiManifestPath(x, apiDependency.ApiDeploymentBaseUrl)).ToArray());
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            if (!skipErrorLog)
                logger.LogCritical("error getting the API manifest: {ExceptionMessage}", ex.Message);
            return null;
        }
    }
    private async Task<(int, OpenApiUrlTreeNode?, bool, OpenApiDiagnostic?)> GetTreeNodeInternalAsync(string inputPath, bool generating, Stopwatch sw, CancellationToken cancellationToken)
    {
        logger.LogDebug("kiota version {Version}", Generated.KiotaVersion.Current());
        var stepId = 0;
        if (config.ShouldGetApiManifest)
        {
            sw.Start();
            var manifestDetails = await GetApiManifestDetailsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (manifestDetails is not null)
            {
                inputPath = manifestDetails.Item1;
                if (config.IncludePatterns.Count == 0)
                    config.IncludePatterns = manifestDetails.Item2.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            StopLogAndReset(sw, $"step {++stepId} - getting the manifest - took");
        }
        sw.Start();
#pragma warning disable CA2007
        await using var input = await LoadStreamAsync(inputPath, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007
        if (input.Length == 0)
            return (0, null, false, null);
        StopLogAndReset(sw, $"step {++stepId} - reading the stream - took");

        // Parse OpenAPI
        sw.Start();
        var readResult = await CreateOpenApiDocumentWithResultAsync(input, generating, cancellationToken).ConfigureAwait(false);
        openApiDocument = readResult?.Document;
        StopLogAndReset(sw, $"step {++stepId} - parsing the document - took");

        sw.Start();
        UpdateConfigurationFromOpenApiDocument();
        StopLogAndReset(sw, $"step {++stepId} - updating generation configuration from kiota extension - took");

        OpenApiUrlTreeNode? openApiTree = null;
        var shouldGenerate = !config.SkipGeneration;
        if (openApiDocument != null)
        {
            // filter paths
            sw.Start();
            FilterPathsByPatterns(openApiDocument);
            StopLogAndReset(sw, $"step {++stepId} - filtering API paths with patterns - took");
            SetApiRootUrl();

            // Should Generate
            sw.Start();
            if (!config.NoWorkspace)
            {
                var hashCode = await openApiDocument.GetHashCodeAsync(cancellationToken).ConfigureAwait(false);
                shouldGenerate &= await workspaceManagementService.ShouldGenerateAsync(config, hashCode, cancellationToken).ConfigureAwait(false);
            }
            StopLogAndReset(sw, $"step {++stepId} - checking whether the output should be updated - took");

            if (shouldGenerate && generating)
            {
                modelNamespacePrefixToTrim = GetDeeperMostCommonNamespaceNameForModels(openApiDocument);
            }

            // OperationId cleanup in the event that we are generating plugins
            if (config.IsPluginConfiguration)
            {
                CleanupOperationIdForPlugins(openApiDocument);
            }

            // Create Uri Space of API
            sw.Start();
            openApiTree = CreateUriSpace(openApiDocument);
            StopLogAndReset(sw, $"step {++stepId} - create uri space - took");
        }

        return (stepId, openApiTree, shouldGenerate, readResult?.Diagnostic);
    }
    private void UpdateConfigurationFromOpenApiDocument()
    {
        if (openApiDocument == null ||
            GetLanguagesInformationInternal() is not LanguagesInformation languagesInfo) return;

        config.UpdateConfigurationFromLanguagesInformation(languagesInfo);
    }

    public async Task<LanguagesInformation?> GetLanguagesInformationAsync(CancellationToken cancellationToken)
    {
        await GetTreeNodeInternalAsync(config.OpenAPIFilePath, false, new Stopwatch(), cancellationToken).ConfigureAwait(false);

        return GetLanguagesInformationInternal();
    }
    private LanguagesInformation? GetLanguagesInformationInternal()
    {
        if (openApiDocument is null || openApiDocument.Extensions is null)
            return null;
        if (openApiDocument.Extensions.TryGetValue(OpenApiKiotaExtension.Name, out var ext) && ext is OpenApiKiotaExtension kiotaExt)
            return kiotaExt.LanguagesInformation;
        return null;
    }
    /// <summary>
    /// Generates the API plugins from the OpenAPI document
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Whether the generated plugin was updated or not</returns>
    public async Task<bool> GeneratePluginAsync(CancellationToken cancellationToken, Boolean handleMultipleFiles = true)
    {
        return await GenerateConsumerAsync(async (sw, stepId, openApiTree, CancellationToken) =>
        {
            if (openApiDocument is null || openApiTree is null)
                throw new InvalidOperationException("The OpenAPI document and the URL tree must be loaded before generating the plugins");
            // generate plugin
            sw.Start();
            var pluginsService = new PluginsGenerationService(openApiDocument, openApiTree, config, Directory.GetCurrentDirectory(), logger);
            // Handle the multiple files generation
            if (handleMultipleFiles)
            {
                pluginsService.DownloadService = openApiDocumentDownloadService;
                await pluginsService.GenerateAndMergeMultipleManifestsAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await pluginsService.GenerateManifestAsync(cancellationToken).ConfigureAwait(false);
            }

            StopLogAndReset(sw, $"step {++stepId} - generate plugin - took");
            return stepId;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates the code from the OpenAPI document
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Whether the generated code was updated or not</returns>
    public async Task<bool> GenerateClientAsync(CancellationToken cancellationToken)
    {
        return await GenerateConsumerAsync(async (sw, stepId, openApiTree, CancellationToken) =>
        {
            // Create Source Model
            sw.Start();
            var generatedCode = CreateSourceModel(openApiTree);
            StopLogAndReset(sw, $"step {++stepId} - create source model - took");

            // RefineByLanguage
            sw.Start();
            await ApplyLanguageRefinementAsync(config, generatedCode, cancellationToken).ConfigureAwait(false);
            StopLogAndReset(sw, $"step {++stepId} - refine by language - took");

            if (config.ExportPublicApi)
            {
                // Generate public API export
                sw.Start();
                var fileStream = File.Create(Path.Combine(config.OutputPath, PublicApiExportService.DomExportFileName));
                await using (fileStream.ConfigureAwait(false))
                {
                    await new PublicApiExportService(config).SerializeDomAsync(fileStream, generatedCode, cancellationToken).ConfigureAwait(false);
                }
                StopLogAndReset(sw, $"step {++stepId} - generated public API export - took");
            }

            // Write language source
            sw.Start();
            await CreateLanguageSourceFilesAsync(config.Language, generatedCode, cancellationToken).ConfigureAwait(false);
            StopLogAndReset(sw, $"step {++stepId} - writing files - took");

            if (config.Language == GenerationLanguage.HTTP && openApiDocument is not null)
            {
                sw.Start();
                await settingsFileManagementService.WriteSettingsFileAsync(config.OutputPath, openApiDocument, cancellationToken).ConfigureAwait(false);
                StopLogAndReset(sw, $"step {++stepId} - generating settings file for HTTP authentication - took");
            }
            return stepId;
        }, cancellationToken).ConfigureAwait(false);
    }
    private async Task<bool> GenerateConsumerAsync(Func<Stopwatch, int, OpenApiUrlTreeNode?, CancellationToken, Task<int>> innerGenerationSteps, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        // Read input stream
        var inputPath = config.OpenAPIFilePath;

        if (!config.NoWorkspace && config.Operation is ConsumerOperation.Add && await workspaceManagementService.IsConsumerPresentAsync(config.ClientClassName, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException($"The client {config.ClientClassName} already exists in the workspace");

        try
        {
            await CleanOutputDirectoryAsync(cancellationToken).ConfigureAwait(false);
            // doing this verification at the beginning to give immediate feedback to the user
            Directory.CreateDirectory(config.OutputPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not open/create output directory {config.OutputPath}, reason: {ex.Message}", ex);
        }
        try
        {
            var (stepId, openApiTree, shouldGenerate, _) = await GetTreeNodeInternalAsync(inputPath, true, sw, cancellationToken).ConfigureAwait(false);

            if (shouldGenerate)
            {
                stepId = await innerGenerationSteps(sw, stepId, openApiTree, cancellationToken).ConfigureAwait(false);

                await FinalizeWorkspaceAsync(sw, stepId, openApiTree, inputPath, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation("No changes detected, skipping generation");
                if (config.Operation is ConsumerOperation.Add or ConsumerOperation.Edit && config.SkipGeneration)
                {
                    await FinalizeWorkspaceAsync(sw, stepId, openApiTree, inputPath, cancellationToken).ConfigureAwait(false);
                }
                return false;
            }
        }
        catch
        {
            if (!config.NoWorkspace)
            {
                await workspaceManagementService.RestoreStateAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
            }
            throw;
        }
        return true;
    }
    private async Task FinalizeWorkspaceAsync(Stopwatch sw, int stepId, OpenApiUrlTreeNode? openApiTree, string inputPath, CancellationToken cancellationToken)
    {
        // Write lock file
        sw.Start();
        using var descriptionStream = !isDescriptionFromWorkspaceCopy ? await LoadStreamAsync(inputPath, cancellationToken).ConfigureAwait(false) : Stream.Null;
        var hashCode = openApiDocument switch
        {
            null => string.Empty,
            _ => await openApiDocument.GetHashCodeAsync(cancellationToken).ConfigureAwait(false),
        };
        if (!config.NoWorkspace)
            await workspaceManagementService.UpdateStateFromConfigurationAsync(config, hashCode, openApiTree?.GetRequestInfo().ToDictionary(static x => x.Key, static x => x.Value) ?? [], descriptionStream, cancellationToken).ConfigureAwait(false);
        StopLogAndReset(sw, $"step {++stepId} - writing lock file - took");
    }
    private readonly WorkspaceManagementService workspaceManagementService;
    private static readonly GlobComparer globComparer = new();
    [GeneratedRegex(@"([\/\\])\{[\w\d-]+\}([\/\\])?", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex MultiIndexSameLevelCleanupRegex();
    internal static string ReplaceAllIndexesWithWildcard(string path, uint depth = 10) => depth == 0 ? path : ReplaceAllIndexesWithWildcard(MultiIndexSameLevelCleanupRegex().Replace(path, "$1{*}$2"), depth - 1); // the bound needs to be greedy to avoid replacing anything else than single path parameters
    private static Dictionary<Glob, HashSet<NetHttpMethod>> GetFilterPatternsFromConfiguration(HashSet<string> configPatterns)
    {
        return configPatterns.Select(static x =>
        {
            var splat = x.Split('#', StringSplitOptions.RemoveEmptyEntries);
            var glob = Glob.Parse(ReplaceAllIndexesWithWildcard(splat[0]));
            var operationTypes = splat.Length > 1 ?
                                    splat[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(static y => NetHttpMethod.Parse(y.Trim()) is { } op ? op : default(NetHttpMethod)) :
                                    [];
            return (glob, operationTypes);
        }).GroupBy(static x => x.glob, globComparer)
        .ToDictionary(static x => x.Key,
                    static x => new HashSet<NetHttpMethod>(x.SelectMany(static y => y.operationTypes)
                                                            .OfType<NetHttpMethod>()),
                    globComparer);
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]+", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex PluginOperationIdCleanupRegex();
    internal static void CleanupOperationIdForPlugins(OpenApiDocument document)
    {
        if (document.Paths is null) return;
        foreach (var (pathItem, operation) in document.Paths.SelectMany(static path => path.Value.Operations?.Select(value => new Tuple<string, KeyValuePair<NetHttpMethod, OpenApiOperation>>(path.Key, value)) ?? []))
        {
            if (string.IsNullOrEmpty(operation.Value.OperationId))
            {
                var stringBuilder = new StringBuilder();
                foreach (var segment in pathItem.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (segment.IsPathSegmentWithSingleSimpleParameter())
                        stringBuilder.Append("item");
                    else if (!string.IsNullOrEmpty(segment.Trim()))
                        stringBuilder.Append(segment.ToLowerInvariant());
                    stringBuilder.Append('_');
                }
                stringBuilder.Append(operation.Key.ToString().ToLowerInvariant());
                operation.Value.OperationId = stringBuilder.ToString();
            }
            else
            {
                operation.Value.OperationId = PluginOperationIdCleanupRegex().Replace(operation.Value.OperationId, "_");//replace non-alphanumeric characters with _
            }
        }
    }
    internal void FilterPathsByPatterns(OpenApiDocument doc)
    {
        var includePatterns = GetFilterPatternsFromConfiguration(config.IncludePatterns);
        var excludePatterns = GetFilterPatternsFromConfiguration(config.ExcludePatterns);
        if (config.PatternsOverride.Count != 0)
        { // loading the patterns from the manifest as we don't want to take the user input one and have new operation creep in from the description being updated since last generation
            includePatterns = GetFilterPatternsFromConfiguration(config.PatternsOverride);
            excludePatterns = [];
        }
        if (includePatterns.Count == 0 && excludePatterns.Count == 0) return;

        var nonOperationIncludePatterns = includePatterns.Where(static x => x.Value.Count == 0).Select(static x => x.Key).ToList();
        var nonOperationExcludePatterns = excludePatterns.Where(static x => x.Value.Count == 0).Select(static x => x.Key).ToList();
        var operationIncludePatterns = includePatterns.Where(static x => x.Value.Count != 0).ToList();

        if (nonOperationIncludePatterns.Count != 0 || nonOperationExcludePatterns.Count != 0)
            doc.Paths.Keys.Where(x => (nonOperationIncludePatterns.Count != 0 && !nonOperationIncludePatterns.Any(y => y.IsMatch(x)) ||
                                nonOperationExcludePatterns.Count != 0 && nonOperationExcludePatterns.Any(y => y.IsMatch(x))) &&
                                !operationIncludePatterns.Any(y => y.Key.IsMatch(x))) // so we don't trim paths that are going to be filtered by operation
            .ToList()
            .ForEach(x => doc.Paths.Remove(x));

        var operationExcludePatterns = excludePatterns.Where(static x => x.Value.Count != 0).ToList();

        if (operationIncludePatterns.Count != 0 || operationExcludePatterns.Count != 0)
        {
            foreach (var path in doc.Paths.Where(x => !nonOperationIncludePatterns.Any(y => y.IsMatch(x.Key))))
            {
                var pathString = path.Key;
                path.Value.Operations?.Keys.Where(x => operationIncludePatterns.Count != 0 && !operationIncludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x)))
                .ToList()
                .ForEach(x => path.Value.Operations.Remove(x));
            }
            foreach (var path in doc.Paths)
            {
                var pathString = path.Key;
                path.Value.Operations?.Keys.Where(x => operationExcludePatterns.Count != 0 && operationExcludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x)))
                .ToList()
                .ForEach(x => path.Value.Operations.Remove(x));
            }
            foreach (var path in doc.Paths.Where(static x => x.Value.Operations is null || x.Value.Operations.Count == 0).ToList())
                doc.Paths.Remove(path.Key);
        }

        if (!doc.Paths.Any())
            logger.LogWarning("No paths were found matching the provided patterns. Check your configuration.");
    }
    internal void SetApiRootUrl()
    {
        if (openApiDocument is not null && openApiDocument.GetAPIRootUrl(config.OpenAPIFilePath) is string candidateUrl)
        {
            config.ApiRootUrl = candidateUrl;
            if (!config.IsPluginConfiguration)
            {
                logger.LogInformation("Client root URL set to {ApiRootUrl}", candidateUrl);
            }
        }
        else
            logger.LogWarning("No server url found in the OpenAPI document. The base url will need to be set when using the client.");
    }
    private void StopLogAndReset(Stopwatch sw, string prefix)
    {
        sw.Stop();
        logger.LogDebug("{Prefix} {SwElapsed}", prefix, sw.Elapsed);
        sw.Reset();
    }
    private bool isDescriptionFromWorkspaceCopy;
    private async Task<Stream> LoadStreamAsync(string inputPath, CancellationToken cancellationToken)
    {
        var (input, isCopy) = await openApiDocumentDownloadService.LoadStreamAsync(inputPath, config, workspaceManagementService, useKiotaConfig, cancellationToken).ConfigureAwait(false);
        isDescriptionFromWorkspaceCopy = isCopy;
        return input;
    }

    internal const char ForwardSlash = '/';
    internal Task<OpenApiDocument?> CreateOpenApiDocumentAsync(Stream input, bool generating = false, CancellationToken cancellationToken = default)
    {
        return openApiDocumentDownloadService.GetDocumentFromStreamAsync(input, config, generating, cancellationToken);
    }
    internal Task<ReadResult?> CreateOpenApiDocumentWithResultAsync(Stream input, bool generating = false, CancellationToken cancellationToken = default)
    {
        return openApiDocumentDownloadService.GetDocumentWithResultFromStreamAsync(input, config, generating, cancellationToken);
    }
    public static string GetDeeperMostCommonNamespaceNameForModels(OpenApiDocument document)
    {
        if (!(document?.Components?.Schemas is { Count: > 0 })) return string.Empty;
        var distinctKeys = document.Components
                                .Schemas
                                .Keys
                                .Select(x => string.Join(NsNameSeparator, x.Split(NsNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                                .SkipLast(1)))
                                .Where(static x => !string.IsNullOrEmpty(x))
                                .Distinct()
                                .OrderByDescending(static x => x.Count(static y => y == NsNameSeparator))
                                .ToArray();
        if (distinctKeys.FirstOrDefault() is not string longestKey) return string.Empty;
        var candidate = string.Empty;
        var longestKeySegments = longestKey.Split(NsNameSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in longestKeySegments)
        {
            var testValue = (candidate + NsNameSeparator + segment).Trim(NsNameSeparator);
            if (Array.TrueForAll(distinctKeys, x => x.StartsWith(testValue, StringComparison.OrdinalIgnoreCase)))
                candidate = testValue;
            else
                break;
        }

        return candidate;
    }

    /// <summary>
    /// Translate OpenApi PathItems into a tree structure that will define the classes
    /// </summary>
    /// <param name="doc">OpenAPI Document of the API to be processed</param>
    /// <returns>Root node of the API URI space</returns>
    public OpenApiUrlTreeNode CreateUriSpace(OpenApiDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        openApiDocument ??= doc;

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var node = OpenApiUrlTreeNode.Create(doc, Constants.DefaultOpenApiLabel);
        node.MergeIndexNodesAtSameLevel(logger);
        stopwatch.Stop();
        logger.LogTrace("{Timestamp}ms: Created UriSpace tree", stopwatch.ElapsedMilliseconds);
        return node;
    }
    private CodeNamespace? rootNamespace;
    private CodeNamespace? modelsNamespace;
    private string? modelNamespacePrefixToTrim;

    /// <summary>
    /// Convert UriSpace of OpenApiPathItems into conceptual SDK Code model
    /// </summary>
    /// <param name="root">Root OpenApiUriSpaceNode of API to be generated</param>
    /// <returns></returns>
    public CodeNamespace CreateSourceModel(OpenApiUrlTreeNode? root)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        rootNamespace = CodeNamespace.InitRootNamespace();
        var codeNamespace = rootNamespace.AddNamespace(config.ClientNamespaceName);
        modelsNamespace = rootNamespace.AddNamespace(config.ModelsNamespaceName);
        InitializeInheritanceIndex();
        StopLogAndReset(stopwatch, nameof(InitializeInheritanceIndex));
        if (root != null)
        {
            CreateRequestBuilderClass(codeNamespace, root, root);
            StopLogAndReset(stopwatch, nameof(CreateRequestBuilderClass));
            stopwatch.Start();
            MapTypeDefinitions(codeNamespace);
            StopLogAndReset(stopwatch, nameof(MapTypeDefinitions));
            TrimInheritedModels();
            StopLogAndReset(stopwatch, nameof(TrimInheritedModels));
            CleanUpInternalState();
            StopLogAndReset(stopwatch, nameof(CleanUpInternalState));

            logger.LogTrace("{Timestamp}ms: Created source model with {Count} classes", stopwatch.ElapsedMilliseconds, codeNamespace.GetChildElements(true).Count());
        }

        return rootNamespace;
    }

    private void AddOperationSecurityRequirementToDOM(OpenApiOperation operation, CodeClass codeClass)
    {
        if (openApiDocument is null)
        {
            logger.LogWarning("OpenAPI document is null");
            return;
        }

        if (operation.Security == null || operation.Security.Count == 0 || openApiDocument.Components?.SecuritySchemes is null)
            return;

        var securitySchemes = openApiDocument.Components.SecuritySchemes;
        foreach (var securityRequirement in operation.Security)
        {
            foreach (var scheme in securityRequirement.Keys)
            {
                if (!string.IsNullOrEmpty(scheme.Reference.Id) && securitySchemes.TryGetValue(scheme.Reference.Id, out var securityScheme))
                {
                    AddSecurity(codeClass, securityScheme);
                }
            }
        }
    }

    private void AddSecurity(CodeClass codeClass, IOpenApiSecurityScheme openApiSecurityScheme)
    {
        if (openApiSecurityScheme.Type is not null)
            codeClass.AddProperty(
                new CodeProperty
                {
                    Type = new CodeType { Name = openApiSecurityScheme.Type.Value.ToString(), IsExternal = true },
                    Kind = CodePropertyKind.Headers
                }
            );
    }

    /// <summary>
    /// Manipulate CodeDOM for language specific issues
    /// </summary>
    /// <param name="config"></param>
    /// <param name="generatedCode"></param>
    /// <param name="token"></param>
    public async Task ApplyLanguageRefinementAsync(GenerationConfiguration config, CodeNamespace generatedCode, CancellationToken token)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await ILanguageRefiner.RefineAsync(config, generatedCode, token).ConfigureAwait(false);

        stopwatch.Stop();
        logger.LogDebug("{Timestamp}ms: Language refinement applied", stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Iterate through Url Space and create request builder classes for each node in the tree
    /// </summary>
    /// <param name="root">Root node of URI space from the OpenAPI described API</param>
    /// <returns>A CodeNamespace object that contains request builder classes for the Uri Space</returns>

    public async Task CreateLanguageSourceFilesAsync(GenerationLanguage language, CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        var languageWriter = LanguageWriter.GetLanguageWriter(language, config.OutputPath, config.ClientNamespaceName, config.UsesBackingStore, config.ExcludeBackwardCompatible);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var codeRenderer = CodeRenderer.GetCodeRender(config);
        await codeRenderer.RenderCodeNamespaceToFilePerClassAsync(languageWriter, generatedCode, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogTrace("{Timestamp}ms: Files written to {Path}", stopwatch.ElapsedMilliseconds, config.OutputPath);
    }
    private const string RequestBuilderSuffix = "RequestBuilder";
    private const string ItemRequestBuilderSuffix = "ItemRequestBuilder";
    private const string VoidType = "void";
    private const string CoreInterfaceType = "IRequestAdapter";
    private const string RequestAdapterParameterName = "requestAdapter";
    private const string ConstructorMethodName = "constructor";
    internal const string UntypedNodeName = "UntypedNode";
    internal const string TrailingSlashPlaceholder = "EmptyPathSegment";
    /// <summary>
    /// Create a CodeClass instance that is a request builder class for the OpenApiUrlTreeNode
    /// </summary>
    private void CreateRequestBuilderClass(CodeNamespace currentNamespace, OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode rootNode)
    {
        // Determine Class Name
        CodeClass codeClass;
        var isApiClientClass = currentNode == rootNode;
        if (isApiClientClass)
            codeClass = currentNamespace.AddClass(new CodeClass
            {
                Name = config.ClientClassName,
                Kind = CodeClassKind.RequestBuilder,
                Documentation = new()
                {
                    DescriptionTemplate = "The main entry point of the SDK, exposes the configuration and the fluent API."
                },
            }).First();
        else
        {
            var targetNS = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNamespace.EnsureItemNamespace() : currentNamespace;
            var className = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, ItemRequestBuilderSuffix, placeholder: TrailingSlashPlaceholder) : currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, RequestBuilderSuffix, placeholder: TrailingSlashPlaceholder);
            codeClass = targetNS.AddClass(new CodeClass
            {
                Name = currentNamespace.Name.EndsWith(OpenApiUrlTreeNodeExtensions.ReservedItemNameEscaped, StringComparison.OrdinalIgnoreCase) ? className.CleanupSymbolName().Replace(OpenApiUrlTreeNodeExtensions.ReservedItemName, OpenApiUrlTreeNodeExtensions.ReservedItemNameEscaped, StringComparison.OrdinalIgnoreCase) : className.CleanupSymbolName(),
                Kind = CodeClassKind.RequestBuilder,
                Documentation = new()
                {
                    DescriptionTemplate = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
                },
            }).First();
        }

        logger.LogTrace("Creating class {Class}", codeClass.Name);

        // Add properties for children
        foreach (var child in currentNode.Children.Select(static x => x.Value))
        {
            var propIdentifier = child.GetNavigationPropertyName(config.StructuredMimeTypes, placeholder: TrailingSlashPlaceholder);
            var propType = child.GetNavigationPropertyName(config.StructuredMimeTypes, child.DoesNodeBelongToItemSubnamespace() ? ItemRequestBuilderSuffix : RequestBuilderSuffix, placeholder: TrailingSlashPlaceholder);
            if (child.Segment.Equals(OpenApiUrlTreeNodeExtensions.ReservedItemName, StringComparison.OrdinalIgnoreCase) && !child.DoesNodeBelongToItemSubnamespace())
                propType = propType.Replace(OpenApiUrlTreeNodeExtensions.ReservedItemName, OpenApiUrlTreeNodeExtensions.ReservedItemNameEscaped, StringComparison.OrdinalIgnoreCase);

            if (child.IsPathSegmentWithSingleSimpleParameter())
            {
                var indexerParameterType = GetIndexerParameter(child);
                codeClass.AddIndexer(CreateIndexer($"{propIdentifier}-indexer", propType, indexerParameterType, child, currentNode));
            }
            else if (child.IsComplexPathMultipleParameters())
                CreateMethod(propIdentifier, propType, codeClass, child);
            else
            {
                var description = child.GetPathItemDescription(Constants.DefaultOpenApiLabel).CleanupDescription();
                var prop = CreateProperty(propIdentifier, propType, kind: CodePropertyKind.RequestBuilder); // we should add the type definition here but we can't as it might not have been generated yet
                if (prop is null)
                {
                    logger.LogWarning("Property {Prop} was not created as its type couldn't be determined", propIdentifier);
                    continue;
                }
                prop.Deprecation = currentNode.GetDeprecationInformation();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    prop.Documentation.DescriptionTemplate = description;
                }
                codeClass.AddProperty(prop);
            }
        }

        CreateUrlManagement(codeClass, currentNode, isApiClientClass);
        // Add methods for Operations
        if (currentNode.HasOperations(Constants.DefaultOpenApiLabel))
        {
            if (!isApiClientClass) // do not generate for API client class with operations as the class won't have the rawUrl constructor.
                CreateWithUrlMethod(currentNode, codeClass);
            if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) && pathItem.Operations is not null)
            {
                foreach (var operation in pathItem.Operations)
                {

                    CreateOperationMethods(currentNode, operation.Key, operation.Value, codeClass);
                    if (config.Language == GenerationLanguage.HTTP)
                    {
                        AddOperationSecurityRequirementToDOM(operation.Value, codeClass);
                    }
                }
            }
        }

        if (rootNamespace != null)
            Parallel.ForEach(currentNode.Children.Values, parallelOptions, childNode =>
            {
                if (childNode.GetNodeNamespaceFromPath(config.ClientNamespaceName) is string targetNamespaceName &&
                    !string.IsNullOrEmpty(targetNamespaceName))
                {
                    var targetNamespace = rootNamespace.FindOrAddNamespace(targetNamespaceName);
                    CreateRequestBuilderClass(targetNamespace, childNode, rootNode);
                }
            });
    }
    private static void CreateWithUrlMethod(OpenApiUrlTreeNode currentNode, CodeClass currentClass)
    {
        var methodToAdd = new CodeMethod
        {
            Name = "WithUrl",
            Kind = CodeMethodKind.RawUrlBuilder,
            Documentation = new()
            {
                DescriptionTemplate = "Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.",
            },
            Access = AccessModifier.Public,
            IsAsync = false,
            IsStatic = false,
            ReturnType = new CodeType
            {
                ActionOf = false,
                IsExternal = false,
                IsNullable = false,
                TypeDefinition = currentClass,
            },
            Deprecation = currentNode.GetDeprecationInformation(),
        };
        methodToAdd.AddParameter(new CodeParameter
        {
            Name = "rawUrl",
            Type = new CodeType { Name = "string", IsExternal = true },
            Optional = false,
            Documentation = new()
            {
                DescriptionTemplate = "The raw URL to use for the request builder.",
            },
            Kind = CodeParameterKind.RawUrl,
        });
        currentClass.AddMethod(methodToAdd);
    }
    private static void CreateMethod(string propIdentifier, string propType, CodeClass codeClass, OpenApiUrlTreeNode currentNode)
    {
        var methodToAdd = new CodeMethod
        {
            Name = propIdentifier.CleanupSymbolName(),
            Kind = CodeMethodKind.RequestBuilderWithParameters,
            Documentation = new()
            {
                DescriptionTemplate = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
            },
            Access = AccessModifier.Public,
            IsAsync = false,
            IsStatic = false,
            Parent = codeClass,
            ReturnType = new CodeType
            {
                Name = propType,
                ActionOf = false,
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None,
                IsExternal = false,
                IsNullable = false,
            },
            Deprecation = currentNode.GetDeprecationInformation(),
        };
        AddPathParametersToMethod(currentNode, methodToAdd, false);
        codeClass.AddMethod(methodToAdd);
    }
    private static void AddPathParametersToMethod(OpenApiUrlTreeNode currentNode, CodeMethod methodToAdd, bool asOptional)
    {
        foreach (var parameter in currentNode.GetPathParametersForCurrentSegment())
        {
            if (parameter.Name?.SanitizeParameterNameForCodeSymbols() is not string codeName)
                continue;
            var parameterType = GetPrimitiveType(parameter.Schema ?? parameter.Content?.Values.FirstOrDefault()?.Schema) ??
            new CodeType
            {
                Name = "string",
                IsExternal = true,
            };
            parameterType.CollectionKind = parameter.Schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default;
            var mParameter = new CodeParameter
            {
                Name = codeName,
                Optional = asOptional,
                Documentation = new()
                {
                    DescriptionTemplate = !string.IsNullOrEmpty(parameter.Description) ? parameter.Description.CleanupDescription() : $"The path parameter: {codeName}",
                },
                Kind = CodeParameterKind.Path,
                SerializationName = parameter.Name.Equals(codeName, StringComparison.OrdinalIgnoreCase) ? string.Empty : parameter.Name.SanitizeParameterNameForUrlTemplate(),
                Type = parameterType,
                Deprecation = parameter.GetDeprecationInformation(),
            };
            // not using the content schema as RFC6570 will serialize arrays as CSVs and content expects a JSON array, we failsafe to opaque string, it could be improved by involving the serialization layers.
            methodToAdd.AddParameter(mParameter);
        }
    }
    private const string PathParametersParameterName = "pathParameters";
    private void CreateUrlManagement(CodeClass currentClass, OpenApiUrlTreeNode currentNode, bool isApiClientClass)
    {
        var pathProperty = new CodeProperty
        {
            Access = AccessModifier.Private,
            Name = "urlTemplate",
            DefaultValue = $"\"{currentNode.GetUrlTemplate()}\"",
            ReadOnly = true,
            Documentation = new()
            {
                DescriptionTemplate = "Url template to use to build the URL for the current request builder",
            },
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "string",
                IsNullable = false,
                IsExternal = true,
            },
        };
        currentClass.AddProperty(pathProperty);

        var requestAdapterProperty = new CodeProperty
        {
            Name = RequestAdapterParameterName,
            Documentation = new()
            {
                DescriptionTemplate = "The request adapter to use to execute the requests.",
            },
            Kind = CodePropertyKind.RequestAdapter,
            Access = AccessModifier.Private,
            ReadOnly = true,
            Type = new CodeType
            {
                Name = CoreInterfaceType,
                IsExternal = true,
                IsNullable = false,
            }
        };
        currentClass.AddProperty(requestAdapterProperty);
        var constructor = new CodeMethod
        {
            Name = ConstructorMethodName,
            Kind = isApiClientClass ? CodeMethodKind.ClientConstructor : CodeMethodKind.Constructor,
            IsAsync = false,
            IsStatic = false,
            Documentation = new(new() {
                                {"TypeName", new CodeType {
                                    IsExternal = false,
                                    TypeDefinition = currentClass,
                                }
                            }
            })
            {
                DescriptionTemplate = "Instantiates a new {TypeName} and sets the default values.",
            },
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = VoidType, IsExternal = true },
            Parent = currentClass,
        };
        var pathParametersProperty = new CodeProperty
        {
            Name = PathParametersParameterName,
            Documentation = new()
            {
                DescriptionTemplate = "Path parameters for the request",
            },
            Kind = CodePropertyKind.PathParameters,
            Access = AccessModifier.Private,
            ReadOnly = true,
            Type = new CodeType
            {
                Name = "Dictionary<string, object>",
                IsExternal = true,
                IsNullable = false,
            },
        };
        currentClass.AddProperty(pathParametersProperty);
        if (isApiClientClass)
        {
            constructor.SerializerModules = ReplaceNoneSerializersByEmptySet(config.Serializers);
            constructor.DeserializerModules = ReplaceNoneSerializersByEmptySet(config.Deserializers);
            constructor.BaseUrl = config.ApiRootUrl ?? string.Empty;
            pathParametersProperty.DefaultValue = $"new {pathParametersProperty.Type.Name}()";
        }
        else
        {
            constructor.AddParameter(new CodeParameter
            {
                Name = PathParametersParameterName,
                Type = pathParametersProperty.Type,
                Optional = false,
                Documentation = (CodeDocumentation)pathParametersProperty.Documentation.Clone(),
                Kind = CodeParameterKind.PathParameters,
            });
            AddPathParametersToMethod(currentNode, constructor, true);
        }
        constructor.AddParameter(new CodeParameter
        {
            Name = RequestAdapterParameterName,
            Type = requestAdapterProperty.Type,
            Optional = false,
            Documentation = (CodeDocumentation)requestAdapterProperty.Documentation.Clone(),
            Kind = CodeParameterKind.RequestAdapter,
        });
        if (isApiClientClass && config.UsesBackingStore)
        {
            var factoryInterfaceName = $"{BackingStoreInterface}Factory";
            var backingStoreParam = new CodeParameter
            {
                Name = "backingStore",
                Optional = true,
                Documentation = new()
                {
                    DescriptionTemplate = "The backing store to use for the models.",
                },
                Kind = CodeParameterKind.BackingStore,
                Type = new CodeType
                {
                    Name = factoryInterfaceName,
                    IsNullable = true,
                }
            };
            constructor.AddParameter(backingStoreParam);
        }
        currentClass.AddMethod(constructor);
        if (!isApiClientClass)
        {
            var overloadCtor = (CodeMethod)constructor.Clone();
            overloadCtor.Kind = CodeMethodKind.RawUrlConstructor;
            overloadCtor.OriginalMethod = constructor;
            overloadCtor.RemoveParametersByKind(CodeParameterKind.PathParameters, CodeParameterKind.Path);
            overloadCtor.AddParameter(new CodeParameter
            {
                Name = "rawUrl",
                Type = new CodeType { Name = "string", IsExternal = true },
                Optional = false,
                Documentation = new()
                {
                    DescriptionTemplate = "The raw URL to use for the request builder.",
                },
                Kind = CodeParameterKind.RawUrl,
            });
            currentClass.AddMethod(overloadCtor);
        }
    }
    private static HashSet<string> ReplaceNoneSerializersByEmptySet(HashSet<string> serializers)
    {
        if (serializers.Count == 1 && serializers.Contains("none")) return [];
        return serializers;
    }

    private static readonly Func<CodeClass, int> shortestNamespaceOrder = x => x.GetNamespaceDepth();
    /// <summary>
    /// Remaps definitions to custom types so they can be used later in generation or in refiners
    /// </summary>
    private void MapTypeDefinitions(CodeElement codeElement)
    {
        var unmappedTypes = GetUnmappedTypeDefinitions(codeElement).Distinct().ToArray();

        var unmappedTypesWithNoName = unmappedTypes.Where(static x => string.IsNullOrEmpty(x.Name)).ToList();

        unmappedTypesWithNoName.ForEach(x =>
        {
            logger.LogWarning("Type with empty name and parent {ParentName}", x.Parent?.Name);
        });

        var unmappedTypesWithName = unmappedTypes.Except(unmappedTypesWithNoName);

        var unmappedRequestBuilderTypes = unmappedTypesWithName
                                .Where(static x =>
                                x.Parent is CodeProperty property && property.IsOfKind(CodePropertyKind.RequestBuilder) ||
                                x.Parent is CodeIndexer ||
                                x.Parent is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestBuilderWithParameters))
                                .ToList();

        Parallel.ForEach(unmappedRequestBuilderTypes, parallelOptions, x =>
        {
            var parentNS = x.Parent?.Parent?.Parent as CodeNamespace;
            CodeClass[] exceptions = x.Parent?.Parent is CodeClass parentClass ? [parentClass] : [];
            x.TypeDefinition = parentNS?.FindChildrenByName<CodeClass>(x.Name)
                .Except(exceptions)// the property method should not reference itself as a return type.
                .MinBy(shortestNamespaceOrder);
            // searching down first because most request builder properties on a request builder are just sub paths on the API
            if (x.TypeDefinition == null)
            {
                parentNS = parentNS?.Parent as CodeNamespace;
                x.TypeDefinition = (parentNS
                    ?.FindNamespaceByName($"{parentNS?.Name}.{x.Name[..^RequestBuilderSuffix.Length].ToFirstCharacterLowerCase()}".TrimEnd(NsNameSeparator))
                    ?.FindChildrenByName<CodeClass>(x.Name))?.MinBy(shortestNamespaceOrder);
                // in case of the .item namespace, going to the parent and then down to the target by convention
                // this avoid getting the wrong request builder in case we have multiple request builders with the same name in the parent branch
                // in both cases we always take the uppermost item (smaller numbers of segments in the namespace name)
            }
        });

        Parallel.ForEach(unmappedTypesWithName.Where(static x => x.TypeDefinition == null).GroupBy(static x => x.Name), parallelOptions, x =>
        {
            if (rootNamespace?.FindChildByName<ITypeDefinition>(x.First().Name) is CodeElement definition)
                foreach (var type in x)
                {
                    type.TypeDefinition = definition;
                    logger.LogWarning("Mapped type {TypeName} for {ParentName} using the fallback approach.", type.Name, type.Parent?.Name);
                }
        });
    }
    private const char NsNameSeparator = '.';
    private static IEnumerable<CodeType> filterUnmappedTypeDefinitions(IEnumerable<CodeTypeBase?> source) =>
    source.OfType<CodeType>()
            .Union(source
                    .OfType<CodeComposedTypeBase>()
                    .SelectMany(x => x.Types))
            .Where(static x => !x.IsExternal && x.TypeDefinition == null);
    private IEnumerable<CodeType> GetUnmappedTypeDefinitions(CodeElement codeElement)
    {
        var childElementsUnmappedTypes = codeElement.GetChildElements(true).SelectMany(GetUnmappedTypeDefinitions);
        return codeElement switch
        {
            CodeMethod method => filterUnmappedTypeDefinitions(method.Parameters.Select(static x => x.Type).Union(new[] { method.ReturnType })).Union(childElementsUnmappedTypes),
            CodeProperty property => filterUnmappedTypeDefinitions(new[] { property.Type }).Union(childElementsUnmappedTypes),
            CodeIndexer indexer => filterUnmappedTypeDefinitions(new[] { indexer.ReturnType }).Union(childElementsUnmappedTypes),
            _ => childElementsUnmappedTypes,
        };
    }
    private static CodeType DefaultIndexerParameterType => new() { Name = "string", IsExternal = true };
    private CodeParameter GetIndexerParameter(OpenApiUrlTreeNode currentNode)
    {
        var parameterName = (currentNode.AdditionalData.TryGetValue(Constants.KiotaSegmentNameTreeNodeExtensionKey, out var newNames) && newNames is { Count: > 0 } ?
                        newNames[0] :
                        currentNode.Segment).Trim('{', '}');
        var pathItems = GetPathItems(currentNode);
        var parameter = pathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) ?
                        (pathItem.Parameters ?? Enumerable.Empty<IOpenApiParameter>())
                            .Select(static x => new { Parameter = x, IsPathParameter = true })
                            .Union(pathItem.Operations?.SelectMany(static x => x.Value.Parameters ?? []).Select(static x => new { Parameter = x, IsPathParameter = false }) ?? [])
                            .OrderBy(static x => x.IsPathParameter)
                            .Select(static x => x.Parameter)
                            .FirstOrDefault(x => parameterName.Equals(x.Name, StringComparison.OrdinalIgnoreCase) && x.In == ParameterLocation.Path) :
                        default;
        var type = parameter switch
        {
            null => DefaultIndexerParameterType,
            _ => GetPrimitiveType(parameter.Schema),
        } ?? DefaultIndexerParameterType;
        type.IsNullable = false;
        var segment = currentNode.DeduplicatedSegment();
        var result = new CodeParameter
        {
            Type = type,
            SerializationName = segment.SanitizeParameterNameForUrlTemplate(),
            Name = segment.CleanupSymbolName(),
            Documentation = new()
            {
                DescriptionTemplate = parameter?.Description.CleanupDescription() is string description && !string.IsNullOrEmpty(description) ? description : "Unique identifier of the item",
            },
        };
        return result;
    }
    private static IDictionary<string, IOpenApiPathItem> GetPathItems(OpenApiUrlTreeNode currentNode, bool validateIsParameterNode = true)
    {
        if ((!validateIsParameterNode || currentNode.IsParameter) && currentNode.PathItems.Count != 0)
        {
            return currentNode.PathItems;
        }

        if (currentNode.Children.Count != 0)
        {
            return currentNode.Children
                .SelectMany(static x => GetPathItems(x.Value, false))
                .DistinctBy(static x => x.Key, StringComparer.Ordinal)
                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal);
        }

        return ImmutableDictionary<string, IOpenApiPathItem>.Empty;
    }
    private CodeIndexer[] CreateIndexer(string childIdentifier, string childType, CodeParameter parameter, OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode parentNode)
    {
        logger.LogTrace("Creating indexer {Name}", childIdentifier);
        var result = new List<CodeIndexer> { new() {
            Name = childIdentifier,
            Documentation = new()
            {
                DescriptionTemplate = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Gets an item from the {currentNode.GetNodeNamespaceFromPath(config.ClientNamespaceName)} collection"),
            },
            ReturnType = new CodeType { Name = childType },
            PathSegment = parentNode.GetNodeNamespaceFromPath(string.Empty).Split('.')[^1],
            Deprecation = currentNode.GetDeprecationInformation(),
            IndexParameter = parameter,
        }};

        if (!"string".Equals(parameter.Type.Name, StringComparison.OrdinalIgnoreCase) && config.IncludeBackwardCompatible)
        { // adding a second indexer for the string version of the parameter so we keep backward compatibility
            //TODO remove for v2
            var backCompatibleValue = (CodeIndexer)result[0].Clone();
            backCompatibleValue.Name += "-string";
            backCompatibleValue.IndexParameter.Type = DefaultIndexerParameterType;
            backCompatibleValue.Deprecation = new DeprecationInformation("This indexer is deprecated and will be removed in the next major version. Use the one with the typed parameter instead.");
            backCompatibleValue.IsLegacyIndexer = true;
            result.Add(backCompatibleValue);
        }

        return [.. result];
    }
    private static readonly StructuralPropertiesReservedNameProvider structuralPropertiesReservedNameProvider = new();

    private CodeProperty? CreateProperty(string childIdentifier, string childType, IOpenApiSchema? propertySchema = null, CodeTypeBase? existingType = null, CodePropertyKind kind = CodePropertyKind.Custom)
    {
        var propertyName = childIdentifier.CleanupSymbolName();
        if (structuralPropertiesReservedNameProvider.ReservedNames.Contains(propertyName))
            propertyName += "Property";
        var resultType = existingType ?? GetPrimitiveType(propertySchema, childType);
        if ((propertySchema?.Items?.IsEnum() ?? false) && resultType is CodeType codeType)
            codeType.Name = childType;
        if (resultType == null) return null;
        var prop = new CodeProperty
        {
            Name = propertyName,
            Kind = kind,
            Documentation = new()
            {
                DescriptionTemplate = propertySchema?.Description.CleanupDescription() is string description && !string.IsNullOrEmpty(description) ?
                    description :
                    $"The {propertyName} property",
            },
            ReadOnly = propertySchema?.ReadOnly ?? false,
            Type = resultType,
            Deprecation = propertySchema?.GetDeprecationInformation(),
            IsPrimaryErrorMessage = kind == CodePropertyKind.Custom &&
                                        propertySchema is { Extensions: not null } &&
                                        propertySchema.Extensions.TryGetValue(OpenApiPrimaryErrorMessageExtension.Name, out var openApiExtension) &&
                                        openApiExtension is OpenApiPrimaryErrorMessageExtension primaryErrorMessageExtension &&
                                        primaryErrorMessageExtension.IsPrimaryErrorMessage
        };
        if (prop.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.QueryParameter) &&
            !propertyName.Equals(childIdentifier, StringComparison.Ordinal))
            prop.SerializationName = childIdentifier;
        if (kind == CodePropertyKind.Custom &&
            propertySchema?.Default is JsonValue stringDefaultJsonValue &&
            !stringDefaultJsonValue.IsJsonNullSentinel() &&
            stringDefaultJsonValue.TryGetValue<string>(out var stringDefaultValue) &&
            !string.IsNullOrEmpty(stringDefaultValue) &&
            !"null".Equals(stringDefaultValue, StringComparison.OrdinalIgnoreCase))
            prop.DefaultValue = $"\"{stringDefaultValue}\"";

        if (existingType == null)
        {
            prop.Type.CollectionKind = propertySchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default;
            logger.LogTrace("Creating property {Name} of {Type}", prop.Name, prop.Type.Name);
        }
        return prop;
    }
    private static readonly HashSet<JsonSchemaType> typeNamesToSkip = [JsonSchemaType.Object, JsonSchemaType.Array, JsonSchemaType.Object | JsonSchemaType.Null, JsonSchemaType.Array | JsonSchemaType.Null];
    private static CodeType? GetPrimitiveType(IOpenApiSchema? typeSchema, string? childType = default)
    {
        if (typeSchema?.Items?.IsEnum() ?? false)
            return null;
        var typeNames = new List<JsonSchemaType?> { typeSchema?.Items?.Type, typeSchema?.Type };
        if (typeSchema?.AnyOf is { Count: > 0 })
            typeNames.AddRange(typeSchema.AnyOf.Select(x => x.Type)); // double is sometimes an anyof string, number and enum
        if (typeSchema?.OneOf is { Count: > 0 })
            typeNames.AddRange(typeSchema.OneOf.Select(x => x.Type)); // double is sometimes an oneof string, number and enum
                                                                      // first value that's not null, and not "object" for primitive collections, the items type matters
        var typeName = typeNames.Find(static x => x is not null && !typeNamesToSkip.Contains(x.Value));

        var format = typeSchema?.Format ?? typeSchema?.Items?.Format;
        return (typeName & ~JsonSchemaType.Null, format?.ToLowerInvariant()) switch
        {
            (_, "byte") => new CodeType { Name = "base64", IsExternal = true },
            (_, "binary") => new CodeType { Name = "binary", IsExternal = true },
            (JsonSchemaType.String, "base64url") => new CodeType { Name = "base64url", IsExternal = true },
            (JsonSchemaType.String, "duration") => new CodeType { Name = "TimeSpan", IsExternal = true },
            (JsonSchemaType.String, "time") => new CodeType { Name = "TimeOnly", IsExternal = true },
            (JsonSchemaType.String, "date") => new CodeType { Name = "DateOnly", IsExternal = true },
            (JsonSchemaType.String, "date-time") => new CodeType { Name = "DateTimeOffset", IsExternal = true },
            (JsonSchemaType.String, "uuid") => new CodeType { Name = "Guid", IsExternal = true },
            (JsonSchemaType.String, _) => new CodeType { Name = "string", IsExternal = true }, // covers commonmark and html
            (JsonSchemaType.Number, "double" or "float" or "decimal") => new CodeType { Name = format.ToLowerInvariant(), IsExternal = true },
            (JsonSchemaType.Number or JsonSchemaType.Integer, "int8") => new CodeType { Name = "sbyte", IsExternal = true },
            (JsonSchemaType.Number or JsonSchemaType.Integer, "uint8") => new CodeType { Name = "byte", IsExternal = true },
            (JsonSchemaType.Number or JsonSchemaType.Integer, "int64") => new CodeType { Name = "int64", IsExternal = true },
            (JsonSchemaType.Number, "int16") => new CodeType { Name = "integer", IsExternal = true },
            (JsonSchemaType.Number, "int32") => new CodeType { Name = "integer", IsExternal = true },
            (JsonSchemaType.Number, _) => new CodeType { Name = "double", IsExternal = true },
            (JsonSchemaType.Integer, _) => new CodeType { Name = "integer", IsExternal = true },
            (JsonSchemaType.Boolean, _) => new CodeType { Name = "boolean", IsExternal = true },
            (_, _) when !string.IsNullOrEmpty(childType) => new CodeType { Name = childType, IsExternal = false, },
            (_, _) => null,
        };
    }
    private const string RequestBodyPlainTextContentType = "text/plain";
    private const string RequestBodyOctetStreamContentType = "application/octet-stream";
    private const string DefaultResponseIndicator = "default";
    private static readonly HashSet<string> redirectStatusCodes = new(StringComparer.OrdinalIgnoreCase) { "301", "302", "303", "307" };
    private static readonly HashSet<string> noContentStatusCodes = new(redirectStatusCodes, StringComparer.OrdinalIgnoreCase) { "201", "202", "204", "205", "304" };
    private static readonly HashSet<string> errorStatusCodes = new(Enumerable.Range(400, 599).Select(static x => x.ToString(CultureInfo.InvariantCulture))
                                                                                 .Concat([CodeMethod.ErrorMappingClientRange, CodeMethod.ErrorMappingServerRange]), StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> errorStatusCodesWithDefault = new(errorStatusCodes, StringComparer.OrdinalIgnoreCase) { DefaultResponseIndicator };
    private void AddErrorMappingsForExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod)
    {
        if (operation.Responses is null) return;
        foreach (var response in operation.Responses.Where(x => errorStatusCodes.Contains(x.Key)))
        {
            if (response.Value.GetResponseSchema(config.StructuredMimeTypes) is { } schema)
            {
                AddErrorMappingToExecutorMethod(currentNode, operation, executorMethod, schema, response.Value, response.Key.ToUpperInvariant());
            }
        }
        if (operation.Responses.TryGetValue(DefaultResponseIndicator, out var defaultResponse) && defaultResponse.GetResponseSchema(config.StructuredMimeTypes) is { } errorSchema)
        {
            if (!executorMethod.HasErrorMappingCode(CodeMethod.ErrorMappingClientRange))
                AddErrorMappingToExecutorMethod(currentNode, operation, executorMethod, errorSchema, defaultResponse, CodeMethod.ErrorMappingClientRange);
            if (!executorMethod.HasErrorMappingCode(CodeMethod.ErrorMappingServerRange))
                AddErrorMappingToExecutorMethod(currentNode, operation, executorMethod, errorSchema, defaultResponse, CodeMethod.ErrorMappingServerRange);
        }
    }
    private void AddErrorMappingToExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod, IOpenApiSchema errorSchema, IOpenApiResponse response, string errorCode)
    {
        if (modelsNamespace != null)
        {
            var parentElement = response is not OpenApiResponseReference && errorSchema is not OpenApiSchemaReference
                ? (CodeElement)executorMethod
                : modelsNamespace;
            var errorType = CreateModelDeclarations(currentNode, errorSchema, operation, parentElement, $"{errorCode}Error", response: response);
            if (errorType is CodeType codeType &&
                codeType.TypeDefinition is CodeClass codeClass)
            {
                if (!codeClass.IsErrorDefinition)
                    codeClass.IsErrorDefinition = true;
                executorMethod.AddErrorMapping(errorCode, errorType, response.Description ?? string.Empty);
            }
            else
                logger.LogWarning("Could not create error type for {Error} in {Operation}", errorCode, operation.OperationId);
        }
    }
    private (CodeTypeBase?, CodeTypeBase?) GetExecutorMethodReturnType(OpenApiUrlTreeNode currentNode, IOpenApiSchema? schema, OpenApiOperation operation, CodeClass parentClass, NetHttpMethod operationType)
    {
        if (schema != null)
        {
            var suffix = $"{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}Response";
            var modelType = CreateModelDeclarations(currentNode, schema, operation, parentClass, suffix);
            if (modelType is not null && config.IncludeBackwardCompatible && config.Language is GenerationLanguage.CSharp or GenerationLanguage.Go && modelType.Name.EndsWith(suffix, StringComparison.Ordinal))
            { //TODO remove for v2
                var obsoleteTypeName = modelType.Name[..^suffix.Length] + "Response";
                if (modelType is CodeType codeType &&
                    codeType.TypeDefinition is CodeClass codeClass)
                {
                    var obsoleteClassDefinition = new CodeClass
                    {
                        Kind = CodeClassKind.Model,
                        Name = obsoleteTypeName,
                        Deprecation = new("This class is obsolete. Use {TypeName} instead.", IsDeprecated: true, TypeReferences: new() { { "TypeName", codeType } }),
                        Documentation = (CodeDocumentation)codeClass.Documentation.Clone()
                    };
                    var originalFactoryMethod = codeClass.Methods.First(static x => x.Kind is CodeMethodKind.Factory);
                    var obsoleteFactoryMethod = (CodeMethod)originalFactoryMethod.Clone();
                    obsoleteFactoryMethod.ReturnType = new CodeType { Name = obsoleteTypeName, TypeDefinition = obsoleteClassDefinition };
                    obsoleteClassDefinition.AddMethod(obsoleteFactoryMethod);
                    obsoleteClassDefinition.StartBlock.Inherits = (CodeType)codeType.Clone();
                    var obsoleteClass = codeClass.Parent switch
                    {
                        CodeClass modelParentClass => modelParentClass.AddInnerClass(obsoleteClassDefinition).First(),
                        CodeNamespace modelParentNamespace => modelParentNamespace.AddClass(obsoleteClassDefinition).First(),
                        _ => throw new InvalidOperationException("Could not find a valid parent for the obsolete class")
                    };
                    return (modelType, new CodeType
                    {
                        TypeDefinition = obsoleteClass,
                    });
                }
                else if (modelType is CodeComposedTypeBase codeComposedTypeBase)
                {
                    var obsoleteComposedType = codeComposedTypeBase switch
                    {
                        CodeUnionType u => (CodeComposedTypeBase)u.Clone(),
                        CodeIntersectionType i => (CodeComposedTypeBase)i.Clone(),
                        _ => throw new InvalidOperationException("Could not create an obsolete composed type"),
                    };
                    obsoleteComposedType.Name = obsoleteTypeName;
                    obsoleteComposedType.Deprecation = new("This class is obsolete. Use {TypeName} instead.", IsDeprecated: true, TypeReferences: new() { { "TypeName", modelType } });
                    return (modelType, obsoleteComposedType);
                }
            }
            else if (modelType is null)
            {
                return (GetExecutorMethodDefaultReturnType(operation), null);
            }
            return (modelType, null);
        }
        else
        {
            return (GetExecutorMethodDefaultReturnType(operation), null);
        }
    }
    private static CodeType GetExecutorMethodDefaultReturnType(OpenApiOperation operation)
    {
        string returnType;
        if (operation.Responses?.Any(static x => (x.Value.Content?.ContainsKey(RequestBodyOctetStreamContentType) ?? false) && redirectStatusCodes.Contains(x.Key)) is true)
            returnType = "binary";
        else if (operation.Responses?.Any(static x => noContentStatusCodes.Contains(x.Key)) is true)
            returnType = VoidType;
        else if (operation.Responses?.Any(static x => x.Value.Content?.ContainsKey(RequestBodyPlainTextContentType) ?? false) is true)
            returnType = "string";
        else
            returnType = "binary";
        return new CodeType { Name = returnType, IsExternal = true, };
    }
    private void CreateOperationMethods(OpenApiUrlTreeNode currentNode, NetHttpMethod operationType, OpenApiOperation operation, CodeClass parentClass)
    {
        try
        {
            var parameterClass = CreateOperationParameterClass(currentNode, operationType, operation, parentClass);
            var requestConfigClass = parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration",
                Kind = CodeClassKind.RequestConfiguration,
                Documentation = new()
                {
                    DescriptionTemplate = "Configuration for the request such as headers, query parameters, and middleware options.",
                },
            }).First();

            var schema = operation.GetResponseSchema(config.StructuredMimeTypes);
            var method = Enum.Parse<DomHttpMethod>(operationType.ToString(), true);
            var deprecationInformation = operation.GetDeprecationInformation();
            var returnTypes = GetExecutorMethodReturnType(currentNode, schema, operation, parentClass, operationType);
            var executorMethod = new CodeMethod
            {
                Name = operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase(),
                Kind = CodeMethodKind.RequestExecutor,
                HttpMethod = method,
                Parent = parentClass,
                Documentation = new()
                {
                    DocumentationLink = operation.ExternalDocs?.Url,
                    DocumentationLabel = operation.ExternalDocs?.Description ?? string.Empty,
                    DescriptionTemplate = (operation.Description is string description && !string.IsNullOrEmpty(description) ?
                                    description :
                                    operation.Summary)
                                    .CleanupDescription(),
                },
                ReturnType = returnTypes.Item1 ?? throw new InvalidSchemaException(),
                Deprecation = deprecationInformation,
            };

            if (operation.Extensions is not null && operation.Extensions.TryGetValue(OpenApiPagingExtension.Name, out var extension) && extension is OpenApiPagingExtension pagingExtension)
            {
                executorMethod.PagingInformation = new PagingInformation
                {
                    ItemName = pagingExtension.ItemName,
                    NextLinkName = pagingExtension.NextLinkName,
                    OperationName = pagingExtension.OperationName,
                };
            }

            AddErrorMappingsForExecutorMethod(currentNode, operation, executorMethod);
            AddRequestConfigurationProperties(parameterClass, requestConfigClass);
            AddRequestBuilderMethodParameters(currentNode, operationType, operation, requestConfigClass, executorMethod);
            parentClass.AddMethod(executorMethod);

            var cancellationParam = new CodeParameter
            {
                Name = "cancellationToken",
                Optional = true,
                Kind = CodeParameterKind.Cancellation,
                Documentation = new()
                {
                    DescriptionTemplate = "Cancellation token to use when cancelling requests",
                },
                Type = new CodeType { Name = "CancellationToken", IsExternal = true },
            };
            executorMethod.AddParameter(cancellationParam);// Add cancellation token parameter

            if (returnTypes.Item2 is not null && config.IncludeBackwardCompatible)
            { //TODO remove for v2
                var additionalExecutorMethod = (CodeMethod)executorMethod.Clone();
                additionalExecutorMethod.ReturnType = returnTypes.Item2;
                additionalExecutorMethod.OriginalMethod = executorMethod;
                var newName = $"{executorMethod.Name}As{executorMethod.ReturnType.Name.ToFirstCharacterUpperCase()}";
                additionalExecutorMethod.Deprecation = new("This method is obsolete. Use {TypeName} instead.", IsDeprecated: true, TypeReferences: new() { { "TypeName", new CodeType { TypeDefinition = executorMethod, IsExternal = false } } });
                parentClass.RenameChildElement(executorMethod.Name, newName);
                parentClass.AddMethod(additionalExecutorMethod);
            }
            logger.LogTrace("Creating method {Name} of {Type}", executorMethod.Name, executorMethod.ReturnType);

            var generatorMethod = new CodeMethod
            {
                Name = $"To{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestInformation",
                Kind = CodeMethodKind.RequestGenerator,
                IsAsync = false,
                HttpMethod = method,
                Documentation = new()
                {
                    DescriptionTemplate = (operation.Description ?? operation.Summary).CleanupDescription(),
                },
                ReturnType = new CodeType { Name = "RequestInformation", IsNullable = false, IsExternal = true },
                Parent = parentClass,
                Deprecation = deprecationInformation,
            };
            var operationUrlTemplate = currentNode.GetUrlTemplate(operationType);
            if (!operationUrlTemplate.Equals(parentClass.Properties.FirstOrDefault(static x => x.Kind is CodePropertyKind.UrlTemplate)?.DefaultValue?.Trim('"'), StringComparison.Ordinal)
                && currentNode.HasRequiredQueryParametersAcrossOperations())// no need to generate extra strings/templates as optional parameters will have no effect on resolved url.
                generatorMethod.UrlTemplateOverride = operationUrlTemplate;

            var mediaTypes = (schema, operation.Responses is null) switch
            {
                (_, true) => [],
                (null, _) => operation.Responses!
                            .Where(static x => !errorStatusCodesWithDefault.Contains(x.Key) && x.Value.Content is not null)
                            .SelectMany(static x => x.Value.Content!)
                            .Select(static x => x.Key)
                            .Select(static x => x.Split(';', StringSplitOptions.RemoveEmptyEntries)[0]) //get the successful non structured media types first, with a default 1 priority
                            .Union(config.StructuredMimeTypes
                                        .GetAcceptedTypes(
                                            operation.Responses!
                                                .Where(static x => errorStatusCodesWithDefault.Contains(x.Key) && x.Value.Content is not null) // get any structured error ones, with the priority from the configuration
                                                .SelectMany(static x => x.Value.Content!) // we can safely ignore unstructured ones as they won't be used in error mappings anyway and the body won't be read
                                                .Select(static x => x.Key)))
                            .Distinct(StringComparer.OrdinalIgnoreCase),
                (_, false) => config.StructuredMimeTypes.GetAcceptedTypes(operation.Responses!.Values.Where(static x => x.Content is not null).SelectMany(static x => x.Content!).Where(x => schemaReferenceComparer.Equals(schema, x.Value.Schema)).Select(static x => x.Key)),
            };
            generatorMethod.AddAcceptedResponsesTypes(mediaTypes);
            AddRequestBuilderMethodParameters(currentNode, operationType, operation, requestConfigClass, generatorMethod);
            parentClass.AddMethod(generatorMethod);
            logger.LogTrace("Creating method {Name} of {Type}", generatorMethod.Name, generatorMethod.ReturnType);
        }
        catch (InvalidSchemaException ex)
        {
            logger.LogWarning(ex, "Could not create method for {Operation} in {Path} because the schema was invalid", operation.OperationId, currentNode.Path);
        }
    }
    private static readonly OpenApiSchemaReferenceComparer schemaReferenceComparer = new();

    private static void AddRequestConfigurationProperties(CodeClass? parameterClass, CodeClass requestConfigClass)
    {
        if (parameterClass != null)
        {
            requestConfigClass.AddProperty(new CodeProperty
            {
                Name = "queryParameters",
                Kind = CodePropertyKind.QueryParameters,
                Documentation = new()
                {
                    DescriptionTemplate = "Request query parameters",
                },
                Type = new CodeType { TypeDefinition = parameterClass },
            });
        }
        requestConfigClass.AddProperty(new CodeProperty
        {
            Name = "headers",
            Kind = CodePropertyKind.Headers,
            Documentation = new()
            {
                DescriptionTemplate = "Request headers",
            },
            Type = new CodeType { Name = "RequestHeaders", IsExternal = true },
        },
        new CodeProperty
        {
            Name = "options",
            Kind = CodePropertyKind.Options,
            Documentation = new()
            {
                DescriptionTemplate = "Request options",
            },
            Type = new CodeType { Name = "IList<IRequestOption>", IsExternal = true },
        });
    }

    private readonly ConcurrentDictionary<CodeElement, bool> multipartPropertiesModels = new();

    private static bool IsSupportedMultipartDefault(IOpenApiSchema openApiSchema,
        StructuredMimeTypesCollection structuredMimeTypes)
    {
        // https://spec.openapis.org/oas/v3.0.3.html#special-considerations-for-multipart-content
        if (openApiSchema.IsObjectType() && structuredMimeTypes.Contains("application/json"))
            return true;

        if (GetPrimitiveType(openApiSchema) is { IsExternal: true } primitiveType &&     // it s a primitive
               (primitiveType.Name.Equals("binary", StringComparison.OrdinalIgnoreCase)
                || primitiveType.Name.Equals("base64", StringComparison.OrdinalIgnoreCase) // streams are handled irrespective of configs
                || structuredMimeTypes.Contains("text/plain")))                             // other primitives need text/plain
            return true;

        return false;
    }

    private void AddRequestBuilderMethodParameters(OpenApiUrlTreeNode currentNode, NetHttpMethod operationType, OpenApiOperation operation, CodeClass requestConfigClass, CodeMethod method)
    {
        if (operation.RequestBody is not null)
        {
            if (operation.GetRequestSchema(config.StructuredMimeTypes) is IOpenApiSchema requestBodySchema)
            {
                CodeTypeBase requestBodyType;
                if (operation.RequestBody is { Content: not null }
                    && operation.RequestBody.Content.IsMultipartFormDataSchema(config.StructuredMimeTypes)
                    && operation.RequestBody.Content.IsMultipartTopMimeType(config.StructuredMimeTypes)
                    && requestBodySchema.Properties is not null)
                {
                    var mediaType = operation.RequestBody.Content.First(x => x.Value.Schema == requestBodySchema).Value;
                    if (mediaType.Encoding is not null && mediaType.Encoding.Count != 0)
                    {
                        requestBodyType = new CodeType { Name = "MultipartBody", IsExternal = true, };
                        foreach (var encodingEntry in mediaType.Encoding
                                    .Where(x => !string.IsNullOrEmpty(x.Value.ContentType) &&
                                                config.StructuredMimeTypes.Contains(x.Value.ContentType)))
                        {
                            if (CreateModelDeclarations(currentNode, requestBodySchema.Properties[encodingEntry.Key],
                                    operation, method, $"{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestBody",
                                    isRequestBody: true) is CodeType propertyType &&
                                propertyType.TypeDefinition is not null)
                                multipartPropertiesModels.TryAdd(propertyType.TypeDefinition, true);
                        }
                    }
                    else if (requestBodySchema.Properties.Values.Any(schema => IsSupportedMultipartDefault(schema, config.StructuredMimeTypes))
                            && operation.RequestBody.Content.Count == 1)// it's the only content type.
                    {
                        requestBodyType = new CodeType { Name = "MultipartBody", IsExternal = true, };
                        foreach (var property in requestBodySchema.Properties.Values.Where(schema => IsSupportedMultipartDefault(schema, config.StructuredMimeTypes)))
                        {
                            if (CreateModelDeclarations(currentNode, property,
                                    operation, method, $"{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestBody",
                                    isRequestBody: true) is CodeType { TypeDefinition: not null } propertyType)
                                multipartPropertiesModels.TryAdd(propertyType.TypeDefinition, true);
                        }
                    }
                    else
                    {
                        requestBodyType = CreateModelDeclarations(currentNode, requestBodySchema, operation, method,
                                            $"{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestBody", isRequestBody: true) ??
                                        throw new InvalidSchemaException();
                    }
                }
                else
                {
                    requestBodyType = CreateModelDeclarations(currentNode, requestBodySchema, operation, method,
                                        $"{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestBody", isRequestBody: true) ??
                                    throw new InvalidSchemaException();
                }
                method.AddParameter(new CodeParameter
                {
                    Name = "body",
                    Type = requestBodyType,
                    Optional = false,
                    Kind = CodeParameterKind.RequestBody,
                    Documentation = new()
                    {
                        DescriptionTemplate = requestBodySchema.Description.CleanupDescription() is string description && !string.IsNullOrEmpty(description) ?
                                        description :
                                        "The request body"
                    },
                    Deprecation = requestBodySchema.GetDeprecationInformation(),
                });
                method.RequestBodyContentType = config.StructuredMimeTypes.GetContentTypes(operation.RequestBody.Content?.Where(x => schemaReferenceComparer.Equals(x.Value.Schema, requestBodySchema)).Select(static x => x.Key) ?? []).First();
            }
            else if (operation.RequestBody.Content is { Count: > 0 })
            {
                var nParam = new CodeParameter
                {
                    Name = "body",
                    Optional = false,
                    Kind = CodeParameterKind.RequestBody,
                    Documentation = new()
                    {
                        DescriptionTemplate = "Binary request body",
                    },
                    Type = new CodeType
                    {
                        Name = "binary",
                        IsExternal = true,
                        IsNullable = false,
                    },
                };
                method.AddParameter(nParam);
                var contentTypes = operation.RequestBody.Content.Select(static x => x.Key).ToArray();
                if (contentTypes.Length == 1 && !"*/*".Equals(contentTypes[0], StringComparison.OrdinalIgnoreCase))
                    method.RequestBodyContentType = contentTypes[0];
                else
                    method.AddParameter(new CodeParameter
                    {
                        Kind = CodeParameterKind.RequestBodyContentType,
                        Name = "contentType",
                        Optional = false,
                        Type = new CodeType
                        {
                            Name = "string",
                            IsExternal = true,
                            IsNullable = true,
                        },
                        Documentation = new()
                        {
                            DescriptionTemplate = "The request body content type."
                        },
                        PossibleValues = contentTypes.ToList()
                    });
            }
        }

        method.AddParameter(new CodeParameter
        {
            Name = "requestConfiguration",
            Optional = true,
            Type = new CodeType { TypeDefinition = requestConfigClass, ActionOf = true },
            Kind = CodeParameterKind.RequestConfiguration,
            Documentation = new()
            {
                DescriptionTemplate = "Configuration for the request such as headers, query parameters, and middleware options.",
            },
        });
    }
    private string GetModelsNamespaceNameFromReferenceId(string? referenceId)
    {
        if (string.IsNullOrEmpty(referenceId)) return string.Empty;
        if (referenceId.StartsWith(config.ClientClassName, StringComparison.OrdinalIgnoreCase)) // the client class having a namespace segment name can be problematic in some languages
            referenceId = referenceId[config.ClientClassName.Length..];
        referenceId = referenceId.Trim(NsNameSeparator);
        if (!string.IsNullOrEmpty(modelNamespacePrefixToTrim) && referenceId.StartsWith(modelNamespacePrefixToTrim, StringComparison.OrdinalIgnoreCase))
            referenceId = referenceId[modelNamespacePrefixToTrim.Length..];
        referenceId = referenceId.Trim(NsNameSeparator);
        var lastDotIndex = referenceId.LastIndexOf(NsNameSeparator);
        var namespaceSuffix = lastDotIndex != -1 ? $".{referenceId[..lastDotIndex]}" : string.Empty;
        return $"{modelsNamespace?.Name}{string.Join(NsNameSeparator, namespaceSuffix.Split(NsNameSeparator).Select(static x => x.CleanupSymbolName()))}";
    }
    private CodeType CreateModelDeclarationAndType(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string classNameSuffix = "", IOpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false)
    {
        var className = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, response: response, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, codeNamespace);
        return new CodeType
        {
            TypeDefinition = codeDeclaration,
        };
    }
    private CodeType CreateInheritedModelDeclarationAndType(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, OpenApiOperation? operation, string classNameSuffix, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema, bool isViaDiscriminator = false)
    {
        return new CodeType
        {
            TypeDefinition = CreateInheritedModelDeclaration(currentNode, schema, operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema, isViaDiscriminator),
        };
    }
    private CodeClass CreateInheritedModelDeclaration(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, OpenApiOperation? operation, string classNameSuffix, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema, bool isViaDiscriminator = false)
    {
        var flattenedAllOfs = schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf).ToArray();
        var referenceId = schema.GetReferenceId();
        var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(referenceId);
        var codeNamespaceFromParent = GetShortestNamespace(codeNamespace, schema);
        if (rootNamespace is null)
            throw new InvalidOperationException("Root namespace is not set");
        var shortestNamespace = string.IsNullOrEmpty(referenceId) ? codeNamespaceFromParent : rootNamespace.FindOrAddNamespace(shortestNamespaceName);
        var inlineSchemas = Array.FindAll(flattenedAllOfs, static x => !x.IsReferencedSchema());
        var referencedSchemas = Array.FindAll(flattenedAllOfs, static x => x.IsReferencedSchema());
        var rootSchemaHasProperties = schema.HasAnyProperty();
        // if the schema is meaningful, we only want to consider the root schema for naming to avoid "grabbing" the name of the parent
        // if the schema has no reference id we're either at the beginning of an inline schema, or expanding the inheritance tree
        var shouldNameLookupConsiderSubSchemas = schema.IsSemanticallyMeaningful() || string.IsNullOrEmpty(referenceId);
        var className = (schema.GetSchemaName(shouldNameLookupConsiderSubSchemas) is string cName && !string.IsNullOrEmpty(cName) ?
                cName :
                (!string.IsNullOrEmpty(typeNameForInlineSchema) ?
                    typeNameForInlineSchema :
                    currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, schema: schema, requestBody: isRequestBody)))
            .CleanupSymbolName();
        var codeDeclaration = (rootSchemaHasProperties, inlineSchemas, referencedSchemas, isViaDiscriminator) switch
        {
            // greatest parent type
            (true, { Length: 0 }, { Length: 0 }, _) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace),
            // inline schema + referenced schema
            (false, { Length: > 0 }, { Length: 1 }, _) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema.MergeAllOfSchemaEntries([.. referencedSchemas], static x => x is not null)!, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // properties + referenced schema
            (true, { Length: 0 }, { Length: 1 }, _) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // properties + inline schema
            (true, { Length: 1 }, { Length: 0 }, _) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, inlineSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema)),
            // empty schema + referenced schema
            (false, { Length: 0 }, { Length: 1 }, false) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, referencedSchemas[0], className, shortestNamespace),
            // empty schema + inline schema
            (false, { Length: 1 }, { Length: 0 }, false) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, inlineSchemas[0], className, shortestNamespace),
            // empty schema + referenced schema and referenced by oneOf discriminator
            (false, { Length: 0 }, { Length: 1 }, true) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // empty schema + inline schema and referenced by oneOf discriminator
            (false, { Length: 1 }, { Length: 0 }, true) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, inlineSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema)),
            // too much information but we can make a choice -> maps to properties + inline schema
            (true, { Length: 1 }, { Length: 1 }, _) when inlineSchemas[0].HasAnyProperty() && !referencedSchemas[0].HasAnyProperty() =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, inlineSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema)),
            // too much information but we can make a choice -> maps to properties + referenced schema
            (true, { Length: 1 }, { Length: 1 }, _) when referencedSchemas[0].HasAnyProperty() && !inlineSchemas[0].HasAnyProperty() =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // too much information but we can merge root + inline schema
            (true, { Length: 1 }, { Length: 1 }, _) when referencedSchemas[0].HasAnyProperty() && inlineSchemas[0].HasAnyProperty() && schema.MergeAllOfSchemaEntries([.. referencedSchemas]) is { } mergedSchema =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, mergedSchema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchemas[0], operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // none of the allOf entries have properties, it's a grandparent schema
            (true, { Length: 1 }, { Length: 1 }, _) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema, className, shortestNamespace),
            // too many entries, we mush everything together
            (_, { Length: > 1 }, { Length: > 1 }, _) or (_, { Length: 0 or 1 }, { Length: > 1 }, _) or (_, { Length: > 1 }, { Length: 0 or 1 }, _) =>
                AddModelDeclarationIfDoesntExist(currentNode, operation, schema.MergeAllOfSchemaEntries()!, className, shortestNamespace),
            // meaningless scenario
            (false, { Length: 0 }, { Length: 0 }, _) =>
                throw new InvalidOperationException($"The type does not contain any information Path: {currentNode.Path}, Reference Id: {referenceId}"),
        };
        if (codeDeclaration is not CodeClass currentClass) throw new InvalidOperationException("Inheritance is only supported for classes");
        if (!currentClass.Documentation.DescriptionAvailable &&
            new string[] { schema.Description ?? string.Empty }
                        .Union(schema.AllOf?
                                    .OfType<OpenApiSchema>()
                                    .Select(static x => x.Description) ?? [])
                        .FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string description)
            currentClass.Documentation.DescriptionTemplate = description.CleanupDescription(); // the last allof entry often is not a reference and doesn't have a description.

        return currentClass;
    }
    private CodeTypeBase CreateComposedModelDeclaration(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, OpenApiOperation? operation, string suffixForInlineSchema, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema)
    {
        var typeName = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: suffixForInlineSchema, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var typesCount = schema.AnyOf?.Count ?? schema.OneOf?.Count ?? 0;
        if ((typesCount == 1 && (schema.Type & JsonSchemaType.Null) is JsonSchemaType.Null && schema.IsInclusiveUnion() || // nullable on the root schema outside of anyOf
            typesCount == 2 && (schema.AnyOf?.Any(static x => // nullable on a schema in the anyOf
                                                        (x.Type & JsonSchemaType.Null) is JsonSchemaType.Null &&
                                                        !x.HasAnyProperty() &&
                                                        !x.IsExclusiveUnion() &&
                                                        !x.IsInclusiveUnion() &&
                                                        !x.IsInherited() &&
                                                        !x.IsIntersection() &&
                                                        !x.IsArray() &&
                                                        !x.IsReferencedSchema()) ?? false)) &&
            schema.AnyOf?.FirstOrDefault(static x => !string.IsNullOrEmpty(x.GetSchemaName())) is { } targetSchema)
        {
            var className = targetSchema.GetSchemaName().CleanupSymbolName();
            var shortestNamespace = GetShortestNamespace(codeNamespace, targetSchema);
            return new CodeType
            {
                TypeDefinition = AddModelDeclarationIfDoesntExist(currentNode, operation, targetSchema, className, shortestNamespace),
                CollectionKind = targetSchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default
            };// so we don't create unnecessary union types when anyOf was used only for nullable.
        }
        var (unionType, schemas) = (schema.IsExclusiveUnion(), schema.IsInclusiveUnion()) switch
        {
            (true, false) => (new CodeUnionType
            {
                Name = typeName,
            } as CodeComposedTypeBase, schema.OneOf),
            (false, true) => (new CodeIntersectionType
            {
                Name = typeName,
            }, schema.AnyOf),
            (_, _) => throw new InvalidOperationException("Schema is not oneOf nor anyOf"),
        };
        if (schema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId))
            unionType.TargetNamespace = codeNamespace.GetRootNamespace().FindOrAddNamespace(GetModelsNamespaceNameFromReferenceId(refId));
        unionType.DiscriminatorInformation.DiscriminatorPropertyName = schema.GetDiscriminatorPropertyName();
        GetDiscriminatorMappings(currentNode, schema, codeNamespace, null, operation)
            ?.ToList()
            .ForEach(x => unionType.DiscriminatorInformation.AddDiscriminatorMapping(x.Key, x.Value));
        var membersWithNoName = 0;
        if (schemas is not null)
            foreach (var currentSchema in schemas)
            {
                var shortestNamespace = GetShortestNamespace(codeNamespace, currentSchema);
                var className = currentSchema.GetSchemaName().CleanupSymbolName();
                if (string.IsNullOrEmpty(className))
                    if (GetPrimitiveType(currentSchema) is CodeType primitiveType && !string.IsNullOrEmpty(primitiveType.Name))
                    {
                        if (currentSchema.IsArray())
                            primitiveType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
                        if (!unionType.ContainsType(primitiveType))
                            unionType.AddType(primitiveType);
                        continue;
                    }
                    else
                        className = $"{unionType.Name}Member{++membersWithNoName}";
                var declarationType = new CodeType
                {
                    TypeDefinition = AddModelDeclarationIfDoesntExist(currentNode, operation, currentSchema, className, shortestNamespace, null),
                    CollectionKind = currentSchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default
                };
                if (!unionType.ContainsType(declarationType))
                    unionType.AddType(declarationType);
            }
        if (schema.IsArrayOfTypes())
        {
            AddTypeArrayMemberToComposedType(schema, JsonSchemaType.Boolean, unionType);
            AddTypeArrayMemberToComposedType(schema, JsonSchemaType.Integer, unionType);
            AddTypeArrayMemberToComposedType(schema, JsonSchemaType.Number, unionType);
            AddTypeArrayMemberToComposedType(schema, JsonSchemaType.String, unionType);
        }
        return unionType;
    }
    private void AddTypeArrayMemberToComposedType(IOpenApiSchema schema, JsonSchemaType typeToScan, CodeComposedTypeBase codeComposedTypeBase)
    {
        if (!schema.Type.HasValue || (schema.Type.Value & typeToScan) != typeToScan) return;

        var temporarySchema = new OpenApiSchema { Type = typeToScan, Format = schema.Format };
        if (GetPrimitiveType(temporarySchema) is CodeType primitiveType && !string.IsNullOrEmpty(primitiveType.Name))
        {
            if (schema.IsArray())
                primitiveType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
            if (!codeComposedTypeBase.ContainsType(primitiveType))
                codeComposedTypeBase.AddType(primitiveType);
        }
    }
    private CodeTypeBase CreateModelDeclarations(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, OpenApiOperation? operation, CodeElement parentElement, string suffixForInlineSchema, IOpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false, bool isViaDiscriminator = false)
    {
        var (codeNamespace, responseValue, suffix) = schema.IsReferencedSchema() switch
        {
            true => (GetShortestNamespace(parentElement.GetImmediateParentOfType<CodeNamespace>(), schema), response, string.Empty), // referenced schema
            false => (parentElement.GetImmediateParentOfType<CodeNamespace>(), null, suffixForInlineSchema), // Inline schema, i.e. specific to the Operation
        };

        // If typeNameForInlineSchema is not null and the schema is referenced, we have most likely unwrapped a referenced schema(most likely from an AllOf/OneOf/AnyOf).
        // Therefore the current type/schema is not really inlined, so invalidate the typeNameForInlineSchema and just work with the information from the schema reference.
        if (schema.IsReferencedSchema() && !string.IsNullOrEmpty(typeNameForInlineSchema))
        {
            typeNameForInlineSchema = string.Empty;
        }

        if (schema.IsInherited())
        {
            // Pass isViaDiscriminator so that we can handle the special case where this model was referenced by a discriminator and we always want to generate a base class.
            return CreateInheritedModelDeclarationAndType(currentNode, schema, operation, suffix, codeNamespace, isRequestBody, typeNameForInlineSchema, isViaDiscriminator);
        }

        if (schema.IsIntersection() && schema.MergeIntersectionSchemaEntries() is IOpenApiSchema mergedSchema)
        {
            // multiple allOf entries that do not translate to inheritance
            return CreateModelDeclarationAndType(currentNode, mergedSchema, operation, codeNamespace, suffix, response: responseValue, typeNameForInlineSchema: typeNameForInlineSchema, isRequestBody);
        }

        if ((schema.IsInclusiveUnion() || schema.IsExclusiveUnion()) && string.IsNullOrEmpty(schema.Format)
            && !schema.IsODataPrimitiveType())
        { // OData types are oneOf string, type + format, enum
            return CreateComposedModelDeclaration(currentNode, schema, operation, suffix, codeNamespace, isRequestBody, typeNameForInlineSchema);
        }

        // type: object with single oneOf referring to inheritance or intersection
        if (schema.IsObjectType() && schema.MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries() is IOpenApiSchema mergedExclusiveUnionSchema)
        {
            return CreateModelDeclarations(currentNode, mergedExclusiveUnionSchema, operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema, isRequestBody);
        }

        // type: object with single anyOf referring to inheritance or intersection
        if (schema.IsObjectType() && schema.MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries() is IOpenApiSchema mergedInclusiveUnionSchema)
        {
            return CreateModelDeclarations(currentNode, mergedInclusiveUnionSchema, operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema, isRequestBody);
        }

        if (schema.IsObjectType() || schema.HasAnyProperty() || schema.IsEnum() || schema.AdditionalProperties?.Type is not null)
        {
            // no inheritance or union type, often empty definitions with only additional properties are used as property bags.
            return CreateModelDeclarationAndType(currentNode, schema, operation, codeNamespace, suffix, response: responseValue, typeNameForInlineSchema: typeNameForInlineSchema, isRequestBody);
        }

        if (schema.IsArray() &&
            !schema.Items.IsArray()) // Only handle collections of primitives and complex types. Otherwise, multi-dimensional arrays would be recursively unwrapped undesirably to lead to incorrect serialization types.
        {
            // collections at root
            return CreateCollectionModelDeclaration(currentNode, schema, operation, codeNamespace, typeNameForInlineSchema, isRequestBody);
        }

        if (schema.Type is not null && (schema.Type & ~JsonSchemaType.Null) != 0 || !string.IsNullOrEmpty(schema.Format))
            return GetPrimitiveType(schema) ?? new CodeType { Name = UntypedNodeName, IsExternal = true };
        if ((schema.AnyOf is { Count: > 0 } || schema.OneOf is { Count: > 0 } || schema.AllOf is { Count: > 0 }) &&
           (schema.AnyOf?.FirstOrDefault(static x => x.IsSemanticallyMeaningful(true)) ?? schema.OneOf?.FirstOrDefault(static x => x.IsSemanticallyMeaningful(true)) ?? schema.AllOf?.FirstOrDefault(static x => x.IsSemanticallyMeaningful(true))) is { } childSchema) // we have an empty node because of some local override for schema properties and need to unwrap it.
            return CreateModelDeclarations(currentNode, childSchema, operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema, isRequestBody);
        return new CodeType { Name = UntypedNodeName, IsExternal = true };
    }
    private CodeTypeBase CreateCollectionModelDeclaration(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string typeNameForInlineSchema, bool isRequestBody)
    {
        CodeTypeBase? type = GetPrimitiveType(schema.Items);
        var isEnumOrComposedCollectionType = schema.Items.IsEnum() //the collection could be an enum type so override with strong type instead of string type.
                                    || schema.Items.IsComposedEnum() && string.IsNullOrEmpty(schema.Items?.Format);//the collection could be a composed type with an enum type so override with strong type instead of string type.
        if ((string.IsNullOrEmpty(type?.Name)
               || isEnumOrComposedCollectionType)
               && schema.Items != null)
        {
            var targetNamespace = GetShortestNamespace(codeNamespace, schema.Items);
            type = CreateModelDeclarations(currentNode, schema.Items, operation, targetNamespace, string.Empty, typeNameForInlineSchema: typeNameForInlineSchema, isRequestBody: isRequestBody);
        }
        if (type is null)
            return new CodeType { Name = UntypedNodeName, IsExternal = true };
        type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        return type;
    }
    private CodeElement? GetExistingDeclaration(CodeNamespace currentNamespace, OpenApiUrlTreeNode currentNode, string declarationName)
    {
        var localNameSpace = GetSearchNamespace(currentNode, currentNamespace);
        return localNameSpace.FindChildByName<ITypeDefinition>(declarationName, false) as CodeElement;
    }
    private CodeNamespace GetSearchNamespace(OpenApiUrlTreeNode currentNode, CodeNamespace currentNamespace)
    {
        if (modelsNamespace != null && currentNode.DoesNodeBelongToItemSubnamespace() && !currentNamespace.Name.Contains(modelsNamespace.Name, StringComparison.Ordinal))
            return currentNamespace.EnsureItemNamespace();
        return currentNamespace;
    }
    private ConcurrentDictionary<string, ModelClassBuildLifecycle> classLifecycles = new(StringComparer.OrdinalIgnoreCase);
    private CodeElement AddModelDeclarationIfDoesntExist(OpenApiUrlTreeNode currentNode, OpenApiOperation? currentOperation, IOpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, CodeClass? inheritsFrom = null)
    {
        if (GetExistingDeclaration(currentNamespace, currentNode, declarationName) is not CodeElement existingDeclaration) // we can find it in the components
        {
            if (AddEnumDeclaration(currentNode, schema, declarationName, currentNamespace) is CodeEnum enumDeclaration)
                return enumDeclaration;

            if (schema.IsIntersection() && schema.MergeIntersectionSchemaEntries() is { } mergedSchema &&
                AddModelDeclarationIfDoesntExist(currentNode, currentOperation, mergedSchema, declarationName, currentNamespace, inheritsFrom) is CodeClass createdClass)
            {
                // multiple allOf entries that do not translate to inheritance
                return createdClass;
            }
            else if (schema.MergeInclusiveUnionSchemaEntries() is { } iUMergedSchema &&
                AddModelClass(currentNode, iUMergedSchema, declarationName, currentNamespace, currentOperation, inheritsFrom) is CodeClass uICreatedClass)
            {
                return uICreatedClass;
            }
            else if (schema.MergeExclusiveUnionSchemaEntries() is { } eUMergedSchema &&
                AddModelClass(currentNode, eUMergedSchema, declarationName, currentNamespace, currentOperation, inheritsFrom) is CodeClass uECreatedClass)
            {
                return uECreatedClass;
            }
            return AddModelClass(currentNode, schema, declarationName, currentNamespace, currentOperation, inheritsFrom);
        }
        return existingDeclaration;
    }
    private CodeEnum? AddEnumDeclarationIfDoesntExist(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, string declarationName, CodeNamespace currentNamespace)
    {
        if (GetExistingDeclaration(currentNamespace, currentNode, declarationName) is not CodeEnum existingDeclaration) // we can find it in the components
        {
            return AddEnumDeclaration(currentNode, schema, declarationName, currentNamespace);
        }
        return existingDeclaration;
    }
    private static CodeEnum? AddEnumDeclaration(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, string declarationName, CodeNamespace currentNamespace)
    {
        if (schema.IsEnum())
        {
            var schemaDescription = schema.Description.CleanupDescription();
            OpenApiEnumFlagsExtension? enumFlagsExtension = null;
            if (schema.Extensions is not null && schema.Extensions.TryGetValue(OpenApiEnumFlagsExtension.Name, out var rawExtension) &&
                rawExtension is OpenApiEnumFlagsExtension flagsExtension)
            {
                enumFlagsExtension = flagsExtension;
            }
            var newEnum = new CodeEnum
            {
                Name = declarationName,
                Flags = enumFlagsExtension?.IsFlags ?? false,
                Documentation = new()
                {
                    DescriptionTemplate = !string.IsNullOrEmpty(schemaDescription) || !string.IsNullOrEmpty(schema.GetReferenceId()) ?
                                        schemaDescription : // if it's a referenced component, we shouldn't use the path item description as it makes it indeterministic
                                        currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel),
                },
                Deprecation = schema.GetDeprecationInformation(),
            };
            SetEnumOptions(schema, newEnum);
            return currentNamespace.AddEnum(newEnum).First();
        }
        return default;
    }
    private static void SetEnumOptions(IOpenApiSchema schema, CodeEnum target)
    {
        OpenApiEnumValuesDescriptionExtension? extensionInformation = null;
        if (schema.Extensions is not null && schema.Extensions.TryGetValue(OpenApiEnumValuesDescriptionExtension.Name, out var rawExtension) && rawExtension is OpenApiEnumValuesDescriptionExtension localExtInfo)
            extensionInformation = localExtInfo;
        target.AddOption(schema.Enum?.OfType<JsonValue>()
                        .Where(static x => x.GetValueKind() is JsonValueKind.String or JsonValueKind.Number)
                        .Select(static x => x.GetValueKind() is JsonValueKind.String ? x.GetValue<string>() : x.GetValue<decimal>().ToString(CultureInfo.InvariantCulture))
                        .Where(static x => !string.IsNullOrEmpty(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select((x) =>
                        {
                            var optionDescription = extensionInformation?.ValuesDescriptions.Find(y => y.Value.Equals(x, StringComparison.OrdinalIgnoreCase));
                            return new CodeEnumOption
                            {
                                Name = (optionDescription?.Name is string name && !string.IsNullOrEmpty(name) ?
                                        name :
                                        x).CleanupSymbolName(),
                                SerializationName = x,
                                Documentation = new()
                                {
                                    DescriptionTemplate = optionDescription?.Description ?? string.Empty,
                                },
                            };
                        })
                        .Where(static x => !string.IsNullOrEmpty(x.Name))
                        .ToArray() ?? []);
    }
    private CodeNamespace GetShortestNamespace(CodeNamespace currentNamespace, IOpenApiSchema currentSchema)
    {
        if (currentSchema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId) && rootNamespace != null)
        {
            var parentClassNamespaceName = GetModelsNamespaceNameFromReferenceId(refId);
            return rootNamespace.AddNamespace(parentClassNamespaceName);
        }
        return currentNamespace;
    }
    private CodeClass AddModelClass(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, OpenApiOperation? currentOperation, CodeClass? inheritsFrom = null)
    {
        if (inheritsFrom == null && schema.AllOf?.OfType<OpenApiSchemaReference>().ToArray() is { Length: 1 } referencedSchemas)
        {// any non-reference would be the current class in some description styles
            var parentSchema = referencedSchemas[0];
            var parentClassNamespace = GetShortestNamespace(currentNamespace, parentSchema);
            inheritsFrom = (CodeClass)AddModelDeclarationIfDoesntExist(currentNode, currentOperation, parentSchema, parentSchema.GetSchemaName().CleanupSymbolName(), parentClassNamespace);
        }
        var newClassStub = new CodeClass
        {
            Name = declarationName,
            Kind = CodeClassKind.Model,
            Documentation = new()
            {
                DocumentationLabel = schema.ExternalDocs?.Description ?? string.Empty,
                DocumentationLink = schema.ExternalDocs?.Url,
                DescriptionTemplate = (string.IsNullOrEmpty(schema.Description) ? schema.AllOf?.FirstOrDefault(static x => !x.IsReferencedSchema() && !string.IsNullOrEmpty(x.Description))?.Description : schema.Description).CleanupDescription(),
            },
            Deprecation = schema.GetDeprecationInformation(),
        };
        if (inheritsFrom != null)
            newClassStub.StartBlock.Inherits = new CodeType { TypeDefinition = inheritsFrom };

        // Add the class to the namespace after the serialization members
        // as other threads looking for the existence of the class may find the class but the additional data/backing store properties may not be fully populated causing duplication
        var includeAdditionalDataProperties = config.IncludeAdditionalData && (schema.AdditionalPropertiesAllowed || schema.AdditionalProperties is not null);
        AddSerializationMembers(newClassStub, includeAdditionalDataProperties, config.UsesBackingStore, static s => s);

        var newClass = currentNamespace.AddClass(newClassStub).First();
        var lifecycle = classLifecycles.GetOrAdd(currentNamespace.Name + "." + declarationName, static n => new());
        if (!lifecycle.IsPropertiesBuilt())
        {
            try
            {
                lifecycle.StartBuildingProperties();
                if (!lifecycle.IsPropertiesBuilt())
                {
                    if (inheritsFrom != null)
                    {
                        classLifecycles.TryGetValue(inheritsFrom.Parent!.Name + "." + inheritsFrom.Name, out var superClassLifecycle);
                        superClassLifecycle!.WaitForPropertiesBuilt();
                    }
                    CreatePropertiesForModelClass(currentNode, schema, currentNamespace, newClass); // order matters since we might be recursively generating ancestors for discriminator mappings and duplicating additional data/backing store properties
                }
            }
            finally
            {
                lifecycle.PropertiesBuildingDone();
            }
        }

        var lookupSchema = schema.GetMergedSchemaOriginalReferenceId() is string originalName ?
            new OpenApiSchemaReference(originalName, openApiDocument) :
            schema;
        // Recurse into the discriminator & generate its referenced types
        var mappings = GetDiscriminatorMappings(currentNode, lookupSchema, currentNamespace, newClass, currentOperation)
                        .Where(x => x.Value is { TypeDefinition: CodeClass definition } &&
                                    definition.DerivesFrom(newClass)); // only the mappings that derive from the current class

        AddDiscriminatorMethod(newClass, schema.GetDiscriminatorPropertyName(), mappings, static s => s);
        return newClass;
    }
    /// <summary>
    /// Creates a reference to the component so inheritance scenarios get the right class name
    /// </summary>
    /// <param name="componentName">Component to lookup</param>
    /// <returns>A reference to the component schema</returns>
    private OpenApiSchemaReference? GetSchemaReferenceToComponentSchema(string componentName)
    {
        if (openApiDocument?.Components?.Schemas?.ContainsKey(componentName) ?? false)
            return new OpenApiSchemaReference(componentName, openApiDocument);
        return null;
    }
    private IEnumerable<KeyValuePair<string, CodeType>> GetDiscriminatorMappings(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, CodeNamespace currentNamespace, CodeClass? baseClass, OpenApiOperation? currentOperation)
    {
        // Generate types that this discriminator references
        return schema.GetDiscriminatorMappings(inheritanceIndex)
                .Union(baseClass is not null && modelsNamespace is not null &&
                        (openApiDocument?.Components?.Schemas?.TryGetValue(baseClass.GetComponentSchemaName(modelsNamespace), out var componentSchema) ?? false) ?
                        componentSchema.GetDiscriminatorMappings(inheritanceIndex) :
                         [])
                .Select(x => KeyValuePair.Create(x.Key, GetCodeTypeForMapping(currentNode, x.Value, currentNamespace, baseClass, currentOperation)))
                .Where(static x => x.Value != null)
                .Select(static x => KeyValuePair.Create(x.Key, x.Value!));
    }
    private static IEnumerable<ITypeDefinition> GetAllModels(CodeNamespace currentNamespace)
    {
        var classes = currentNamespace.Classes.ToArray();
        return classes.Union(classes.SelectMany(GetAllInnerClasses))
                            .Where(static x => x.IsOfKind(CodeClassKind.Model))
                            .OfType<ITypeDefinition>()
                            .Union(currentNamespace.Enums)
                            .Union(currentNamespace.Namespaces.SelectMany(static x => GetAllModels(x)));
    }
    private static IEnumerable<CodeClass> GetAllInnerClasses(CodeClass currentClass)
    {
        return currentClass.InnerClasses.Union(currentClass.InnerClasses.SelectMany(static x => GetAllInnerClasses(x)));
    }
    private void TrimInheritedModels()
    {
        if (modelsNamespace is null || rootNamespace is null || modelsNamespace.Parent is not CodeNamespace clientNamespace) return;
        var reusableModels = GetAllModels(modelsNamespace).ToArray();//to avoid multiple enumerations
        var modelsDirectlyInUse = GetTypeDefinitionsInNamespace(rootNamespace).Union(multipartPropertiesModels.Keys).ToArray();
        var classesDirectlyInUse = modelsDirectlyInUse.OfType<CodeClass>().ToHashSet();
        var allModelClassesIndex = GetDerivationIndex(GetAllModels(clientNamespace).OfType<CodeClass>());
        var derivedClassesInUse = GetDerivedDefinitions(allModelClassesIndex, classesDirectlyInUse.ToArray());
        var baseOfModelsInUse = classesDirectlyInUse.SelectMany(static x => x.GetInheritanceTree(false, false));
        var classesInUse = derivedClassesInUse.Union(classesDirectlyInUse).Union(baseOfModelsInUse).ToHashSet();
        var reusableClassesDerivationIndex = GetDerivationIndex(reusableModels.OfType<CodeClass>());
        var reusableClassesInheritanceIndex = GetInheritanceIndex(allModelClassesIndex);
        var relatedModels = classesInUse.SelectMany(x => GetRelatedDefinitions(x, reusableClassesDerivationIndex, reusableClassesInheritanceIndex)).Union(modelsDirectlyInUse.OfType<CodeEnum>()).ToHashSet();// re-including models directly in use for enums
        Parallel.ForEach(reusableModels, parallelOptions, x =>
        {
            if (relatedModels.Contains(x) || classesInUse.Contains(x)) return;
            if (x is CodeClass currentClass)
            {
                var parents = currentClass.GetInheritanceTree(false, false);
                if (parents.Any(y => classesDirectlyInUse.Contains(y))) return; // to support the inheritance recursive downcast
                foreach (var baseClass in parents) // discriminator might also be in grand parent types
                    baseClass.DiscriminatorInformation.RemoveDiscriminatorMapping(currentClass);
            }
            logger.LogInformation("Removing unused model {ModelName} as it is not referenced by the client API surface", x.Name);
            x.GetImmediateParentOfType<CodeNamespace>().RemoveChildElement(x);
        });
        foreach (var leafNamespace in FindLeafNamespaces(modelsNamespace))
            RemoveEmptyNamespaces(leafNamespace, modelsNamespace);
    }
    private ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> GetDerivationIndex(IEnumerable<CodeClass> models)
    {
        var result = new ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>>();
        Parallel.ForEach(models, parallelOptions, x =>
        {
            if (x.BaseClass is CodeClass parentClass && !result.TryAdd(parentClass, [x]))
                result[parentClass].Add(x);
        });
        return result;
    }
    private ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> GetInheritanceIndex(ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> derivedIndex)
    {
        var result = new ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>>();
        Parallel.ForEach(derivedIndex, parallelOptions, entry =>
        {
            foreach (var derivedClass in entry.Value)
                if (!result.TryAdd(derivedClass, [entry.Key]))
                    result[derivedClass].Add(entry.Key);
        });
        return result;
    }
    private static IEnumerable<CodeNamespace> FindLeafNamespaces(CodeNamespace currentNamespace)
    {
        if (!currentNamespace.Namespaces.Any()) return new[] { currentNamespace };
        return currentNamespace.Namespaces.SelectMany(FindLeafNamespaces);
    }
    private static void RemoveEmptyNamespaces(CodeNamespace currentNamespace, CodeNamespace stopAtNamespace)
    {
        if (currentNamespace == stopAtNamespace) return;
        if (currentNamespace.Parent is not CodeNamespace parentNamespace) return;
        if (!currentNamespace.Classes.Any() &&
            !currentNamespace.Enums.Any() &&
            !currentNamespace.Namespaces.Any())
            parentNamespace.RemoveChildElement(currentNamespace);
        RemoveEmptyNamespaces(parentNamespace, stopAtNamespace);
    }
    private static IEnumerable<CodeClass> GetDerivedDefinitions(ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> models, CodeClass[] modelsInUse)
    {
        var currentDerived = modelsInUse.SelectMany(x => models.TryGetValue(x, out var res) ? res : Enumerable.Empty<CodeClass>()).ToArray();
        return currentDerived.Union(currentDerived.SelectMany(x => GetDerivedDefinitions(models, [x])));
    }
    private static IEnumerable<ITypeDefinition> GetRelatedDefinitions(ITypeDefinition currentElement, ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> derivedIndex, ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> inheritanceIndex, ConcurrentDictionary<CodeElement, bool>? visited = null)
    {
        visited ??= new();
        if (currentElement is not CodeClass currentClass || !visited.TryAdd(currentClass, true)) return Enumerable.Empty<ITypeDefinition>();
        var propertiesDefinitions = currentClass.Properties
                            .SelectMany(static x => x.Type.AllTypes)
                            .Select(static x => x.TypeDefinition)
                            .OfType<ITypeDefinition>()
                            .Where(static x => x is CodeClass || x is CodeEnum)
                            .SelectMany(x => x is CodeClass classDefinition ?
                                            (inheritanceIndex.TryGetValue(classDefinition, out var res) ? res : Enumerable.Empty<CodeClass>())
                                                .Union(GetDerivedDefinitions(derivedIndex, [classDefinition]))
                                                .Union(new[] { classDefinition })
                                                .OfType<ITypeDefinition>() :
                                            new[] { x })
                            .Distinct()
                            .ToArray();
        var propertiesParentTypes = propertiesDefinitions.OfType<CodeClass>().SelectMany(static x => x.GetInheritanceTree(false, false)).OfType<ITypeDefinition>().ToArray();
        return propertiesDefinitions
                .Union(propertiesParentTypes)
                .Union(propertiesParentTypes.SelectMany(x => GetRelatedDefinitions(x, derivedIndex, inheritanceIndex, visited)))
                .Union(propertiesDefinitions.SelectMany(x => GetRelatedDefinitions(x, derivedIndex, inheritanceIndex, visited))).ToArray();
    }
    private IEnumerable<CodeNamespace> GetAllNamespaces(CodeNamespace currentNamespace)
    {
        if (currentNamespace == modelsNamespace) return Enumerable.Empty<CodeNamespace>();
        return new[] { currentNamespace }.Union(currentNamespace.Namespaces.SelectMany(GetAllNamespaces));
    }
    private IEnumerable<CodeElement> GetTypeDefinitionsInNamespace(CodeNamespace currentNamespace)
    {
        var requestExecutors = GetAllNamespaces(currentNamespace)
                            .SelectMany(static x => x.Classes)
                            .Where(static x => x.IsOfKind(CodeClassKind.RequestBuilder))
                            .SelectMany(static x => x.Methods)
                            .Where(static x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        return requestExecutors.SelectMany(static x => x.ReturnType.AllTypes)
                        .Union(requestExecutors
                                .SelectMany(static x => x.Parameters)
                                .Where(static x => x.IsOfKind(CodeParameterKind.RequestBody))
                                .SelectMany(static x => x.Type.AllTypes))
                        .Union(requestExecutors.SelectMany(static x => x.Parameters)
                                .Where(static x => x.IsOfKind(CodeParameterKind.RequestConfiguration))
                                .SelectMany(static x => x.Type.AllTypes.Select(static y => y.TypeDefinition))
                                .OfType<CodeClass>()
                                .Select(static x => x.Properties.FirstOrDefault(static y => y.Kind is CodePropertyKind.QueryParameters)?.Type)
                                .OfType<CodeType>())
                        .Union(requestExecutors.SelectMany(static x => x.ErrorMappings.SelectMany(static y => y.Value.AllTypes)))
                        .Where(static x => x.TypeDefinition != null)
                        .Select(static x => x.TypeDefinition!)
                        .Where(static x => x is CodeClass || x is CodeEnum);
    }
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex = new();
    private void InitializeInheritanceIndex()
    {
        openApiDocument?.InitializeInheritanceIndex(inheritanceIndex);
    }
    internal static void AddDiscriminatorMethod(CodeClass newClass, string discriminatorPropertyName, IEnumerable<KeyValuePair<string, CodeType>> discriminatorMappings, Func<string, string> refineMethodName)
    {
        var factoryMethod = new CodeMethod
        {
            Name = refineMethodName("CreateFromDiscriminatorValue"),
            Documentation = new()
            {
                DescriptionTemplate = "Creates a new instance of the appropriate class based on discriminator value",
            },
            ReturnType = new CodeType { TypeDefinition = newClass, IsNullable = false },
            Kind = CodeMethodKind.Factory,
            IsStatic = true,
            IsAsync = false,
            Parent = newClass,
        };
        discriminatorMappings?.ToList()
                .ForEach(x => newClass.DiscriminatorInformation.AddDiscriminatorMapping(x.Key, x.Value));
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Documentation = new()
            {
                DescriptionTemplate = "The parse node to use to read the discriminator value and create the object",
            },
            Optional = false,
            Type = new CodeType { Name = ParseNodeInterface, IsExternal = true },
        });
        newClass.DiscriminatorInformation.DiscriminatorPropertyName = discriminatorPropertyName;
        newClass.AddMethod(factoryMethod);
    }
    private CodeType? GetCodeTypeForMapping(OpenApiUrlTreeNode currentNode, string referenceId, CodeNamespace currentNamespace, CodeClass? baseClass, OpenApiOperation? currentOperation)
    {
        var componentKey = referenceId?.Replace("#/components/schemas/", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (openApiDocument == null || string.IsNullOrEmpty(componentKey) || openApiDocument.Components?.Schemas is null || GetSchemaReferenceToComponentSchema(componentKey) is not { } discriminatorSchema)
        {
            logger.LogWarning("Discriminator {ComponentKey} not found in the OpenAPI document.", componentKey);
            return null;
        }
        var schemaClone = discriminatorSchema.CreateShallowCopy();
        // Call CreateModelDeclarations with isViaDiscriminator=true. This is for a special case where we always generate a base class when types are referenced via a oneOf discriminator.
        if (CreateModelDeclarations(currentNode, schemaClone, currentOperation, GetShortestNamespace(currentNamespace, schemaClone), string.Empty, null, string.Empty, false, true) is not CodeType result)
        {
            logger.LogWarning("Discriminator {ComponentKey} is not a valid model and points to a union type.", componentKey);
            return null;
        }
        if (baseClass is not null && (result.TypeDefinition is not CodeClass codeClass || codeClass.StartBlock.Inherits is null))
        {
            if (!baseClass.Equals(result.TypeDefinition))// don't log warning if the discriminator points to the base type itself as this is implicitly the default case.
                logger.LogWarning("Discriminator {ComponentKey} is not inherited from {ClassName}.", componentKey, baseClass.Name);
            return null;
        }
        return result;
    }
    private void CreatePropertiesForModelClass(OpenApiUrlTreeNode currentNode, IOpenApiSchema schema, CodeNamespace ns, CodeClass model)
    {
        var propertiesToAdd = schema.Properties
                ?.Select(x =>
                {
                    var propertySchema = x.Value;
                    var className = $"{model.Name}_{x.Key.CleanupSymbolName()}";
                    var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(propertySchema.GetReferenceId());
                    var targetNamespace = string.IsNullOrEmpty(shortestNamespaceName) ? ns :
                                        rootNamespace?.FindOrAddNamespace(shortestNamespaceName) ?? ns;
                    var definition = CreateModelDeclarations(currentNode, propertySchema, default, targetNamespace, string.Empty, typeNameForInlineSchema: className);
                    if (definition == null)
                    {
                        logger.LogWarning("Omitted property {PropertyName} for model {ModelName} in API path {ApiPath}, the schema is invalid.", x.Key, model.Name, currentNode.Path);
                        return null;
                    }
                    return CreateProperty(x.Key, definition.Name, propertySchema: propertySchema, existingType: definition);
                })
                .OfType<CodeProperty>()
                .ToArray() ?? [];
        if (propertiesToAdd.Length != 0)
            model.AddProperty(propertiesToAdd);
    }
    private const string FieldDeserializersMethodName = "GetFieldDeserializers";
    private const string SerializeMethodName = "Serialize";
    private const string AdditionalDataPropName = "AdditionalData";
    private const string BackingStorePropertyName = "BackingStore";
    private const string BackingStoreInterface = "IBackingStore";
    internal const string BackedModelInterface = "IBackedModel";
    private const string ParseNodeInterface = "IParseNode";
    internal const string AdditionalHolderInterface = "IAdditionalDataHolder";
    private static readonly char[] manifestPathSeparator = ['#'];
    internal static void AddSerializationMembers(CodeClass model, bool includeAdditionalProperties, bool usesBackingStore, Func<string, string> refineMethodName)
    {
        var serializationPropsType = $"IDictionary<string, Action<{ParseNodeInterface}>>";
        if (!model.ContainsMember(FieldDeserializersMethodName))
        {
            var deserializeProp = new CodeMethod
            {
                Name = refineMethodName(FieldDeserializersMethodName),
                Kind = CodeMethodKind.Deserializer,
                Access = AccessModifier.Public,
                Documentation = new()
                {
                    DescriptionTemplate = "The deserialization information for the current model",
                },
                IsAsync = false,
                ReturnType = new CodeType
                {
                    Name = serializationPropsType,
                    IsNullable = false,
                    IsExternal = true,
                },
                Parent = model,
            };
            model.AddMethod(deserializeProp);
        }
        if (!model.ContainsMember(SerializeMethodName))
        {
            var serializeMethod = new CodeMethod
            {
                Name = refineMethodName(SerializeMethodName),
                Kind = CodeMethodKind.Serializer,
                IsAsync = false,
                Documentation = new()
                {
                    DescriptionTemplate = "Serializes information the current object",
                },
                ReturnType = new CodeType { Name = VoidType, IsNullable = false, IsExternal = true },
                Parent = model,
            };
            var parameter = new CodeParameter
            {
                Name = "writer",
                Documentation = new()
                {
                    DescriptionTemplate = "Serialization writer to use to serialize this model",
                },
                Kind = CodeParameterKind.Serializer,
                Type = new CodeType { Name = "ISerializationWriter", IsExternal = true, IsNullable = false },
            };
            serializeMethod.AddParameter(parameter);

            model.AddMethod(serializeMethod);
        }
        if (!model.ContainsMember(AdditionalDataPropName) &&
            includeAdditionalProperties &&
            !(model.GetGreatestGrandparent(model)?.ContainsMember(AdditionalDataPropName) ?? false))
        {
            // we don't want to add the property if the parent already has it
            var additionalDataProp = new CodeProperty
            {
                Name = AdditionalDataPropName,
                Access = AccessModifier.Public,
                DefaultValue = "new Dictionary<string, object>()",
                Kind = CodePropertyKind.AdditionalData,
                Documentation = new()
                {
                    DescriptionTemplate = "Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.",
                },
                Type = new CodeType
                {
                    Name = "IDictionary<string, object>",
                    IsNullable = false,
                    IsExternal = true,
                },
            };
            model.AddProperty(additionalDataProp);
            model.StartBlock.AddImplements(new CodeType
            {
                Name = AdditionalHolderInterface,
                IsExternal = true,
            });
        }
        if (!model.ContainsMember(BackingStorePropertyName) &&
            usesBackingStore &&
            !(model.GetGreatestGrandparent(model)?.ContainsMember(BackingStorePropertyName) ?? false))
        {
            var backingStoreProperty = new CodeProperty
            {
                Name = BackingStorePropertyName,
                Access = AccessModifier.Public,
                DefaultValue = "BackingStoreFactorySingleton.Instance.CreateBackingStore()",
                Kind = CodePropertyKind.BackingStore,
                Documentation = new()
                {
                    DescriptionTemplate = "Stores model information.",
                },
                ReadOnly = true,
                Type = new CodeType
                {
                    Name = BackingStoreInterface,
                    IsNullable = false,
                    IsExternal = true,
                },
            };
            model.AddProperty(backingStoreProperty);
            model.StartBlock.AddImplements(new CodeType
            {
                Name = BackedModelInterface,
                IsExternal = true,
            });
        }
    }
    private CodeClass? CreateOperationParameterClass(OpenApiUrlTreeNode node, NetHttpMethod operationType, OpenApiOperation operation, CodeClass parentClass)
    {
        var parameters = (node.PathItems[Constants.DefaultOpenApiLabel].Parameters ?? Enumerable.Empty<IOpenApiParameter>()).Union(operation.Parameters ?? Enumerable.Empty<IOpenApiParameter>()).Where(static p => p.In == ParameterLocation.Query).ToArray();
        if (parameters.Length != 0)
        {
            var parameterClass = parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters",
                Kind = CodeClassKind.QueryParameters,
                Documentation = new()
                {
                    DescriptionTemplate = (operation.Description is string description && !string.IsNullOrEmpty(description) ?
                                    description :
                                    operation.Summary).CleanupDescription(),
                },
            }).First();
            foreach (var parameter in parameters)
                AddPropertyForQueryParameter(node, operationType, parameter, parameterClass);

            return parameterClass;
        }

        return null;
    }
    private void AddPropertyForQueryParameter(OpenApiUrlTreeNode node, NetHttpMethod operationType, IOpenApiParameter parameter, CodeClass parameterClass)
    {
        CodeType? resultType = default;
        var addBackwardCompatibleParameter = false;

        if (parameter.Schema is not null && (parameter.Schema.IsEnum() || (parameter.Schema.IsArray() && parameter.Schema.Items.IsEnum())))
        {
            var enumSchema = parameter.Schema.IsArray() ? parameter.Schema.Items! : parameter.Schema;
            var codeNamespace = enumSchema.IsReferencedSchema() switch
            {
                true => GetShortestNamespace(parameterClass.GetImmediateParentOfType<CodeNamespace>(), enumSchema), // referenced schema
                false => parameterClass.GetImmediateParentOfType<CodeNamespace>(), // Inline schema, i.e. specific to the Operation
            };
            var shortestNamespace = GetShortestNamespace(codeNamespace, enumSchema);
            var enumName = enumSchema.GetSchemaName().CleanupSymbolName();
            if (string.IsNullOrEmpty(enumName))
                enumName = $"{operationType.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}{parameter.Name.CleanupSymbolName().ToFirstCharacterUpperCase()}QueryParameterType";
            if (AddEnumDeclarationIfDoesntExist(node, enumSchema, enumName, shortestNamespace) is { } enumDeclaration)
            {
                resultType = new CodeType
                {
                    TypeDefinition = enumDeclaration,
                    IsNullable = !parameter.Schema.IsArray()
                };
                addBackwardCompatibleParameter = true;
            }
        }
        resultType ??= GetPrimitiveType(parameter.Schema) ?? new CodeType()
        {
            // since its a query parameter default to string if there is no schema
            // it also be an object type, but we'd need to create the model in that case and there's no standard on how to serialize those as query parameters
            Name = "string",
            IsExternal = true,
        };
        resultType.CollectionKind = parameter.Schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default;
        if (parameter.Name?.SanitizeParameterNameForCodeSymbols() is not string propName) return;
        var prop = new CodeProperty
        {
            Name = propName,
            Documentation = new()
            {
                DescriptionTemplate = parameter.Description.CleanupDescription(),
            },
            Kind = CodePropertyKind.QueryParameter,
            Type = resultType,
            Deprecation = parameter.GetDeprecationInformation(),
        };

        if (!parameter.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase))
        {
            prop.SerializationName = parameter.Name.SanitizeParameterNameForUrlTemplate();
        }

        if (!parameterClass.ContainsPropertyWithWireName(prop.WireName))
        {
            if (addBackwardCompatibleParameter && config.IncludeBackwardCompatible && config.Language is GenerationLanguage.CSharp or GenerationLanguage.Go)
            { //TODO remove for v2
                var modernProp = (CodeProperty)prop.Clone();
                modernProp.Name = $"{prop.Name}As{modernProp.Type.Name.ToFirstCharacterUpperCase()}";
                modernProp.SerializationName = prop.WireName;
                prop.Deprecation = new("This property is deprecated, use {TypeName} instead", IsDeprecated: true, TypeReferences: new() { { "TypeName", new CodeType { TypeDefinition = modernProp, IsExternal = false } } });
                prop.Type = GetDefaultQueryParameterType();
                prop.Type.CollectionKind = modernProp.Type.CollectionKind;
                parameterClass.AddProperty(modernProp, prop);
            }
            else
            {
                parameterClass.AddProperty(prop);
            }
        }
        else
        {
            logger.LogWarning("Ignoring duplicate parameter {Name}", parameter.Name);
        }
    }

    private static CodeType GetDefaultQueryParameterType()
    {
        return new()
        {
            IsExternal = true,
            Name = "string",
        };
    }
    private static CodeType GetQueryParameterType(IOpenApiSchema schema)
    {
        var paramType = GetPrimitiveType(schema) ?? new()
        {
            IsExternal = true,
            Name = schema.Items is not null && (schema.Items.Type & ~JsonSchemaType.Null)?.ToIdentifiers().FirstOrDefault() is string name ? name : "null",
        };

        paramType.CollectionKind = schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default;
        return paramType;
    }

    private void CleanUpInternalState()
    {
        foreach (var lifecycle in classLifecycles.Values)
            lifecycle.Dispose();
        classLifecycles.Clear();
        multipartPropertiesModels.Clear();
    }
}
