using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
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
using Kiota.Builder.Extensions;
using Kiota.Builder.Logging;
using Kiota.Builder.Manifest;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.Plugins;
using Kiota.Builder.Refiners;
using Kiota.Builder.WorkspaceManagement;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.VisualBasic;
using HttpMethod = Kiota.Builder.CodeDOM.HttpMethod;
[assembly: InternalsVisibleTo("Kiota.Builder.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100957cb48387b2a5f54f5ce39255f18f26d32a39990db27cf48737afc6bc62759ba996b8a2bfb675d4e39f3d06ecb55a178b1b4031dcb2a767e29977d88cce864a0d16bfc1b3bebb0edf9fe285f10fffc0a85f93d664fa05af07faa3aad2e545182dbf787e3fd32b56aca95df1a3c4e75dec164a3f1a4c653d971b01ffc39eb3c4")]

namespace Kiota.Builder;

public partial class KiotaBuilder
{
    private readonly ILogger<KiotaBuilder> logger;
    private readonly GenerationConfiguration config;
    private readonly ParallelOptions parallelOptions;
    private readonly HttpClient httpClient;
    private OpenApiDocument? openApiDocument;
    internal void SetOpenApiDocument(OpenApiDocument document) => openApiDocument = document ?? throw new ArgumentNullException(nameof(document));

    public KiotaBuilder(ILogger<KiotaBuilder> logger, GenerationConfiguration config, HttpClient client, bool useKiotaConfig = false)
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
    }
    private readonly OpenApiDocumentDownloadService openApiDocumentDownloadService;
    private readonly bool useKiotaConfig;
    private async Task CleanOutputDirectory(CancellationToken cancellationToken)
    {
        if (config.CleanOutput && Directory.Exists(config.OutputPath))
        {
            logger.LogInformation("Cleaning output directory {Path}", config.OutputPath);
            
            // Clean directories
            foreach (var subDir in Directory.EnumerateDirectories(config.OutputPath))
            {
                Directory.Delete(subDir, true);
            }
            
            // Backup state
            await workspaceManagementService.BackupStateAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
            
            // Clean files
            foreach (var subFile in Directory.EnumerateFiles(config.OutputPath))
            {
                if (!subFile.EndsWith(FileLogLogger.LogFileName, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(subFile);
                }
            }
        }
    }
    public async Task<OpenApiUrlTreeNode?> GetUrlTreeNodeAsync(CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        var inputPath = config.OpenAPIFilePath;
        var (_, openApiTree, _) = await GetTreeNodeInternal(inputPath, false, sw, cancellationToken).ConfigureAwait(false);
        return openApiTree;
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
            using  var enumerator = manifest.ApiDependencies.Values.GetEnumerator();
            enumerator.MoveNext();
            var apiDependency = (manifest.ApiDependencies.Count, string.IsNullOrEmpty(apiIdentifier)) switch
            {
                (0, _) => throw new InvalidOperationException("The manifest contains no APIs"),
                (1, _) => enumerator.Current,
                (_, true) => throw new InvalidOperationException("The manifest contains multiple APIs, please specify the API identifier"),
                (_, false) => manifest.ApiDependencies.TryGetValue(apiIdentifier, out var apiDep) ? apiDep : throw new InvalidOperationException($"The manifest does not contain the API {apiIdentifier}")
            };

            if (apiDependency.ApiDescriptionUrl is null)
                throw new InvalidOperationException("The manifest does not contain an API description URL");

           var normalizedPaths = new List<string>();
            foreach (var request in apiDependency.Requests)
            {
                normalizedPaths.Add(NormalizeApiManifestPath(request, apiDependency.ApiDeploymentBaseUrl));
            }

            return new Tuple<string, IEnumerable<string>>(apiDependency.ApiDescriptionUrl, normalizedPaths);
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
    private async Task<(int, OpenApiUrlTreeNode?, bool)> GetTreeNodeInternal(string inputPath, bool generating, Stopwatch sw, CancellationToken cancellationToken)
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
                    config.IncludePatterns = new HashSet<string>(manifestDetails.Item2, StringComparer.OrdinalIgnoreCase);
            }
            StopLogAndReset(sw, $"step {++stepId} - getting the manifest - took");
        }
        sw.Start();
#pragma warning disable CA2007
        await using var input = await LoadStream(inputPath, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007
        if (input.Length == 0)
            return (0, null, false);
        StopLogAndReset(sw, $"step {++stepId} - reading the stream - took");

        // Parse OpenAPI
        sw.Start();
        openApiDocument = await CreateOpenApiDocumentAsync(input, generating, cancellationToken).ConfigureAwait(false);
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
            shouldGenerate &= await workspaceManagementService.ShouldGenerateAsync(config, openApiDocument.HashCode, cancellationToken).ConfigureAwait(false);
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

        return (stepId, openApiTree, shouldGenerate);
    }
    private void UpdateConfigurationFromOpenApiDocument()
    {
        if (openApiDocument == null ||
            GetLanguagesInformationInternal() is not LanguagesInformation languagesInfo) return;

        config.UpdateConfigurationFromLanguagesInformation(languagesInfo);
    }

    public async Task<LanguagesInformation?> GetLanguagesInformationAsync(CancellationToken cancellationToken)
    {
        await GetTreeNodeInternal(config.OpenAPIFilePath, false, new Stopwatch(), cancellationToken).ConfigureAwait(false);

        return GetLanguagesInformationInternal();
    }
    private LanguagesInformation? GetLanguagesInformationInternal()
    {
        if (openApiDocument == null)
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
    public async Task<bool> GeneratePluginAsync(CancellationToken cancellationToken)
    {
        return await GenerateConsumerAsync(async (sw, stepId, openApiTree, CancellationToken) =>
        {
            if (openApiDocument is null || openApiTree is null)
                throw new InvalidOperationException("The OpenAPI document and the URL tree must be loaded before generating the plugins");
            // generate plugin
            sw.Start();
            var pluginsService = new PluginsGenerationService(openApiDocument, openApiTree, config, Directory.GetCurrentDirectory());
            await pluginsService.GenerateManifestAsync(cancellationToken).ConfigureAwait(false);
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
            await ApplyLanguageRefinement(config, generatedCode, cancellationToken).ConfigureAwait(false);
            StopLogAndReset(sw, $"step {++stepId} - refine by language - took");

            // Write language source
            sw.Start();
            await CreateLanguageSourceFilesAsync(config.Language, generatedCode, cancellationToken).ConfigureAwait(false);
            StopLogAndReset(sw, $"step {++stepId} - writing files - took");
            return stepId;
        }, cancellationToken).ConfigureAwait(false);
    }
    private async Task<bool> GenerateConsumerAsync(Func<Stopwatch, int, OpenApiUrlTreeNode?, CancellationToken, Task<int>> innerGenerationSteps, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        // Read input stream
        var inputPath = config.OpenAPIFilePath;

        if (config.Operation is ConsumerOperation.Add && await workspaceManagementService.IsConsumerPresent(config.ClientClassName, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException($"The client {config.ClientClassName} already exists in the workspace");

        try
        {
            await CleanOutputDirectory(cancellationToken).ConfigureAwait(false);
            // doing this verification at the beginning to give immediate feedback to the user
            Directory.CreateDirectory(config.OutputPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not open/create output directory {config.OutputPath}, reason: {ex.Message}", ex);
        }
        try
        {
            var (stepId, openApiTree, shouldGenerate) = await GetTreeNodeInternal(inputPath, true, sw, cancellationToken).ConfigureAwait(false);

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
            await workspaceManagementService.RestoreStateAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
            throw;
        }
        return true;
    }
    private async Task FinalizeWorkspaceAsync(Stopwatch sw, int stepId, OpenApiUrlTreeNode? openApiTree, string inputPath, CancellationToken cancellationToken)
    {
        // Write lock file
        sw.Start();
        using var descriptionStream = !isDescriptionFromWorkspaceCopy ? await LoadStream(inputPath, cancellationToken).ConfigureAwait(false) : Stream.Null;

        var requestInfoDictionary = new Dictionary<string, HashSet<string>>();
        if (openApiTree != null)
        {
            foreach (var kvp in openApiTree.GetRequestInfo())
            {
                requestInfoDictionary[kvp.Key] = kvp.Value;
            }
        }

        await workspaceManagementService.UpdateStateFromConfigurationAsync(config, openApiDocument?.HashCode ?? string.Empty, requestInfoDictionary, descriptionStream, cancellationToken).ConfigureAwait(false);
        StopLogAndReset(sw, $"step {++stepId} - writing lock file - took");
    }
    private readonly WorkspaceManagementService workspaceManagementService;
    private static readonly GlobComparer globComparer = new();
    [GeneratedRegex(@"([\/\\])\{[\w\d-]+\}([\/\\])?", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex MultiIndexSameLevelCleanupRegex();
    internal static string ReplaceAllIndexesWithWildcard(string path, uint depth = 10) => depth == 0 ? path : ReplaceAllIndexesWithWildcard(MultiIndexSameLevelCleanupRegex().Replace(path, "$1{*}$2"), depth - 1); // the bound needs to be greedy to avoid replacing anything else than single path parameters
    private static Dictionary<Glob, HashSet<OperationType>> GetFilterPatternsFromConfiguration(HashSet<string> configPatterns)
    {
        var groupedPatterns = new Dictionary<Glob, List<OperationType?>>(globComparer);

        foreach (var pattern in configPatterns)
        {
            var splat = pattern.Split('#', StringSplitOptions.RemoveEmptyEntries);
            var glob = Glob.Parse(ReplaceAllIndexesWithWildcard(splat[0]));
            var operationTypes = new List<OperationType?>();

            if (splat.Length > 1)
            {
                var operations = splat[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var operation in operations)
                {
                    if (Enum.TryParse<OperationType>(operation.Trim(), true, out var op))
                    {
                        operationTypes.Add(op);
                    }
                }
            }

            if (groupedPatterns.TryGetValue(glob, out var existingOperationTypes))
            {
                existingOperationTypes.AddRange(operationTypes);
            }
            else
            {
                groupedPatterns[glob] = operationTypes;
            }
        }

        var result = new Dictionary<Glob, HashSet<OperationType>>(globComparer);
        foreach (var group in groupedPatterns)
        {
            var operationTypes = new HashSet<OperationType>();
            foreach (var operationType in group.Value)
            {
                if (operationType != null && operationType.HasValue)
                {
                    operationTypes.Add(operationType.Value);
                }
            }
            result[group.Key] = operationTypes;
        }

        return result;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]+", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex PluginOperationIdCleanupRegex();
    internal static void CleanupOperationIdForPlugins(OpenApiDocument document)
    {
        if (document.Paths is null) return;
        
        foreach (var pathKeyValue in document.Paths)
        {
            var path = pathKeyValue.Key;
            var pathItem = pathKeyValue.Value;

            foreach (var operationKeyValue in pathItem.Operations)
            {
                var operationType = operationKeyValue.Key;
                var operation = operationKeyValue.Value;

                if (string.IsNullOrEmpty(operation.OperationId))
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var segment in path.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (segment.IsPathSegmentWithSingleSimpleParameter())
                            stringBuilder.Append("item");
                        else if (!string.IsNullOrEmpty(segment.Trim()))
                            stringBuilder.Append(segment.ToLowerInvariant());
                        stringBuilder.Append('_');
                    }
                    stringBuilder.Append(operationType.ToString().ToLowerInvariant());
                    operation.OperationId = stringBuilder.ToString();
                }
                else
                {
                    operation.OperationId = PluginOperationIdCleanupRegex().Replace(operation.OperationId, "_");//replace non-alphanumeric characters with _
                }
            }
        }
    }
    internal void FilterPathsByPatterns(OpenApiDocument doc)
    {
        var includePatterns = GetFilterPatternsFromConfiguration(config.IncludePatterns);
        var excludePatterns = GetFilterPatternsFromConfiguration(config.ExcludePatterns);
        if (config.PatternsOverride.Count != 0)
        {
            includePatterns = GetFilterPatternsFromConfiguration(config.PatternsOverride);
            excludePatterns = new Dictionary<Glob, HashSet<OperationType>>();
        }
        if (includePatterns.Count == 0 && excludePatterns.Count == 0) return;

        var nonOperationIncludePatterns = new List<Glob>();
        var nonOperationExcludePatterns = new List<Glob>();
        var operationIncludePatterns = new List<KeyValuePair<Glob, HashSet<OperationType>>>();

        foreach (var pattern in includePatterns)
        {
            if (pattern.Value.Count == 0)
            {
                nonOperationIncludePatterns.Add(pattern.Key);
            }
            else
            {
                operationIncludePatterns.Add(pattern);
            }
        }

        foreach (var pattern in excludePatterns)
        {
            if (pattern.Value.Count == 0)
            {
                nonOperationExcludePatterns.Add(pattern.Key);
            }
        }

        var pathsToRemove = new List<string>();
        foreach (var path in doc.Paths.Keys)
        {
            bool nonOperationIncludeMatch = false;
            foreach (var pattern in nonOperationIncludePatterns)
            {
                if (pattern.IsMatch(path))
                {
                    nonOperationIncludeMatch = true;
                    break;
                }
            }

            bool nonOperationExcludeMatch = false;
            foreach (var pattern in nonOperationExcludePatterns)
            {
                if (pattern.IsMatch(path))
                {
                    nonOperationExcludeMatch = true;
                    break;
                }
            }

            bool operationIncludeMatch = false;
            foreach (var pattern in operationIncludePatterns)
            {
                if (pattern.Key.IsMatch(path))
                {
                    operationIncludeMatch = true;
                    break;
                }
            }

            if ((nonOperationIncludePatterns.Count != 0 && !nonOperationIncludeMatch ||
                nonOperationExcludePatterns.Count != 0 && nonOperationExcludeMatch) &&
                !operationIncludeMatch)
            {
                pathsToRemove.Add(path);
            }
        }

        foreach (var path in pathsToRemove)
        {
            doc.Paths.Remove(path);
        }

        var operationExcludePatterns = new List<KeyValuePair<Glob, HashSet<OperationType>>>();
        foreach (var pattern in excludePatterns)
        {
            if (pattern.Value.Count != 0)
            {
                operationExcludePatterns.Add(pattern);
            }
        }

        if (operationIncludePatterns.Count != 0 || operationExcludePatterns.Count != 0)
        {
            foreach (var path in doc.Paths)
            {
                var operationsToRemove = new List<OperationType>();
                foreach (var operation in path.Value.Operations.Keys)
                {
                    bool operationIncludeMatch = false;
                    foreach (var pattern in operationIncludePatterns)
                    {
                        if (pattern.Key.IsMatch(path.Key) && pattern.Value.Contains(operation))
                        {
                            operationIncludeMatch = true;
                            break;
                        }
                    }

                    if (operationIncludePatterns.Count != 0 && !operationIncludeMatch)
                    {
                        operationsToRemove.Add(operation);
                    }
                }

                foreach (var operation in operationsToRemove)
                {
                    path.Value.Operations.Remove(operation);
                }
            }

            foreach (var path in doc.Paths)
            {
                var operationsToRemove = new List<OperationType>();
                foreach (var operation in path.Value.Operations.Keys)
                {
                    bool operationExcludeMatch = false;
                    foreach (var pattern in operationExcludePatterns)
                    {
                        if (pattern.Key.IsMatch(path.Key) && pattern.Value.Contains(operation))
                        {
                            operationExcludeMatch = true;
                            break;
                        }
                    }

                    if (operationExcludePatterns.Count != 0 && operationExcludeMatch)
                    {
                        operationsToRemove.Add(operation);
                    }
                }

                foreach (var operation in operationsToRemove)
                {
                    path.Value.Operations.Remove(operation);
                }
            }

            pathsToRemove.Clear();
            foreach (var path in doc.Paths)
            {
                if (path.Value.Operations.Count == 0)
                {
                    pathsToRemove.Add(path.Key);
                }
            }

            foreach (var path in pathsToRemove)
            {
                doc.Paths.Remove(path);
            }
        }

        if (doc.Paths.Count == 0)
            logger.LogWarning("No paths were found matching the provided patterns. Check your configuration.");
    }
    internal void SetApiRootUrl()
    {
        if (openApiDocument is not null && openApiDocument.GetAPIRootUrl(config.OpenAPIFilePath) is string candidateUrl)
        {
            config.ApiRootUrl = candidateUrl;
            logger.LogInformation("Client root URL set to {ApiRootUrl}", candidateUrl);
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
    private async Task<Stream> LoadStream(string inputPath, CancellationToken cancellationToken)
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
    public static string GetDeeperMostCommonNamespaceNameForModels(OpenApiDocument document)
    {
        if (document == null || document.Components == null || document.Components.Schemas == null || document.Components.Schemas.Count == 0)
            return string.Empty;

        var distinctKeys = new List<string>();
        foreach (var key in document.Components.Schemas.Keys)
        {
            var splitKey = key.Split(NsNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var joinedKey = string.Empty;
            for (int i = 0; i < splitKey.Length - 1; i++)
            {
                joinedKey += splitKey[i] + NsNameSeparator;
            }
            joinedKey = joinedKey.TrimEnd(NsNameSeparator);
            if (!string.IsNullOrEmpty(joinedKey) && !distinctKeys.Contains(joinedKey))
                distinctKeys.Add(joinedKey);
        }

        distinctKeys.Sort((x, y) => CountChar(y, NsNameSeparator).CompareTo(CountChar(x, NsNameSeparator)));

        var distinctKeysArray = distinctKeys.ToArray();
        if (distinctKeysArray.Length == 0)
            return string.Empty;

        var longestKey = distinctKeysArray[0];
        if (longestKey == null)
            return string.Empty;

        var candidate = string.Empty;
        var longestKeySegments = longestKey.Split(NsNameSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in longestKeySegments)
        {
            var testValue = (candidate + NsNameSeparator + segment).Trim(NsNameSeparator);
            bool allStartWithTestValue = true;
            foreach (var key in distinctKeysArray)
            {
                if (!key.StartsWith(testValue, StringComparison.OrdinalIgnoreCase))
                {
                    allStartWithTestValue = false;
                    break;
                }
            }

            if (allStartWithTestValue)
                candidate = testValue;
            else
                break;
        }

        return candidate;

        static int CountChar(string str, char c)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                    count++;
            }
            return count;
        }
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
        StopLogAndReset(stopwatch, $"{nameof(InitializeInheritanceIndex)}");
        if (root != null)
        {
            CreateRequestBuilderClass(codeNamespace, root, root);
            StopLogAndReset(stopwatch, $"{nameof(CreateRequestBuilderClass)}");
            stopwatch.Start();
            MapTypeDefinitions(codeNamespace);
            StopLogAndReset(stopwatch, $"{nameof(MapTypeDefinitions)}");
            TrimInheritedModels();
            StopLogAndReset(stopwatch, $"{nameof(TrimInheritedModels)}");
            CleanUpInternalState();
            StopLogAndReset(stopwatch, $"{nameof(CleanUpInternalState)}");

            logger.LogTrace("{Timestamp}ms: Created source model with {Count} classes", stopwatch.ElapsedMilliseconds, CountClasses(codeNamespace));

            int CountClasses(CodeNamespace codeNamespace)
            {
                int count = 0;
                foreach (var element in codeNamespace.GetChildElements(true)) count++;
                return count;
            }
        }

        return rootNamespace;
    }

    /// <summary>
    /// Manipulate CodeDOM for language specific issues
    /// </summary>
    /// <param name="config"></param>
    /// <param name="generatedCode"></param>
    /// <param name="token"></param>
    public async Task ApplyLanguageRefinement(GenerationConfiguration config, CodeNamespace generatedCode, CancellationToken token)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await ILanguageRefiner.Refine(config, generatedCode, token).ConfigureAwait(false);

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
    /// <summary>
    /// Create a CodeClass instance that is a request builder class for the OpenApiUrlTreeNode
    /// </summary>
    private void CreateRequestBuilderClass(CodeNamespace currentNamespace, OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode rootNode)
    {
        // Determine Class Name
        CodeClass codeClass;
        var isApiClientClass = currentNode == rootNode;
        if (isApiClientClass)
        {
            var classes = currentNamespace.AddClass(new CodeClass
            {
                Name = config.ClientClassName,
                Kind = CodeClassKind.RequestBuilder,
                Documentation = new()
                {
                    DescriptionTemplate = "The main entry point of the SDK, exposes the configuration and the fluent API."
                },
            });
            using var enumerator = classes.GetEnumerator();
            codeClass = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException($"No classes found in namespace '{currentNamespace}'.", nameof(currentNamespace));
        }
        else
        {
            var targetNS = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNamespace.EnsureItemNamespace() : currentNamespace;
            var className = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, ItemRequestBuilderSuffix) : currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, RequestBuilderSuffix);
            var classes = targetNS.AddClass(new CodeClass
            {
                Name = className.CleanupSymbolName(),
                Kind = CodeClassKind.RequestBuilder,
                Documentation = new()
                {
                    DescriptionTemplate = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
                },
            });
            var enumerator = classes.GetEnumerator();
            codeClass = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException($"No classes found in namespace '{currentNamespace}'.", nameof(currentNamespace));
        }

        logger.LogTrace("Creating class {Class}", codeClass.Name);

        // Add properties for children
        foreach (var child in currentNode.Children)
        {
            var propIdentifier = child.Value.GetNavigationPropertyName(config.StructuredMimeTypes);
            var propType = child.Value.GetNavigationPropertyName(config.StructuredMimeTypes, child.Value.DoesNodeBelongToItemSubnamespace() ? ItemRequestBuilderSuffix : RequestBuilderSuffix);

            if (child.Value.IsPathSegmentWithSingleSimpleParameter())
            {
                var indexerParameterType = GetIndexerParameter(child.Value, currentNode);
                codeClass.AddIndexer(CreateIndexer($"{propIdentifier}-indexer", propType, indexerParameterType, child.Value, currentNode));
            }
            else if (child.Value.IsComplexPathMultipleParameters())
                CreateMethod(propIdentifier, propType, codeClass, child.Value);
            else
            {
                var description = child.Value.GetPathItemDescription(Constants.DefaultOpenApiLabel).CleanupDescription();
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
            foreach (var operation in currentNode
                                    .PathItems[Constants.DefaultOpenApiLabel]
                                    .Operations)
                CreateOperationMethods(currentNode, operation.Key, operation.Value, codeClass);
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
            var codeName = parameter.Name.SanitizeParameterNameForCodeSymbols();
            using var contentValuesEnumerator = parameter.Content.Values.GetEnumerator();
            var firstSchema = contentValuesEnumerator.MoveNext() ? contentValuesEnumerator.Current?.Schema : null;
            var parameterType = GetPrimitiveType(parameter.Schema ?? firstSchema) ??
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
        var unmappedTypes = new List<CodeType>();
        var unmappedTypesWithNoName = new List<CodeType>();
        var unmappedTypesWithName = new List<CodeType>();
        var unmappedRequestBuilderTypes = new List<CodeType>();
        var groupedTypes = new Dictionary<string, List<CodeType>>();

        foreach (var type in GetUnmappedTypeDefinitions(codeElement))
        {
            if (!unmappedTypes.Contains(type))
            {
                unmappedTypes.Add(type);
            }
        }

        foreach (var type in unmappedTypes)
        {
            if (string.IsNullOrEmpty(type.Name))
            {
                unmappedTypesWithNoName.Add(type);
                logger.LogWarning("Type with empty name and parent {ParentName}", type.Parent?.Name);
            }
            else
            {
                unmappedTypesWithName.Add(type);
            }
        }

        foreach (var type in unmappedTypesWithName)
        {
            if (type.Parent is CodeProperty property && property.IsOfKind(CodePropertyKind.RequestBuilder) ||
                type.Parent is CodeIndexer ||
                type.Parent is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestBuilderWithParameters))
            {
                unmappedRequestBuilderTypes.Add(type);
            }
        }

        Parallel.ForEach(unmappedRequestBuilderTypes, parallelOptions, x =>
        {
            var parentNS = x.Parent?.Parent?.Parent as CodeNamespace;
            var minItem = default(CodeClass);
            var minValue = int.MaxValue;

            foreach (var item in parentNS?.FindChildrenByName<CodeClass>(x.Name) ?? [])
        {
            var value = shortestNamespaceOrder(item);
            if (value < minValue)
            {
                minValue = value;
                minItem = item;
            }
        }

        x.TypeDefinition = minItem;

        if (x.TypeDefinition == null)
        {
            parentNS = parentNS?.Parent as CodeNamespace;
            minItem = default;
            minValue = int.MaxValue;

            foreach (var item in parentNS
                ?.FindNamespaceByName($"{parentNS?.Name}.{x.Name[..^RequestBuilderSuffix.Length].ToFirstCharacterLowerCase()}".TrimEnd(NsNameSeparator))
                ?.FindChildrenByName<CodeClass>(x.Name) ?? [])
            {
                var value = shortestNamespaceOrder(item);
                if (value < minValue)
                {
                    minValue = value;
                    minItem = item;
                }
            }

            x.TypeDefinition = minItem;
        }
        });

        foreach (var type in unmappedTypesWithName)
        {
            if (type.TypeDefinition == null)
            {
                if (!groupedTypes.TryGetValue(type.Name, out var typeList))
                {
                    typeList = new List<CodeType>();
                    groupedTypes[type.Name] = typeList;
                }

                typeList.Add(type);
            }
        }

        Parallel.ForEach(groupedTypes, parallelOptions, group =>
        {
            if (rootNamespace?.FindChildByName<ITypeDefinition>(group.Key) is CodeElement definition)
                foreach (var type in group.Value)
                {
                    type.TypeDefinition = definition;
                    logger.LogWarning("Mapped type {TypeName} for {ParentName} using the fallback approach.", type.Name, type.Parent?.Name);
                }
        });
    }

    private const char NsNameSeparator = '.';

    private static List<CodeType> FilterUnmappedTypeDefinitions(IEnumerable<CodeTypeBase?> source)
    {
        var result = new List<CodeType>();

        foreach (var item in source)
        {
            if (item is CodeType codeType && !codeType.IsExternal && codeType.TypeDefinition == null)
            {
                result.Add(codeType);
            }
            else if (item is CodeComposedTypeBase composedType)
            {
                foreach (var type in composedType.Types)
                {
                    if (!type.IsExternal && type.TypeDefinition == null)
                    {
                        result.Add(type);
                    }
                }
            }
        }

        return result;
    }

    private List<CodeType> GetUnmappedTypeDefinitions(CodeElement codeElement)
    {
        var childElementsUnmappedTypes = new List<CodeType>();

        foreach (var child in codeElement.GetChildElements(true))
        {
            childElementsUnmappedTypes.AddRange(GetUnmappedTypeDefinitions(child));
        }

        switch (codeElement)
        {
            case CodeMethod method:
                var methodTypes = new List<CodeTypeBase?> { method.ReturnType };
                foreach (var parameter in method.Parameters)
                {
                    methodTypes.Add(parameter.Type);
                }
                var methodResult = FilterUnmappedTypeDefinitions(methodTypes);
                methodResult.AddRange(childElementsUnmappedTypes);
                return methodResult;

            case CodeProperty property:
                var propertyResult = FilterUnmappedTypeDefinitions(new[] { property.Type });
                propertyResult.AddRange(childElementsUnmappedTypes);
                return propertyResult;

            case CodeIndexer indexer:
                var indexerResult = FilterUnmappedTypeDefinitions(new[] { indexer.ReturnType });
                indexerResult.AddRange(childElementsUnmappedTypes);
                return indexerResult;

            default:
                return childElementsUnmappedTypes;
        }
    }

    private static CodeType DefaultIndexerParameterType => new() { Name = "string", IsExternal = true };
    private const char OpenAPIUrlTreeNodePathSeparator = '\\';
    private CodeParameter GetIndexerParameter(OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode parentNode)
    {
        var parentNodeSeparatorCount = 0;
        foreach (char c in parentNode.Path)
        {
            if (c == OpenAPIUrlTreeNodePathSeparator)
                parentNodeSeparatorCount++;
        }

        var pathSegments = currentNode.Path.Split(OpenAPIUrlTreeNodePathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var parameterNameSegments = new List<string>();

        for (int i = parentNodeSeparatorCount; i < pathSegments.Length; i++)
        {
            parameterNameSegments.Add(pathSegments[i]);
        }

        var parameterName = string.Join(OpenAPIUrlTreeNodePathSeparator, parameterNameSegments)
                                        .Trim(OpenAPIUrlTreeNodePathSeparator, ForwardSlash, '{', '}');

        var pathItems = GetPathItems(currentNode);
        OpenApiParameter? parameter = null;

        if (pathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var defaultPathItem))
        {
            foreach (var operation in defaultPathItem.Operations.Values)
            {
                foreach (var opParameter in operation.Parameters)
                {
                    if (opParameter.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase) && opParameter.In == ParameterLocation.Path)
                    {
                        parameter = opParameter;
                        break;
                    }
                }
                if (parameter != null)
                    break;
            }
        }

        var type = parameter != null ? GetPrimitiveType(parameter.Schema) ?? DefaultIndexerParameterType : DefaultIndexerParameterType;
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

    private static IDictionary<string, OpenApiPathItem> GetPathItems(OpenApiUrlTreeNode currentNode, bool validateIsParameterNode = true)
    {
        if ((!validateIsParameterNode || currentNode.IsParameter) && currentNode.PathItems.Count > 0)
        {
            return currentNode.PathItems;
        }

        var pathItems = new Dictionary<string, OpenApiPathItem>();

        foreach (var child in currentNode.Children)
        {
            var childPathItems = GetPathItems(child.Value, false);
            foreach (var pathItem in childPathItems)
            {
                if (!pathItems.ContainsKey(pathItem.Key))
                {
                    pathItems.Add(pathItem.Key, pathItem.Value);
                }
            }
        }

        return pathItems;
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

        return result.ToArray();
    }
    private static readonly StructuralPropertiesReservedNameProvider structuralPropertiesReservedNameProvider = new();

    private CodeProperty? CreateProperty(string childIdentifier, string childType, OpenApiSchema? propertySchema = null, CodeTypeBase? existingType = null, CodePropertyKind kind = CodePropertyKind.Custom)
    {
        var propertyName = childIdentifier.CleanupSymbolName();
        if (structuralPropertiesReservedNameProvider.ReservedNames.Contains(propertyName))
            propertyName += "Property";
        var resultType = existingType ?? GetPrimitiveType(propertySchema, childType);
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
                                        propertySchema is not null &&
                                        propertySchema.Extensions.TryGetValue(OpenApiPrimaryErrorMessageExtension.Name, out var openApiExtension) &&
                                        openApiExtension is OpenApiPrimaryErrorMessageExtension primaryErrorMessageExtension &&
                                        primaryErrorMessageExtension.IsPrimaryErrorMessage
        };
        if (prop.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.QueryParameter) &&
            !propertyName.Equals(childIdentifier, StringComparison.Ordinal))
            prop.SerializationName = childIdentifier;
        if (kind == CodePropertyKind.Custom &&
            propertySchema?.Default is OpenApiString stringDefaultValue &&
            !string.IsNullOrEmpty(stringDefaultValue.Value))
            prop.DefaultValue = $"\"{stringDefaultValue.Value}\"";

        if (existingType == null)
        {
            prop.Type.CollectionKind = propertySchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default;
            logger.LogTrace("Creating property {Name} of {Type}", prop.Name, prop.Type.Name);
        }
        return prop;
    }
    private static readonly HashSet<string> typeNamesToSkip = new(StringComparer.OrdinalIgnoreCase) { "object", "array" };
    private static CodeType? GetPrimitiveType(OpenApiSchema? typeSchema, string? childType = default)
    {
        var typeNames = new List<string?> { typeSchema?.Items?.Type, childType, typeSchema?.Type };
        if (typeSchema?.AnyOf?.Count > 0)
            foreach (var anyOfType in typeSchema.AnyOf) // double is sometimes an anyof string, number and enum
            {
                if (anyOfType.Type != null)
                    typeNames.Add(anyOfType.Type);
            }
        if (typeSchema?.OneOf?.Count > 0)
            foreach (var oneOfType in typeSchema.OneOf) // double is sometimes an oneof string, number and enum
            {                                           // first value that's not null, and not "object" for primitive collections, the items type matters
                if (oneOfType.Type != null)
                    typeNames.Add(oneOfType.Type);
            }
        var typeName = typeNames.Find(static x => !string.IsNullOrEmpty(x) && !typeNamesToSkip.Contains(x));

        var isExternal = false;
        if (typeSchema?.Items?.IsEnum() ?? false)
            typeName = childType;
        else
        {
            var format = typeSchema?.Format ?? typeSchema?.Items?.Format;
            var primitiveTypeName = (typeName?.ToLowerInvariant(), format?.ToLowerInvariant()) switch
            {
                ("string", "base64url") => "base64url",
                ("file", _) => "binary",
                ("string", "duration") => "TimeSpan",
                ("string", "time") => "TimeOnly",
                ("string", "date") => "DateOnly",
                ("string", "date-time") => "DateTimeOffset",
                ("string", "uuid") => "Guid",
                ("string", _) => "string", // covers commonmark and html
                ("number", "double" or "float" or "decimal") => format.ToLowerInvariant(),
                ("number" or "integer", "int8") => "sbyte",
                ("number" or "integer", "uint8") => "byte",
                ("number" or "integer", "int64") => "int64",
                ("number", "int32") => "integer",
                ("number", _) => "double",
                ("integer", _) => "integer",
                ("boolean", _) => "boolean",
                (_, "byte") => "base64",
                (_, "binary") => "binary",
                (_, _) => string.Empty,
            };
            if (!string.IsNullOrEmpty(primitiveTypeName))
            {
                typeName = primitiveTypeName;
                isExternal = true;
            }
        }
        if (string.IsNullOrEmpty(typeName))
            return null;
        return new CodeType
        {
            Name = typeName,
            IsExternal = isExternal,
        };
    }
    private const string RequestBodyPlainTextContentType = "text/plain";
    private static readonly HashSet<string> noContentStatusCodes = new(StringComparer.OrdinalIgnoreCase) { "201", "202", "204", "205", "301", "302", "303", "304", "307" };
    private static HashSet<string> errorStatusCodes = MakeErrorCodes();

    private static HashSet<string> MakeErrorCodes()
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add status codes ranging from 400 to 499
        for (int statusCode = 400; statusCode <= 499; statusCode++)
        {
            codes.Add(statusCode.ToString(CultureInfo.InvariantCulture));
        }

        // Add status codes ranging from 500 to 599
        for (int statusCode = 500; statusCode <= 599; statusCode++)
        {
            codes.Add(statusCode.ToString(CultureInfo.InvariantCulture));
        }

        // Add the error mapping client range
        codes.Add(CodeMethod.ErrorMappingClientRange);

        // Add the error mapping server range
        codes.Add(CodeMethod.ErrorMappingServerRange);

        return codes;
    }

    private void AddErrorMappingsForExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod)
    {
        foreach (var response in operation.Responses)
        {
            if (errorStatusCodes.Contains(response.Key))
            if (response.Value.GetResponseSchema(config.StructuredMimeTypes) is { } schema)
            {
                AddErrorMappingToExecutorMethod(currentNode, operation, executorMethod, schema, response.Value, response.Key.ToUpperInvariant());
            }
        }
        if (operation.Responses.TryGetValue("default", out var defaultResponse) && defaultResponse.GetResponseSchema(config.StructuredMimeTypes) is { } errorSchema)
        {
            if (!executorMethod.HasErrorMappingCode(CodeMethod.ErrorMappingClientRange))
                AddErrorMappingToExecutorMethod(currentNode, operation, executorMethod, errorSchema, defaultResponse, CodeMethod.ErrorMappingClientRange);
            if (!executorMethod.HasErrorMappingCode(CodeMethod.ErrorMappingServerRange))
                AddErrorMappingToExecutorMethod(currentNode, operation, executorMethod, errorSchema, defaultResponse, CodeMethod.ErrorMappingServerRange);
        }
    }
    private void AddErrorMappingToExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod, OpenApiSchema errorSchema, OpenApiResponse response, string errorCode)
    {
        if (modelsNamespace != null)
        {
            var parentElement = string.IsNullOrEmpty(response.Reference?.Id) && string.IsNullOrEmpty(errorSchema.Reference?.Id)
                ? (CodeElement)executorMethod
                : modelsNamespace;
            var errorType = CreateModelDeclarations(currentNode, errorSchema, operation, parentElement, $"{errorCode}Error", response: response);
            if (errorType is CodeType codeType &&
                codeType.TypeDefinition is CodeClass codeClass)
            {
                if (!codeClass.IsErrorDefinition)
                    codeClass.IsErrorDefinition = true;
                executorMethod.AddErrorMapping(errorCode, errorType);
            }
            else
                logger.LogWarning("Could not create error type for {Error} in {Operation}", errorCode, operation.OperationId);
        }
    }
    private (CodeTypeBase?, CodeTypeBase?) GetExecutorMethodReturnType(OpenApiUrlTreeNode currentNode, OpenApiSchema? schema, OpenApiOperation operation, CodeClass parentClass, OperationType operationType)
    {
        if (schema != null)
        {
            var suffix = $"{operationType}Response";
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

                    CodeMethod? originalFactoryMethod = null;
                    foreach (var method in codeClass.Methods)
                    {
                        if (method.Kind is CodeMethodKind.Factory)
                        {
                            originalFactoryMethod = method;
                            break;
                        }
                    }

                    if (originalFactoryMethod == null)
                        throw new InvalidOperationException("Could not find a factory method");

                    var obsoleteFactoryMethod = (CodeMethod)originalFactoryMethod.Clone();
                    obsoleteFactoryMethod.ReturnType = new CodeType { Name = obsoleteTypeName, TypeDefinition = obsoleteClassDefinition };
                    obsoleteClassDefinition.AddMethod(obsoleteFactoryMethod);
                    obsoleteClassDefinition.StartBlock.Inherits = (CodeType)codeType.Clone();

                    CodeTypeBase? obsoleteClass = null;
                    if (codeClass.Parent is CodeClass modelParentClass)
                    {
                        foreach (var innerClass in modelParentClass.AddInnerClass(obsoleteClassDefinition))
                        {
                            obsoleteClass = new CodeType { TypeDefinition = innerClass };
                            break;
                        }
                    }
                    else if (codeClass.Parent is CodeNamespace modelParentNamespace)
                    {
                        foreach (var classInNamespace in modelParentNamespace.AddClass(obsoleteClassDefinition))
                        {
                            obsoleteClass = new CodeType { TypeDefinition = classInNamespace };
                            break;
                        }
                    }

                    if (obsoleteClass == null)
                        throw new InvalidOperationException("Could not find a valid parent for the obsolete class");

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
                string returnType = "binary";
                foreach (var response in operation.Responses)
                {
                    if (noContentStatusCodes.Contains(response.Key))
                    {
                        returnType = VoidType;
                        break;
                    }
                    else if (response.Value.Content.ContainsKey(RequestBodyPlainTextContentType))
                    {
                        returnType = "string";
                        break;
                    }
                }
                return (new CodeType { Name = returnType, IsExternal = true, }, null);
            }
            return (modelType, null);
        }
        else
        {
            string returnType = "binary";
            foreach (var response in operation.Responses)
            {
                if (noContentStatusCodes.Contains(response.Key))
                {
                    returnType = VoidType;
                    break;
                }
                else if (response.Value.Content.ContainsKey(RequestBodyPlainTextContentType))
                {
                    returnType = "string";
                    break;
                }
            }
            return (new CodeType { Name = returnType, IsExternal = true, }, null);
        }
    }
    private void CreateOperationMethods(OpenApiUrlTreeNode currentNode, OperationType operationType, OpenApiOperation operation, CodeClass parentClass)
    {
        try
        {
            var parameterClass = CreateOperationParameterClass(currentNode, operationType, operation, parentClass);
            var requestConfigClass = parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType}RequestConfiguration",
                Kind = CodeClassKind.RequestConfiguration,
                Documentation = new()
                {
                    DescriptionTemplate = "Configuration for the request such as headers, query parameters, and middleware options.",
                },
            }).GetEnumerator();
            requestConfigClass.MoveNext();

            OpenApiSchema? schema = null;
            if (operation.Responses != null && operation.Responses.Count > 0)
            {
                schema = operation.GetResponseSchema(config.StructuredMimeTypes);
            }

            var method = (HttpMethod)Enum.Parse(typeof(HttpMethod), operationType.ToString());
            var deprecationInformation = operation.GetDeprecationInformation();
            var returnTypes = GetExecutorMethodReturnType(currentNode, schema, operation, parentClass, operationType);
            var executorMethod = new CodeMethod
            {
                Name = operationType.ToString(),
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

            if (operation.Extensions != null)
            {
                if (operation.Extensions.TryGetValue(OpenApiPagingExtension.Name, out var extension) && extension is OpenApiPagingExtension pagingExtension)
                {
                    executorMethod.PagingInformation = new PagingInformation
                    {
                        ItemName = pagingExtension.ItemName,
                        NextLinkName = pagingExtension.NextLinkName,
                        OperationName = pagingExtension.OperationName,
                    };
                }
            }

            AddErrorMappingsForExecutorMethod(currentNode, operation, executorMethod);
            AddRequestConfigurationProperties(parameterClass, requestConfigClass.Current);
            AddRequestBuilderMethodParameters(currentNode, operationType, operation, requestConfigClass.Current, executorMethod);
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
            executorMethod.AddParameter(cancellationParam); // Add cancellation token parameter

            if (returnTypes.Item2 != null && config.IncludeBackwardCompatible)
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
                Name = $"To{operationType.ToString().ToFirstCharacterUpperCase()}RequestInformation",
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
            if (!operationUrlTemplate.Equals(parentClass.Properties.GetEnumerator().Current?.Kind is CodePropertyKind.UrlTemplate ? parentClass.Properties.GetEnumerator().Current.DefaultValue.Trim('"') : null, StringComparison.Ordinal)
                && currentNode.HasRequiredQueryParametersAcrossOperations())// no need to generate extra strings/templates as optional parameters will have no effect on resolved url.
                generatorMethod.UrlTemplateOverride = operationUrlTemplate;

            IEnumerable<string> mediaTypes = [];
            if (operation.Responses != null && operation.Responses.Count > 0)
            {
                if (schema == null)
                {
                    var nonErrorMediaTypes = new List<string>();
                    var errorMediaTypes = new List<string>();

                    foreach (var response in operation.Responses)
                    {
                        if (!errorStatusCodes.Contains(response.Key))
                        {
                            foreach (var content in response.Value.Content)
                            {
                                nonErrorMediaTypes.Add(content.Key);
                            }
                        }
                        else
                        {
                            foreach (var content in response.Value.Content)
                            {
                                errorMediaTypes.Add(content.Key);
                            }
                        }
                    }

                    nonErrorMediaTypes.AddRange(config.StructuredMimeTypes.GetAcceptedTypes(errorMediaTypes));
                    mediaTypes = nonErrorMediaTypes;
                }
                else
                {
                    var responseMediaTypes = new List<string>();
                    foreach (var response in operation.Responses)
                    {
                        foreach (var content in response.Value.Content)
                        {
                            if (schemaReferenceComparer.Equals(schema, content.Value.Schema))
                            {
                                responseMediaTypes.Add(content.Key);
                            }
                        }
                    }
                    mediaTypes = config.StructuredMimeTypes.GetAcceptedTypes(responseMediaTypes);
                }
            }

            generatorMethod.AddAcceptedResponsesTypes(mediaTypes);
            if (config.Language == GenerationLanguage.CLI)
                SetPathAndQueryParameters(generatorMethod, currentNode, operation);
            AddRequestBuilderMethodParameters(currentNode, operationType, operation, requestConfigClass.Current, generatorMethod);
            parentClass.AddMethod(generatorMethod);
            logger.LogTrace("Creating method {Name} of {Type}", generatorMethod.Name, generatorMethod.ReturnType);
        }
        catch (InvalidSchemaException ex)
        {
            logger.LogWarning(ex, "Could not create method for {Operation} in {Path} because the schema was invalid", operation.OperationId, currentNode.Path);
        }
    }

    private static readonly OpenApiSchemaReferenceComparer schemaReferenceComparer = new();
    private static readonly Func<OpenApiParameter, CodeParameter> GetCodeParameterFromApiParameter = x =>
    {
        var codeName = x.Name.SanitizeParameterNameForCodeSymbols();
        return new CodeParameter
        {
            Name = codeName,
            SerializationName = codeName.Equals(x.Name, StringComparison.Ordinal) ? string.Empty : x.Name,
            Type = x.Schema is null ? GetDefaultQueryParameterType() : GetQueryParameterType(x.Schema),
            Documentation = new()
            {
                DescriptionTemplate = x.Description.CleanupDescription(),
            },
            Kind = x.In switch
            {
                ParameterLocation.Query => CodeParameterKind.QueryParameter,
                ParameterLocation.Header => CodeParameterKind.Headers,
                ParameterLocation.Path => CodeParameterKind.Path,
                _ => throw new NotSupportedException($"No matching parameter kind is supported for parameters in {x.In}"),
            },
            Optional = !x.Required
        };
    };
    private static readonly Func<OpenApiParameter, bool> ParametersFilter = x => x.In == ParameterLocation.Path || x.In == ParameterLocation.Query || x.In == ParameterLocation.Header;
    private static void SetPathAndQueryParameters(CodeMethod target, OpenApiUrlTreeNode currentNode, OpenApiOperation operation)
    {
        List<CodeParameter> pathAndQueryParameters = new List<CodeParameter>();

        if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var value))
        {
            foreach (var parameter in value.Parameters)
            {
                if (ParametersFilter(parameter))
                {
                    var codeParameter = GetCodeParameterFromApiParameter(parameter);
                    pathAndQueryParameters.Add(codeParameter);
                }
            }
        }

        foreach (var parameter in operation.Parameters)
        {
            if (ParametersFilter(parameter))
            {
                var codeParameter = GetCodeParameterFromApiParameter(parameter);
                pathAndQueryParameters.Add(codeParameter);
            }
        }

        target.AddPathQueryOrHeaderParameter(pathAndQueryParameters.ToArray());
    }
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
    private void AddRequestBuilderMethodParameters(OpenApiUrlTreeNode currentNode, OperationType operationType, OpenApiOperation operation, CodeClass requestConfigClass, CodeMethod method)
    {
        if (operation.GetRequestSchema(config.StructuredMimeTypes) is OpenApiSchema requestBodySchema)
        {
            CodeTypeBase requestBodyType;
            if (operation.RequestBody.Content.IsMultipartFormDataSchema(config.StructuredMimeTypes))
            {
                requestBodyType = new CodeType
                {
                    Name = "MultipartBody",
                    IsExternal = true,
                };

                foreach (var contentEntry in operation.RequestBody.Content)
                {
                    if (contentEntry.Value.Schema == requestBodySchema)
                    {
                        var mediaType = contentEntry.Value;
                        foreach (var encodingEntry in mediaType.Encoding)
                        {
                            if (!string.IsNullOrEmpty(encodingEntry.Value.ContentType) && config.StructuredMimeTypes.Contains(encodingEntry.Value.ContentType))
                            {
                                if (CreateModelDeclarations(currentNode, requestBodySchema.Properties[encodingEntry.Key], operation, method, $"{operationType}RequestBody", isRequestBody: true) is CodeType propertyType &&
                                    propertyType.TypeDefinition is not null)
                                {
                                    multipartPropertiesModels.TryAdd(propertyType.TypeDefinition, true);
                                }
                            }
                        }
                        break; // Stop iteration after finding the matching schema
                    }
                }
            }
            else
                requestBodyType = CreateModelDeclarations(currentNode, requestBodySchema, operation, method, $"{operationType}RequestBody", isRequestBody: true) ??
                    throw new InvalidSchemaException();
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
            string? requestBodyContentType = null;
            foreach (var entry in operation.RequestBody.Content)
            {
                if (schemaReferenceComparer.Equals(entry.Value.Schema, requestBodySchema))
                {
                    requestBodyContentType = entry.Key;
                    break;
                }
            }

            if (requestBodyContentType == null)
            {
                throw new ArgumentException("No matching content type found.", nameof(operation));
            }

            method.RequestBodyContentType = requestBodyContentType;
        }
        else if (operation.RequestBody?.Content?.Count > 0)
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
            var contentTypes = operation.RequestBody.Content.Keys;
            using var enumerator = contentTypes.GetEnumerator();
            if (contentTypes.Count == 1 && enumerator.MoveNext() && !"*/*".Equals( enumerator.Current, StringComparison.OrdinalIgnoreCase))
                method.RequestBodyContentType =  enumerator.Current;
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
                    PossibleValues = new List<string>(contentTypes)
                });
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

        // Split the namespace suffix into segments
        var segments = namespaceSuffix.Split(NsNameSeparator);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = segments[i].CleanupSymbolName();
        }

        // Join the cleaned segments using the namespace separator
        return $"{modelsNamespace?.Name}{string.Join(NsNameSeparator, segments)}";
    }
    private CodeType CreateModelDeclarationAndType(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string classNameSuffix = "", OpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false)
    {
        var className = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, response: response, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, schema, className, codeNamespace);
        return new CodeType
        {
            TypeDefinition = codeDeclaration,
        };
    }
    private CodeType CreateInheritedModelDeclarationAndType(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string classNameSuffix, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema)
    {
        return new CodeType
        {
            TypeDefinition = CreateInheritedModelDeclaration(currentNode, schema, operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema),
        };
    }
    private CodeClass CreateInheritedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string classNameSuffix, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema)
    {
        OpenApiSchema? inlineSchema = null;
        OpenApiSchema? referencedSchema = null;
        foreach (var item in schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf))
        {
            if (referencedSchema == null && item.IsReferencedSchema())
            {
                referencedSchema = item;
            }
            else
                inlineSchema = item;
        }

        var referenceId = schema.Reference?.Id;
        var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(referenceId);
        var codeNamespaceFromParent = GetShortestNamespace(codeNamespace, schema);
        if (rootNamespace is null)
            throw new InvalidOperationException("Root namespace is not set");
        var shortestNamespace = string.IsNullOrEmpty(referenceId) ? codeNamespaceFromParent : rootNamespace.FindOrAddNamespace(shortestNamespaceName);
        var rootSchemaHasProperties = schema.HasAnyProperty();
        var className = (schema.GetSchemaName(schema.IsSemanticallyMeaningful()) is string cName && !string.IsNullOrEmpty(cName) ?
                cName :
                (!string.IsNullOrEmpty(typeNameForInlineSchema) ?
                    typeNameForInlineSchema :
                    currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, schema: schema, requestBody: isRequestBody)))
            .CleanupSymbolName();
        var codeDeclaration = (rootSchemaHasProperties, inlineSchema, referencedSchema) switch
        {
            // greatest parent type
            (true, null, null) => AddModelDeclarationIfDoesntExist(currentNode, schema, className, shortestNamespace),
            // inline schema + referenced schema
            (false, not null, not null) => AddModelDeclarationIfDoesntExist(currentNode, inlineSchema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchema, operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // properties + referenced schema
            (true, null, not null) => AddModelDeclarationIfDoesntExist(currentNode, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchema, operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // properties + inline schema
            (true, not null, null) => AddModelDeclarationIfDoesntExist(currentNode, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, inlineSchema, operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema)),
            // empty schema + referenced schema
            (false, null, not null) => AddModelDeclarationIfDoesntExist(currentNode, referencedSchema, className, shortestNamespace),
            // empty schema + inline schema
            (false, not null, null) => AddModelDeclarationIfDoesntExist(currentNode, inlineSchema, className, shortestNamespace),
            // too much information but we can make a choice -> maps to properties + inline schema
            (true, not null, not null) when inlineSchema.HasAnyProperty() => AddModelDeclarationIfDoesntExist(currentNode, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, inlineSchema, operation, classNameSuffix, codeNamespace, isRequestBody, typeNameForInlineSchema)),
            // too much information but we can make a choice -> maps to properties + referenced schema
            (true, not null, not null) when referencedSchema.HasAnyProperty() => AddModelDeclarationIfDoesntExist(currentNode, schema, className, shortestNamespace, CreateInheritedModelDeclaration(currentNode, referencedSchema, operation, classNameSuffix, codeNamespace, isRequestBody, string.Empty)),
            // meaningless scenario
            (false, null, null) or (true, not null, not null) => throw new InvalidOperationException("invalid inheritance case"),
        };
        if (codeDeclaration is not CodeClass currentClass) throw new InvalidOperationException("Inheritance is only supported for classes");
        if (!currentClass.Documentation.DescriptionAvailable &&
            string.IsNullOrEmpty(schema.AllOf.Count > 0 ? schema.AllOf[^1]?.Description : null) &&
            !string.IsNullOrEmpty(schema.Description))
            currentClass.Documentation.DescriptionTemplate = schema.Description.CleanupDescription(); // the last allof entry often is not a reference and doesn't have a description.

        return currentClass;
    }
    private CodeTypeBase CreateComposedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string suffixForInlineSchema, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema)
    {
        var typeName = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: suffixForInlineSchema, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var typesCount = schema.AnyOf?.Count ?? schema.OneOf?.Count ?? 0;
        bool nullableSchemaInAnyOf = false;
        if (schema.AnyOf != null)
        {
            foreach (var x in schema.AnyOf)
            {
                if (x.Nullable && !x.HasAnyProperty() && !x.IsExclusiveUnion() && !x.IsInclusiveUnion() && !x.IsInherited() && !x.IsIntersection() && !x.IsArray() && !x.IsReferencedSchema())
                {
                    nullableSchemaInAnyOf = true;
                    break;
                }
            }
        }
        if (typesCount == 1 && schema.Nullable && schema.IsInclusiveUnion() || // nullable on the root schema outside of anyOf
            typesCount == 2 && nullableSchemaInAnyOf) // nullable on a schema in the anyOf
        {
            OpenApiSchema? targetSchema = null;
            if (schema.AnyOf != null)
            {
                foreach (var x in schema.AnyOf)
                {
                    if (!string.IsNullOrEmpty(x.GetSchemaName()))
                    {
                        targetSchema = x;
                        break;
                    }
                }
            }
            if (targetSchema is not null)
            {
                var className = targetSchema.GetSchemaName().CleanupSymbolName();
                var shortestNamespace = GetShortestNamespace(codeNamespace, targetSchema);
                return new CodeType
                {
                    TypeDefinition = AddModelDeclarationIfDoesntExist(currentNode, targetSchema, className, shortestNamespace),
                    CollectionKind = targetSchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default
                }; // so we don't create unnecessary union types when anyOf was used only for nullable.
            }
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
        if (!string.IsNullOrEmpty(schema.Reference?.Id))
            unionType.TargetNamespace = codeNamespace.GetRootNamespace().FindOrAddNamespace(GetModelsNamespaceNameFromReferenceId(schema.Reference.Id));
        unionType.DiscriminatorInformation.DiscriminatorPropertyName = schema.GetDiscriminatorPropertyName();
        var discriminatorMappings = GetDiscriminatorMappings(currentNode, schema, codeNamespace, null);
        if (discriminatorMappings != null)
        {
            foreach (var x in discriminatorMappings)
            {
                unionType.DiscriminatorInformation.AddDiscriminatorMapping(x.Key, x.Value);
            }
        }
        var membersWithNoName = 0;
        foreach (var currentSchema in schemas!)
        {
            var shortestNamespace = GetShortestNamespace(codeNamespace, currentSchema);
            var className = currentSchema.GetSchemaName().CleanupSymbolName();
            if (string.IsNullOrEmpty(className))
                if (GetPrimitiveType(currentSchema) is CodeType primitiveType && !string.IsNullOrEmpty(primitiveType.Name))
                {
                    if (!unionType.ContainsType(primitiveType))
                        unionType.AddType(primitiveType);
                    continue;
                }
                else
                    className = $"{unionType.Name}Member{++membersWithNoName}";
            var declarationType = new CodeType
            {
                TypeDefinition = AddModelDeclarationIfDoesntExist(currentNode, currentSchema, className, shortestNamespace),
                CollectionKind = currentSchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default
            };
            if (!unionType.ContainsType(declarationType))
                unionType.AddType(declarationType);
        }
        return unionType;
    }
    private CodeTypeBase CreateModelDeclarations(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeElement parentElement, string suffixForInlineSchema, OpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false)
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
            return CreateInheritedModelDeclarationAndType(currentNode, schema, operation, suffix, codeNamespace, isRequestBody, typeNameForInlineSchema);
        }

        if (schema.IsIntersection() && schema.MergeIntersectionSchemaEntries() is OpenApiSchema mergedSchema)
        {
            // multiple allOf entries that do not translate to inheritance
            return CreateModelDeclarationAndType(currentNode, mergedSchema, operation, codeNamespace, suffix, response: responseValue, typeNameForInlineSchema: typeNameForInlineSchema, isRequestBody);
        }

        if ((schema.IsInclusiveUnion() || schema.IsExclusiveUnion()) && string.IsNullOrEmpty(schema.Format)
            && !schema.IsODataPrimitiveType())
        { // OData types are oneOf string, type + format, enum
            return CreateComposedModelDeclaration(currentNode, schema, operation, suffix, codeNamespace, isRequestBody, typeNameForInlineSchema);
        }

        if (schema.IsObjectType() || schema.HasAnyProperty() || schema.IsEnum() || !string.IsNullOrEmpty(schema.AdditionalProperties?.Type))
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

        if (!string.IsNullOrEmpty(schema.Type) || !string.IsNullOrEmpty(schema.Format))
            return GetPrimitiveType(schema, string.Empty) ?? new CodeType { Name = UntypedNodeName, IsExternal = true };
        if (schema.AnyOf.Count > 0 || schema.OneOf.Count > 0 || schema.AllOf.Count > 0)
        {
            OpenApiSchema? childSchema = null;
            foreach (var item in schema.AnyOf)
            {
                if (item.IsSemanticallyMeaningful(true))
                {
                    childSchema = item;
                    break;
                }
            }
            
            if (childSchema == null)
            {
                foreach (var item in schema.OneOf)
                {
                    if (item.IsSemanticallyMeaningful(true))
                    {
                        childSchema = item;
                        break;
                    }
                }
            }

            if (childSchema == null)
            {
                foreach (var item in schema.AllOf)
                {
                    if (item.IsSemanticallyMeaningful(true))
                    {
                        childSchema = item;
                        break;
                    }
                }
            }

            if (childSchema != null)
            {
                // We have found a child schema that is semantically meaningful
                return CreateModelDeclarations(currentNode, childSchema, operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema, isRequestBody);
            }
        }
  return new CodeType { Name = UntypedNodeName, IsExternal = true };
    }
    private CodeTypeBase CreateCollectionModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string typeNameForInlineSchema, bool isRequestBody)
    {
        CodeTypeBase? type = GetPrimitiveType(schema.Items, string.Empty);
        var isEnumOrComposedCollectionType = schema.Items.IsEnum() //the collection could be an enum type so override with strong type instead of string type.
                                    || schema.Items.IsComposedEnum() && string.IsNullOrEmpty(schema.Items.Format);//the collection could be a composed type with an enum type so override with strong type instead of string type.
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
    private CodeElement AddModelDeclarationIfDoesntExist(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, CodeClass? inheritsFrom = null, OpenApiSchema? parentSchemaToExcludeForIntersections = null)
    {
        if (GetExistingDeclaration(currentNamespace, currentNode, declarationName) is not CodeElement existingDeclaration) // we can find it in the components
        {
            if (AddEnumDeclaration(currentNode, schema, declarationName, currentNamespace) is CodeEnum enumDeclaration)
                return enumDeclaration;

            if (schema.IsIntersection() &&
                (parentSchemaToExcludeForIntersections is null ?
                    schema.MergeIntersectionSchemaEntries() :
                    schema.MergeIntersectionSchemaEntries([parentSchemaToExcludeForIntersections])) is OpenApiSchema mergedSchema &&
                AddModelDeclarationIfDoesntExist(currentNode, mergedSchema, declarationName, currentNamespace, inheritsFrom) is CodeClass createdClass)
            {
                // multiple allOf entries that do not translate to inheritance
                return createdClass;
            }
            return AddModelClass(currentNode, schema, declarationName, currentNamespace, inheritsFrom);
        }
        return existingDeclaration;
    }
    private CodeEnum? AddEnumDeclarationIfDoesntExist(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace)
    {
        if (GetExistingDeclaration(currentNamespace, currentNode, declarationName) is not CodeEnum existingDeclaration) // we can find it in the components
        {
            return AddEnumDeclaration(currentNode, schema, declarationName, currentNamespace);
        }
        return existingDeclaration;
    }
    private static CodeEnum? AddEnumDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace)
    {
        if (schema.IsEnum())
        {
            var schemaDescription = schema.Description.CleanupDescription();
            OpenApiEnumFlagsExtension? enumFlagsExtension = null;
            if (schema.Extensions.TryGetValue(OpenApiEnumFlagsExtension.Name, out var rawExtension) &&
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
                    DescriptionTemplate = !string.IsNullOrEmpty(schemaDescription) || !string.IsNullOrEmpty(schema.Reference?.Id) ?
                                        schemaDescription : // if it's a referenced component, we shouldn't use the path item description as it makes it indeterministic
                                        currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel),
                },
                Deprecation = schema.GetDeprecationInformation(),
            };
            SetEnumOptions(schema, newEnum);
            using var enumerator = currentNamespace.AddEnum(newEnum).GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Couldn't add enum to the namespace", nameof(currentNamespace));
        }
        return default;
        }
        private static void SetEnumOptions(OpenApiSchema schema, CodeEnum target)
        {
            OpenApiEnumValuesDescriptionExtension? extensionInformation = null;
            if (schema.Extensions.TryGetValue(OpenApiEnumValuesDescriptionExtension.Name, out var rawExtension) && rawExtension is OpenApiEnumValuesDescriptionExtension localExtInfo)
                extensionInformation = localExtInfo;

            List<CodeEnumOption> options = new List<CodeEnumOption>();
            HashSet<string> distinctValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in schema.Enum)
            {
                if (item is OpenApiString openApiString)
                {
                    string value = openApiString.Value;
                    if (!value.Equals("null", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value) && distinctValues.Add(value))
                    {
                        var optionDescription = extensionInformation?.ValuesDescriptions.Find(y => y.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
                        string? name = optionDescription?.Name;
                        var option = new CodeEnumOption
                        {
                            Name = (!string.IsNullOrEmpty(name) ? name : value).CleanupSymbolName(),
                            SerializationName = value,
                            Documentation = new()
                            {
                                DescriptionTemplate = optionDescription?.Description ?? string.Empty,
                            },
                        };
                        if (!string.IsNullOrEmpty(option.Name))
                        {
                            options.Add(option);
                        }
                    }
                }
            }

            target.AddOption(options.ToArray());
        }
    private CodeNamespace GetShortestNamespace(CodeNamespace currentNamespace, OpenApiSchema currentSchema)
    {
        if (!string.IsNullOrEmpty(currentSchema.Reference?.Id) && rootNamespace != null)
        {
            var parentClassNamespaceName = GetModelsNamespaceNameFromReferenceId(currentSchema.Reference.Id);
            return rootNamespace.AddNamespace(parentClassNamespaceName);
        }
        return currentNamespace;
    }
    private CodeClass AddModelClass(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, CodeClass? inheritsFrom = null)
    {
        if (inheritsFrom == null)
        {
            foreach (var x in schema.AllOf)
            {
                if (x.Reference != null)
                {
                    var parentSchema = x;
                    var parentClassNamespace = GetShortestNamespace(currentNamespace, parentSchema);
                    inheritsFrom = (CodeClass)AddModelDeclarationIfDoesntExist(currentNode, parentSchema, parentSchema.GetSchemaName().CleanupSymbolName(), parentClassNamespace);
                    break;
                }
            }
        }

        string? descriptionTemplate = null;
        if (schema.AllOf != null && string.IsNullOrEmpty(schema.Description))
        {
            for (int i = 0; i < schema.AllOf.Count; i++)
            {
                if (!schema.AllOf[i].IsReferencedSchema() && !string.IsNullOrEmpty(schema.AllOf[i].Description))
                {
                    descriptionTemplate = schema.AllOf[i].Description;
                    break;
                }
            }
        }
        else
        {
            descriptionTemplate = schema.Description.CleanupDescription();
        }

        var newClassStub = new CodeClass
        {
            Name = declarationName,
            Kind = CodeClassKind.Model,
            Documentation = new()
            {
                DocumentationLabel = schema.ExternalDocs?.Description ?? string.Empty,
                DocumentationLink = schema.ExternalDocs?.Url,
                DescriptionTemplate = descriptionTemplate!,
            },
            Deprecation = schema.GetDeprecationInformation(),
        };
        if (inheritsFrom != null)
            newClassStub.StartBlock.Inherits = new CodeType { TypeDefinition = inheritsFrom };

        // Add the class to the namespace after the serialization members
        // as other threads looking for the existence of the class may find the class but the additional data/backing store properties may not be fully populated causing duplication
        var includeAdditionalDataProperties = config.IncludeAdditionalData && schema.AdditionalPropertiesAllowed;
        AddSerializationMembers(newClassStub, includeAdditionalDataProperties, config.UsesBackingStore, static s => s);

        using var newClassEnumerator = currentNamespace.AddClass(newClassStub).GetEnumerator();
        var newClass = newClassEnumerator.MoveNext() ? newClassEnumerator.Current : throw new ArgumentException("Couldn't add class to the current namespace.", nameof(currentNamespace));
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

        var allMappings = GetDiscriminatorMappings(currentNode, schema, currentNamespace, newClass);
        var mappings = new Dictionary<string, CodeType>();

        foreach (var mapping in allMappings)
        {
            if (mapping.Value is { TypeDefinition: CodeClass definition } &&
                definition.DerivesFrom(newClass)) // only the mappings that derive from the current class
            {
                mappings.Add(mapping.Key, mapping.Value);
            }
        }

        AddDiscriminatorMethod(newClass, schema.GetDiscriminatorPropertyName(), mappings, static s => s);
        return newClass;
    }
    private IEnumerable<KeyValuePair<string, CodeType>> GetDiscriminatorMappings(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace currentNamespace, CodeClass? baseClass)
    {
        foreach (var mapping in schema.GetDiscriminatorMappings(inheritanceIndex))
        {
            var valueType = GetCodeTypeForMapping(currentNode, mapping.Value, currentNamespace, baseClass, schema);
            if (valueType != null)
            {
                yield return new KeyValuePair<string, CodeType>(mapping.Key, valueType);
            }
        }
    }
    private static List<ITypeDefinition> GetAllModels(CodeNamespace currentNamespace)
    {
        var models = new List<ITypeDefinition>();
        foreach (var classItem in currentNamespace.Classes)
        {
            if (classItem.IsOfKind(CodeClassKind.Model))
            {
                models.Add(classItem);
            }
            models.AddRange(GetAllInnerClasses(classItem));
        }
        foreach (var innerNamespace in currentNamespace.Namespaces)
        {
            models.AddRange(GetAllModels(innerNamespace));
        }
        models.AddRange(currentNamespace.Enums);
        return models;
    }

    private static List<CodeClass> GetAllInnerClasses(CodeClass currentClass)
    {
        var innerClasses = new List<CodeClass>();
        foreach (var innerClass in currentClass.InnerClasses)
        {
            innerClasses.Add(innerClass);
            innerClasses.AddRange(GetAllInnerClasses(innerClass));
        }
        return innerClasses;
    }
private void TrimInheritedModels()
{
    if (modelsNamespace is null || rootNamespace is null || modelsNamespace.Parent is not CodeNamespace clientNamespace) return;

    var reusableModels = GetAllModels(modelsNamespace);
    var modelsDirectlyInUse = new List<CodeElement>();
    modelsDirectlyInUse.AddRange(GetTypeDefinitionsInNamespace(rootNamespace));
    foreach (var key in multipartPropertiesModels.Keys)
    {
        if (!modelsDirectlyInUse.Contains(key))
        {
            modelsDirectlyInUse.Add(key);
        }
    }

    var classesDirectlyInUse = new HashSet<CodeClass>();
    foreach (var model in modelsDirectlyInUse)
    {
        if (model is CodeClass codeClass)
        {
            classesDirectlyInUse.Add(codeClass);
        }
    }

    var allModelClasses = new List<CodeClass>();
    foreach (var model in GetAllModels(clientNamespace))
    {
        if (model is CodeClass codeClass)
        {
            allModelClasses.Add(codeClass);
        }
    }
    var allModelClassesIndex = GetDerivationIndex(allModelClasses);
    CodeClass[] classesDirectlyInUseArray = new CodeClass[classesDirectlyInUse.Count];
    int index = 0;
    foreach (CodeClass codeClass in classesDirectlyInUse)
    {
        classesDirectlyInUseArray[index++] = codeClass;
    }
    var derivedClassesInUse = GetDerivedDefinitions(allModelClassesIndex, classesDirectlyInUseArray);

    var baseOfModelsInUse = new HashSet<CodeClass>();
    foreach (var codeClass in classesDirectlyInUse)
    {
        foreach (var baseClass in codeClass.GetInheritanceTree(false, false))
        {
            baseOfModelsInUse.Add(baseClass);
        }
    }

    var classesInUse = new HashSet<CodeClass>(derivedClassesInUse);
    foreach (var codeClass in classesDirectlyInUse)
    {
        classesInUse.Add(codeClass);
    }
    foreach (var baseClass in baseOfModelsInUse)
    {
        classesInUse.Add(baseClass);
    }

    var reusableClasses = new List<CodeClass>();
    foreach (var model in reusableModels)
    {
        if (model is CodeClass codeClass)
        {
            reusableClasses.Add(codeClass);
        }
    }
    var reusableClassesDerivationIndex = GetDerivationIndex(reusableClasses);
    var reusableClassesInheritanceIndex = GetInheritanceIndex(allModelClassesIndex);

    var relatedModels = new HashSet<CodeType>();
    foreach (var codeClass in classesInUse)
    {
        foreach (var relatedDefinition in GetRelatedDefinitions(codeClass, reusableClassesDerivationIndex, reusableClassesInheritanceIndex))
        {
            if (relatedDefinition is CodeType ct)
                relatedModels.Add(ct);
        }
    }
    foreach (var model in modelsDirectlyInUse)
    {
        if (model is CodeType ct)
        {
            relatedModels.Add(ct);
        }
    }

    Parallel.ForEach(reusableModels, parallelOptions, x =>
    {
        if (x is CodeType codeType && relatedModels.Contains(codeType)) return;

        if (x is CodeClass currentClass)
        {
            if (classesInUse.Contains(currentClass)) return;
            var parents = currentClass.GetInheritanceTree(false, false);
            foreach (var parent in parents)
            {
                if (classesDirectlyInUse.Contains(parent)) return;
            }
            foreach (var baseClass in parents)
            {
                baseClass.DiscriminatorInformation.RemoveDiscriminatorMapping(currentClass);
            }
        }
        logger.LogInformation("Removing unused model {ModelName} as it is not referenced by the client API surface", x.Name);
        x.GetImmediateParentOfType<CodeNamespace>().RemoveChildElement(x);
    });

    foreach (var leafNamespace in FindLeafNamespaces(modelsNamespace))
    {
        RemoveEmptyNamespaces(leafNamespace, modelsNamespace);
    }
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
        bool hasNamespaces = false;
        foreach (var childNamespace in currentNamespace.Namespaces)
        {
            hasNamespaces = true;
            foreach (var leafNamespace in FindLeafNamespaces(childNamespace))
            {
                yield return leafNamespace;
            }
        }
        
        if (!hasNamespaces)
        {
            yield return currentNamespace;
        }
    }
    private static void RemoveEmptyNamespaces(CodeNamespace currentNamespace, CodeNamespace stopAtNamespace)
    {
        if (currentNamespace == stopAtNamespace) 
            return;

        if (!(currentNamespace.Parent is CodeNamespace parentNamespace)) 
            return;

        bool isEmpty = true;
        foreach (var childNamespace in currentNamespace.Namespaces)
        {
            isEmpty = false;
            break;
        }

        if (isEmpty)
        {
            parentNamespace.RemoveChildElement(currentNamespace);
        }
        RemoveEmptyNamespaces(parentNamespace, stopAtNamespace);
    }
    private static List<CodeClass> GetDerivedDefinitions(ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> models, CodeClass[] modelsInUse)
    {
        List<CodeClass> currentDerived = new List<CodeClass>();
        foreach (var model in modelsInUse)
        {
            if (models.TryGetValue(model, out var res))
            {
                currentDerived.AddRange(res);
            }
        }

        List<CodeClass> derivedDefinitions = new List<CodeClass>(currentDerived);
        foreach (var derived in currentDerived)
        {
            derivedDefinitions.AddRange(GetDerivedDefinitions(models, [derived]));
        }

        return derivedDefinitions;
    }
    private static List<ITypeDefinition> GetRelatedDefinitions(ITypeDefinition currentElement, ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> derivedIndex, ConcurrentDictionary<CodeClass, ConcurrentBag<CodeClass>> inheritanceIndex, ConcurrentDictionary<CodeElement, bool>? visited = null)
    {
        visited ??= new();
        if (currentElement is not CodeClass currentClass || !visited.TryAdd(currentClass, true)) return new List<ITypeDefinition>();

        List<ITypeDefinition> propertiesDefinitions = new List<ITypeDefinition>();
        foreach (var property in currentClass.Properties)
        {
            foreach (var type in property.Type.AllTypes)
            {
                if (type.TypeDefinition is ITypeDefinition typeDefinition && (typeDefinition is CodeClass || typeDefinition is CodeEnum))
                {
                    if (typeDefinition is CodeClass classDefinition)
                    {
                        if (inheritanceIndex.TryGetValue(classDefinition, out var res))
                        {
                            propertiesDefinitions.AddRange(res);
                        }
                        propertiesDefinitions.AddRange(GetDerivedDefinitions(derivedIndex, new[] { classDefinition }));
                        propertiesDefinitions.Add(classDefinition);
                    }
                    else
                    {
                        propertiesDefinitions.Add(typeDefinition);
                    }
                }
            }
        }

        List<ITypeDefinition> propertiesParentTypes = new List<ITypeDefinition>();
        foreach (var definition in propertiesDefinitions)
        {
            if (definition is CodeClass codeClass)
            {
                foreach (var parentType in codeClass.GetInheritanceTree(false, false))
                {
                    if (parentType is ITypeDefinition typeDefinition)
                    {
                        propertiesParentTypes.Add(typeDefinition);
                    }
                }
            }
        }

        List<ITypeDefinition> result = new List<ITypeDefinition>(propertiesDefinitions);
        result.AddRange(propertiesParentTypes);

        foreach (var parentType in propertiesParentTypes)
        {
            result.AddRange(GetRelatedDefinitions(parentType, derivedIndex, inheritanceIndex, visited));
        }

        foreach (var definition in propertiesDefinitions)
        {
            result.AddRange(GetRelatedDefinitions(definition, derivedIndex, inheritanceIndex, visited));
        }

        // Remove duplicates
        List<ITypeDefinition> distinctResult = new List<ITypeDefinition>();
        foreach (var item in result)
        {
            if (!distinctResult.Contains(item))
            {
                distinctResult.Add(item);
            }
        }

        return distinctResult;
    }
    private List<CodeNamespace> GetAllNamespaces(CodeNamespace currentNamespace)
    {
        if (currentNamespace == modelsNamespace)
        {
            return new List<CodeNamespace>();
        }

        var allNamespaces = new List<CodeNamespace>();
        allNamespaces.Add(currentNamespace);
        foreach (var namespaceItem in currentNamespace.Namespaces)
        {
            allNamespaces.AddRange(GetAllNamespaces(namespaceItem));
        }
        return allNamespaces;
    }
    private List<CodeElement> GetTypeDefinitionsInNamespace(CodeNamespace currentNamespace)
    {
        List<CodeType> allTypes = new List<CodeType>();
        List<CodeElement> typeDefinitions = new List<CodeElement>();

        foreach (var ns in GetAllNamespaces(currentNamespace))
        {
            foreach (var cls in ns.Classes)
            {
                if (cls.IsOfKind(CodeClassKind.RequestBuilder))
                {
                    foreach (var method in cls.Methods)
                    {
                        if (method.IsOfKind(CodeMethodKind.RequestExecutor))
                        {
                            allTypes.AddRange(method.ReturnType.AllTypes);

                            foreach (var param in method.Parameters)
                            {
                                if (param.IsOfKind(CodeParameterKind.RequestBody))
                                {
                                    allTypes.AddRange(param.Type.AllTypes);
                                }

                                if (param.IsOfKind(CodeParameterKind.RequestConfiguration))
                                {
                                    foreach (var type in param.Type.AllTypes)
                                    {
                                        if (type.TypeDefinition is CodeClass codeClass)
                                        {
                                            foreach (var prop in codeClass.Properties)
                                            {
                                                if (prop.Kind is CodePropertyKind.QueryParameters && prop.Type is CodeType codeType)
                                                {
                                                    allTypes.Add(codeType);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (var mapping in method.ErrorMappings)
                            {
                                allTypes.AddRange(mapping.Value.AllTypes);
                            }
                        }
                    }
                }
            }
        }

        foreach (var type in allTypes)
        {
            if (type.TypeDefinition != null)
            {
                CodeElement typeDefinition = type.TypeDefinition;
                if (typeDefinition is CodeClass || typeDefinition is CodeEnum)
                {
                    typeDefinitions.Add(typeDefinition);
                }
            }
        }

        return typeDefinitions;
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
        foreach (var x in discriminatorMappings)
            newClass.DiscriminatorInformation.AddDiscriminatorMapping(x.Key, x.Value);
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
    private CodeType? GetCodeTypeForMapping(OpenApiUrlTreeNode currentNode, string referenceId, CodeNamespace currentNamespace, CodeClass? baseClass, OpenApiSchema currentSchema)
    {
        var componentKey = referenceId?.Replace("#/components/schemas/", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (openApiDocument == null || !openApiDocument.Components.Schemas.TryGetValue(componentKey, out var discriminatorSchema))
        {
            logger.LogWarning("Discriminator {ComponentKey} not found in the OpenAPI document.", componentKey);
            return null;
        }
        var className = currentNode.GetClassName(config.StructuredMimeTypes, schema: discriminatorSchema).CleanupSymbolName();
        
        var shouldInherit = false;
        foreach (var schema in discriminatorSchema.AllOf)
        {
            if (currentSchema.Reference?.Id.Equals(schema.Reference?.Id, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                shouldInherit = true;
                break;
            }
        }
        
        if (baseClass != null && shouldInherit && !discriminatorSchema.IsInherited())
        {
            logger.LogWarning("Discriminator {ComponentKey} is not inherited from {ClassName}.", componentKey, baseClass.Name);
            return null;
        }
        
        var codeClass = AddModelDeclarationIfDoesntExist(currentNode, discriminatorSchema, className, GetShortestNamespace(currentNamespace, discriminatorSchema), shouldInherit ? baseClass : null, currentSchema);
        return new CodeType
        {
            TypeDefinition = codeClass,
        };
    }
    private void CreatePropertiesForModelClass(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace ns, CodeClass model)
    {
        var properties = CollectAllProperties(schema);
        if (properties != null && properties.Count != 0)
        {
            List<CodeProperty> propertiesToAdd = new List<CodeProperty>();
            foreach (var property in properties)
            {
                var propertySchema = property.Value;
                var className = $"{model.Name}_{property.Key.CleanupSymbolName()}";
                var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(propertySchema.Reference?.Id);
                var targetNamespace = string.IsNullOrEmpty(shortestNamespaceName) ? ns :
                                    rootNamespace?.FindOrAddNamespace(shortestNamespaceName) ?? ns;
                var definition = CreateModelDeclarations(currentNode, propertySchema, default, targetNamespace, string.Empty, typeNameForInlineSchema: className);
                if (definition == null)
                {
                    logger.LogWarning("Omitted property {PropertyName} for model {ModelName} in API path {ApiPath}, the schema is invalid.", property.Key, model.Name, currentNode.Path);
                    continue;
                }
                var propertyToAdd = CreateProperty(property.Key, definition.Name, propertySchema: propertySchema, existingType: definition);
                if (propertyToAdd != null)
                {
                    propertiesToAdd.Add(propertyToAdd);
                }
            }
            if (propertiesToAdd.Count != 0)
            {
                model.AddProperty(propertiesToAdd.ToArray());
            }
        }
    }
    private Dictionary<string, OpenApiSchema> CollectAllProperties(OpenApiSchema schema)
    {
        Dictionary<string, OpenApiSchema> result = schema.Properties != null
            ? new Dictionary<string, OpenApiSchema>(schema.Properties, StringComparer.Ordinal)
            : new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);

        if (schema.AllOf != null && schema.AllOf.Count > 0)
        {
            foreach (var supSchema in schema.AllOf)
            {
                if (!supSchema.IsReferencedSchema() && supSchema.HasAnyProperty())
                {
                    foreach (var supProperty in supSchema.Properties)
                    {
                        result[supProperty.Key] = supProperty.Value;
                    }
                }
            }
        }

        return result;
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
    private CodeClass? CreateOperationParameterClass(OpenApiUrlTreeNode node, OperationType operationType, OpenApiOperation operation, CodeClass parentClass)
    {
        List<OpenApiParameter> parameters = new List<OpenApiParameter>();
        foreach (var parameter in node.PathItems[Constants.DefaultOpenApiLabel].Parameters)
        {
            parameters.Add(parameter);
        }
        foreach (var parameter in operation.Parameters)
        {
            if (!parameters.Contains(parameter) && parameter.In == ParameterLocation.Query)
            {
                parameters.Add(parameter);
            }
        }

        if (parameters.Count != 0)
        {
            CodeClass? parameterClass = null;
            foreach (var item in parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType}QueryParameters",
                Kind = CodeClassKind.QueryParameters,
                Documentation = new()
                {
                    DescriptionTemplate = (operation.Description is string description && !string.IsNullOrEmpty(description) ?
                                    description :
                                    operation.Summary).CleanupDescription(),
                },
            }))
            {
                parameterClass = item;
                break;
            }

            if (parameterClass is null) throw new ArgumentException("Couldn't add inner class.", nameof(parentClass));
            foreach (var parameter in parameters)
                AddPropertyForQueryParameter(node, operationType, parameter, parameterClass);

            return parameterClass;
        }

        return null;
    }
    private void AddPropertyForQueryParameter(OpenApiUrlTreeNode node, OperationType operationType, OpenApiParameter parameter, CodeClass parameterClass)
    {
        CodeType? resultType = default;
        var addBackwardCompatibleParameter = false;

        if (parameter.Schema.IsEnum() || (parameter.Schema.IsArray() && parameter.Schema.Items.IsEnum()))
        {
            var enumSchema = parameter.Schema.IsArray() ? parameter.Schema.Items : parameter.Schema;
            var codeNamespace = enumSchema.IsReferencedSchema() switch
            {
                true => GetShortestNamespace(parameterClass.GetImmediateParentOfType<CodeNamespace>(), enumSchema), // referenced schema
                false => parameterClass.GetImmediateParentOfType<CodeNamespace>(), // Inline schema, i.e. specific to the Operation
            };
            var shortestNamespace = GetShortestNamespace(codeNamespace, enumSchema);
            var enumName = enumSchema.GetSchemaName().CleanupSymbolName();
            if (string.IsNullOrEmpty(enumName))
                enumName = $"{operationType.ToString().ToFirstCharacterUpperCase()}{parameter.Name.CleanupSymbolName().ToFirstCharacterUpperCase()}QueryParameterType";
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
        var prop = new CodeProperty
        {
            Name = parameter.Name.SanitizeParameterNameForCodeSymbols(),
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
    private static CodeType GetQueryParameterType(OpenApiSchema schema)
    {
        var paramType = GetPrimitiveType(schema) ?? new()
        {
            IsExternal = true,
            Name = schema.Items?.Type ?? schema.Type,
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
