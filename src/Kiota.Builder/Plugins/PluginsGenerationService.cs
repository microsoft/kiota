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
            NameForHuman = OAIDocument.Info?.Title.CleanupXMLString(),
            // TODO name for model
            DescriptionForHuman = OAIDocument.Info?.Description.CleanupXMLString() is string d && !string.IsNullOrEmpty(d) ? d : $"Description for {OAIDocument.Info?.Title.CleanupXMLString()}",
            DescriptionForModel = OAIDocument.Info?.Description.CleanupXMLString() is string e && !string.IsNullOrEmpty(e) ? e : $"Description for {OAIDocument.Info?.Title.CleanupXMLString()}",
            ContactEmail = OAIDocument.Info?.Contact?.Email,
            //TODO namespace
            //TODO logo
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
                var oasParameters = operation.Parameters
                                        .Union(pathItem.Parameters.Where(static x => x.In is ParameterLocation.Path))
                                        .Where(static x => x.Schema?.Type is not null && scalarTypes.Contains(x.Schema.Type))
                                        .ToArray();
                //TODO add request body

                functions.Add(new Function
                {
                    Name = operation.OperationId,
                    Description = operation.Summary.CleanupXMLString() is string summary && !string.IsNullOrEmpty(summary) ? summary : operation.Description.CleanupXMLString(),
                    Parameters = oasParameters.Length == 0 ? null :
                                    new Parameters(
                                        "object",
                                        new Properties(oasParameters.ToDictionary(
                                                                        static x => x.Name,
                                                                        static x => new Property(
                                                                                        x.Schema.Type ?? string.Empty,
                                                                                        x.Description.CleanupXMLString(),
                                                                                        x.Schema.Default?.ToString() ?? string.Empty,
                                                                                        null), //TODO enums
                                                                        StringComparer.OrdinalIgnoreCase)),
                                        oasParameters.Where(static x => x.Required).Select(static x => x.Name).ToList()),
                    //TODO states with reasoning and instructions from OAS extensions
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
    private static readonly HashSet<string> scalarTypes = new(StringComparer.OrdinalIgnoreCase) { "string", "number", "integer", "boolean" };
    //TODO validate this is right, in OAS integer are under type number for the json schema, but integer is ok for query parameters
}
