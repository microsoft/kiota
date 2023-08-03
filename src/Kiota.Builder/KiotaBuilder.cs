using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Globbing;
using Kiota.Builder.Caching;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;
using Kiota.Builder.Exceptions;
using Kiota.Builder.Extensions;
using Kiota.Builder.Lock;
using Kiota.Builder.Logging;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Validation;
using Kiota.Builder.Writers;

using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Validations;
using HttpMethod = Kiota.Builder.CodeDOM.HttpMethod;

namespace Kiota.Builder;

public partial class KiotaBuilder
{
    private readonly ILogger<KiotaBuilder> logger;
    private readonly GenerationConfiguration config;
    private readonly ParallelOptions parallelOptions;
    private readonly HttpClient httpClient;
    private OpenApiDocument? originalDocument;
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
    public OpenApiDocument? OriginalOpenApiDocument => originalDocument;
    private async Task<(int, OpenApiUrlTreeNode?, bool)> GetTreeNodeInternal(string inputPath, bool generating, Stopwatch sw, CancellationToken cancellationToken)
    {
        logger.LogDebug("kiota version {Version}", Generated.KiotaVersion.Current());
        var stepId = 0;
        sw.Start();
#pragma warning disable CA2007
        await using var input = await (originalDocument == null ?
                                        LoadStream(inputPath, cancellationToken).ConfigureAwait(false) :
                                        Task.FromResult<Stream>(new MemoryStream()).ConfigureAwait(false));
#pragma warning restore CA2007
        if (input.Length == 0)
            return (0, null, false);
        StopLogAndReset(sw, $"step {++stepId} - reading the stream - took");

        // Parse OpenAPI
        sw.Start();
        if (originalDocument == null)
        {
            openApiDocument = await CreateOpenApiDocumentAsync(input, generating, cancellationToken).ConfigureAwait(false);
            if (openApiDocument != null)
                originalDocument = new OpenApiDocument(openApiDocument);
        }
        else
            openApiDocument = new OpenApiDocument(originalDocument);
        StopLogAndReset(sw, $"step {++stepId} - parsing the document - took");

        sw.Start();
        UpdateConfigurationFromOpenApiDocument();
        StopLogAndReset(sw, $"step {++stepId} - updating generation configuration from kiota extension - took");

        // Should Generate
        sw.Start();
        var shouldGenerate = await ShouldGenerate(cancellationToken).ConfigureAwait(false);
        StopLogAndReset(sw, $"step {++stepId} - checking whether the output should be updated - took");

        OpenApiUrlTreeNode? openApiTree = null;
        if (openApiDocument != null && (shouldGenerate || !generating))
        {

            // filter paths
            sw.Start();
            FilterPathsByPatterns(openApiDocument);
            StopLogAndReset(sw, $"step {++stepId} - filtering API paths with patterns - took");

            SetApiRootUrl();

            modelNamespacePrefixToTrim = GetDeeperMostCommonNamespaceNameForModels(openApiDocument);

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
    [GeneratedRegex(@"([\/\\])\{[\w\d-]+\}([\/\\])", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, 2000)]
    private static partial Regex MultiIndexSameLevelCleanupRegexTemplate();
    private static readonly Regex MultiIndexSameLevelCleanupRegex = MultiIndexSameLevelCleanupRegexTemplate();
    private static string ReplaceAllIndexesWithWildcard(string path, uint depth = 10) => depth == 0 ? path : ReplaceAllIndexesWithWildcard(MultiIndexSameLevelCleanupRegex.Replace(path, "$1{*}$2"), depth - 1); // the bound needs to be greedy to avoid replacing anything else than single path parameters
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
        if (!includePatterns.Any() && !excludePatterns.Any()) return;

        var nonOperationIncludePatterns = includePatterns.Where(static x => !x.Value.Any()).Select(static x => x.Key).ToList();
        var nonOperationExcludePatterns = excludePatterns.Where(static x => !x.Value.Any()).Select(static x => x.Key).ToList();
        var operationIncludePatterns = includePatterns.Where(static x => x.Value.Any()).ToList();

        if (nonOperationIncludePatterns.Any() || nonOperationExcludePatterns.Any())
            doc.Paths.Keys.Where(x => (nonOperationIncludePatterns.Any() && !nonOperationIncludePatterns.Any(y => y.IsMatch(x)) ||
                                nonOperationExcludePatterns.Any() && nonOperationExcludePatterns.Any(y => y.IsMatch(x))) &&
                                !operationIncludePatterns.Any(y => y.Key.IsMatch(x))) // so we don't trim paths that are going to be filtered by operation
            .ToList()
            .ForEach(x => doc.Paths.Remove(x));

        var operationExcludePatterns = excludePatterns.Where(static x => x.Value.Any()).ToList();

        if (operationIncludePatterns.Any() || operationExcludePatterns.Any())
        {
            foreach (var path in doc.Paths.Where(x => !nonOperationIncludePatterns.Any(y => y.IsMatch(x.Key))))
            {
                var pathString = path.Key;
                path.Value.Operations.Keys.Where(x => operationIncludePatterns.Any() && !operationIncludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x)) ||
                                        operationExcludePatterns.Any() && operationExcludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x)))
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
                var targetUri = new Uri(inputPath);
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
                input = new FileStream(inputPath, FileMode.Open);
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
            ExtensionParsers = new()
            {
                {
                    OpenApiPagingExtension.Name,
                    static (i, _) => OpenApiPagingExtension.Parse(i)
                },
                {
                    OpenApiEnumValuesDescriptionExtension.Name,
                    static (i, _ ) => OpenApiEnumValuesDescriptionExtension.Parse(i)
                },
                {
                    OpenApiKiotaExtension.Name,
                    static (i, _ ) => OpenApiKiotaExtension.Parse(i)
                },
                {
                    OpenApiDeprecationExtension.Name,
                    static (i, _ ) => OpenApiDeprecationExtension.Parse(i)
                }
            },
            RuleSet = ruleSet,
        };
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
        var longestKeySegments = longestKey?.Split(NsNameSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
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
        MergeIndexNodesAtSameLevel(node);
        stopwatch.Stop();
        logger.LogTrace("{Timestamp}ms: Created UriSpace tree", stopwatch.ElapsedMilliseconds);
        return node;
    }
    private void MergeIndexNodesAtSameLevel(OpenApiUrlTreeNode node)
    {
        var indexNodes = node.Children
                        .Where(static x => x.Value.IsPathSegmentWithSingleSimpleParameter())
                        .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
        if (indexNodes.Length > 1)
        {
            var indexNode = indexNodes[0];
            foreach (var child in indexNodes.Except(new[] { indexNode }))
            {
                node.Children.Remove(child.Key);
                CopyNodeIntoOtherNode(child.Value, indexNode.Value, child.Key, indexNode.Key);
            }
        }

        foreach (var child in node.Children.Values)
            MergeIndexNodesAtSameLevel(child);
    }
    private void CopyNodeIntoOtherNode(OpenApiUrlTreeNode source, OpenApiUrlTreeNode destination, string pathParameterNameToReplace, string pathParameterNameReplacement)
    {
        foreach (var child in source.Children)
        {
            child.Value.Path = child.Value.Path.Replace(pathParameterNameToReplace, pathParameterNameReplacement, StringComparison.OrdinalIgnoreCase);
            if (!destination.Children.TryAdd(child.Key, child.Value))
                CopyNodeIntoOtherNode(child.Value, destination.Children[child.Key], pathParameterNameToReplace, pathParameterNameReplacement);
        }
        pathParameterNameToReplace = pathParameterNameToReplace.TrimStart('{').TrimEnd('}');
        pathParameterNameReplacement = pathParameterNameReplacement.TrimStart('{').TrimEnd('}');
        foreach (var pathItem in source.PathItems)
        {
            foreach (var pathParameter in pathItem
                                        .Value
                                        .Parameters
                                        .Where(x => x.In == ParameterLocation.Path && pathParameterNameToReplace.Equals(x.Name, StringComparison.Ordinal))
                                        .Union(
                                            pathItem
                                                .Value
                                                .Operations
                                                .SelectMany(static x => x.Value.Parameters)
                                                .Where(x => x.In == ParameterLocation.Path && pathParameterNameToReplace.Equals(x.Name, StringComparison.Ordinal))
                                        ))
            {
                pathParameter.Name = pathParameterNameReplacement;
            }
            if (!destination.PathItems.TryAdd(pathItem.Key, pathItem.Value))
            {
                var destinationPathItem = destination.PathItems[pathItem.Key];
                foreach (var operation in pathItem.Value.Operations)
                    if (!destinationPathItem.Operations.TryAdd(operation.Key, operation.Value))
                    {
                        logger.LogWarning("Duplicate operation {Operation} in path {Path}", operation.Key, pathItem.Key);
                    }
                foreach (var pathParameter in pathItem.Value.Parameters)
                    destinationPathItem.Parameters.Add(pathParameter);
                foreach (var extension in pathItem.Value.Extensions)
                    if (!destinationPathItem.Extensions.TryAdd(extension.Key, extension.Value))
                    {
                        logger.LogWarning("Duplicate extension {Extension} in path {Path}", extension.Key, pathItem.Key);
                    }
            }
        }
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
        var languageWriter = LanguageWriter.GetLanguageWriter(language, config.OutputPath, config.ClientNamespaceName, config.UsesBackingStore);
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
                    Description = "The main entry point of the SDK, exposes the configuration and the fluent API."
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
                    Description = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
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
                codeClass.Indexer = CreateIndexer($"{propIdentifier}-indexer", propType, child.Value, currentNode);
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
                    prop.Documentation.Description = description;
                }
                codeClass.AddProperty(prop);
            }
        }

        // Add methods for Operations
        if (currentNode.HasOperations(Constants.DefaultOpenApiLabel))
        {
            foreach (var operation in currentNode
                                    .PathItems[Constants.DefaultOpenApiLabel]
                                    .Operations)
                CreateOperationMethods(currentNode, operation.Key, operation.Value, codeClass);
        }
        CreateUrlManagement(codeClass, currentNode, isApiClientClass);

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
    private static void CreateMethod(string propIdentifier, string propType, CodeClass codeClass, OpenApiUrlTreeNode currentNode)
    {
        var methodToAdd = new CodeMethod
        {
            Name = propIdentifier.CleanupSymbolName(),
            Kind = CodeMethodKind.RequestBuilderWithParameters,
            Documentation = new()
            {
                Description = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
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
                    Description = parameter.Description.CleanupDescription(),
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
                Description = "Url template to use to build the URL for the current request builder",
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
                Description = "The request adapter to use to execute the requests.",
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
            Documentation = new()
            {
                Description = $"Instantiates a new {currentClass.Name.ToFirstCharacterUpperCase()} and sets the default values.",
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
                Description = "Path parameters for the request",
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
            constructor.SerializerModules = config.Serializers;
            constructor.DeserializerModules = config.Deserializers;
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
                    Description = "The backing store to use for the models.",
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
    private CodeIndexer CreateIndexer(string childIdentifier, string childType, OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode parentNode)
    {
        logger.LogTrace("Creating indexer {Name}", childIdentifier);
        return new CodeIndexer
        {
            Name = childIdentifier,
            Documentation = new()
            {
                Description = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Gets an item from the {currentNode.GetNodeNamespaceFromPath(config.ClientNamespaceName)} collection"),
            },
            IndexType = new CodeType { Name = "string", IsExternal = true, },
            ReturnType = new CodeType { Name = childType },
            SerializationName = currentNode.Segment.SanitizeParameterNameForUrlTemplate(),
            PathSegment = parentNode.GetNodeNamespaceFromPath(string.Empty).Split('.').Last(),
            IndexParameterName = currentNode.Segment.CleanupSymbolName(),
            Deprecation = currentNode.GetDeprecationInformation(),
        };
    }

    private CodeProperty? CreateProperty(string childIdentifier, string childType, OpenApiSchema? propertySchema = null, CodeTypeBase? existingType = null, CodePropertyKind kind = CodePropertyKind.Custom)
    {
        var propertyName = childIdentifier.CleanupSymbolName();
        var resultType = existingType ?? GetPrimitiveType(propertySchema, childType);
        if (resultType == null) return null;
        var prop = new CodeProperty
        {
            Name = propertyName,
            Kind = kind,
            Documentation = new()
            {
                Description = propertySchema?.Description.CleanupDescription() is string description && !string.IsNullOrEmpty(description) ?
                    description :
                    $"The {propertyName} property",
            },
            ReadOnly = propertySchema?.ReadOnly ?? false,
            Type = resultType,
            Deprecation = propertySchema?.GetDeprecationInformation(),
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
    private static readonly HashSet<string> noContentStatusCodes = new() { "201", "202", "204", "205" };
    private static readonly HashSet<string> errorStatusCodes = new(Enumerable.Range(400, 599).Select(static x => x.ToString(CultureInfo.InvariantCulture))
                                                                                 .Concat(new[] { "4XX", "5XX" }), StringComparer.OrdinalIgnoreCase);

    private void AddErrorMappingsForExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod)
    {
        foreach (var response in operation.Responses.Where(x => errorStatusCodes.Contains(x.Key)))
        {
            var errorCode = response.Key.ToUpperInvariant();
            var errorSchema = response.Value.GetResponseSchema(config.StructuredMimeTypes);
            if (errorSchema != null && modelsNamespace != null)
            {
                var parentElement = string.IsNullOrEmpty(response.Value.Reference?.Id) && string.IsNullOrEmpty(errorSchema.Reference?.Id)
                    ? (CodeElement)executorMethod
                    : modelsNamespace;
                var errorType = CreateModelDeclarations(currentNode, errorSchema, operation, parentElement, $"{errorCode}Error", response: response.Value);
                if (errorType is CodeType codeType &&
                    codeType.TypeDefinition is CodeClass codeClass &&
                    !codeClass.IsErrorDefinition)
                {
                    codeClass.IsErrorDefinition = true;
                }
                if (errorType is null)
                    logger.LogWarning("Could not create error type for {Error} in {Operation}", errorCode, operation.OperationId);
                else
                    executorMethod.AddErrorMapping(errorCode, errorType);
            }
        }
    }
    private CodeTypeBase? GetExecutorMethodReturnType(OpenApiUrlTreeNode currentNode, OpenApiSchema? schema, OpenApiOperation operation, CodeClass parentClass)
    {
        if (schema != null)
        {
            return CreateModelDeclarations(currentNode, schema, operation, parentClass, "Response");
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
            return new CodeType { Name = returnType, IsExternal = true, };
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
                    Description = "Configuration for the request such as headers, query parameters, and middleware options.",
                },
            }).First();

            var schema = operation.GetResponseSchema(config.StructuredMimeTypes);
            var method = (HttpMethod)Enum.Parse(typeof(HttpMethod), operationType.ToString());
            var deprecationInformation = operation.GetDeprecationInformation();
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
                    Description = (operation.Description is string description && !string.IsNullOrEmpty(description) ?
                                    description :
                                    operation.Summary)
                                    .CleanupDescription(),
                },
                ReturnType = GetExecutorMethodReturnType(currentNode, schema, operation, parentClass) ?? throw new InvalidSchemaException(),
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

            var handlerParam = new CodeParameter
            {
                Name = "responseHandler",
                Optional = true,
                Kind = CodeParameterKind.ResponseHandler,
                Documentation = new()
                {
                    Description = "Response handler to use in place of the default response handling provided by the core service",
                },
                Type = new CodeType { Name = "IResponseHandler", IsExternal = true },
            };
            executorMethod.AddParameter(handlerParam);// Add response handler parameter

            var cancellationParam = new CodeParameter
            {
                Name = "cancellationToken",
                Optional = true,
                Kind = CodeParameterKind.Cancellation,
                Documentation = new()
                {
                    Description = "Cancellation token to use when cancelling requests",
                },
                Type = new CodeType { Name = "CancellationToken", IsExternal = true },
            };
            executorMethod.AddParameter(cancellationParam);// Add cancellation token parameter
            logger.LogTrace("Creating method {Name} of {Type}", executorMethod.Name, executorMethod.ReturnType);

            var generatorMethod = new CodeMethod
            {
                Name = $"To{operationType.ToString().ToFirstCharacterUpperCase()}RequestInformation",
                Kind = CodeMethodKind.RequestGenerator,
                IsAsync = false,
                HttpMethod = method,
                Documentation = new()
                {
                    Description = (operation.Description ?? operation.Summary).CleanupDescription(),
                },
                ReturnType = new CodeType { Name = "RequestInformation", IsNullable = false, IsExternal = true },
                Parent = parentClass,
                Deprecation = deprecationInformation,
            };
            if (schema != null)
            {
                var mediaType = operation.Responses.Values.SelectMany(static x => x.Content).First(x => x.Value.Schema == schema).Key;
                generatorMethod.AcceptedResponseTypes.Add(mediaType);
            }
            if (config.Language == GenerationLanguage.Shell)
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
    private static readonly Func<OpenApiParameter, CodeParameter> GetCodeParameterFromApiParameter = x =>
    {
        var codeName = x.Name.SanitizeParameterNameForCodeSymbols();
        return new CodeParameter
        {
            Name = codeName,
            SerializationName = codeName.Equals(x.Name, StringComparison.Ordinal) ? string.Empty : x.Name,
            Type = GetQueryParameterType(x.Schema),
            Documentation = new()
            {
                Description = x.Description.CleanupDescription(),
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
                    Description = "Request query parameters",
                },
                Type = new CodeType { Name = parameterClass.Name, TypeDefinition = parameterClass },
            });
        }
        requestConfigClass.AddProperty(new CodeProperty
        {
            Name = "headers",
            Kind = CodePropertyKind.Headers,
            Documentation = new()
            {
                Description = "Request headers",
            },
            Type = new CodeType { Name = "RequestHeaders", IsExternal = true },
        },
        new CodeProperty
        {
            Name = "options",
            Kind = CodePropertyKind.Options,
            Documentation = new()
            {
                Description = "Request options",
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
                                                                config.StructuredMimeTypes.Contains(x.Value.ContentType.Split(';', StringSplitOptions.RemoveEmptyEntries)[0])))
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
                    Description = requestBodySchema.Description.CleanupDescription() is string description && !string.IsNullOrEmpty(description) ?
                                    description :
                                    "The request body"
                },
                Deprecation = requestBodySchema.GetDeprecationInformation(),
            });
            method.RequestBodyContentType = operation.RequestBody.Content.First(x => x.Value.Schema == requestBodySchema).Key;
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
                    Description = "Binary request body",
                },
                Type = new CodeType
                {
                    Name = "binary",
                    IsExternal = true,
                    IsNullable = false,
                },
            };
            method.AddParameter(nParam);
        }
        method.AddParameter(new CodeParameter
        {
            Name = "requestConfiguration",
            Optional = true,
            Type = new CodeType { Name = requestConfigClass.Name, TypeDefinition = requestConfigClass, ActionOf = true },
            Kind = CodeParameterKind.RequestConfiguration,
            Documentation = new()
            {
                Description = "Configuration for the request such as headers, query parameters, and middleware options.",
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
        return $"{modelsNamespace?.Name}{namespaceSuffix}";
    }
    private CodeType CreateModelDeclarationAndType(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, CodeNamespace codeNamespace, string classNameSuffix = "", OpenApiResponse? response = default, string typeNameForInlineSchema = "", bool isRequestBody = false)
    {
        var className = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, response: response, schema: schema, requestBody: isRequestBody).CleanupSymbolName() : typeNameForInlineSchema;
        var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, schema, className, codeNamespace);
        return new CodeType
        {
            TypeDefinition = codeDeclaration,
            Name = className,
        };
    }
    private CodeTypeBase CreateInheritedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string classNameSuffix, CodeNamespace codeNamespace, bool isRequestBody)
    {
        var allOfs = schema.AllOf.FlattenEmptyEntries(static x => x.AllOf);
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
            string.IsNullOrEmpty(currentClass.Documentation.Description) &&
            string.IsNullOrEmpty(schema.AllOf.LastOrDefault()?.Description) &&
            !string.IsNullOrEmpty(schema.Description))
            currentClass.Documentation.Description = schema.Description.CleanupDescription(); // the last allof entry often is not a reference and doesn't have a description.

        return new CodeType
        {
            TypeDefinition = codeDeclaration,
            Name = className,
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
    private CodeTypeBase CreateComposedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation? operation, string suffixForInlineSchema, CodeNamespace codeNamespace, bool isRequestBody)
    {
        var typeName = currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: suffixForInlineSchema, schema: schema, requestBody: isRequestBody).CleanupSymbolName();
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
                    Name = className,
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
                    unionType.AddType(primitiveType);
                    continue;
                }
                else
                    className = $"{unionType.Name}Member{++membersWithNoName}";
            var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, currentSchema, className, shortestNamespace);
            unionType.AddType(new CodeType
            {
                TypeDefinition = codeDeclaration,
                Name = className,
            });
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
            return CreateComposedModelDeclaration(currentNode, schema, operation, suffix, codeNamespace, isRequestBody);
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
        if (schema.AnyOf.Any() || schema.OneOf.Any() || schema.AllOf.Any()) // we have an empty node because of some local override for schema properties and need to unwrap it.
            return CreateModelDeclarations(currentNode, (schema.AnyOf.FirstOrDefault() ?? schema.OneOf.FirstOrDefault() ?? schema.AllOf.FirstOrDefault())!, operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema, isRequestBody);
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
            if (schema.IsEnum())
            {
                var schemaDescription = schema.Description.CleanupDescription();
                var newEnum = new CodeEnum
                {
                    Name = declarationName,//TODO set the flag property
                    Documentation = new()
                    {
                        Description = !string.IsNullOrEmpty(schemaDescription) || !string.IsNullOrEmpty(schema.Reference?.Id) ?
                                            schemaDescription : // if it's a referenced component, we shouldn't use the path item description as it makes it indeterministic
                                            currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel),
                    },
                    Deprecation = schema.GetDeprecationInformation(),
                };
                SetEnumOptions(schema, newEnum);
                return currentNamespace.AddEnum(newEnum).First();
            }

            return AddModelClass(currentNode, schema, declarationName, currentNamespace, inheritsFrom);
        }
        return existingDeclaration;
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
                                    Description = optionDescription?.Description ?? string.Empty,
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
                Description = (string.IsNullOrEmpty(schema.Description) ? schema.AllOf?.FirstOrDefault(static x => !x.IsReferencedSchema() && !string.IsNullOrEmpty(x.Description))?.Description : schema.Description).CleanupDescription(),
            },
            Deprecation = schema.GetDeprecationInformation(),
        };
        if (inheritsFrom != null)
            newClassStub.StartBlock.Inherits = new CodeType { TypeDefinition = inheritsFrom, Name = inheritsFrom.Name };

        // Add the class to the namespace after the serialization members
        // as other threads looking for the existence of the class may find the class but the additional data/backing store properties may not be fully populated causing duplication
        var includeAdditionalDataProperties = config.IncludeAdditionalData && schema.AdditionalPropertiesAllowed;
        AddSerializationMembers(newClassStub, includeAdditionalDataProperties, config.UsesBackingStore);

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

        AddDiscriminatorMethod(newClass, schema.GetDiscriminatorPropertyName(), mappings);
        return newClass;
    }
    private IEnumerable<KeyValuePair<string, CodeTypeBase>> GetDiscriminatorMappings(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace currentNamespace, CodeClass? baseClass)
    {
        return schema.GetDiscriminatorMappings(inheritanceIndex)
                .Select(x => KeyValuePair.Create(x.Key, GetCodeTypeForMapping(currentNode, x.Value, currentNamespace, baseClass, schema)))
                .Where(static x => x.Value != null)
                .Select(static x => KeyValuePair.Create(x.Key, x.Value!));
    }
    private static IEnumerable<CodeElement> GetAllModels(CodeNamespace currentNamespace)
    {
        var classes = currentNamespace.Classes.ToArray();
        return classes.Union(classes.SelectMany(GetAllInnerClasses))
                            .Where(static x => x.IsOfKind(CodeClassKind.Model))
                            .OfType<CodeElement>()
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
        var relatedModels = classesInUse.SelectMany(x => GetRelatedDefinitions(x, reusableClassesDerivationIndex, reusableClassesInheritanceIndex)).Union(modelsDirectlyInUse.Where(x => x is CodeEnum)).ToHashSet();// re-including models directly in use for enums
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
    private ConcurrentDictionary<CodeClass, List<CodeClass>> GetDerivationIndex(IEnumerable<CodeClass> models)
    {
        var result = new ConcurrentDictionary<CodeClass, List<CodeClass>>();
        Parallel.ForEach(models, parallelOptions, x =>
        {
            if (x.BaseClass is CodeClass parentClass && !result.TryAdd(parentClass, new() { x }))
                result[parentClass].Add(x);
        });
        return result;
    }
    private ConcurrentDictionary<CodeClass, List<CodeClass>> GetInheritanceIndex(ConcurrentDictionary<CodeClass, List<CodeClass>> derivedIndex)
    {
        var result = new ConcurrentDictionary<CodeClass, List<CodeClass>>();
        Parallel.ForEach(derivedIndex, parallelOptions, entry =>
        {
            foreach (var derivedClass in entry.Value)
                if (!result.TryAdd(derivedClass, new() { entry.Key }))
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
    private static IEnumerable<CodeClass> GetDerivedDefinitions(ConcurrentDictionary<CodeClass, List<CodeClass>> models, CodeClass[] modelsInUse)
    {
        var currentDerived = modelsInUse.SelectMany(x => models.TryGetValue(x, out var res) ? res : Enumerable.Empty<CodeClass>()).ToArray();
        return currentDerived.Union(currentDerived.SelectMany(x => GetDerivedDefinitions(models, new CodeClass[] { x })));
    }
    private static IEnumerable<CodeElement> GetRelatedDefinitions(CodeElement currentElement, ConcurrentDictionary<CodeClass, List<CodeClass>> derivedIndex, ConcurrentDictionary<CodeClass, List<CodeClass>> inheritanceIndex, ConcurrentDictionary<CodeElement, bool>? visited = null)
    {
        visited ??= new();
        if (currentElement is not CodeClass currentClass || !visited.TryAdd(currentClass, true)) return Enumerable.Empty<CodeElement>();
        var propertiesDefinitions = currentClass.Properties
                            .SelectMany(static x => x.Type.AllTypes)
                            .Select(static x => x.TypeDefinition!)
                            .Where(static x => x is CodeClass || x is CodeEnum)
                            .SelectMany(x => x is CodeClass classDefinition ?
                                            (inheritanceIndex.TryGetValue(classDefinition, out var res) ? res : Enumerable.Empty<CodeClass>())
                                                .Union(GetDerivedDefinitions(derivedIndex, new CodeClass[] { classDefinition }))
                                                .Union(new[] { classDefinition })
                                                .OfType<CodeElement>() :
                                            new[] { x })
                            .Distinct()
                            .ToArray();
        var propertiesParentTypes = propertiesDefinitions.OfType<CodeClass>().SelectMany(static x => x.GetInheritanceTree(false, false)).ToArray();
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
    internal static void AddDiscriminatorMethod(CodeClass newClass, string discriminatorPropertyName, IEnumerable<KeyValuePair<string, CodeTypeBase>> discriminatorMappings)
    {
        var factoryMethod = new CodeMethod
        {
            Name = "CreateFromDiscriminatorValue",
            Documentation = new()
            {
                Description = "Creates a new instance of the appropriate class based on discriminator value",
            },
            ReturnType = new CodeType { TypeDefinition = newClass, Name = newClass.Name, IsNullable = false },
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
                Description = "The parse node to use to read the discriminator value and create the object",
            },
            Optional = false,
            Type = new CodeType { Name = ParseNodeInterface, IsExternal = true },
        });
        newClass.DiscriminatorInformation.DiscriminatorPropertyName = discriminatorPropertyName;
        newClass.AddMethod(factoryMethod);
    }
    private CodeTypeBase? GetCodeTypeForMapping(OpenApiUrlTreeNode currentNode, string referenceId, CodeNamespace currentNamespace, CodeClass? baseClass, OpenApiSchema currentSchema)
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
            Name = codeClass.Name,
            TypeDefinition = codeClass,
        };
    }
    private void CreatePropertiesForModelClass(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace ns, CodeClass model)
    {
        if (CollectAllProperties(schema) is var properties && properties.Any())
        {
            model.AddProperty(properties
                                .Select(x =>
                                {
                                    var propertySchema = x.Value;
                                    var className = propertySchema.GetSchemaName().CleanupSymbolName();
                                    if (string.IsNullOrEmpty(className))
                                        className = $"{model.Name}_{x.Key.CleanupSymbolName()}";
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
                                .ToArray());
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
    internal static void AddSerializationMembers(CodeClass model, bool includeAdditionalProperties, bool usesBackingStore)
    {
        var serializationPropsType = $"IDictionary<string, Action<{ParseNodeInterface}>>";
        if (!model.ContainsMember(FieldDeserializersMethodName))
        {
            var deserializeProp = new CodeMethod
            {
                Name = FieldDeserializersMethodName,
                Kind = CodeMethodKind.Deserializer,
                Access = AccessModifier.Public,
                Documentation = new()
                {
                    Description = "The deserialization information for the current model",
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
                Name = SerializeMethodName,
                Kind = CodeMethodKind.Serializer,
                IsAsync = false,
                Documentation = new()
                {
                    Description = "Serializes information the current object",
                },
                ReturnType = new CodeType { Name = VoidType, IsNullable = false, IsExternal = true },
                Parent = model,
            };
            var parameter = new CodeParameter
            {
                Name = "writer",
                Documentation = new()
                {
                    Description = "Serialization writer to use to serialize this model",
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
                    Description = "Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.",
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
                    Description = "Stores model information.",
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
        if (parameters.Any())
        {
            var parameterClass = parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType}QueryParameters",
                Kind = CodeClassKind.QueryParameters,
                Documentation = new()
                {
                    Description = (operation.Description is string description && !string.IsNullOrEmpty(description) ?
                                    description :
                                    operation.Summary).CleanupDescription(),
                },
            }).First();
            foreach (var parameter in parameters)
                AddPropertyForQueryParameter(parameter, parameterClass);

            return parameterClass;
        }

        return null;
    }
    private void AddPropertyForQueryParameter(OpenApiParameter parameter, CodeClass parameterClass)
    {
        var resultType = GetPrimitiveType(parameter.Schema) ?? new CodeType()
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
                Description = parameter.Description.CleanupDescription(),
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
            parameterClass.AddProperty(prop);
        }
        else
        {
            logger.LogWarning("Ignoring duplicate parameter {Name}", parameter.Name);
        }
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
