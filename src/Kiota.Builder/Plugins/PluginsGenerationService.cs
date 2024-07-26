using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.Plugins.Models;
using Microsoft.Extensions.FileProviders;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;
public class PluginsGenerationService
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
    private const string AppManifestFileName = "manifest.json";
    public async Task GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        // 1. write the OpenApi description
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

        // 2. write the plugins

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

        // 3. write the app manifest if its an Api Plugin
        if (Configuration.PluginTypes.Any(static plugin => plugin == PluginType.APIPlugin))
        {
            var manifestFullPath = Path.Combine(Configuration.OutputPath, AppManifestFileName);
            var pluginFileName = $"{Configuration.ClientClassName.ToLowerInvariant()}-{PluginType.APIPlugin.ToString().ToLowerInvariant()}{ManifestFileNameSuffix}";
            var appManifestModel = await GetAppManifestModelAsync(pluginFileName, manifestFullPath, cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2007
            await using var appManifestStream = File.Open(manifestFullPath, FileMode.Create);
#pragma warning restore CA2007
            await JsonSerializer.SerializeAsync(appManifestStream, appManifestModel, AppManifestModelGenerationContext.AppManifestModel, cancellationToken).ConfigureAwait(false);
        }
    }

    private const string ColorFileName = "color.png";
    private const string OutlineFileName = "outline.png";

    private async Task<AppManifestModel> GetAppManifestModelAsync(string pluginFileName, string manifestFullPath, CancellationToken cancellationToken)
    {
        var manifestInfo = ExtractInfoFromDocument(OAIDocument.Info);
        // create default model
        var manifestModel = new AppManifestModel
        {
            Id = Guid.NewGuid().ToString(),
            Developer = new Developer
            {
                Name = !string.IsNullOrEmpty(OAIDocument.Info?.Contact?.Name) ? OAIDocument.Info?.Contact?.Name : "Microsoft Kiota.",
                WebsiteUrl = !string.IsNullOrEmpty(OAIDocument.Info?.Contact?.Url?.OriginalString) ? OAIDocument.Info?.Contact?.Url?.OriginalString : "https://www.example.com/contact/",
                PrivacyUrl = !string.IsNullOrEmpty(manifestInfo.PrivacyUrl) ? manifestInfo.PrivacyUrl : "https://www.example.com/privacy/",
                TermsOfUseUrl = !string.IsNullOrEmpty(OAIDocument.Info?.TermsOfService?.OriginalString) ? OAIDocument.Info?.TermsOfService?.OriginalString : "https://www.example.com/terms/",
            },
            PackageName = $"com.microsoft.kiota.plugin.{Configuration.ClientClassName}",
            Name = new Name
            {
                ShortName = Configuration.ClientClassName,
                FullName = $"API Plugin {Configuration.ClientClassName} for {OAIDocument.Info?.Title.CleanupXMLString() ?? "OpenApi Document"}"
            },
            Description = new Description
            {
                ShortName = !string.IsNullOrEmpty(OAIDocument.Info?.Description.CleanupXMLString()) ? $"API Plugin for {OAIDocument.Info?.Description.CleanupXMLString()}." : OAIDocument.Info?.Title.CleanupXMLString() ?? "OpenApi Document",
                FullName = !string.IsNullOrEmpty(OAIDocument.Info?.Description.CleanupXMLString()) ? $"API Plugin for {OAIDocument.Info?.Description.CleanupXMLString()}." : OAIDocument.Info?.Title.CleanupXMLString() ?? "OpenApi Document"
            },
            Icons = new Icons(),
            AccentColor = "#FFFFFF"
        };

        if (File.Exists(manifestFullPath)) // No need for default, try to update the model from the file
        {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var fileStream = File.OpenRead(manifestFullPath);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            var manifestModelFromFile = await JsonSerializer.DeserializeAsync(fileStream, AppManifestModelGenerationContext.AppManifestModel, cancellationToken).ConfigureAwait(false);
            if (manifestModelFromFile != null)
                manifestModel = manifestModelFromFile;
        }
        else
        {
            // The manifest file did not exist, so setup any dependencies needed.
            // If it already existed, the user has setup them up in another way. 

            // 1. Check if icons exist and write them out.
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            var iconFilePath = Path.Combine(Configuration.OutputPath, ColorFileName);
            if (!File.Exists(iconFilePath))
            {
#pragma warning disable CA2007
                await using var reader = embeddedProvider.GetFileInfo(ColorFileName).CreateReadStream();
                await using var defaultColorFile = File.Open(iconFilePath, FileMode.Create);
#pragma warning restore CA2007
                await reader.CopyToAsync(defaultColorFile, cancellationToken).ConfigureAwait(false);
            }
            // 2. Check if outline exist and write them out.
            var outlineFilePath = Path.Combine(Configuration.OutputPath, OutlineFileName);
            if (!File.Exists(outlineFilePath))
            {
#pragma warning disable CA2007
                await using var reader = embeddedProvider.GetFileInfo(OutlineFileName).CreateReadStream();
                await using var defaultColorFile = File.Open(outlineFilePath, FileMode.Create);
#pragma warning restore CA2007
                await reader.CopyToAsync(defaultColorFile, cancellationToken).ConfigureAwait(false);
            }
        }

        manifestModel.CopilotExtensions ??= new CopilotExtensions();// ensure its not null.

        if (manifestModel.CopilotExtensions.Plugins is not null && manifestModel.CopilotExtensions.Plugins.FirstOrDefault(pluginItem => Configuration.ClientClassName.Equals(pluginItem.Id, StringComparison.OrdinalIgnoreCase)) is { } plugin)
        {
            plugin.File = pluginFileName; // id is already consistent so make sure the file name is ok
        }
        else
        {
            manifestModel.CopilotExtensions.Plugins ??= [];
            // Add a new plugin entry
            manifestModel.CopilotExtensions.Plugins.Add(new Plugin
            {
                File = pluginFileName,
                Id = Configuration.ClientClassName
            });
        }

        return manifestModel;
    }

    internal static readonly AppManifestModelGenerationContext AppManifestModelGenerationContext = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });

    private OpenApiDocument GetDocumentWithTrimmedComponentsAndResponses(OpenApiDocument doc)
    {
        // ensure the info and components are not null
        doc.Info ??= new OpenApiInfo();
        doc.Components ??= new OpenApiComponents();

        if (string.IsNullOrEmpty(doc.Info?.Version)) // filtering fails if there's no version.
            doc.Info!.Version = "1.0";

        //empty out all the responses with a single empty 2XX
        foreach (var operation in doc.Paths.SelectMany(static item => item.Value.Operations.Values))
        {
            var responseDescription = operation.Responses.Values.Select(static response => response.Description)
                                                                      .FirstOrDefault(static desc => !string.IsNullOrEmpty(desc)) ?? "Api Response";
            operation.Responses = new OpenApiResponses()
            {
                {
                    "2XX",new OpenApiResponse
                    {
                        Description = responseDescription,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "text/plain", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string"
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        // remove unused components using the OpenApi.Net
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
