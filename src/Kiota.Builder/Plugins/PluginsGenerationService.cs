using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
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
    private static readonly OpenAPIRuntimeComparer _openAPIRuntimeComparer = new();
    public async Task GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        var (runtimes, functions) = GetRuntimesAndFunctionsFromTree(TreeNode);
        var descriptionForHuman = OAIDocument.Info?.Description.CleanupXMLString() is string d && !string.IsNullOrEmpty(d) ? d : $"Description for {OAIDocument.Info?.Title.CleanupXMLString()}";
        var descriptionForModel = descriptionForHuman;
        if (OAIDocument.Info is not null &&
                OAIDocument.Info.Extensions.TryGetValue(OpenApiDescriptionForModelExtension.Name, out var descriptionExtension) &&
                descriptionExtension is OpenApiDescriptionForModelExtension extension &&
                !string.IsNullOrEmpty(extension.Description))
            descriptionForModel = extension.Description.CleanupXMLString();
        var pluginDocument = new ManifestDocument
        {
            SchemaVersion = "v2",
            NameForHuman = OAIDocument.Info?.Title.CleanupXMLString(),
            // TODO name for model ???
            DescriptionForHuman = descriptionForHuman,
            DescriptionForModel = descriptionForModel,
            ContactEmail = OAIDocument.Info?.Contact?.Email,
            //TODO namespace
            //TODO logo
            Runtimes = [.. runtimes
                            .GroupBy(static x => x, _openAPIRuntimeComparer)
                            .Select(static x =>
                            {
                                var result = x.First();
                                result.RunForFunctions = x.SelectMany(static y => y.RunForFunctions).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                                return result;
                            })
                            .OrderBy(static x => x.RunForFunctions[0], StringComparer.OrdinalIgnoreCase)],
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
    private (OpenAPIRuntime[], Function[]) GetRuntimesAndFunctionsFromTree(OpenApiUrlTreeNode currentNode)
    {
        var runtimes = new List<OpenAPIRuntime>();
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
                    States = GetStatesFromOperation(operation),
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
    private static States? GetStatesFromOperation(OpenApiOperation openApiOperation)
    {
        return (GetStateFromExtension<OpenApiAiReasoningInstructionsExtension>(openApiOperation, OpenApiAiReasoningInstructionsExtension.Name, static x => x.ReasoningInstructions),
                GetStateFromExtension<OpenApiAiRespondingInstructionsExtension>(openApiOperation, OpenApiAiRespondingInstructionsExtension.Name, static x => x.RespondingInstructions)) switch
        {
            (State reasoning, State responding) => new States
            {
                Reasoning = reasoning,
                Responding = responding
            },
            (State reasoning, _) => new States
            {
                Reasoning = reasoning
            },
            (_, State responding) => new States
            {
                Responding = responding
            },
            _ => null
        };
    }
    private static State? GetStateFromExtension<T>(OpenApiOperation openApiOperation, string extensionName, Func<T, List<string>> instructionsExtractor)
    {
        if (openApiOperation.Extensions.TryGetValue(extensionName, out var rExtRaw) &&
            rExtRaw is T rExt &&
            instructionsExtractor(rExt).Exists(static x => !string.IsNullOrEmpty(x)))
        {
            return new State
            {
                Instructions = instructionsExtractor(rExt).Where(static x => !string.IsNullOrEmpty(x)).Select(static x => x.CleanupXMLString()).ToList()
            };
        }
        return null;
    }
    private static readonly HashSet<string> scalarTypes = new(StringComparer.OrdinalIgnoreCase) { "string", "number", "integer", "boolean" };
    //TODO validate this is right, in OAS integer are under type number for the json schema, but integer is ok for query parameters
}
