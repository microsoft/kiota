using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
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

public class KiotaBuilder
{
    private readonly ILogger<KiotaBuilder> logger;
    private readonly GenerationConfiguration config;
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
        this.httpClient = client;
    }
    private async Task CleanOutputDirectory(CancellationToken cancellationToken)
    {
        if (config.CleanOutput && Directory.Exists(config.OutputPath))
        {
            logger.LogInformation("Cleaning output directory {path}", config.OutputPath);
            // not using Directory.Delete on the main directory because it's locked when mapped in a container
            foreach (var subDir in Directory.EnumerateDirectories(config.OutputPath))
                Directory.Delete(subDir, true);
            await lockManagementService.BackupLockFileAsync(config.OutputPath, cancellationToken);
            foreach (var subFile in Directory.EnumerateFiles(config.OutputPath))
                File.Delete(subFile);
        }
    }
    public async Task<OpenApiUrlTreeNode?> GetUrlTreeNodeAsync(CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        string inputPath = config.OpenAPIFilePath;
        var (_, openApiTree, _) = await GetTreeNodeInternal(inputPath, false, sw, cancellationToken);
        return openApiTree;
    }
    private async Task<(int, OpenApiUrlTreeNode?, bool)> GetTreeNodeInternal(string inputPath, bool generating, Stopwatch sw, CancellationToken cancellationToken)
    {
        logger.LogDebug("kiota version {version}", Kiota.Generated.KiotaVersion.Current());
        var stepId = 0;
        sw.Start();
        await using var input = await (originalDocument == null ?
                                        LoadStream(inputPath, cancellationToken) :
                                        Task.FromResult<Stream>(new MemoryStream()));
        if (input.Length == 0)
            return (0, null, false);
        StopLogAndReset(sw, $"step {++stepId} - reading the stream - took");

        // Parse OpenAPI
        sw.Start();
        if (originalDocument == null)
        {
            openApiDocument = await CreateOpenApiDocumentAsync(input, generating, cancellationToken);
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
        var shouldGenerate = await ShouldGenerate(cancellationToken);
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
        var existingLock = await lockManagementService.GetLockFromDirectoryAsync(config.OutputPath, cancellationToken);
        var configurationLock = new KiotaLock(config)
        {
            DescriptionHash = openApiDocument?.HashCode ?? string.Empty,
        };
        var comparer = new KiotaLockComparer();
        return !comparer.Equals(existingLock, configurationLock);
    }

    public async Task<LanguagesInformation?> GetLanguagesInformationAsync(CancellationToken cancellationToken)
    {
        await GetTreeNodeInternal(config.OpenAPIFilePath, false, new Stopwatch(), cancellationToken);

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
            await CleanOutputDirectory(cancellationToken);
            // doing this verification at the beginning to give immediate feedback to the user
            Directory.CreateDirectory(config.OutputPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not open/create output directory {config.OutputPath}, reason: {ex.Message}", ex);
        }
        try
        {
            var (stepId, openApiTree, shouldGenerate) = await GetTreeNodeInternal(inputPath, true, sw, cancellationToken);

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
            await ApplyLanguageRefinement(config, generatedCode, cancellationToken);
            StopLogAndReset(sw, $"step {++stepId} - refine by language - took");

            // Write language source
            sw.Start();
            await CreateLanguageSourceFilesAsync(config.Language, generatedCode, cancellationToken);
            StopLogAndReset(sw, $"step {++stepId} - writing files - took");

            // Write lock file
            sw.Start();
            await UpdateLockFile(cancellationToken);
            StopLogAndReset(sw, $"step {++stepId} - writing lock file - took");
        }
        catch
        {
            await lockManagementService.RestoreLockFileAsync(config.OutputPath, cancellationToken);
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
        await lockManagementService.WriteLockFileAsync(config.OutputPath, configurationLock, cancellationToken);
    }
    private static readonly GlobComparer globComparer = new();
    private static Dictionary<Glob, HashSet<OperationType>> GetFilterPatternsFromConfiguration(HashSet<string> configPatterns)
    {
        return configPatterns.Select(static x =>
        {
            var splat = x.Split('#', StringSplitOptions.RemoveEmptyEntries);
            var glob = Glob.Parse(splat[0]);
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

        if (nonOperationIncludePatterns.Any() || nonOperationExcludePatterns.Any())
            doc.Paths.Keys.Where(x => (nonOperationIncludePatterns.Any() && !nonOperationIncludePatterns.Any(y => y.IsMatch(x))) ||
                                (nonOperationExcludePatterns.Any() && nonOperationExcludePatterns.Any(y => y.IsMatch(x))))
            .ToList()
            .ForEach(x => doc.Paths.Remove(x));

        var operationIncludePatterns = includePatterns.Where(static x => x.Value.Any()).ToList();
        var operationExcludePatterns = excludePatterns.Where(static x => x.Value.Any()).ToList();

        if (operationIncludePatterns.Any() || operationExcludePatterns.Any())
        {
            foreach (var path in doc.Paths)
            {
                var pathString = path.Key;
                path.Value.Operations.Keys.Where(x => (operationIncludePatterns.Any() && !operationIncludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x))) ||
                                        (operationExcludePatterns.Any() && operationExcludePatterns.Any(y => y.Key.IsMatch(pathString) && y.Value.Contains(x))))
                .ToList()
                .ForEach(x => path.Value.Operations.Remove(x));
            }
            foreach (var path in doc.Paths.Where(static x => !x.Value.Operations.Any()).ToList())
                doc.Paths.Remove(path.Key);
        }
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
            catch
            {
                logger.LogWarning("Could not resolve the server url from the OpenAPI document. The base url will need to be set when using the client.");
                return;
            }
        }
        config.ApiRootUrl = candidateUrl.TrimEnd(ForwardSlash);
    }
    private void StopLogAndReset(Stopwatch sw, string prefix)
    {
        sw.Stop();
        logger.LogDebug("{prefix} {swElapsed}", prefix, sw.Elapsed);
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
                input = await cachingProvider.GetDocumentAsync(targetUri, "generation", fileName, cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Could not download the file at {inputPath}, reason: {ex.Message}", ex);
            }
        else
            try
            {
                input = new FileStream(inputPath, FileMode.Open);
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
        logger.LogTrace("{timestamp}ms: Read OpenAPI file {file}", stopwatch.ElapsedMilliseconds, inputPath);
        return input;
    }

    private static readonly char ForwardSlash = '/';
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
        catch
        {
            // couldn't parse the URL, it's probably a local file
        }
        var reader = new OpenApiStreamReader(settings);
        var readResult = await reader.ReadAsync(input, cancellationToken);
        stopwatch.Stop();
        if (generating)
            foreach (var warning in readResult.OpenApiDiagnostic.Warnings)
                logger.LogWarning("OpenAPI warning: {pointer} - {warning}", warning.Pointer, warning.Message);
        if (readResult.OpenApiDiagnostic.Errors.Any())
        {
            logger.LogTrace("{timestamp}ms: Parsed OpenAPI with errors. {count} paths found.", stopwatch.ElapsedMilliseconds, readResult.OpenApiDocument?.Paths?.Count ?? 0);
            foreach (var parsingError in readResult.OpenApiDiagnostic.Errors)
            {
                logger.LogError("OpenAPI error: {pointer} - {message}", parsingError.Pointer, parsingError.Message);
            }
        }
        else
        {
            logger.LogTrace("{timestamp}ms: Parsed OpenAPI successfully. {count} paths found.", stopwatch.ElapsedMilliseconds, readResult.OpenApiDocument?.Paths?.Count ?? 0);
        }

        return readResult.OpenApiDocument;
    }
    public static string GetDeeperMostCommonNamespaceNameForModels(OpenApiDocument document)
    {
        if (!(document?.Components?.Schemas?.Any() ?? false)) return string.Empty;
        var distinctKeys = document.Components
                                .Schemas
                                .Keys
                                .Select(x => string.Join(nsNameSeparator, x.Split(nsNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                                .SkipLast(1)))
                                .Where(x => !string.IsNullOrEmpty(x))
                                .Distinct()
                                .OrderByDescending(x => x.Count(y => y == nsNameSeparator));
        if (!distinctKeys.Any()) return string.Empty;
        var longestKey = distinctKeys.FirstOrDefault();
        var candidate = string.Empty;
        var longestKeySegments = longestKey?.Split(nsNameSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
        foreach (var segment in longestKeySegments)
        {
            var testValue = (candidate + nsNameSeparator + segment).Trim(nsNameSeparator);
            if (distinctKeys.All(x => x.StartsWith(testValue, StringComparison.OrdinalIgnoreCase)))
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
        logger.LogTrace("{timestamp}ms: Created UriSpace tree", stopwatch.ElapsedMilliseconds);
        return node;
    }
    private void MergeIndexNodesAtSameLevel(OpenApiUrlTreeNode node)
    {
        var indexNodes = node.Children.Where(static x => x.Value.IsPathSegmentWithSingleSimpleParameter());
        if (indexNodes.Count() > 1)
        {
            var indexNode = indexNodes.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase).First();
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
                        logger.LogWarning("Duplicate operation {operation} in path {path}", operation.Key, pathItem.Key);
                    }
                foreach (var pathParameter in pathItem.Value.Parameters)
                    destinationPathItem.Parameters.Add(pathParameter);
                foreach (var extension in pathItem.Value.Extensions)
                    if (!destinationPathItem.Extensions.TryAdd(extension.Key, extension.Value))
                    {
                        logger.LogWarning("Duplicate extension {extension} in path {path}", extension.Key, pathItem.Key);
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

            logger.LogTrace("{timestamp}ms: Created source model with {count} classes", stopwatch.ElapsedMilliseconds, codeNamespace.GetChildElements(true).Count());
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

        await ILanguageRefiner.Refine(config, generatedCode, token);

        stopwatch.Stop();
        logger.LogDebug("{timestamp}ms: Language refinement applied", stopwatch.ElapsedMilliseconds);
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
        await codeRenderer.RenderCodeNamespaceToFilePerClassAsync(languageWriter, generatedCode, cancellationToken);
        stopwatch.Stop();
        logger.LogTrace("{timestamp}ms: Files written to {path}", stopwatch.ElapsedMilliseconds, config.OutputPath);
    }
    private static readonly string requestBuilderSuffix = "RequestBuilder";
    private static readonly string itemRequestBuilderSuffix = "ItemRequestBuilder";
    private static readonly string voidType = "void";
    private static readonly string coreInterfaceType = "IRequestAdapter";
    private static readonly string requestAdapterParameterName = "requestAdapter";
    private static readonly string constructorMethodName = "constructor";
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
            var className = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, itemRequestBuilderSuffix) : currentNode.GetNavigationPropertyName(config.StructuredMimeTypes, requestBuilderSuffix);
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

        logger.LogTrace("Creating class {class}", codeClass.Name);

        // Add properties for children
        foreach (var child in currentNode.Children)
        {
            var propIdentifier = child.Value.GetNavigationPropertyName(config.StructuredMimeTypes);
            var propType = child.Value.GetNavigationPropertyName(config.StructuredMimeTypes, child.Value.DoesNodeBelongToItemSubnamespace() ? itemRequestBuilderSuffix : requestBuilderSuffix);

            if (child.Value.IsPathSegmentWithSingleSimpleParameter())
                codeClass.Indexer = CreateIndexer($"{propIdentifier}-indexer", propType, child.Value, currentNode);
            else if (child.Value.IsComplexPathMultipleParameters())
                CreateMethod(propIdentifier, propType, codeClass, child.Value);
            else
            {
                var description = child.Value.GetPathItemDescription(Constants.DefaultOpenApiLabel).CleanupDescription();
                var prop = CreateProperty(propIdentifier, propType, kind: CodePropertyKind.RequestBuilder); // we should add the type definition here but we can't as it might not have been generated yet
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
            Parallel.ForEach(currentNode.Children.Values, childNode =>
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
            }
        };
        AddPathParametersToMethod(currentNode, methodToAdd, false);
        codeClass.AddMethod(methodToAdd);
    }
    private static void AddPathParametersToMethod(OpenApiUrlTreeNode currentNode, CodeMethod methodToAdd, bool asOptional)
    {
        foreach (var parameter in currentNode.GetPathParametersForCurrentSegment())
        {
            var codeName = parameter.Name.SanitizeParameterNameForCodeSymbols();
            var mParameter = new CodeParameter
            {
                Name = codeName,
                Optional = asOptional,
                Documentation = new()
                {
                    Description = parameter.Description.CleanupDescription(),
                },
                Kind = CodeParameterKind.Path,
                SerializationName = parameter.Name.Equals(codeName) ? string.Empty : parameter.Name.SanitizeParameterNameForUrlTemplate(),
                Type = GetPrimitiveType(parameter.Schema ?? parameter.Content.Values.FirstOrDefault()?.Schema)
            };
            mParameter.Type.CollectionKind = parameter.Schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default;
            // not using the content schema as RFC6570 will serialize arrays as CSVs and content expects a JSON array, we failsafe to opaque string, it could be improved by involving the serialization layers.
            methodToAdd.AddParameter(mParameter);
        }
    }
    private static readonly string PathParametersParameterName = "pathParameters";
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
            Name = requestAdapterParameterName,
            Documentation = new()
            {
                Description = "The request adapter to use to execute the requests.",
            },
            Kind = CodePropertyKind.RequestAdapter,
            Access = AccessModifier.Private,
            ReadOnly = true,
            Type = new CodeType
            {
                Name = coreInterfaceType,
                IsExternal = true,
                IsNullable = false,
            }
        };
        currentClass.AddProperty(requestAdapterProperty);
        var constructor = new CodeMethod
        {
            Name = constructorMethodName,
            Kind = isApiClientClass ? CodeMethodKind.ClientConstructor : CodeMethodKind.Constructor,
            IsAsync = false,
            IsStatic = false,
            Documentation = new()
            {
                Description = $"Instantiates a new {currentClass.Name.ToFirstCharacterUpperCase()} and sets the default values.",
            },
            Access = AccessModifier.Public,
            ReturnType = new CodeType { Name = voidType, IsExternal = true },
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
            Name = requestAdapterParameterName,
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
        var unmappedTypes = GetUnmappedTypeDefinitions(codeElement).Distinct();

        var unmappedTypesWithNoName = unmappedTypes.Where(x => string.IsNullOrEmpty(x.Name)).ToList();

        unmappedTypesWithNoName.ForEach(x =>
        {
            logger.LogWarning("Type with empty name and parent {ParentName}", x.Parent?.Name);
        });

        var unmappedTypesWithName = unmappedTypes.Except(unmappedTypesWithNoName);

        var unmappedRequestBuilderTypes = unmappedTypesWithName
                                .Where(x =>
                                x.Parent is CodeProperty property && property.IsOfKind(CodePropertyKind.RequestBuilder) ||
                                x.Parent is CodeIndexer ||
                                x.Parent is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestBuilderWithParameters))
                                .ToList();

        Parallel.ForEach(unmappedRequestBuilderTypes, x =>
        {
            var parentNS = x.Parent?.Parent?.Parent as CodeNamespace;
            x.TypeDefinition = parentNS?.FindChildrenByName<CodeClass>(x.Name).MinBy(shortestNamespaceOrder);
            // searching down first because most request builder properties on a request builder are just sub paths on the API
            if (x.TypeDefinition == null)
            {
                parentNS = parentNS?.Parent as CodeNamespace;
                x.TypeDefinition = (parentNS
                    ?.FindNamespaceByName($"{parentNS?.Name}.{x.Name.Substring(0, x.Name.Length - requestBuilderSuffix.Length).ToFirstCharacterLowerCase()}".TrimEnd(nsNameSeparator))
                    ?.FindChildrenByName<CodeClass>(x.Name))?.MinBy(shortestNamespaceOrder);
                // in case of the .item namespace, going to the parent and then down to the target by convention
                // this avoid getting the wrong request builder in case we have multiple request builders with the same name in the parent branch
                // in both cases we always take the uppermost item (smaller numbers of segments in the namespace name)
            }
        });

        Parallel.ForEach(unmappedTypesWithName.Where(x => x.TypeDefinition == null).GroupBy(x => x.Name), x =>
        {
            if (rootNamespace?.FindChildByName<ITypeDefinition>(x.First().Name) is CodeElement definition)
                foreach (var type in x)
                {
                    type.TypeDefinition = definition;
                    logger.LogWarning("Mapped type {typeName} for {ParentName} using the fallback approach.", type.Name, type.Parent?.Name);
                }
        });
    }
    private static readonly char nsNameSeparator = '.';
    private static IEnumerable<CodeType> filterUnmappedTypeDefinitions(IEnumerable<CodeTypeBase?> source) =>
    source.OfType<CodeType>()
            .Union(source
                    .OfType<CodeComposedTypeBase>()
                    .SelectMany(x => x.Types))
            .Where(x => !x.IsExternal && x.TypeDefinition == null);
    private IEnumerable<CodeType> GetUnmappedTypeDefinitions(CodeElement codeElement)
    {
        var childElementsUnmappedTypes = codeElement.GetChildElements(true).SelectMany(x => GetUnmappedTypeDefinitions(x));
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
        logger.LogTrace("Creating indexer {name}", childIdentifier);
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
        };
    }

    private CodeProperty CreateProperty(string childIdentifier, string childType, OpenApiSchema? propertySchema = null, CodeTypeBase? existingType = null, CodePropertyKind kind = CodePropertyKind.Custom)
    {
        var propertyName = childIdentifier.CleanupSymbolName();
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
            Type = existingType ?? GetPrimitiveType(propertySchema, childType),
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
            logger.LogTrace("Creating property {name} of {type}", prop.Name, prop.Type.Name);
        }
        return prop;
    }
    private static readonly HashSet<string> typeNamesToSkip = new(StringComparer.OrdinalIgnoreCase) { "object", "array" };
    private static CodeType GetPrimitiveType(OpenApiSchema? typeSchema, string? childType = default)
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
        return new CodeType
        {
            Name = typeName ?? string.Empty,
            IsExternal = isExternal,
        };
    }
    private const string RequestBodyPlainTextContentType = "text/plain";
    private static readonly HashSet<string> noContentStatusCodes = new() { "201", "202", "204", "205" };
    private static readonly HashSet<string> errorStatusCodes = new(Enumerable.Range(400, 599).Select(static x => x.ToString())
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
                    logger.LogWarning("Could not create error type for {error} in {operation}", errorCode, operation.OperationId);
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
                returnType = voidType;
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
            logger.LogTrace("Creating method {name} of {type}", executorMethod.Name, executorMethod.ReturnType);

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
            logger.LogTrace("Creating method {name} of {type}", generatorMethod.Name, generatorMethod.ReturnType);
        }
        catch (InvalidSchemaException ex)
        {
            logger.LogWarning(ex, "Could not create method for {operation} in {path} because the schema was invalid", operation.OperationId, currentNode.Path);
        }
    }
    private static readonly Func<OpenApiParameter, CodeParameter> GetCodeParameterFromApiParameter = x =>
    {
        var codeName = x.Name.SanitizeParameterNameForCodeSymbols();
        return new CodeParameter
        {
            Name = codeName,
            SerializationName = codeName.Equals(x.Name) ? string.Empty : x.Name,
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

    private void AddRequestBuilderMethodParameters(OpenApiUrlTreeNode currentNode, OperationType operationType, OpenApiOperation operation, CodeClass requestConfigClass, CodeMethod method)
    {
        if (operation.GetRequestSchema(config.StructuredMimeTypes) is OpenApiSchema requestBodySchema)
        {
            var requestBodyType = CreateModelDeclarations(currentNode, requestBodySchema, operation, method, $"{operationType}RequestBody", isRequestBody: true) ??
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
        referenceId = referenceId.Trim(nsNameSeparator);
        if (!string.IsNullOrEmpty(modelNamespacePrefixToTrim) && referenceId.StartsWith(modelNamespacePrefixToTrim, StringComparison.OrdinalIgnoreCase))
            referenceId = referenceId[modelNamespacePrefixToTrim.Length..];
        referenceId = referenceId.Trim(nsNameSeparator);
        var lastDotIndex = referenceId.LastIndexOf(nsNameSeparator);
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
        if ((typesCount == 1 && schema.Nullable && schema.IsAnyOf()) || // nullable on the root schema outside of anyOf
            typesCount == 2 && (schema.AnyOf?.Any(static x => // nullable on a schema in the anyOf
                                                        x.Nullable &&
                                                        !x.Properties.Any() &&
                                                        !x.IsOneOf() &&
                                                        !x.IsAnyOf() &&
                                                        !x.IsAllOf() &&
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
        var (unionType, schemas) = (schema.IsOneOf(), schema.IsAnyOf()) switch
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

        if (schema.IsAllOf())
        {
            return CreateInheritedModelDeclaration(currentNode, schema, operation, suffix, codeNamespace, isRequestBody);
        }

        if ((schema.IsAnyOf() || schema.IsOneOf()) && string.IsNullOrEmpty(schema.Format)
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
        bool isEnumOrComposedCollectionType = schema.Items.IsEnum() //the collection could be an enum type so override with strong type instead of string type.
                                    || (schema.Items.IsComposedEnum() && string.IsNullOrEmpty(schema.Items.Format));//the collection could be a composed type with an enum type so override with strong type instead of string type.
        if ((string.IsNullOrEmpty(type.Name)
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
        if (modelsNamespace != null && currentNode.DoesNodeBelongToItemSubnamespace() && !currentNamespace.Name.Contains(modelsNamespace.Name))
            return currentNamespace.EnsureItemNamespace();
        return currentNamespace;
    }
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
        var entries = schema.Enum.OfType<OpenApiString>().Where(static x => !x.Value.Equals("null", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(x.Value)).Select(static x => x.Value);
        foreach (var enumValue in entries)
        {
            var optionDescription = extensionInformation?.ValuesDescriptions.FirstOrDefault(x => x.Value.Equals(enumValue, StringComparison.OrdinalIgnoreCase));
            var newOption = new CodeEnumOption
            {
                Name = (optionDescription?.Name is string name && !string.IsNullOrEmpty(name) ?
                        name :
                        enumValue).CleanupSymbolName(),
                SerializationName = enumValue,
                Documentation = new()
                {
                    Description = optionDescription?.Description ?? string.Empty,
                },
            };
            if (!string.IsNullOrEmpty(newOption.Name))
                target.AddOption(newOption);
        }
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
                Description = schema.Description.CleanupDescription(),
            },
        };
        if (inheritsFrom != null)
            newClassStub.StartBlock.Inherits = new CodeType { TypeDefinition = inheritsFrom, Name = inheritsFrom.Name };
        
        // Add the class to the namespace after the serialization members
        // as other threads looking for the existence of the class may find the class but the additional data/backing store properties may not be fully populated causing duplication
        var includeAdditionalDataProperties = config.IncludeAdditionalData && schema.AdditionalPropertiesAllowed;
        AddSerializationMembers(newClassStub, includeAdditionalDataProperties, config.UsesBackingStore);

        var newClass = currentNamespace.AddClass(newClassStub).First();
        CreatePropertiesForModelClass(currentNode, schema, currentNamespace, newClass); // order matters since we might be recursively generating ancestors for discriminator mappings and duplicating additional data/backing store properties

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
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex = new();
    private void InitializeInheritanceIndex()
    {
        openApiDocument?.InitializeInheritanceIndex(inheritanceIndex);
    }
    public static void AddDiscriminatorMethod(CodeClass newClass, string discriminatorPropertyName, IEnumerable<KeyValuePair<string, CodeTypeBase>> discriminatorMappings)
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
        var componentKey = referenceId?.Replace("#/components/schemas/", string.Empty);
        if (openApiDocument == null || !openApiDocument.Components.Schemas.TryGetValue(componentKey, out var discriminatorSchema))
        {
            logger.LogWarning("Discriminator {componentKey} not found in the OpenAPI document.", componentKey);
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
        if (schema?.Properties?.Any() ?? false)
        {
            model.AddProperty(schema
                                .Properties
                                .Select(x =>
                                {
                                    var propertySchema = x.Value;
                                    var className = propertySchema.GetSchemaName().CleanupSymbolName();
                                    if (string.IsNullOrEmpty(className))
                                        className = $"{model.Name}_{x.Key.CleanupSymbolName()}";
                                    var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(propertySchema.Reference?.Id);
                                    var targetNamespace = string.IsNullOrEmpty(shortestNamespaceName) ? ns :
                                                        (rootNamespace?.FindOrAddNamespace(shortestNamespaceName) ?? ns);
                                    var definition = CreateModelDeclarations(currentNode, propertySchema, default, targetNamespace, string.Empty, typeNameForInlineSchema: className);
                                    if (definition == null)
                                    {
                                        logger.LogWarning("Omitted property {propertyName} for model {modelName} in API path {apiPath}, the schema is invalid.", x.Key, model.Name, currentNode.Path);
                                        return null;
                                    }
                                    return CreateProperty(x.Key, definition.Name, propertySchema: propertySchema, existingType: definition);
                                })
                                .Where(static x => x != null)
                                .Select(static x => x!)
                                .ToArray());
        }
        else if (schema?.AllOf?.Any(x => x.IsObject()) ?? false)
            CreatePropertiesForModelClass(currentNode, schema.AllOf.Last(x => x.IsObject()), ns, model);
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
                ReturnType = new CodeType { Name = voidType, IsNullable = false, IsExternal = true },
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
        var parameters = node.PathItems[Constants.DefaultOpenApiLabel].Parameters.Union(operation.Parameters).Where(static p => p.In == ParameterLocation.Query);
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
                AddPropertyForParameter(parameter, parameterClass);

            return parameterClass;
        }

        return null;
    }
    private void AddPropertyForParameter(OpenApiParameter parameter, CodeClass parameterClass)
    {
        var prop = new CodeProperty
        {
            Name = parameter.Name.SanitizeParameterNameForCodeSymbols(),
            Documentation = new()
            {
                Description = parameter.Description.CleanupDescription(),
            },
            Kind = CodePropertyKind.QueryParameter,
            Type = GetPrimitiveType(parameter.Schema),
        };
        prop.Type.CollectionKind = parameter.Schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default;
        if (string.IsNullOrEmpty(prop.Type.Name) && prop.Type is CodeType parameterType)
        {
            // since its a query parameter default to string if there is no schema
            // it also be an object type, but we'd need to create the model in that case and there's no standard on how to serialize those as query parameters
            parameterType.Name = "string";
            parameterType.IsExternal = true;
        }

        if (!parameter.Name.Equals(prop.Name))
        {
            prop.SerializationName = parameter.Name.SanitizeParameterNameForUrlTemplate();
        }

        if (!parameterClass.ContainsMember(parameter.Name))
        {
            parameterClass.AddProperty(prop);
        }
        else
        {
            logger.LogWarning("Ignoring duplicate parameter {name}", parameter.Name);
        }
    }
    private static CodeType GetQueryParameterType(OpenApiSchema schema) =>
        new()
        {
            IsExternal = true,
            Name = schema.Items?.Type ?? schema.Type,
            CollectionKind = schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default,
        };
}
