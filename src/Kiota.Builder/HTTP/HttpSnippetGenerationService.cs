using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers.http;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.http
{
    public partial class HttpSnippetGenerationService
    {
        private readonly OpenApiDocument OAIDocument;
        private readonly GenerationConfiguration Configuration;

        public HttpSnippetGenerationService(OpenApiDocument document, GenerationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(configuration);
            OAIDocument = document;
            Configuration = configuration;
        }

        public async Task GenerateHttpSnippetAsync(CancellationToken cancellationToken = default)
        {
            // Create a http snippet file for each uri path segment
            // Get all the paths with at least one operation
            var tasks = OAIDocument.Paths
                .Where(x => x.Value.Operations.Any())
                .Select(x => new { Path = x.Key, PathItem = x.Value })
                .Select(x => GenerateSnippetForPathAsync(x.Path, x.PathItem, cancellationToken)); // Create tasks for each path

            await Task.WhenAll(tasks).ConfigureAwait(false); // Wait for all tasks to complete
        }

        private async Task GenerateSnippetForPathAsync(string path, OpenApiPathItem pathItem, CancellationToken cancellationToken)
        {
            var descriptionFullPath = Path.Combine(Configuration.OutputPath, SanitizePathSegment(path));
            var directory = Path.GetDirectoryName(descriptionFullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var descriptionStream = File.Create($"{descriptionFullPath}.http", 4096);
            await using var fileWriter = new StreamWriter(descriptionStream);
            var serverUrl = ExtractServerUrl(OAIDocument);
            await fileWriter.WriteLineAsync($"# Http snippet for {serverUrl}");
            await fileWriter.WriteLineAsync($"@url = {serverUrl}");
            await fileWriter.WriteLineAsync();
            var httpSnippetWriter = new HttpSnippetWriter(fileWriter);
            httpSnippetWriter.WriteOpenApiPathItem(pathItem, path);
            httpSnippetWriter.Flush();
            await fileWriter.FlushAsync(cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        }

        private static string? ExtractServerUrl(OpenApiDocument document)
        {
            return document.Servers?.FirstOrDefault()?.Url;
        }

        private static string SanitizePathSegment(string pathSegment)
        {
            // remove the leading '/' 
            return pathSegment.TrimStart('/');
        }
    }
}
