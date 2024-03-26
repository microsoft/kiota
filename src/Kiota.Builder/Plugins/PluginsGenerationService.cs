using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;
public class PluginsGenerationService
{
    private readonly OpenApiDocument OAIDocument;
    private readonly OpenApiUrlTreeNode TreeNode;
    private readonly GenerationConfiguration Configuration;

    public PluginsGenerationService(OpenApiDocument document, OpenApiUrlTreeNode openApiUrlTreeNode, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(openApiUrlTreeNode);
        ArgumentNullException.ThrowIfNull(configuration);
        OAIDocument = document;
        TreeNode = openApiUrlTreeNode;
        Configuration = configuration;
    }
    public async Task GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        var (runtimes, functions) = GetRuntimesAndFunctionsFromTree(TreeNode);
        var pluginDocument = new ManifestDocument
        {
            SchemaVersion = "v2",
            //TODO name for human
            //TODO description for human
            //TODO contact email
            //TODO namespace
            Runtimes = [.. runtimes.OrderBy(static x => x.RunForFunctions[0], StringComparer.OrdinalIgnoreCase)],
            Functions = [.. functions.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)]
        };
        var outputPath = Path.Combine(Configuration.OutputPath, "manifest.json");

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var fileStream = File.Create(outputPath, 4096);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true });
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        pluginDocument.Write(writer);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
    private (Runtime[], Function[]) GetRuntimesAndFunctionsFromTree(OpenApiUrlTreeNode currentNode)
    {
        var runtimes = new List<Runtime>();
        var functions = new List<Function>();
        if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
        {
            foreach (var operation in pathItem.Operations.Values.Where(static x => !string.IsNullOrEmpty(x.OperationId)))
            {
                runtimes.Add(new OpenAPIRuntime
                {
                    Auth = new Auth("none"),
                    Spec = new Dictionary<string, string> { { "url", "./openapi.yaml" } }, //TODO update from context once the slice copy is implemented
                    RunForFunctions = [operation.OperationId]
                });
                functions.Add(new Function
                {
                    Name = operation.OperationId,
                    //TODO map parameters
                    Description = operation.Summary.CleanupXMLString() is string summary && !string.IsNullOrEmpty(summary) ? summary : operation.Description.CleanupXMLString(),
                    // Parameters = operation.Parameters.Select(static x => new Parameter
                    // {
                    //     Name = x.Name,
                    //     Type = x.Schema.Type,
                    //     Required = x.Required
                    // }).ToArray()
                });
            }
        }
        foreach (var node in currentNode.Children)
        {
            var (childRuntimes, childFunctions) = GetRuntimesAndFunctionsFromTree(node.Value);
            runtimes.AddRange(childRuntimes);
            functions.AddRange(childFunctions);
        }
        return (runtimes.ToArray(), functions.ToArray());
    }
}
