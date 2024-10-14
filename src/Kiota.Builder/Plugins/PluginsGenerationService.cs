using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
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

    public PluginsGenerationService(OpenApiDocument document, OpenApiUrlTreeNode openApiUrlTreeNode,
        GenerationConfiguration configuration, string workingDirectory)
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
    public async Task GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        // 1. cleanup any namings to be used later on.
        Configuration.ClientClassName =
            PluginNameCleanupRegex().Replace(Configuration.ClientClassName, string.Empty); //drop any special characters
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
        trimmedPluginDocument = InlineRequestBodyAllOf(trimmedPluginDocument);
        trimmedPluginDocument.SerializeAsV3(descriptionWriter);
        descriptionWriter.Flush();

        // 3. write the plugins

        foreach (var pluginType in Configuration.PluginTypes)
        {
            var manifestFileName = $"{Configuration.ClientClassName.ToLowerInvariant()}-{pluginType.ToString().ToLowerInvariant()}";
            var manifestOutputPath = Path.Combine(Configuration.OutputPath, $"{manifestFileName}{ManifestFileNameSuffix}");
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var fileStream = pluginType == PluginType.OpenAI ? Stream.Null : File.Create(manifestOutputPath, 4096);
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
                    apiManifest.ApiDependencies[Configuration.ClientClassName] = Configuration.ToApiDependency(string.Empty, TreeNode?.GetRequestInfo().ToDictionary(static x => x.Key, static x => x.Value) ?? [], WorkingDirectory);
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
                    // OpenAI plugins have been retired and are no longer supported. They only require the OpenAPI description now.
                    break;
                default:
                    throw new NotImplementedException($"The {pluginType} plugin is not implemented.");
            }

            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static OpenApiDocument InlineRequestBodyAllOf(OpenApiDocument openApiDocument)
    {
        if (openApiDocument.Paths is null) return openApiDocument;
        var contentItems = openApiDocument.Paths.Values.Where(static x => x?.Operations is not null)
            .SelectMany(static x => x.Operations.Values.Where(static x => x?.RequestBody?.Content is not null)
                .SelectMany(static x => x.RequestBody!.Content.Values));
        foreach (var contentItem in contentItems)
        {
            var schema = contentItem.Schema;
            // Merge all schemas in allOf `schema.MergeAllOfSchemaEntries()` doesn't seem to do the right thing.
            schema = MergeAllOfInSchema(schema);
            schema = SelectFirstAnyOfOrOneOf(schema);
            contentItem.Schema = schema;
        }

        return openApiDocument;

        static OpenApiSchema? SelectFirstAnyOfOrOneOf(OpenApiSchema? schema)
        {
            if (schema?.AnyOf is not { Count: > 0 } && schema?.OneOf is not { Count: > 0 }) return schema;
            OpenApiSchema newSchema;
            if (schema.AnyOf is { Count: > 0 })
            {
                newSchema = schema.AnyOf[0];
            }
            else if (schema.OneOf is { Count: > 0 })
            {
                newSchema = schema.OneOf[0];
            }
            else
            {
                newSchema = schema;
            }
            return newSchema;
        }
        static OpenApiSchema? MergeAllOfInSchema(OpenApiSchema? schema)
        {
            if (schema?.AllOf is not { Count: > 0 }) return schema;
            var newSchema = new OpenApiSchema();
            foreach (var apiSchema in schema.AllOf)
            {
                if (apiSchema.Title is not null) newSchema.Title = apiSchema.Title;
                if (!string.IsNullOrEmpty(apiSchema.Type))
                {
                    if (!string.IsNullOrEmpty(newSchema.Type) && newSchema.Type != apiSchema.Type)
                    {
                        throw new InvalidOperationException(
                            $"The schemas in allOf cannot have different types: '{newSchema.Type}' and '{apiSchema.Type}'.");
                    }
                    newSchema.Type = apiSchema.Type;
                }
                if (apiSchema.Format is not null) newSchema.Format = apiSchema.Format;
                if (!string.IsNullOrEmpty(apiSchema.Description)) newSchema.Description = apiSchema.Description;
                if (apiSchema.Maximum is not null) newSchema.Maximum = apiSchema.Maximum;
                if (apiSchema.ExclusiveMaximum is not null) newSchema.ExclusiveMaximum = apiSchema.ExclusiveMaximum;
                if (apiSchema.Minimum is not null) newSchema.Minimum = apiSchema.Minimum;
                if (apiSchema.ExclusiveMinimum is not null) newSchema.ExclusiveMinimum = apiSchema.ExclusiveMinimum;
                if (apiSchema.MaxLength is not null) newSchema.MaxLength = apiSchema.MaxLength;
                if (apiSchema.MinLength is not null) newSchema.MinLength = apiSchema.MinLength;
                if (!string.IsNullOrEmpty(apiSchema.Pattern)) newSchema.Pattern = apiSchema.Pattern;
                if (apiSchema.MultipleOf is not null) newSchema.MultipleOf = apiSchema.MultipleOf;
                if (apiSchema.Default is not null) newSchema.Default = apiSchema.Default;
                if (apiSchema.ReadOnly) newSchema.ReadOnly = apiSchema.ReadOnly;
                if (apiSchema.WriteOnly) newSchema.WriteOnly = apiSchema.WriteOnly;
                if (apiSchema.Not is not null) newSchema.Not = apiSchema.Not;
                if (apiSchema.Required is { Count: > 0 })
                {
                    foreach (var r in apiSchema.Required.Where(static r => !string.IsNullOrEmpty(r)))
                    {
                        newSchema.Required.Add(r);
                    }
                }
                if (apiSchema.Items is not null) newSchema.Items = apiSchema.Items;
                if (apiSchema.MaxItems is not null) newSchema.MaxItems = apiSchema.MaxItems;
                if (apiSchema.MinItems is not null) newSchema.MinItems = apiSchema.MinItems;
                if (apiSchema.UniqueItems is not null) newSchema.UniqueItems = apiSchema.UniqueItems;
                if (apiSchema.Properties is not null)
                {
                    foreach (var property in apiSchema.Properties)
                    {
                        newSchema.Properties.TryAdd(property.Key, property.Value);
                    }
                }
                if (apiSchema.MaxProperties is not null) newSchema.MaxProperties = apiSchema.MaxProperties;
                if (apiSchema.MinProperties is not null) newSchema.MinProperties = apiSchema.MinProperties;
                if (apiSchema.AdditionalPropertiesAllowed) newSchema.AdditionalPropertiesAllowed = true;
                if (apiSchema.AdditionalProperties is not null) newSchema.AdditionalProperties = apiSchema.AdditionalProperties;
                if (apiSchema.Discriminator is not null) newSchema.Discriminator = apiSchema.Discriminator;
                if (apiSchema.Example is not null) newSchema.Example = apiSchema.Example;
                if (apiSchema.Enum is not null)
                {
                    foreach (var enumValue in apiSchema.Enum)
                    {
                        newSchema.Enum.Add(enumValue);
                    }
                }
                if (apiSchema.Nullable) newSchema.Nullable = apiSchema.Nullable;
                if (apiSchema.ExternalDocs is not null) newSchema.ExternalDocs = apiSchema.ExternalDocs;
                if (apiSchema.Deprecated) newSchema.Deprecated = apiSchema.Deprecated;
                if (apiSchema.Xml is not null) newSchema.Xml = apiSchema.Xml;
                if (apiSchema.Extensions is not null)
                {
                    foreach (var extension in apiSchema.Extensions)
                    {
                        newSchema.Extensions.Add(extension.Key, extension.Value);
                    }
                }
                if (apiSchema.Reference is not null) newSchema.Reference = apiSchema.Reference;
                if (apiSchema.Annotations is not null)
                {
                    foreach (var annotation in apiSchema.Annotations)
                    {
                        newSchema.Annotations.Add(annotation.Key, annotation.Value);
                    }
                }
            }
            return newSchema;
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
            var key = string.IsNullOrEmpty(basePath)
                ? path.Key
                : $"{basePath}/{path.Key.TrimStart(KiotaBuilder.ForwardSlash)}";
            requestUrls[key] = path.Value.Operations.Keys.Select(static key => key.ToString().ToUpperInvariant()).ToList();
        }

        if (requestUrls.Count == 0)
            throw new InvalidOperationException("No paths found in the OpenAPI document.");

        var predicate = OpenApiFilterService.CreatePredicate(requestUrls: requestUrls, source: doc);
        return OpenApiFilterService.CreateFilteredDocument(doc, predicate);
    }

    private PluginManifestDocument GetManifestDocument(string openApiDocumentPath)
    {
        var (runtimes, functions, conversationStarters) = GetRuntimesFunctionsAndConversationStartersFromTree(OAIDocument, Configuration.PluginAuthInformation, TreeNode, openApiDocumentPath);
        var descriptionForHuman = OAIDocument.Info?.Description is string d && !string.IsNullOrEmpty(d) ? d : $"Description for {OAIDocument.Info?.Title}";
        var manifestInfo = ExtractInfoFromDocument(OAIDocument.Info);
        var pluginManifestDocument = new PluginManifestDocument
        {
            Schema = "https://developer.microsoft.com/json-schemas/copilot/plugin/v2.1/schema.json",
            SchemaVersion = "v2.1",
            NameForHuman = OAIDocument.Info?.Title.CleanupXMLString(),
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
            Functions = [.. functions.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)],
        };

        if (conversationStarters.Length > 0)
            pluginManifestDocument.Capabilities = new Capabilities
            {
                ConversationStarters = conversationStarters.Where(static x => !string.IsNullOrEmpty(x.Text))
                                            .Select(static x => new ConversationStarter
                                            {
                                                Text = x.Text?.Length < 50 ? x.Text : x.Text?[..50]
                                            })
                                            .ToList()
            };
        return pluginManifestDocument;
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

    private sealed record OpenApiManifestInfo(
        string? DescriptionForModel = null,
        string? LegalUrl = null,
        string? LogoUrl = null,
        string? PrivacyUrl = null,
        string ContactEmail = DefaultContactEmail);

    private static (OpenApiRuntime[], Function[], ConversationStarter[]) GetRuntimesFunctionsAndConversationStartersFromTree(OpenApiDocument document, PluginAuthConfiguration? authInformation, OpenApiUrlTreeNode currentNode,
        string openApiDocumentPath)
    {
        var runtimes = new List<OpenApiRuntime>();
        var functions = new List<Function>();
        var conversationStarters = new List<ConversationStarter>();
        var configAuth = authInformation?.ToPluginManifestAuth();
        if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
        {
            foreach (var operation in pathItem.Operations.Values.Where(static x => !string.IsNullOrEmpty(x.OperationId)))
            {
                runtimes.Add(new OpenApiRuntime
                {
                    // Configuration overrides document information
                    Auth = configAuth ?? GetAuth(operation.Security ?? document.SecurityRequirements ?? []),
                    Spec = new OpenApiRuntimeSpec { Url = openApiDocumentPath },
                    RunForFunctions = [operation.OperationId!]
                });

                var summary = operation.Summary.CleanupXMLString();
                var description = operation.Description.CleanupXMLString();

                functions.Add(new Function
                {
                    Name = operation.OperationId!,
                    Description = !string.IsNullOrEmpty(description) ? description : summary,
                    States = GetStatesFromOperation(operation),

                });
                conversationStarters.Add(new ConversationStarter
                {
                    Text = !string.IsNullOrEmpty(summary) ? summary : description
                });

            }
        }

        foreach (var node in currentNode.Children)
        {
            var (childRuntimes, childFunctions, childConversationStarters) = GetRuntimesFunctionsAndConversationStartersFromTree(document, authInformation, node.Value, openApiDocumentPath);
            runtimes.AddRange(childRuntimes);
            functions.AddRange(childFunctions);
            conversationStarters.AddRange(childConversationStarters);
        }

        return (runtimes.ToArray(), functions.ToArray(), conversationStarters.ToArray());
    }

    private static Auth GetAuth(IList<OpenApiSecurityRequirement> securityRequirements)
    {
        // Only one security requirement object is allowed
        const string tooManySchemesError = "Multiple security requirements are not supported. Operations can only list one security requirement.";
        if (securityRequirements.Count > 1 || securityRequirements.FirstOrDefault()?.Keys.Count > 1)
        {
            throw new InvalidOperationException(tooManySchemesError);
        }
        var security = securityRequirements.FirstOrDefault();
        var opSecurity = security?.Keys.FirstOrDefault();
        return (opSecurity is null || opSecurity.UnresolvedReference) ? new AnonymousAuth() : GetAuthFromSecurityScheme(opSecurity);
    }

    private static Auth GetAuthFromSecurityScheme(OpenApiSecurityScheme securityScheme)
    {
        string name = securityScheme.Reference.Id;
        return securityScheme.Type switch
        {
            SecuritySchemeType.ApiKey => new ApiKeyPluginVault
            {
                ReferenceId = $"{{{name}_REGISTRATION_ID}}"
            },
            // Only Http bearer is supported
            SecuritySchemeType.Http when securityScheme.Scheme.Equals("bearer", StringComparison.OrdinalIgnoreCase) =>
                new ApiKeyPluginVault { ReferenceId = $"{{{name}_REGISTRATION_ID}}" },
            SecuritySchemeType.OpenIdConnect => new ApiKeyPluginVault
            {
                ReferenceId = $"{{{name}_REGISTRATION_ID}}"
            },
            SecuritySchemeType.OAuth2 => new OAuthPluginVault
            {
                ReferenceId = $"{{{name}_CONFIGURATION_ID}}"
            },
            _ => throw new UnsupportedSecuritySchemeException(["Bearer Token", "Api Key", "OpenId Connect", "OAuth"],
                $"Unsupported security scheme type '{securityScheme.Type}'.")
        };
    }

    private static States? GetStatesFromOperation(OpenApiOperation openApiOperation)
    {
        return (
                GetStateFromExtension<OpenApiAiReasoningInstructionsExtension>(openApiOperation,
                    OpenApiAiReasoningInstructionsExtension.Name, static x => x.ReasoningInstructions),
                GetStateFromExtension<OpenApiAiRespondingInstructionsExtension>(openApiOperation,
                    OpenApiAiRespondingInstructionsExtension.Name, static x => x.RespondingInstructions)) switch
        {
            (State reasoning, State responding) => new States { Reasoning = reasoning, Responding = responding },
            (State reasoning, _) => new States { Reasoning = reasoning },
            (_, State responding) => new States { Responding = responding },
            _ => null
        };
    }

    private static State? GetStateFromExtension<T>(OpenApiOperation openApiOperation, string extensionName,
        Func<T, List<string>> instructionsExtractor)
    {
        if (openApiOperation.Extensions is not null &&
            openApiOperation.Extensions.TryGetValue(extensionName, out var rExtRaw) &&
            rExtRaw is T rExt &&
            instructionsExtractor(rExt).Exists(static x => !string.IsNullOrEmpty(x)))
        {
            return new State
            {
                Instructions = new Instructions(instructionsExtractor(rExt)
                    .Where(static x => !string.IsNullOrEmpty(x)).Select(static x => x.CleanupXMLString()).ToList())
            };
        }

        return null;
    }
}
