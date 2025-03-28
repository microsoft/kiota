using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.Plugins;

public partial class PluginsGenerationService
{
    private readonly OpenApiDocument OAIDocument;
    private readonly OpenApiUrlTreeNode TreeNode;
    private readonly GenerationConfiguration Configuration;
    private readonly string WorkingDirectory;
    private readonly ILogger<KiotaBuilder> Logger;

    public PluginsGenerationService(OpenApiDocument document, OpenApiUrlTreeNode openApiUrlTreeNode,
        GenerationConfiguration configuration, string workingDirectory, ILogger<KiotaBuilder> logger)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(openApiUrlTreeNode);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);
        OAIDocument = document;
        TreeNode = openApiUrlTreeNode;
        Configuration = configuration;
        WorkingDirectory = workingDirectory;
        Logger = logger;
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
        var descriptionWriter = new OpenApiYamlWriter(fileWriter, new() { InlineLocalReferences = true, InlineExternalReferences = true });
        var trimmedPluginDocument = GetDocumentWithTrimmedComponentsAndResponses(OAIDocument);
        PrepareDescriptionForCopilot(trimmedPluginDocument);
        // trimming a second time to remove any components that are no longer used after the inlining
        trimmedPluginDocument = GetDocumentWithTrimmedComponentsAndResponses(trimmedPluginDocument);
        trimmedPluginDocument.Info.Title = trimmedPluginDocument.Info.Title?[..^9]; // removing the second ` - Subset` suffix from the title
        trimmedPluginDocument.SerializeAsV3(descriptionWriter);
        await descriptionWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

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

    private sealed class MappingCleanupVisitor(OpenApiDocument openApiDocument) : OpenApiVisitorBase
    {
        private readonly OpenApiDocument _document = openApiDocument;

        public override void Visit(IOpenApiSchema schema)
        {
            if (schema.Discriminator?.Mapping is null)
                return;
            var keysToRemove = schema.Discriminator.Mapping.Where(x => _document.Components?.Schemas is null || !_document.Components.Schemas.ContainsKey(x.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1])).Select(static x => x.Key).ToArray();
            foreach (var key in keysToRemove)
                schema.Discriminator.Mapping.Remove(key);
            base.Visit(schema);
        }
    }

    private sealed class AllOfPropertiesRetrievalVisitor : OpenApiVisitorBase
    {
        public override void Visit(IOpenApiSchema schema)
        {
            var targetSchema = schema switch
            {
                OpenApiSchemaReference openApiSchemaReference => openApiSchemaReference.RecursiveTarget,
                OpenApiSchema openApiSchema => openApiSchema,
                _ => null
            };
            if (targetSchema is not { AllOf.Count: > 0 })
                return;
            var allPropertiesToAdd = GetAllProperties(targetSchema).ToArray();
            foreach (var allOfEntry in targetSchema.AllOf)
                SelectFirstAnyOneOfVisitor.CopyRelevantInformation(allOfEntry, targetSchema, false, false);
            targetSchema.Properties ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
            foreach (var (key, value) in allPropertiesToAdd)
                targetSchema.Properties.TryAdd(key, value);
            targetSchema.AllOf.Clear();
            base.Visit(schema);
        }

        private static IEnumerable<KeyValuePair<string, IOpenApiSchema>> GetAllProperties(IOpenApiSchema schema)
        {
            return schema.AllOf is not null ?
                schema.AllOf.SelectMany(static x => GetAllProperties(x)).Union(schema.Properties ?? new Dictionary<string, IOpenApiSchema>(0)) :
                (schema.Properties ?? new Dictionary<string, IOpenApiSchema>(0));
        }
    }

    private sealed class ReplaceFirstSchemaByReference : OpenApiVisitorBase
    {
        public override void Visit(OpenApiMediaType mediaType)
        {
            mediaType.Schema = GetFirstSchema(mediaType.Schema);
            base.Visit(mediaType);
        }
        public override void Visit(IOpenApiParameter parameter)
        {
            if (parameter is OpenApiParameter openApiParameter)
                openApiParameter.Schema = GetFirstSchema(parameter.Schema);
            base.Visit(parameter);
        }
        public override void Visit(IOpenApiHeader header)
        {
            if (header is OpenApiHeader openApiHeader)
                openApiHeader.Schema = GetFirstSchema(header.Schema);
            base.Visit(header);
        }
        public override void Visit(IOpenApiSchema schema)
        {
            if (schema is OpenApiSchema { Properties.Count: > 0 } openApiSchema)
            {
                openApiSchema.Items = GetFirstSchema(schema.Items);
                var properties = new Dictionary<string, IOpenApiSchema>(openApiSchema.Properties);
                foreach (var (key, value) in properties)
                    if (GetFirstSchema(value) is { } firstSchema)
                        openApiSchema.Properties[key] = firstSchema;
            }
            base.Visit(schema);
        }
        private static IOpenApiSchema? GetFirstSchema(IOpenApiSchema? schema)
        {
            if (schema is null) return null;
            if (schema.AnyOf is { Count: > 0 } && schema.AnyOf[0] is OpenApiSchemaReference anyOfSchemaReference)
                return anyOfSchemaReference;
            if (schema.OneOf is { Count: > 0 } && schema.OneOf[0] is OpenApiSchemaReference oneOfSchemaReference)
                return oneOfSchemaReference;
            return schema;
        }
    }
    private sealed class SelectFirstAnyOneOfVisitor : OpenApiVisitorBase
    {
        public override void Visit(IOpenApiSchema schema)
        {
            if (schema.AnyOf is { Count: > 0 })
            {
                CopyRelevantInformation(schema.AnyOf[0], schema);
                schema.AnyOf.Clear();
            }
            if (schema.OneOf is { Count: > 0 })
            {
                CopyRelevantInformation(schema.OneOf[0], schema);
                schema.OneOf.Clear();
            }
            base.Visit(schema);
        }
        internal static void CopyRelevantInformation(IOpenApiSchema source, IOpenApiSchema target, bool includeProperties = true, bool includeDiscriminator = true)
        {
            if (target is OpenApiSchema openApiSchema)
            {
                if (source.Type is not null && source.Type.HasValue)
                    openApiSchema.Type = source.Type;
                if (!string.IsNullOrEmpty(source.Format))
                    openApiSchema.Format = source.Format;
                if (source.Items is not null)
                    openApiSchema.Items = source.Items;
                if (source.Properties is not null && includeProperties)
                    openApiSchema.Properties = new Dictionary<string, IOpenApiSchema>(source.Properties);
                if (source.Required is not null)
                    openApiSchema.Required = new HashSet<string>(source.Required);
                if (source.AdditionalProperties is not null)
                    openApiSchema.AdditionalProperties = source.AdditionalProperties;
                if (source.Enum is not null)
                    openApiSchema.Enum = [.. source.Enum];
                if (source.ExclusiveMaximum is not null)
                    openApiSchema.ExclusiveMaximum = source.ExclusiveMaximum;
                if (source.ExclusiveMinimum is not null)
                    openApiSchema.ExclusiveMinimum = source.ExclusiveMinimum;
                if (source.Maximum is not null)
                    openApiSchema.Maximum = source.Maximum;
                if (source.Minimum is not null)
                    openApiSchema.Minimum = source.Minimum;
                if (source.MaxItems is not null)
                    openApiSchema.MaxItems = source.MaxItems;
                if (source.MinItems is not null)
                    openApiSchema.MinItems = source.MinItems;
                if (source.MaxLength is not null)
                    openApiSchema.MaxLength = source.MaxLength;
                if (source.MinLength is not null)
                    openApiSchema.MinLength = source.MinLength;
                if (source.Pattern is not null)
                    openApiSchema.Pattern = source.Pattern;
                if (source.MaxProperties is not null)
                    openApiSchema.MaxProperties = source.MaxProperties;
                if (source.MinProperties is not null)
                    openApiSchema.MinProperties = source.MinProperties;
                if (source.UniqueItems is not null)
                    openApiSchema.UniqueItems = source.UniqueItems;
                if (source.ReadOnly)
                    openApiSchema.ReadOnly = true;
                if (source.WriteOnly)
                    openApiSchema.WriteOnly = true;
                if (source.Deprecated)
                    openApiSchema.Deprecated = true;
                if (source.Xml is not null)
                    openApiSchema.Xml = source.Xml;
                if (source.ExternalDocs is not null)
                    openApiSchema.ExternalDocs = source.ExternalDocs;
                if (source.Example is not null)
                    openApiSchema.Example = source.Example;
                if (source.Extensions is not null)
                    openApiSchema.Extensions = new Dictionary<string, IOpenApiExtension>(source.Extensions);
                if (source.Discriminator is not null && includeDiscriminator)
                    openApiSchema.Discriminator = source.Discriminator;
                if (!string.IsNullOrEmpty(source.Description))
                    openApiSchema.Description = source.Description;
                if (!string.IsNullOrEmpty(source.Title))
                    openApiSchema.Title = source.Title;
                if (source.Default is not null)
                    openApiSchema.Default = source.Default;
            }
        }
    }

    private sealed class ErrorResponsesCleanupVisitor : OpenApiVisitorBase
    {
        public override void Visit(OpenApiOperation operation)
        {
            if (operation.Responses is null)
                return;
            var errorResponses = operation.Responses.Where(static x => x.Key.StartsWith('4') || x.Key.StartsWith('5')).ToArray();
            foreach (var (key, value) in errorResponses)
                operation.Responses.Remove(key);
            base.Visit(operation);
        }
    }

    private sealed class ExternalDocumentationCleanupVisitor : OpenApiVisitorBase
    {
        public override void Visit(OpenApiDocument doc)
        {
            if (doc.ExternalDocs is not null)
                doc.ExternalDocs = null;
            base.Visit(doc);
        }
        public override void Visit(OpenApiOperation operation)
        {
            if (operation.ExternalDocs is not null)
                operation.ExternalDocs = null;
            base.Visit(operation);
        }
        public override void Visit(IOpenApiSchema schema)
        {
            if (schema.ExternalDocs is not null && schema is OpenApiSchema openApiSchema)
                openApiSchema.ExternalDocs = null;
            base.Visit(schema);
        }
        public override void Visit(OpenApiTag tag)
        {
            if (tag.ExternalDocs is not null)
                tag.ExternalDocs = null;
            base.Visit(tag);
        }
    }

    private static void PrepareDescriptionForCopilot(OpenApiDocument document)
    {
        var externalDocumentationCleanupVisitor = new ExternalDocumentationCleanupVisitor();
        var externalDocumentationCleanupWalker = new OpenApiWalker(externalDocumentationCleanupVisitor);
        externalDocumentationCleanupWalker.Walk(document);

        var errorResponsesCleanupVisitor = new ErrorResponsesCleanupVisitor();
        var errorResponsesCleanupWalker = new OpenApiWalker(errorResponsesCleanupVisitor);
        errorResponsesCleanupWalker.Walk(document);

        var replaceFirstSchemaByReference = new ReplaceFirstSchemaByReference();
        var replaceFirstSchemaByReferenceWalker = new OpenApiWalker(replaceFirstSchemaByReference);
        replaceFirstSchemaByReferenceWalker.Walk(document);

        var selectFirstAnyOneOfVisitor = new SelectFirstAnyOneOfVisitor();
        var selectFirstAnyOneOfWalker = new OpenApiWalker(selectFirstAnyOneOfVisitor);
        selectFirstAnyOneOfWalker.Walk(document);

        var allOfPropertiesRetrievalVisitor = new AllOfPropertiesRetrievalVisitor();
        var allOfPropertiesRetrievalWalker = new OpenApiWalker(allOfPropertiesRetrievalVisitor);
        allOfPropertiesRetrievalWalker.Walk(document);

        var mappingCleanupVisitor = new MappingCleanupVisitor(document);
        var mappingCleanupWalker = new OpenApiWalker(mappingCleanupVisitor);
        mappingCleanupWalker.Walk(document);
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
        foreach (var path in doc.Paths.Where(static path => path.Value.Operations is { Count: > 0 }))
        {
            var key = string.IsNullOrEmpty(basePath)
                ? path.Key
                : $"{basePath}/{path.Key.TrimStart(KiotaBuilder.ForwardSlash)}";
            requestUrls[key] = path.Value.Operations!.Keys.Select(static key => key.ToString().ToUpperInvariant()).ToList();
        }

        if (requestUrls.Count == 0)
            throw new InvalidOperationException("No paths found in the OpenAPI document.");

        var predicate = OpenApiFilterService.CreatePredicate(requestUrls: requestUrls, source: doc);
        return OpenApiFilterService.CreateFilteredDocument(doc, predicate);
    }

    private PluginManifestDocument GetManifestDocument(string openApiDocumentPath)
    {
        var (runtimes, functions, conversationStarters) = GetRuntimesFunctionsAndConversationStartersFromTree(OAIDocument, Configuration.PluginAuthInformation, TreeNode, openApiDocumentPath, Logger);
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

        if (openApiInfo.Extensions is not null)
        {
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
        }

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
        string openApiDocumentPath, ILogger<KiotaBuilder> logger)
    {
        var runtimes = new List<OpenApiRuntime>();
        var functions = new List<Function>();
        var conversationStarters = new List<ConversationStarter>();
        var configAuth = authInformation?.ToPluginManifestAuth();
        if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) && pathItem.Operations is not null)
        {
            foreach (var operation in pathItem.Operations.Values.Where(static x => !string.IsNullOrEmpty(x.OperationId)))
            {
                var auth = configAuth;

                try
                {
                    // Priority order: operation security > document security > empty list
                    var securityToUse = operation.Security?.Count > 0 ? operation.Security :
                                    document.Security?.Count > 0 ? document.Security :
                                    new List<OpenApiSecurityRequirement>();
                    auth = configAuth ?? GetAuth(securityToUse);
                }
                catch (UnsupportedSecuritySchemeException e)
                {
                    auth = new AnonymousAuth();
                    logger.LogWarning("Authentication warning: {OperationId} - {Message}", operation.OperationId, e.Message);
                }

                runtimes.Add(new OpenApiRuntime
                {
                    // Configuration overrides document information
                    Auth = auth,
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
                    Capabilities = GetFunctionCapabilitiesFromOperation(operation),

                });
                conversationStarters.Add(new ConversationStarter
                {
                    Text = !string.IsNullOrEmpty(summary) ? summary : description
                });

            }
        }

        foreach (var node in currentNode.Children)
        {
            var (childRuntimes, childFunctions, childConversationStarters) = GetRuntimesFunctionsAndConversationStartersFromTree(document, authInformation, node.Value, openApiDocumentPath, logger);
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
            throw new UnsupportedSecuritySchemeException(tooManySchemesError);
        }
        var security = securityRequirements.FirstOrDefault();
        var opSecurity = security?.Keys.FirstOrDefault();
        return (opSecurity is null || opSecurity.UnresolvedReference) ? new AnonymousAuth() : GetAuthFromSecurityScheme(opSecurity);
    }

    private static Auth GetAuthFromSecurityScheme(OpenApiSecuritySchemeReference securityScheme)
    {
        var name = securityScheme.Reference.Id;
        string? authenticationReferenceId = null;

        if (securityScheme.Extensions is not null && securityScheme.Extensions.TryGetValue(OpenApiAiAuthReferenceIdExtension.Name, out var authReferenceIdExtension) && authReferenceIdExtension is OpenApiAiAuthReferenceIdExtension authReferenceId)
            authenticationReferenceId = authReferenceId.AuthenticationReferenceId;

        return securityScheme.Type switch
        {
            SecuritySchemeType.ApiKey => new ApiKeyPluginVault
            {
                ReferenceId = string.IsNullOrEmpty(authenticationReferenceId) ? $"{{{name}_REGISTRATION_ID}}" : authenticationReferenceId
            },
            // Only Http bearer is supported
            SecuritySchemeType.Http when "bearer".Equals(securityScheme.Scheme, StringComparison.OrdinalIgnoreCase) =>
                new ApiKeyPluginVault
                {
                    ReferenceId = string.IsNullOrEmpty(authenticationReferenceId) ? $"{{{name}_REGISTRATION_ID}}" : authenticationReferenceId
                },
            SecuritySchemeType.OpenIdConnect => new ApiKeyPluginVault
            {
                ReferenceId = string.IsNullOrEmpty(authenticationReferenceId) ? $"{{{name}_REGISTRATION_ID}}" : authenticationReferenceId
            },
            SecuritySchemeType.OAuth2 => new OAuthPluginVault
            {
                ReferenceId = string.IsNullOrEmpty(authenticationReferenceId) ? $"{{{name}_CONFIGURATION_ID}}" : authenticationReferenceId
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

    private static FunctionCapabilities? GetFunctionCapabilitiesFromOperation(OpenApiOperation openApiOperation)
    {
        var responseSemantics = GetResponseSemanticsFromAdaptiveCardExtension(openApiOperation, OpenApiAiAdaptiveCardExtension.Name);
        if (responseSemantics is null)
            return null;
        return new FunctionCapabilities
        {
            ResponseSemantics = responseSemantics,
        };
    }

    private static ResponseSemantics? GetResponseSemanticsFromAdaptiveCardExtension(OpenApiOperation openApiOperation, string extensionName)
    {
        if (openApiOperation.Extensions is not null &&
            openApiOperation.Extensions.TryGetValue(extensionName, out var adaptiveCardExtension) && adaptiveCardExtension is OpenApiAiAdaptiveCardExtension adaptiveCard)
        {
            JsonNode node = new JsonObject();
            node["file"] = JsonValue.Create(adaptiveCard.File);
            using JsonDocument doc = JsonDocument.Parse(node.ToJsonString());
            JsonElement staticTemplate = doc.RootElement.Clone();
            return new ResponseSemantics
            {
                DataPath = adaptiveCard.DataPath,
                StaticTemplate = staticTemplate,
            };
        }

        return null;
    }
}
