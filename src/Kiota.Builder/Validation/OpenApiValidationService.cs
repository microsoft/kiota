using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Validation;
public class OpenApiValidationService
{
    private readonly OpenApiDocumentDownloadService openApiDocumentDownloadService;
    private readonly ILogger Logger;

    public OpenApiValidationService(HttpClient httpClient, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
        openApiDocumentDownloadService = new OpenApiDocumentDownloadService(httpClient, logger);
    }

    public async Task<OpenApiDocument?> GetDocumentAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        bool generating = true;
        GenerationConfiguration config = new GenerationConfiguration();
        var (stream, isDescriptionFromWorkspaceCopy) = await openApiDocumentDownloadService.LoadStreamAsync(inputPath, config, cancellationToken: cancellationToken).ConfigureAwait(false);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var ms = new MemoryStream();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);
        var document = await openApiDocumentDownloadService.GetDocumentFromStreamAsync(ms, config, generating, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            Logger.LogError("The OpenAPI document could not be loaded");
        }
        return document;
    }
}
