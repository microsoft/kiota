using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Kiota.Builder.Caching;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.SearchProviders.APIsGuru;
using Kiota.Builder.Validation;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Kiota.Builder;

internal partial class OpenApiDocumentDownloadService
{
    private readonly ILogger Logger;
    private readonly HttpClient HttpClient;
    public OpenApiDocumentDownloadService(HttpClient httpClient, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        HttpClient = httpClient;
        Logger = logger;
    }
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    internal async Task<(Stream, bool)> LoadStreamAsync(string inputPath, GenerationConfiguration config, WorkspaceManagementService? workspaceManagementService = default, bool useKiotaConfig = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        inputPath = inputPath.Trim();

        Stream input;
        var isDescriptionFromWorkspaceCopy = false;
        if (useKiotaConfig &&
            config.Operation is ConsumerOperation.Edit or ConsumerOperation.Add &&
            workspaceManagementService is not null &&
            await workspaceManagementService.GetDescriptionCopyAsync(config.ClientClassName, inputPath, config.CleanOutput, cancellationToken).ConfigureAwait(false) is { } descriptionStream)
        {
            LogLoadedWorkspaceCopy();
            input = descriptionStream;
            isDescriptionFromWorkspaceCopy = true;
        }
        else if (inputPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            try
            {
                var cachingProvider = new DocumentCachingProvider(HttpClient, Logger)
                {
                    ClearCache = config.ClearCache,
                };
                var targetUri = APIsGuruSearchProvider.ChangeSourceUrlToGitHub(new Uri(inputPath)); // so updating existing clients doesn't break
                var fileName = targetUri.GetFileName() is string name && !string.IsNullOrEmpty(name) ? name : "description.yml";
                input = await cachingProvider.GetDocumentAsync(targetUri, "generation", fileName, cancellationToken: cancellationToken).ConfigureAwait(false);
                LogLoadedRemoteSource();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Could not download the file at {inputPath}, reason: {ex.Message}", ex);
            }
        else
            try
            {
                var inMemoryStream = new MemoryStream();
                using (await localFilesLock.LockAsync(inputPath, cancellationToken).ConfigureAwait(false))
                {// To avoid deadlocking on update with multiple clients for the same local description
                    using var fileStream = new FileStream(inputPath, FileMode.Open);
                    await fileStream.CopyToAsync(inMemoryStream, cancellationToken).ConfigureAwait(false);
                }
                inMemoryStream.Position = 0;
                input = inMemoryStream;
                LogLoadedLocalSource();
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
        LogReadOpenApiFile(stopwatch.ElapsedMilliseconds, inputPath);
        return (input, isDescriptionFromWorkspaceCopy);
    }

    internal async Task<ReadResult?> GetDocumentWithResultFromStreamAsync(Stream input, GenerationConfiguration config, bool generating = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        LogParsingOpenApi();
        var ruleSet = config.DisabledValidationRules.Contains(ValidationRuleSetExtensions.AllValidationRule) ?
                    ValidationRuleSet.GetEmptyRuleSet() :
                    ValidationRuleSet.GetDefaultRuleSet(); //workaround since validation rule set doesn't support clearing rules
        bool generatingMode = generating || config.IncludeKiotaValidationRules == true;
        if (generatingMode)
            ruleSet.AddKiotaValidationRules(config);
        var settings = new OpenApiReaderSettings
        {
            RuleSet = ruleSet,
            LoadExternalRefs = true,
            LeaveStreamOpen = true,
        };

        // Add all extensions for generation
        settings.AddGenerationExtensions();
        settings.AddYamlReader();
        // Add plugins extensions to parse from the OpenAPI file
        bool addPluginsExtensions = config.IsPluginConfiguration || config.IncludePluginExtensions == true;
        if (addPluginsExtensions)
            settings.AddPluginsExtensions();// Add all extensions for plugins

        try
        {
            var rawUri = config.OpenAPIFilePath.TrimEnd(KiotaBuilder.ForwardSlash);
            var lastSlashIndex = rawUri.LastIndexOf(KiotaBuilder.ForwardSlash);
            if (lastSlashIndex < 0)
                lastSlashIndex = rawUri.Length - 1;
            var documentUri = new Uri(rawUri[..lastSlashIndex]);
            settings.BaseUrl = documentUri;
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            // couldn't parse the URL, it's probably a local file
        }
        var readResult = await OpenApiDocument.LoadAsync(input, settings: settings, cancellationToken: cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        if (generatingMode && readResult.Diagnostic?.Warnings is { Count: > 0 })
            foreach (var warning in readResult.Diagnostic.Warnings)
                LogOpenApiWarning(warning.Pointer, warning.Message);
        if (readResult.Diagnostic?.Errors is { Count: > 0 })
        {
            LogParsedOpenApiWithErrors(stopwatch.ElapsedMilliseconds, readResult.Document?.Paths?.Count ?? 0);
            foreach (var parsingError in readResult.Diagnostic.Errors)
            {
                LogOpenApiError(parsingError.Pointer, parsingError.Message);
            }
        }
        else
        {
            LogParsedOpenApiSuccessfully(stopwatch.ElapsedMilliseconds, readResult.Document?.Paths?.Count ?? 0);
        }

        return readResult;
    }

    internal async Task<OpenApiDocument?> GetDocumentFromStreamAsync(Stream input, GenerationConfiguration config, bool generating = false, CancellationToken cancellationToken = default)
    {
        var result = await GetDocumentWithResultFromStreamAsync(input, config, generating, cancellationToken).ConfigureAwait(false);
        return result?.Document;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "loaded description from the workspace copy")]
    private partial void LogLoadedWorkspaceCopy();

    [LoggerMessage(Level = LogLevel.Information, Message = "loaded description from remote source")]
    private partial void LogLoadedRemoteSource();

    [LoggerMessage(Level = LogLevel.Information, Message = "loaded description from local source")]
    private partial void LogLoadedLocalSource();

    [LoggerMessage(Level = LogLevel.Trace, Message = "{Timestamp}ms: Read OpenAPI file {File}")]
    private partial void LogReadOpenApiFile(long timestamp, string file);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Parsing OpenAPI file")]
    private partial void LogParsingOpenApi();

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenAPI warning: {Pointer} - {Warning}")]
    private partial void LogOpenApiWarning(string? pointer, string warning);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{Timestamp}ms: Parsed OpenAPI with errors. {Count} paths found.")]
    private partial void LogParsedOpenApiWithErrors(long timestamp, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "OpenAPI error: {Pointer} - {Message}")]
    private partial void LogOpenApiError(string? pointer, string message);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{Timestamp}ms: Parsed OpenAPI successfully. {Count} paths found.")]
    private partial void LogParsedOpenApiSuccessfully(long timestamp, int count);
}
