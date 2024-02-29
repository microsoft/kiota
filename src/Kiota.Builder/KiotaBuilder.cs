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
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using DotNet.Globbing;
using Kiota.Builder.Caching;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;
using Kiota.Builder.EqualityComparers;
using Kiota.Builder.Exceptions;
using Kiota.Builder.Extensions;
using Kiota.Builder.Lock;
using Kiota.Builder.Logging;
using Kiota.Builder.Manifest;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.SearchProviders.APIsGuru;
using Kiota.Builder.Validation;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Validations;
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

    public KiotaBuilder(ILogger<KiotaBuilder> logger, GenerationConfiguration config, HttpClient client)
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
    }
    private async Task CleanOutputDirectory(CancellationToken cancellationToken)
    {
        if (config.CleanOutput && Directory.Exists(config.OutputPath))
        {
            logger.LogInformation("Cleaning output directory {Path}", config.OutputPath);
            // not using Directory.Delete on the main directory because it's locked when mapped in a container
            foreach (var subDir in Directory.EnumerateDirectories(config.OutputPath))
                Directory.Delete(subDir, true);
            await lockManagementService.BackupLockFileAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
            foreach (var subFile in Directory.EnumerateFiles(config.OutputPath)
                                            .Where(x => !x.EndsWith(FileLogLogger.LogFileName, StringComparison.OrdinalIgnoreCase)))
                File.Delete(subFile);
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
                    config.IncludePatterns = manifestDetails.Item2.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        // Should Generate
        sw.Start();
        var shouldGenerate = await ShouldGenerate(cancellationToken).ConfigureAwait(false);
        StopLogAndReset(sw, $"step {++stepId} - checking whether the output should be updated - took");

        OpenApiUrlTreeNode? openApiTree = null;
        if (openApiDocument != null)
        {
            // filter paths
            sw.Start();
            FilterPathsByPatterns(openApiDocument);
            StopLogAndReset(sw, $"step {++stepId} - filtering API paths with patterns - took");
            if (shouldGenerate && generating)
            {
                SetApiRootUrl();

                modelNamespacePrefixToTrim = GetDeeperMostCommonNamespaceNameForModels(openApiDocument);
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
    private async Task<bool> ShouldGenerate(CancellationToken cancellationToken)
    {
        if (config.CleanOutput) return true;
        var existingLock = await lockManagementService.GetLockFromDirectoryAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
        var configurationLock = new KiotaLock(config)
        {
            DescriptionHash = openApiDocument?.HashCode ?? string.Empty,
        };
        var comparer = new KiotaLockComparer();
        if (!string.IsNullOrEmpty(existingLock?.KiotaVersion) && !configurationLock.KiotaVersion.Equals(existingLock.KiotaVersion, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("API client was generated with version {ExistingVersion} and the current version is {CurrentVersion}, it will be upgraded and you should upgrade dependencies", existingLock?.KiotaVersion, configurationLock.KiotaVersion);
        }
        return !comparer.Equals(existingLock, configurationLock);
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
    /// Generates the code from the OpenAPI document
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Whether the generated code was updated or not</returns>
    public async Task<bool> GenerateClientAsync(CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        // Read input stream
        var inputPath = config.OpenAPIFilePath;

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

            if (!shouldGenerate)
            {
                logger.LogInformation("No changes detected, skipping generation");
                return false;
            }
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

            // Write lock file
            sw.Start();
            await UpdateLockFile(cancellationToken).ConfigureAwait(false);
            StopLogAndReset(sw, $"step {++stepId} - writing lock file - took");
        }
        catch
        {
            await lockManagementService.RestoreLockFileAsync(config.OutputPath, cancellationToken).ConfigureAwait(false);
            throw;
        }
        return true;
    }
    private readonly LockManagementService lockManagementService = new();
    private async Task UpdateLockFile(CancellationToken cancellationToken)
    {
        var configurationLock = new KiotaLock(config)
        {
            DescriptionHash = openApiDocument?.HashCode ?? string.Empty,
        };
        await lockManagementService.WriteLockFileAsync(config.OutputPath, configurationLock, cancellationToken).ConfigureAwait(false);
    }
    private static readonly GlobComparer globComparer = new();
    [GeneratedRegex(@"([\/\\])\{[\w\d-]+\}([\/\\])", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex MultiIndexSameLevelCleanupRegex();
    private static string ReplaceAllIndexesWithWildcard(string path, uint depth = 10) => depth == 0 ? path : ReplaceAllIndexesWithWildcard(MultiIndexSameLevelCleanupRegex().Replace(path, "$1{*}$2"), depth - 1); // the bound needs to be greedy to avoid replacing anything else than single path parameters
    private static Dictionary<Glob, HashSet<OperationType>> GetFilterPatternsFromConfiguration(HashSet<string> configPatterns)
    {
        return configPatterns.Select(static x =>
        {
            var splat = x.Split('#', StringSplitOptions.RemoveEmptyEntries);
            var glob = Glob.Parse(ReplaceAllIndexesWithWildcard(splat[0]));
            var operationTypes = splat.Length > 1 ?
                                    splat[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(static y => Enum.TryParse<OperationType>(y.Trim(), true, out var op) ? op : default(OperationType?)) :
                                    Enumerable.Empty<OperationType?>();
            return (glob, operationTypes);
        }).GroupBy(static x => x.glob, globComparer)
        .ToDictionary(static x => x.Key,
                    static x => new HashSet<OperationType>(x.SelectMany(static y => y.operationTypes)
                                                            .Where(static y => y != null && y.HasValue)
                                                            .Select(static y => y!.Value)),
                    globComparer);
    }
    internal void FilterPathsByPatterns(OpenApiDocument doc)
    {
        var includePatterns = GetFilterPatternsFromConfiguration(config.IncludePatterns);
        var excludePatterns = GetFilterPatternsFromConfiguration(config.ExcludePatterns);
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
                path.Value.Operations.Keys.Where(x => operationIncludePatterns.Count != 0 && !operationIncludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x)) ||
                                        operationExcludePatterns.Count != 0 && operationExcludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x)))
                .ToList()
                .ForEach(x => path.Value.Operations.Remove(x));
            }
            foreach (var path in doc.Paths.Where(static x => !x.Value.Operations.Any()).ToList())
                doc.Paths.Remove(path.Key);
        }

        if (!doc.Paths.Any())
            logger.LogWarning("No paths were found matching the provided patterns. Check your configuration.");
    }
    internal void SetApiRootUrl()
    {
        if (openApiDocument == null) return;
        var candidateUrl = openApiDocument.Servers
                                        .GroupBy(static x => x, new OpenApiServerComparer()) //group by protocol relative urls
                                        .FirstOrDefault()
                                        ?.OrderByDescending(static x => x?.Url, StringComparer.OrdinalIgnoreCase) // prefer https over http
                                        ?.FirstOrDefault()
                                        ?.Url;
        if (string.IsNullOrEmpty(candidateUrl))
        {
            logger.LogWarning("No server url found in the OpenAPI document. The base url will need to be set when using the client.");
            return;
        }
        else if (!candidateUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && config.OpenAPIFilePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                candidateUrl = new Uri(new Uri(config.OpenAPIFilePath), candidateUrl).ToString();
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(ex, "Could not resolve the server url from the OpenAPI document. The base url will need to be set when using the client.");
                return;
            }
        }
        config.ApiRootUrl = candidateUrl.TrimEnd(ForwardSlash);
    }
    private void StopLogAndReset(Stopwatch sw, string prefix)
    {
        sw.Stop();
        logger.LogDebug("{Prefix} {SwElapsed}", prefix, sw.Elapsed);
        sw.Reset();
    }

    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });

    private async Task<Stream> LoadStream(string inputPath, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        inputPath = inputPath.Trim();

        Stream input;
        if (inputPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            try
            {
                var cachingProvider = new DocumentCachingProvider(httpClient, logger)
                {
                    ClearCache = config.ClearCache,
                };
                var targetUri = APIsGuruSearchProvider.ChangeSourceUrlToGitHub(new Uri(inputPath)); // so updating existing clients doesn't break
                var fileName = targetUri.GetFileName() is string name && !string.IsNullOrEmpty(name) ? name : "description.yml";
                input = await cachingProvider.GetDocumentAsync(targetUri, "generation", fileName, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Could not download the file at {inputPath}, reason: {ex.Message}", ex);
            }
        else
            try
            {
#pragma warning disable CA2000 // disposed by caller
                var inMemoryStream = new MemoryStream();
                using (await localFilesLock.LockAsync(inputPath, cancellationToken).ConfigureAwait(false))
                {// To avoid deadlocking on update with multiple clients for the same local description
                    using var fileStream = new FileStream(inputPath, FileMode.Open);
                    await fileStream.CopyToAsync(inMemoryStream, cancellationToken).ConfigureAwait(false);
                }
                inMemoryStream.Position = 0;
                input = inMemoryStream;
#pragma warning restore CA2000
            }
            catch (Exception ex) when (ex is FileNotFoundException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is NotSupportedException)
            {
                throw new InvalidOperationException($"Could not open the file at {inputPath}, reason: {ex.Message}", ex);
            }
        stopwatch.Stop();
        logger.LogTrace("{Timestamp}ms: Read OpenAPI file {File}", stopwatch.ElapsedMilliseconds, inputPath);
        return input;
    }

    private const char ForwardSlash = '/';
    public async Task<OpenApiDocument?> CreateOpenApiDocumentAsync(Stream input, bool generating = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        logger.LogTrace("Parsing OpenAPI file");
        var ruleSet = config.DisabledValidationRules.Contains(ValidationRuleSetExtensions.AllValidationRule) ?
                    ValidationRuleSet.GetEmptyRuleSet() :
                    ValidationRuleSet.GetDefaultRuleSet(); //workaround since validation rule set doesn't support clearing rules
        if (generating)
            ruleSet.AddKiotaValidationRules(config);
        var settings = new OpenApiReaderSettings
        {
            RuleSet = ruleSet,
        };
        settings.AddMicrosoftExtensionParsers();
        settings.ExtensionParsers.TryAdd(OpenApiKiotaExtension.Name, static (i, _) => OpenApiKiotaExtension.Parse(i));
        try
        {
            var rawUri = config.OpenAPIFilePath.TrimEnd(ForwardSlash);
            var lastSlashIndex = rawUri.LastIndexOf(ForwardSlash);
            if (lastSlashIndex < 0)
                lastSlashIndex = rawUri.Length - 1;
            var documentUri = new Uri(rawUri[..lastSlashIndex]);
            settings.BaseUrl = documentUri;
            settings.LoadExternalRefs = true;
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            // couldn't parse the URL, it's probably a local file
        }
        var reader = new OpenApiStreamReader(settings);
        var readResult = await reader.ReadAsync(input, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        if (generating)
            foreach (var warning in readResult.OpenApiDiagnostic.Warnings)
                logger.LogWarning("OpenAPI warning: {Pointer} - {Warning}", warning.Pointer, warning.Message);
        if (readResult.OpenApiDiagnostic.Errors.Any())
        {
            logger.LogTrace("{Timestamp}ms: Parsed OpenAPI with errors. {Count} paths found.", stopwatch.ElapsedMilliseconds, readResult.OpenApiDocument?.Paths?.Count ?? 0);
            foreach (var parsingError in readResult.OpenApiDiagnostic.Errors)
            {
                logger.LogError("OpenAPI error: {Pointer} - {Message}", parsingError.Pointer, parsingError.Message);
            }
        }
        else
        {
            logger.LogTrace("{Timestamp}ms: Parsed OpenAPI successfully. {Count} paths found.", stopwatch.ElapsedMilliseconds, readResult.OpenApiDocument?.Paths?.Count ?? 0);
        }

        return readResult.OpenApiDocument;
    }
    public static string GetDeeperMostCommonNamespaceNameForModels(OpenApiDocument document)
    {
        if (!(document?.Components?.Schemas?.Any() ?? false)) return string.Empty;
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

            logger.LogTrace("{Timestamp}ms: Created source model with {Count} classes", stopwatch.ElapsedMilliseconds, codeNamespace.GetChildElements(true).Count());
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
            var className = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, ItemRequestBuilderSuffix) : currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, RequestBuilderSuffix);
            codeClass = targetNS.AddClass(new CodeClass
            {
                Name = className.CleanupSymbolName(),
                Kind = CodeClassKind.RequestBuilder,
                Documentation = new()
                {
                    DescriptionTemplate = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
                },
            }).First();
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
            var parameterType = GetPrimitiveType(parameter.Schema ?? parameter.Content.Values.FirstOrDefault()?.Schema) ??
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
                    DescriptionTemplate = parameter.Description.CleanupDescription(),
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
            x.TypeDefinition = parentNS?.FindChildrenByName<CodeClass>(x.Name).MinBy(shortestNamespaceOrder);
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
    private const char OpenAPIUrlTreeNodePathSeparator = '\\';
    private CodeParameter GetIndexerParameter(OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode parentNode)
    {
        var parameterName = string.Join(OpenAPIUrlTreeNodePathSeparator, currentNode.Path.Split(OpenAPIUrlTreeNodePathSeparator, StringSplitOptions.RemoveEmptyEntries)
                                        .Skip(parentNode.Path.Count(static x => x == OpenAPIUrlTreeNodePathSeparator)))
                                        .Trim(OpenAPIUrlTreeNodePathSeparator, ForwardSlash, '{', '}');
        var pathItems = GetPathItems(currentNode);
        var parameter = pathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) ? pathItem.Parameters
                        .Select(static x => new { Parameter = x, IsPathParameter = true })
                        .Union(pathItems[Constants.DefaultOpenApiLabel].Operations.SelectMany(static x => x.Value.Parameters).Select(static x => new { Parameter = x, IsPathParameter = false }))
                        .OrderBy(static x => x.IsPathParameter)
                        .Select(static x => x.Parameter)
                        .FirstOrDefault(x => x.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase) && x.In == ParameterLocation.Path) :
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
    private static IDictionary<string, OpenApiPathItem> GetPathItems(OpenApiUrlTreeNode currentNode, bool validateIsParameterNode = true)
    {
        if ((!validateIsParameterNode || currentNode.IsParameter) && currentNode.PathItems.Any())
        {
            return currentNode.PathItems;
        }

        if (currentNode.Children.Any())
        {
            return currentNode.Children
                .SelectMany(static x => GetPathItems(x.Value, false))
                .DistinctBy(static x => x.Key, StringComparer.Ordinal)
                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal);
        }

        return ImmutableDictionary<string, OpenApiPathItem>.Empty;
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
        if (typeSchema?.AnyOf?.Any() ?? false)
            typeNames.AddRange(typeSchema.AnyOf.Select(x => x.Type)); // double is sometimes an anyof string, number and enum
        if (typeSchema?.OneOf?.Any() ?? false)
            typeNames.AddRange(typeSchema.OneOf.Select(x => x.Type)); // double is sometimes an oneof string, number and enum
                                                                      // first value that's not null, and not "object" for primitive collections, the items type matters
        var typeName = typeNames.FirstOrDefault(static x => !string.IsNullOrEmpty(x) && !typeNamesToSkip.Contains(x));

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
    private static readonly HashSet<string> noContentStatusCodes = new(StringComparer.OrdinalIgnoreCase) { "201", "202", "204", "205" };
    private static readonly HashSet<string> errorStatusCodes = new(Enumerable.Range(400, 599).Select(static x => x.ToString(CultureInfo.InvariantCulture))
                                                                                 .Concat([CodeMethod.ErrorMappingClientRange, CodeMethod.ErrorMappingServerRange]), StringComparer.OrdinalIgnoreCase);
    private void AddErrorMappingsForExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod)
    {
        foreach (var response in operation.Responses.Where(x => errorStatusCodes.Contains(x.Key)))
        {
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
                string returnType;
                if (operation.Responses.Any(static x => noContentStatusCodes.Contains(x.Key)))
                    returnType = VoidType;
                else if (operation.Responses.Any(static x => x.Value.Content.ContainsKey(RequestBodyPlainTextContentType)))
                    returnType = "string";
                else
                    returnType = "binary";
                return (new CodeType { Name = returnType, IsExternal = true, }, null);
            }
            return (modelType, null);
        }
        else
        {
            string returnType;
            if (operation.Responses.Any(static x => noContentStatusCodes.Contains(x.Key)))
                returnType = VoidType;
            else if (operation.Responses.Any(static x => x.Value.Content.ContainsKey(RequestBodyPlainTextContentType)))
                returnType = "string";
            else
                returnType = "binary";
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
            }).First();

            var schema = operation.GetResponseSchema(config.StructuredMimeTypes);
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

            if (operation.Extensions.TryGetValue(OpenApiPagingExtension.Name, out var extension) && extension is OpenApiPagingExtension pagingExtension)
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
            if (!operationUrlTemplate.Equals(parentClass.Properties.FirstOrDefault(static x => x.Kind is CodePropertyKind.UrlTemplate)?.DefaultValue?.Trim('"'), StringComparison.Ordinal))
                generatorMethod.UrlTemplateOverride = operationUrlTemplate;

            var mediaTypes = schema switch
            {
                null => operation.Responses
                                .Where(static x => !errorStatusCodes.Contains(x.Key))
                                .SelectMany(static x => x.Value.Content)
                                .Select(static x => x.Key) //get the successful non structured media types first, with a default 1 priority
                                .Union(config.StructuredMimeTypes.GetAcceptedTypes(
                                                            operation.Responses
                                                            .Where(static x => errorStatusCodes.Contains(x.Key)) // get any structured error ones, with the priority from the configuration
                                                            .SelectMany(static x => x.Value.Content) // we can safely ignore unstructured ones as they won't be used in error mappings anyway and the body won't be read
                                                            .Select(static x => x.Key)))
                        .Distinct(StringComparer.OrdinalIgnoreCase),
                _ => config.StructuredMimeTypes.GetAcceptedTypes(operation.Responses.Values.SelectMany(static x => x.Content).Where(x => schemaReferenceComparer.Equals(schema, x.Value.Schema)).Select(static x => x.Key)),
            };
            generatorMethod.AddAcceptedResponsesTypes(mediaTypes);
            if (config.Language == GenerationLanguage.CLI)
                SetPathAndQueryParameters(generatorMethod, currentNode, operation);
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
        var pathAndQueryParameters = currentNode
            .PathItems[Constants.DefaultOpenApiLabel]
            .Parameters
            .Where(ParametersFilter)
            .Select(GetCodeParameterFromApiParameter)
            .Union(operation
                    .Parameters
                    .Where(ParametersFilter)
                    .Select(GetCodeParameterFromApiParameter))
            .ToArray();
        target.AddPathQueryOrHeaderParameter(pathAndQueryParameters);
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
                var mediaType = operation.RequestBody.Content.First(x => x.Value.Schema == requestBodySchema).Value;
                foreach (var encodingEntry in mediaType.Encoding
                                                        .Where(x => !string.IsNullOrEmpty(x.Value.ContentType) &&
                                                                config.StructuredMimeTypes.Contains(x.Value.ContentType)))
                {
                    if (CreateModelDeclarations(currentNode, requestBodySchema.Properties[encodingEntry.Key], operation, method, $"{operationType}RequestBody", isRequestBody: true) is CodeType propertyType &&
                        propertyType.TypeDefinition is not null)
                        multipartPropertiesModels.TryAdd(propertyType.TypeDefinition, true);
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
            method.RequestBodyContentType = config.StructuredMimeTypes.GetContentTypes(operation.RequestBody.Content.Where(x => schemaReferenceComparer.Equals(x.Value.Schema, requestBodySchema)).Select(static x => x.Key)).First();
        }
        else if (operation.RequestBody?.Content?.Any() ?? false)
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
    private CodeType CreateModelDeclarationAndType(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string classNameSuffix = "", OpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false)
    {
        var className = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, response: response, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, schema, className, codeNamespace);
        return new CodeType
        {
            TypeDefinition = codeDeclaration,
        };
    }
    private CodeType CreateInheritedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string classNameSuffix, CodeNamespace codeNamespace, bool isRequestBody)
    {
        var allOfs = schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf);
        CodeElement? codeDeclaration = null;
        var className = string.Empty;
        var codeNamespaceFromParent = GetShortestNamespace(codeNamespace, schema);
        foreach (var currentSchema in allOfs)
        {
            var referenceId = GetReferenceIdFromOriginalSchema(currentSchema, schema);
            var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(referenceId);
            var shortestNamespace = string.IsNullOrEmpty(referenceId) ? codeNamespaceFromParent : rootNamespace?.FindOrAddNamespace(shortestNamespaceName);
            className = (currentSchema.GetSchemaName() is string cName && !string.IsNullOrEmpty(cName) ?
                            cName :
                            currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, schema: schema, requestBody: isRequestBody))
                        .CleanupSymbolName();
            if (shortestNamespace != null)
                codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, currentSchema, className, shortestNamespace, codeDeclaration as CodeClass);
        }
        if (codeDeclaration is CodeClass currentClass &&
            !currentClass.Documentation.DescriptionAvailable &&
            string.IsNullOrEmpty(schema.AllOf.LastOrDefault()?.Description) &&
            !string.IsNullOrEmpty(schema.Description))
            currentClass.Documentation.DescriptionTemplate = schema.Description.CleanupDescription(); // the last allof entry often is not a reference and doesn't have a description.

        return new CodeType
        {
            TypeDefinition = codeDeclaration,
        };
    }
    private static string? GetReferenceIdFromOriginalSchema(OpenApiSchema schema, OpenApiSchema parentSchema)
    {
        var title = schema.Title;
        if (!string.IsNullOrEmpty(schema.Reference?.Id)) return schema.Reference.Id;
        if (string.IsNullOrEmpty(title)) return string.Empty;
        if (parentSchema.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false) return parentSchema.Reference.Id;
        if (parentSchema.Items?.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false) return parentSchema.Items.Reference.Id;
        return parentSchema.GetSchemaReferenceIds().FirstOrDefault(refId => refId.EndsWith(title, StringComparison.OrdinalIgnoreCase));
    }
    private CodeTypeBase CreateComposedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string suffixForInlineSchema, CodeNamespace codeNamespace, bool isRequestBody, string typeNameForInlineSchema)
    {
        var typeName = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: suffixForInlineSchema, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var typesCount = schema.AnyOf?.Count ?? schema.OneOf?.Count ?? 0;
        if (typesCount == 1 && schema.Nullable && schema.IsInclusiveUnion() || // nullable on the root schema outside of anyOf
            typesCount == 2 && (schema.AnyOf?.Any(static x => // nullable on a schema in the anyOf
                                                        x.Nullable &&
                                                        !x.Properties.Any() &&
                                                        !x.IsExclusiveUnion() &&
                                                        !x.IsInclusiveUnion() &&
                                                        !x.IsInherited() &&
                                                        !x.IsIntersection() &&
                                                        !x.IsArray() &&
                                                        !x.IsReferencedSchema()) ?? false))
        { // once openAPI 3.1 is supported, there will be a third case oneOf with Ref and type null.
            var targetSchema = schema.AnyOf?.First(static x => !string.IsNullOrEmpty(x.GetSchemaName()));
            if (targetSchema is not null)
            {
                var className = targetSchema.GetSchemaName().CleanupSymbolName();
                var shortestNamespace = GetShortestNamespace(codeNamespace, targetSchema);
                return new CodeType
                {
                    TypeDefinition = AddModelDeclarationIfDoesntExist(currentNode, targetSchema, className, shortestNamespace),
                    CollectionKind = targetSchema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Complex : default
                };// so we don't create unnecessary union types when anyOf was used only for nullable.
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
        GetDiscriminatorMappings(currentNode, schema, codeNamespace, null)
            ?.ToList()
            .ForEach(x => unionType.DiscriminatorInformation.AddDiscriminatorMapping(x.Key, x.Value));
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
    private CodeTypeBase? CreateModelDeclarations(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeElement parentElement, string suffixForInlineSchema, OpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false)
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
            return CreateInheritedModelDeclaration(currentNode, schema, operation, suffix, codeNamespace, isRequestBody);
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

        if (schema.IsObject() || schema.Properties.Any() || schema.IsEnum() || !string.IsNullOrEmpty(schema.AdditionalProperties?.Type))
        {
            // no inheritance or union type, often empty definitions with only additional properties are used as property bags.
            return CreateModelDeclarationAndType(currentNode, schema, operation, codeNamespace, suffix, response: responseValue, typeNameForInlineSchema: typeNameForInlineSchema, isRequestBody);
        }

        if (schema.IsArray())
        {
            // collections at root
            return CreateCollectionModelDeclaration(currentNode, schema, operation, codeNamespace, typeNameForInlineSchema, isRequestBody);
        }

        if (!string.IsNullOrEmpty(schema.Type) || !string.IsNullOrEmpty(schema.Format))
            return GetPrimitiveType(schema, string.Empty);
        if ((schema.AnyOf.Any() || schema.OneOf.Any() || schema.AllOf.Any()) &&
           (schema.AnyOf.FirstOrDefault(static x => x.IsSemanticallyMeaningful(true)) ?? schema.OneOf.FirstOrDefault(static x => x.IsSemanticallyMeaningful(true)) ?? schema.AllOf.FirstOrDefault(static x => x.IsSemanticallyMeaningful(true))) is { } childSchema) // we have an empty node because of some local override for schema properties and need to unwrap it.
            return CreateModelDeclarations(currentNode, childSchema, operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema, isRequestBody);
        return null;
    }
    private CodeTypeBase? CreateCollectionModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string typeNameForInlineSchema, bool isRequestBody)
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
        if (type is null) return null;
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
    private CodeElement AddModelDeclarationIfDoesntExist(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, CodeClass? inheritsFrom = null)
    {
        if (GetExistingDeclaration(currentNamespace, currentNode, declarationName) is not CodeElement existingDeclaration) // we can find it in the components
        {
            if (AddEnumDeclaration(currentNode, schema, declarationName, currentNamespace) is CodeEnum enumDeclaration)
                return enumDeclaration;

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
            return currentNamespace.AddEnum(newEnum).First();
        }
        return default;
    }
    private static void SetEnumOptions(OpenApiSchema schema, CodeEnum target)
    {
        OpenApiEnumValuesDescriptionExtension? extensionInformation = null;
        if (schema.Extensions.TryGetValue(OpenApiEnumValuesDescriptionExtension.Name, out var rawExtension) && rawExtension is OpenApiEnumValuesDescriptionExtension localExtInfo)
            extensionInformation = localExtInfo;
        target.AddOption(schema.Enum.OfType<OpenApiString>()
                        .Where(static x => !x.Value.Equals("null", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(x.Value))
                        .Select(static x => x.Value)
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
                        .ToArray());
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
        if (inheritsFrom == null && schema.AllOf.FirstOrDefault(static x => x.Reference != null) is OpenApiSchema parentSchema)
        {// any non-reference would be the current class in some description styles
            var parentClassNamespace = GetShortestNamespace(currentNamespace, parentSchema);
            inheritsFrom = (CodeClass)AddModelDeclarationIfDoesntExist(currentNode, parentSchema, parentSchema.GetSchemaName().CleanupSymbolName(), parentClassNamespace);
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
        var includeAdditionalDataProperties = config.IncludeAdditionalData && schema.AdditionalPropertiesAllowed;
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

        var mappings = GetDiscriminatorMappings(currentNode, schema, currentNamespace, newClass)
                        .Where(x => x.Value is CodeType type &&
                                    type.TypeDefinition != null &&
                                    type.TypeDefinition is CodeClass definition &&
                                    definition.DerivesFrom(newClass)); // only the mappings that derive from the current class

        AddDiscriminatorMethod(newClass, schema.GetDiscriminatorPropertyName(), mappings, static s => s);
        return newClass;
    }
    private IEnumerable<KeyValuePair<string, CodeType>> GetDiscriminatorMappings(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace currentNamespace, CodeClass? baseClass)
    {
        return schema.GetDiscriminatorMappings(inheritanceIndex)
                .Select(x => KeyValuePair.Create(x.Key, GetCodeTypeForMapping(currentNode, x.Value, currentNamespace, baseClass, schema)))
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
    private CodeType? GetCodeTypeForMapping(OpenApiUrlTreeNode currentNode, string referenceId, CodeNamespace currentNamespace, CodeClass? baseClass, OpenApiSchema currentSchema)
    {
        var componentKey = referenceId?.Replace("#/components/schemas/", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (openApiDocument == null || !openApiDocument.Components.Schemas.TryGetValue(componentKey, out var discriminatorSchema))
        {
            logger.LogWarning("Discriminator {ComponentKey} not found in the OpenAPI document.", componentKey);
            return null;
        }
        var className = currentNode.GetClassName(config.StructuredMimeTypes, schema: discriminatorSchema).CleanupSymbolName();
        var shouldInherit = discriminatorSchema.AllOf.Any(x => currentSchema.Reference?.Id.Equals(x.Reference?.Id, StringComparison.OrdinalIgnoreCase) ?? false);
        var codeClass = AddModelDeclarationIfDoesntExist(currentNode, discriminatorSchema, className, GetShortestNamespace(currentNamespace, discriminatorSchema), shouldInherit ? baseClass : null);
        return new CodeType
        {
            TypeDefinition = codeClass,
        };
    }
    private void CreatePropertiesForModelClass(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace ns, CodeClass model)
    {
        if (CollectAllProperties(schema) is var properties && properties.Count != 0)
        {
            var propertiesToAdd = properties
                    .Select(x =>
                    {
                        var propertySchema = x.Value;
                        var className = $"{model.Name}_{x.Key.CleanupSymbolName()}";
                        var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(propertySchema.Reference?.Id);
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
                    .ToArray();
            if (propertiesToAdd.Length != 0)
                model.AddProperty(propertiesToAdd);
        }
    }
    private Dictionary<string, OpenApiSchema> CollectAllProperties(OpenApiSchema schema)
    {
        Dictionary<string, OpenApiSchema> result = schema.Properties?.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
        if (schema.AllOf?.Any() ?? false)
        {
            foreach (var supProperty in schema.AllOf.Where(static x => x.IsObject() && !x.IsReferencedSchema() && x.Properties is not null).SelectMany(static x => x.Properties))
            {
                result.Add(supProperty.Key, supProperty.Value);
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
        var parameters = node.PathItems[Constants.DefaultOpenApiLabel].Parameters.Union(operation.Parameters).Where(static p => p.In == ParameterLocation.Query).ToArray();
        if (parameters.Length != 0)
        {
            var parameterClass = parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType}QueryParameters",
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
    private void AddPropertyForQueryParameter(OpenApiUrlTreeNode node, OperationType operationType, OpenApiParameter parameter, CodeClass parameterClass)
    {
        CodeType? resultType = default;
        var addBackwardCompatibleParameter = false;
        if (parameter.Schema.IsEnum())
        {
            var schema = parameter.Schema;
            var codeNamespace = schema.IsReferencedSchema() switch
            {
                true => GetShortestNamespace(parameterClass.GetImmediateParentOfType<CodeNamespace>(), schema), // referenced schema
                false => parameterClass.GetImmediateParentOfType<CodeNamespace>(), // Inline schema, i.e. specific to the Operation
            };
            var shortestNamespace = GetShortestNamespace(codeNamespace, schema);
            var enumName = schema.GetSchemaName().CleanupSymbolName();
            if (string.IsNullOrEmpty(enumName))
                enumName = $"{operationType.ToString().ToFirstCharacterUpperCase()}{parameter.Name.CleanupSymbolName().ToFirstCharacterUpperCase()}QueryParameterType";
            if (AddEnumDeclarationIfDoesntExist(node, schema, enumName, shortestNamespace) is { } enumDeclaration)
            {
                resultType = new CodeType
                {
                    TypeDefinition = enumDeclaration,
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
    private static CodeType GetQueryParameterType(OpenApiSchema schema) =>
        new()
        {
            IsExternal = true,
            Name = schema.Items?.Type ?? schema.Type,
            CollectionKind = schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default,
        };

    private void CleanUpInternalState()
    {
        foreach (var lifecycle in classLifecycles.Values)
            lifecycle.Dispose();
        classLifecycles.Clear();
        multipartPropertiesModels.Clear();
    }
}
