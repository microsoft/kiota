using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder
{
    public partial class HttpSnippetGenerationService
    {
        private readonly OpenApiDocument OAIDocument;
        private readonly OpenApiUrlTreeNode TreeNode;
        private readonly GenerationConfiguration Configuration;
        private readonly string WorkingDirectory;

        public HttpSnippetGenerationService(OpenApiDocument document, OpenApiUrlTreeNode openApiUrlTreeNode, GenerationConfiguration configuration, string workingDirectory)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(openApiUrlTreeNode);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrEmpty(workingDirectory);
            OAIDocument = document;
            TreeNode = openApiUrlTreeNode;
            Configuration = configuration;
            WorkingDirectory = workingDirectory;
        }

        public async Task GenerateHttpSnippetAsync(CancellationToken cancellationToken = default)
        {
            var descriptionRelativePath = "index.http";
            var descriptionFullPath = Path.Combine(Configuration.OutputPath, descriptionRelativePath);
            var directory = Path.GetDirectoryName(descriptionFullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var descriptionStream = File.Create(descriptionFullPath, 4096);
            await using var fileWriter = new StreamWriter(descriptionStream);
            var serverUrl = ExtractServerUrl(OAIDocument);
            await fileWriter.WriteLineAsync($"# Http snippet for {serverUrl}");
            await fileWriter.WriteLineAsync($"@url = {serverUrl}");
            await fileWriter.WriteLineAsync();
            var httpSnippetWriter = new HttpSnippetWriter(fileWriter);
            httpSnippetWriter.Write(TreeNode);
            httpSnippetWriter.Flush();
            await fileWriter.FlushAsync(cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        }

        private static string? ExtractServerUrl(OpenApiDocument document)
        {
            return document.Servers?.FirstOrDefault()?.Url;
        }
    }
}
