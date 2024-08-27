using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Kiota.Builder.Caching;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.SearchProviders.APIsGuru;
using Kiota.Builder.Validation;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder;
internal class OpenApiDocumentDownloadService
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
            Logger.LogInformation("loaded description from the workspace copy");
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
                Logger.LogInformation("loaded description from remote source");
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
                Logger.LogInformation("loaded description from local source");
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
        Logger.LogTrace("{Timestamp}ms: Read OpenAPI file {File}", stopwatch.ElapsedMilliseconds, inputPath);
        return (input, isDescriptionFromWorkspaceCopy);
    }

    internal async Task<OpenApiDocument?> GetDocumentFromStreamAsync(Stream input, GenerationConfiguration config, bool generating = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Logger.LogTrace("Parsing OpenAPI file");
        var ruleSet = config.DisabledValidationRules.Contains(ValidationRuleSetExtensions.AllValidationRule) ?
                    ValidationRuleSet.GetEmptyRuleSet() :
                    ValidationRuleSet.GetDefaultRuleSet(); //workaround since validation rule set doesn't support clearing rules
        if (generating)
            ruleSet.AddKiotaValidationRules(config);
        var settings = new OpenApiReaderSettings
        {
            RuleSet = ruleSet,
        };

        // Add all extensions for generation
        settings.AddGenerationExtensions();
        if (config.IsPluginConfiguration)
            settings.AddPluginsExtensions();// Add all extensions for plugins

        try
        {
            var rawUri = config.OpenAPIFilePath.TrimEnd(KiotaBuilder.ForwardSlash);
            var lastSlashIndex = rawUri.LastIndexOf(KiotaBuilder.ForwardSlash);
            if (lastSlashIndex < 0)
                lastSlashIndex = rawUri.Length - 1;
            var documentUri = new Uri(rawUri[..lastSlashIndex]);
            settings.BaseUrl = documentUri;
            settings.LoadExternalRefs = true;
            settings.LeaveStreamOpen = true;
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
                Logger.LogWarning("OpenAPI warning: {Pointer} - {Warning}", warning.Pointer, warning.Message);
        if (readResult.OpenApiDiagnostic.Errors.Any())
        {
            Logger.LogTrace("{Timestamp}ms: Parsed OpenAPI with errors. {Count} paths found.", stopwatch.ElapsedMilliseconds, readResult.OpenApiDocument?.Paths?.Count ?? 0);
            foreach (var parsingError in readResult.OpenApiDiagnostic.Errors)
            {
                Logger.LogError("OpenAPI error: {Pointer} - {Message}", parsingError.Pointer, parsingError.Message);
            }
        }
        else
        {
            Logger.LogTrace("{Timestamp}ms: Parsed OpenAPI successfully. {Count} paths found.", stopwatch.ElapsedMilliseconds, readResult.OpenApiDocument?.Paths?.Count ?? 0);
        }

        return readResult.OpenApiDocument;
    }
}
