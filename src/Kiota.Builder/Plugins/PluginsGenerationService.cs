﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;
public partial class PluginsGenerationService
{
    private readonly OpenApiDocument OAIDocument;
    private readonly OpenApiUrlTreeNode TreeNode;
    private readonly GenerationConfiguration Configuration;
    private readonly string WorkingDirectory;

    public PluginsGenerationService(OpenApiDocument document, OpenApiUrlTreeNode openApiUrlTreeNode, GenerationConfiguration configuration, string workingDirectory)
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
    private static readonly OpenAPIRuntimeComparer _openAPIRuntimeComparer = new();
    private const string ManifestFileNameSuffix = ".json";
    private const string DescriptionPathSuffix = "openapi.yml";
    private const string OpenAIManifestFileName = "openai-plugins";
    public async Task GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        // 1. cleanup any namings to be used later on.
        Configuration.ClientClassName = PluginNameCleanupRegex().Replace(Configuration.ClientClassName, string.Empty); //drop any special characters
        // 2. write the OpenApi description
        var descriptionRelativePath = $"{Configuration.ClientClassName.ToLowerInvariant()}-{DescriptionPathSuffix}";
        var descriptionFullPath = Path.Combine(Configuration.OutputPath, descriptionRelativePath);
        var directory = Path.GetDirectoryName(descriptionFullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var descriptionStream = File.Create(descriptionFullPath, 4096);
        await using var fileWriter = new StreamWriter(descriptionStream);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        var descriptionWriter = new OpenApiYamlWriter(fileWriter);
        var trimmedPluginDocument = GetDocumentWithTrimmedComponentsAndResponses(OAIDocument);
        trimmedPluginDocument.SerializeAsV3(descriptionWriter);
        descriptionWriter.Flush();

        // 3. write the plugins

        foreach (var pluginType in Configuration.PluginTypes)
        {
            var manifestFileName = pluginType == PluginType.OpenAI ? OpenAIManifestFileName : $"{Configuration.ClientClassName.ToLowerInvariant()}-{pluginType.ToString().ToLowerInvariant()}";
            var manifestOutputPath = Path.Combine(Configuration.OutputPath, $"{manifestFileName}{ManifestFileNameSuffix}");
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var fileStream = File.Create(manifestOutputPath, 4096);
            await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true });
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

            switch (pluginType)
            {
                case PluginType.APIPlugin:
                    var pluginDocument = GetManifestDocument(descriptionRelativePath);
                    pluginDocument.Write(writer);
                    break;
                case PluginType.APIManifest:
                    var apiManifest = new ApiManifestDocument("application"); //TODO add application name
                    // pass empty config hash so that its not included in this manifest.
                    apiManifest.ApiDependencies.AddOrReplace(Configuration.ClientClassName, Configuration.ToApiDependency(string.Empty, TreeNode?.GetRequestInfo().ToDictionary(static x => x.Key, static x => x.Value) ?? [], WorkingDirectory));
                    var publisherName = string.IsNullOrEmpty(OAIDocument.Info?.Contact?.Name)
                        ? DefaultContactName
                        : OAIDocument.Info.Contact.Name;
                    var publisherEmail = string.IsNullOrEmpty(OAIDocument.Info?.Contact?.Email)
                        ? DefaultContactEmail
                        : OAIDocument.Info.Contact.Email;
                    apiManifest.Publisher = new Publisher(publisherName, publisherEmail);
                    apiManifest.Write(writer);
                    break;
                case PluginType.OpenAI:
                    var pluginDocumentV1 = GetV1ManifestDocument(descriptionRelativePath);
                    pluginDocumentV1.Write(writer);
                    break;
                default:
                    throw new NotImplementedException($"The {pluginType} plugin is not implemented.");
            }
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]+", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex PluginNameCleanupRegex();

    private OpenApiDocument GetDocumentWithTrimmedComponentsAndResponses(OpenApiDocument doc)
    {
        // ensure the info and components are not null
        doc.Info ??= new OpenApiInfo();
        doc.Components ??= new OpenApiComponents();

        if (string.IsNullOrEmpty(doc.Info?.Version)) // filtering fails if there's no version.
            doc.Info!.Version = "1.0";

        //empty out all the responses with a single empty 2XX and cleanup the extensions
        var openApiWalker = new OpenApiWalker(new OpenApiPluginWalker());
        openApiWalker.Walk(doc);

        // remove unused components using the OpenApi.Net library
        var requestUrls = new Dictionary<string, List<string>>();
        var basePath = doc.GetAPIRootUrl(Configuration.OpenAPIFilePath);
        foreach (var path in doc.Paths.Where(static path => path.Value.Operations.Count > 0))
        {
            var key = string.IsNullOrEmpty(basePath) ? path.Key : $"{basePath}/{path.Key.TrimStart(KiotaBuilder.ForwardSlash)}";
            requestUrls[key] = path.Value.Operations.Keys.Select(static key => key.ToString().ToUpperInvariant()).ToList();
        }

        var predicate = OpenApiFilterService.CreatePredicate(requestUrls: requestUrls, source: doc);
        return OpenApiFilterService.CreateFilteredDocument(doc, predicate);
    }

    private PluginManifestDocument GetV1ManifestDocument(string openApiDocumentPath)
    {
        var descriptionForHuman = OAIDocument.Info?.Description.CleanupXMLString() is string d && !string.IsNullOrEmpty(d) ? d : $"Description for {OAIDocument.Info?.Title.CleanupXMLString()}";
        var manifestInfo = ExtractInfoFromDocument(OAIDocument.Info);
        return new PluginManifestDocument
        {
            SchemaVersion = "v1",
            NameForHuman = OAIDocument.Info?.Title.CleanupXMLString(),
            NameForModel = OAIDocument.Info?.Title.CleanupXMLString(),
            DescriptionForHuman = descriptionForHuman,
            DescriptionForModel = manifestInfo.DescriptionForModel ?? descriptionForHuman,
            Auth = new V1AnonymousAuth(),
            Api = new Api()
            {
                Type = ApiType.openapi,
                URL = openApiDocumentPath
            },
            ContactEmail = manifestInfo.ContactEmail,
            LogoUrl = manifestInfo.LogoUrl,
            LegalInfoUrl = manifestInfo.LegalUrl,
        };
    }

    private PluginManifestDocument GetManifestDocument(string openApiDocumentPath)
    {
        var (runtimes, functions) = GetRuntimesAndFunctionsFromTree(TreeNode, openApiDocumentPath);
        var descriptionForHuman = OAIDocument.Info?.Description.CleanupXMLString() is string d && !string.IsNullOrEmpty(d) ? d : $"Description for {OAIDocument.Info?.Title.CleanupXMLString()}";
        var manifestInfo = ExtractInfoFromDocument(OAIDocument.Info);
        return new PluginManifestDocument
        {
            Schema = "https://aka.ms/json-schemas/copilot-extensions/v2.1/plugin.schema.json",
            SchemaVersion = "v2.1",
            NameForHuman = OAIDocument.Info?.Title.CleanupXMLString(),
            // TODO name for model ???
            DescriptionForHuman = descriptionForHuman,
            DescriptionForModel = manifestInfo.DescriptionForModel ?? descriptionForHuman,
            ContactEmail = manifestInfo.ContactEmail,
            Namespace = Configuration.ClientClassName,
            LogoUrl = manifestInfo.LogoUrl,
            LegalInfoUrl = manifestInfo.LegalUrl,
            PrivacyPolicyUrl = manifestInfo.PrivacyUrl,
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
    }

    private static OpenApiManifestInfo ExtractInfoFromDocument(OpenApiInfo? openApiInfo)
    {
        var manifestInfo = new OpenApiManifestInfo();

        if (openApiInfo is null)
            return manifestInfo;

        string? descriptionForModel = null;
        string? legalUrl = null;
        string? logoUrl = null;
        string? privacyUrl = null;
        string contactEmail = string.IsNullOrEmpty(openApiInfo.Contact?.Email)
            ? DefaultContactEmail
            : openApiInfo.Contact.Email;

        if (openApiInfo.Extensions.TryGetValue(OpenApiDescriptionForModelExtension.Name, out var descriptionExtension) &&
            descriptionExtension is OpenApiDescriptionForModelExtension extension &&
            !string.IsNullOrEmpty(extension.Description))
            descriptionForModel = extension.Description.CleanupXMLString();
        if (openApiInfo.Extensions.TryGetValue(OpenApiLegalInfoUrlExtension.Name, out var legalExtension) && legalExtension is OpenApiLegalInfoUrlExtension legal)
            legalUrl = legal.Legal;
        if (openApiInfo.Extensions.TryGetValue(OpenApiLogoExtension.Name, out var logoExtension) && logoExtension is OpenApiLogoExtension logo)
            logoUrl = logo.Url;
        if (openApiInfo.Extensions.TryGetValue(OpenApiPrivacyPolicyUrlExtension.Name, out var privacyExtension) && privacyExtension is OpenApiPrivacyPolicyUrlExtension privacy)
            privacyUrl = privacy.Privacy;

        return new OpenApiManifestInfo(descriptionForModel, legalUrl, logoUrl, privacyUrl, contactEmail);

    }
    private const string DefaultContactName = "publisher-name";
    private const string DefaultContactEmail = "publisher-email@example.com";
    private sealed record OpenApiManifestInfo(string? DescriptionForModel = null, string? LegalUrl = null, string? LogoUrl = null, string? PrivacyUrl = null, string ContactEmail = DefaultContactEmail);
    private static (OpenApiRuntime[], Function[]) GetRuntimesAndFunctionsFromTree(OpenApiUrlTreeNode currentNode, string openApiDocumentPath)
    {
        var runtimes = new List<OpenApiRuntime>();
        var functions = new List<Function>();
        if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
        {
            foreach (var operation in pathItem.Operations.Values.Where(static x => !string.IsNullOrEmpty(x.OperationId)))
            {
                runtimes.Add(new OpenApiRuntime
                {
                    Auth = new AnonymousAuth(),
                    Spec = new OpenApiRuntimeSpec()
                    {
                        Url = openApiDocumentPath,
                    },
                    RunForFunctions = [operation.OperationId]
                });
                functions.Add(new Function
                {
                    Name = operation.OperationId,
                    Description =
                        operation.Summary.CleanupXMLString() is string summary && !string.IsNullOrEmpty(summary)
                            ? summary
                            : operation.Description.CleanupXMLString(),
                    States = GetStatesFromOperation(operation),
                });
            }
        }
        foreach (var node in currentNode.Children)
        {
            var (childRuntimes, childFunctions) = GetRuntimesAndFunctionsFromTree(node.Value, openApiDocumentPath);
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
                Instructions = new Instructions(instructionsExtractor(rExt).Where(static x => !string.IsNullOrEmpty(x)).Select(static x => x.CleanupXMLString()).ToList())
            };
        }
        return null;
    }
}
