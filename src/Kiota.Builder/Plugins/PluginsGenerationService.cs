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
using Microsoft.OpenApi;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Plugins;

public partial class PluginsGenerationService
{
    private static readonly OpenAPIRuntimeComparer _openAPIRuntimeComparer = new();
    private const string ManifestFileExt = ".json";
    private const string DescriptionFileExt = ".yml";

    /// <summary>
    /// Prefix used for identifying multiple OpenAPI files in a sequence, for example: openapi.agentname.actionname-partial-1-2.json
    /// </summary>
    private const string MultipleFilesPrefix = "-partial-";

    /// <summary>
    /// Regular expression pattern used to identify multiple OpenAPI files in a sequence.
    /// For example: openapi.agentname.actionname-partial-1-2.yaml
    /// </summary>
    private const string MultipleFilesPattern = MultipleFilesPrefix + @"(\d+)-(\d+)\.";

    private OpenApiDocument OAIDocument; // Can not be readonly anymore because of the GenerateMultipleManifestsAsync method
    private OpenApiUrlTreeNode TreeNode; // Can not be readonly anymore because of the GenerateMultipleManifestsAsync method
    private readonly GenerationConfiguration Configuration;
    private readonly string WorkingDirectory;
    private readonly ILogger<KiotaBuilder> Logger;
    internal OpenApiDocumentDownloadService? DownloadService
    {
        get; set;
    }

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

    public async Task<Dictionary<PluginType, string>> GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting GenerateManifestAsync");

        // 1. cleanup any namings to be used later on.
        Configuration.ClientClassName = SanitizeClientClassName(); //drop any special characters

        // 2. write the OpenApi description
        Logger.LogDebug("Writing OpenAPI description.");
        var descriptionRelativePath = $"{Configuration.ClientClassName.ToLowerInvariant()}-openapi{Configuration.FileNameSuffix}{DescriptionFileExt}";
        var descriptionFullPath = Path.Combine(Configuration.OutputPath, descriptionRelativePath);
        EnsureOutputDirectoryExists(descriptionFullPath);

        await WriteOpenApiDescriptionAsync(descriptionFullPath, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("OpenAPI description written to {DescriptionPath}.", descriptionFullPath);

        // Step 3: Generate Plugin Manifests and collect paths
        var manifestPaths = new Dictionary<PluginType, string>();
        foreach (var pluginType in Configuration.PluginTypes)
        {
            Logger.LogDebug("Generating plugin manifest for plugin type: {PluginType}.", pluginType);
            var manifestOutputPath = GetManifestOutputPath(Configuration, pluginType);

            await GeneratePluginManifestAsync(pluginType, descriptionRelativePath, manifestOutputPath, cancellationToken).ConfigureAwait(false);
            Logger.LogInformation("Plugin manifest generated for {PluginType} at {ManifestPath}.", pluginType, manifestOutputPath);

            manifestPaths[pluginType] = manifestOutputPath;
        }

        return manifestPaths;
    }
    internal string GetManifestOutputPath(GenerationConfiguration configuration, PluginType pluginType)
    {
        var manifestFileName = $"{configuration.ClientClassName.ToLowerInvariant()}-{pluginType.ToString().ToLowerInvariant()}{configuration.FileNameSuffix}{ManifestFileExt}";
        var manifestOutputPath = Path.Combine(configuration.OutputPath, manifestFileName);

        return manifestOutputPath;
    }

    internal string GetFileNameSuffixForMultipleFiles(uint fileNumber, uint filesCount)
    {
        // throw ArgumentException if either parameter is negative
        if (fileNumber == 0)
            throw new ArgumentException($"The file number {fileNumber} is invalid. It should be greater than 0.");
        if (filesCount == 0)
            throw new ArgumentException($"The files count {filesCount} is invalid. It should be greater than 0.");

        return $"{MultipleFilesPrefix}{fileNumber}-{filesCount}";
    }

    /// <summary>
    /// Generates multiple plugin manifests based on the OpenAPI document and configuration.
    /// This method processes the primary OpenAPI file and additional files if the configuration specifies multiple files by following the naming convention <see cref="MultipleFilesPattern"/>."/>
    /// It ensures that each file is downloaded, processed, and its corresponding manifest is generated using <see cref="GenerateManifestAsync"/>.
    /// Only the APIPlugin type is processed in this method, other types are ignored.
    /// </summary>
    /// <param name="downloadService">The service used to download OpenAPI documents.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The result is a list of file paths to the generated API plugin manifests.</returns>
    private async Task<List<string>> GenerateMultipleManifestsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting GenerateMultipleManifestsAsync");
        var generatedApiPluginManifestPaths = new List<string>();
        string originalClientClassName = Configuration.ClientClassName;
        string originalFilePath = Configuration.OpenAPIFilePath;
        uint fileNumber = 1;
        uint filesCount = 1;

        if (TryMatchMultipleFilesRequest(originalFilePath, out fileNumber, out filesCount) && filesCount > 1)
        {
            // Set the file name suffix so that the generated manifest file names follow the naming convention
            Configuration.FileNameSuffix = GetFileNameSuffixForMultipleFiles(fileNumber, filesCount);
        }
        Logger.LogInformation("Number of files: {FileNumber} out of {FilesCount} extracted from {FileName}", fileNumber, filesCount, originalFilePath);

        // Generate the first manifest
        var manifestPaths = await GenerateManifestAsync(cancellationToken).ConfigureAwait(false);
        if (manifestPaths.TryGetValue(PluginType.APIPlugin, out var apiPluginManifestPath))
        {
            generatedApiPluginManifestPaths.Add(apiPluginManifestPath);
        }

        // Check if the file path matches the pattern for multiple OpenAPI files
        if (filesCount < 2)
        {
            Logger.LogInformation("No multiple files to process. Only one file will be processed: {Path}", originalFilePath);
            return generatedApiPluginManifestPaths;
        }

        // We are only processing API plugins below and ignoring any other plugin types.
        if (!Configuration.PluginTypes.Contains(PluginType.APIPlugin))
        {
            Logger.LogWarning("Skipping APIPlugin generation as it is not included in the plugin types: {PluginTypes}", Configuration.PluginTypes);
            return generatedApiPluginManifestPaths;
        }

        ArgumentNullException.ThrowIfNull(DownloadService, nameof(DownloadService));
        // Generate manifests for all files
        for (++fileNumber; fileNumber <= filesCount; fileNumber++)
        {
            // Prepare the context for the next file to follow the naming convention
            await PrepareContextForNextFileAsync(DownloadService, originalFilePath, fileNumber, filesCount, cancellationToken).ConfigureAwait(false);

            // Generate the manifest for the new file
            manifestPaths = await GenerateManifestAsync(cancellationToken).ConfigureAwait(false);
            if (manifestPaths.TryGetValue(PluginType.APIPlugin, out apiPluginManifestPath))
            {
                generatedApiPluginManifestPaths.Add(apiPluginManifestPath);
            }

        }

        return generatedApiPluginManifestPaths;
    }

    /// <summary>
    /// Extracts the first partial file name from the given file path by replacing the sequence number with "1".
    /// </summary>
    /// <param name="originalFilePath">The original file path containing the sequence number.</param>
    /// <returns>The updated file path with the sequence number replaced by "1".</returns>
    /// <exception cref="ArgumentException">Thrown when the provided file path is null or empty.</exception>
    internal static string GetFirstPartialFileName(string originalFilePath)
    {
        if (string.IsNullOrEmpty(originalFilePath))
            throw new ArgumentException("The file path cannot be null or empty.", nameof(originalFilePath));

        string result = Regex.Replace(
            originalFilePath,
            MultipleFilesPattern,
            match => $"{MultipleFilesPrefix}1-{match.Groups[2].Value}.",
            RegexOptions.IgnoreCase
        );
        return result;
    }

    /// <summary>
    /// Checks if the provided file path matches the pattern for multiple OpenAPI files
    /// and extracts the total number of files in the sequence.
    /// </summary>
    /// <param name="originalFilePath">The file path to check against the multiple files pattern.</param>
    /// <param name="filesCount">The total number of files in the sequence if the pattern matches.</param>
    /// <returns>True if the file path matches the multiple files pattern; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="originalFilePath"/> is null.</exception>
    internal bool TryMatchMultipleFilesRequest(string originalFilePath, out uint fileNumber, out uint filesCount)
    {
        ArgumentNullException.ThrowIfNull(originalFilePath, nameof(originalFilePath));
        fileNumber = filesCount = 0;

        // Check if the file path matches the pattern for multiple OpenAPI files
        var multipleFilesRequestMatch = Regex.Match(originalFilePath, MultipleFilesPattern, RegexOptions.IgnoreCase);
        if (!multipleFilesRequestMatch.Success)
        {
            return false;
        }

        // Validate both numbers from the regular expression
        if (!uint.TryParse(multipleFilesRequestMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out fileNumber) ||
            !uint.TryParse(multipleFilesRequestMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture, out filesCount))
        {
            // Return false if any of these two numbers could not have been parsed
            return false;
        }

        return true;
    }

    /// <summary>
    /// Generates and merges plugin manifests based on the OpenAPI document and configuration.
    /// This method processes multiple OpenAPI files if specified, generates their respective manifests,
    /// and merges them into a single manifest file.
    /// </summary>
    /// <param name="downloadService">The service used to download OpenAPI documents.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The result is a list of file paths to the generated manifests and the merged plugin manifest (as the last element). 
    /// The merged manifest file will have the suffix defined by <see cref="MergedManifestFileSuffix"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no plugin manifests are generated.</exception>
    public async Task<List<string>> GenerateAndMergeMultipleManifestsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting GenerateAndMergeManifestsAsync");

        // Get the main plugin manifest output path before generating the manifests, as we need FileNameSuffix not to be set in GenerateMultipleManifestsAsync() for the output manifest path here
        var mainPluginManifestOutputPath = GetManifestOutputPath(Configuration, PluginType.APIPlugin);
        Logger.LogInformation("Main plugin manifest output path: {MainPluginManifestOutputPath}", mainPluginManifestOutputPath);

        var manifestPaths = await GenerateMultipleManifestsAsync(cancellationToken).ConfigureAwait(false);
        if (manifestPaths.Count == 0)
            throw new InvalidOperationException("No plugin manifests were generated.");
        if (manifestPaths.Count == 1)
        {
            Logger.LogInformation("Only one plugin manifest was generated. No merging required.");
            return manifestPaths;
        }

        // Assign the first manifest path to a local variable mainManifest
        var mainManifestPath = manifestPaths[0];

        // Read the main manifest content
        var mainPluginManifestDocument = await ReadManifestContentAsync(mainManifestPath, cancellationToken).ConfigureAwait(false);

        // for each manifest path, read the content and merge it into the main manifest

        for (int manifestIndex = 1; manifestIndex < manifestPaths.Count; manifestIndex++) // Skip the first one as it's already read
        {
            var manifestPath = manifestPaths[manifestIndex];
            var pluginManifestDocument = await ReadManifestContentAsync(manifestPath, cancellationToken).ConfigureAwait(false);

            // Merge the data into the main manifest
            MergeConversationStarters(mainPluginManifestDocument, pluginManifestDocument);
            MergeFunctions(mainPluginManifestDocument, pluginManifestDocument, manifestIndex);
        }

        // Write the merged manifest to a new file
        Logger.LogInformation("Writing merged plugin manifest to {ManifestPath}", mainPluginManifestOutputPath);
        await SavePluginManifestAsync(mainPluginManifestOutputPath, mainPluginManifestDocument).ConfigureAwait(false);
        manifestPaths.Add(mainPluginManifestOutputPath);

        return manifestPaths;
    }

    internal static async Task SavePluginManifestAsync(string manifestPath, PluginManifestDocument pluginManifestDocument)
    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var fileStream = File.Create(manifestPath, 4096);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true });
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        pluginManifestDocument.Write(writer);
    }

    /// <summary>
    /// Merges conversation starters from the provided plugin manifest document into the main plugin manifest document.
    /// </summary>
    /// <param name="mainPluginManifestDocument">The main plugin manifest document where conversation starters will be merged.</param>
    /// <param name="pluginManifestDocument">The plugin manifest document containing conversation starters to merge.</param>
    internal void MergeConversationStarters(PluginManifestDocument mainPluginManifestDocument, PluginManifestDocument pluginManifestDocument)
    {
        if (pluginManifestDocument.Capabilities?.ConversationStarters != null)
        {
            foreach (var conversationStarter in pluginManifestDocument.Capabilities.ConversationStarters)
            {
                // Initialize the conversation starters if null
                mainPluginManifestDocument.Capabilities ??= new Capabilities();
                mainPluginManifestDocument.Capabilities.ConversationStarters ??= new List<ConversationStarter>();
                if (!mainPluginManifestDocument.Capabilities.ConversationStarters.Any(cs => cs.Text == conversationStarter.Text))
                {
                    mainPluginManifestDocument.Capabilities.ConversationStarters.Add(conversationStarter);
                }
            }
        }
    }

    /// <summary>
    /// Merges functions from the provided plugin manifest document into the main plugin manifest document.
    /// This method ensures that functions are uniquely named to avoid conflicts and updates runtimes accordingly.
    /// </summary>
    /// <param name="mainPluginManifestDocument">The main plugin manifest document where functions will be merged.</param>
    /// <param name="pluginManifestDocument">The plugin manifest document containing functions to merge.</param>
    /// <param name="manifestIndex">The index of the current manifest being processed, used for renaming functions to avoid conflicts.</param>
    internal void MergeFunctions(PluginManifestDocument mainPluginManifestDocument, PluginManifestDocument pluginManifestDocument, int manifestIndex)
    {
        // return if runtimes is null
        if (pluginManifestDocument.Runtimes == null)
            return;
        mainPluginManifestDocument.Functions ??= new List<Function>();

        // Reference all functions in the original runtimes in the main document explicitly
        foreach (var runtime in mainPluginManifestDocument.Runtimes)
        {
            // Initialize the list of functions to run for if it's null or empty
            runtime.RunForFunctions ??= new List<string>();
            if (!runtime.RunForFunctions.Any())
            {
                // Add all current functions in the first manifest if not already referenced
                runtime.RunForFunctions.AddRange(
                    mainPluginManifestDocument.Functions
                        .Where(function => !runtime.RunForFunctions.Contains(function.Name))
                        .Select(function => function.Name)
                );
            }
        }

        // Merge runtimes and referenced functions
        foreach (var runtime2 in pluginManifestDocument.Runtimes)
        {
            // We need to process either explicitly defined functions or all functions
            runtime2.RunForFunctions ??= new List<string>();
            var isExplicit = runtime2.RunForFunctions?.Count > 0;
            var functionNamesToProcess = isExplicit ? runtime2.RunForFunctions : pluginManifestDocument.Functions?.Select(f => f.Name).ToList();
            if (functionNamesToProcess == null || pluginManifestDocument.Functions == null)
            {
                // log warning and continue
                Logger.LogWarning("No functions to process in the plugin manifest runtime {Runtime}", runtime2);
                continue;
            }
            Logger.LogDebug("Adding {FunctionCount} functions: {FunctionNames}", functionNamesToProcess.Count, string.Join(", ", functionNamesToProcess!));

            // Add all functions referenced from the runtime
            for (int i = 0; i < functionNamesToProcess.Count; i++)
            {
                var functionName = functionNamesToProcess[i];
                var function = pluginManifestDocument.Functions.Single(f => f.Name == functionName);
                // Rename the function when adding to the main manifest document to prevent naming conflicts if needed
                var existingFunction = mainPluginManifestDocument.Functions.FirstOrDefault(f => f.Name == functionName);
                if (existingFunction != null)
                {
                    var newName = $"{functionName}_{manifestIndex + 1}";
                    Logger.LogDebug("Renaming function {FunctionName} to {NewName} in manifest {ManifestIndex}", functionName, newName, manifestIndex);
                    function.Name = newName;
                }
                mainPluginManifestDocument.Functions.Add(function);
                // Change the function name in the runtime
                if (isExplicit)
                {
                    // Replace the function name in the runtime
                    runtime2.RunForFunctions![i] = function.Name;
                }
                else
                {
                    // Add the function name to the runtime to make it explicitly referenced so it works with other runtimes
                    runtime2.RunForFunctions!.Add(function.Name);
                }
                // Add the runtime itself into the main manifest
                mainPluginManifestDocument.Runtimes.Add(runtime2);
            }
        }
    }

    private static async Task<PluginManifestDocument> ReadManifestContentAsync(string manifestPath, CancellationToken cancellationToken)
    {
        var manifestContent = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var jsonDocument = JsonDocument.Parse(manifestContent);
        var documentValidationResults = PluginManifestDocument.Load(jsonDocument.RootElement);

        // Throw exception if the manifest is not valid
        if (!documentValidationResults.IsValid || documentValidationResults.Document is null)
            throw new InvalidOperationException($"The manifest at {manifestPath} is not valid. Issues found: {documentValidationResults.Problems}");

        return documentValidationResults.Document!;
    }

    private async Task PrepareContextForNextFileAsync(OpenApiDocumentDownloadService downloadService, string originalFilePath, uint fileNumber, uint filesCount, CancellationToken cancellationToken)
    {
        Configuration.FileNameSuffix = GetFileNameSuffixForMultipleFiles(fileNumber, filesCount);
        Configuration.OpenAPIFilePath = GetNextFilePath(originalFilePath, fileNumber);
        Logger.LogInformation("Processing file {FileNumber} with FileNameSuffix: {FileNameSuffix}, OpenAPIFilePath: {OpenAPIFilePath}", fileNumber, Configuration.FileNameSuffix, Configuration.OpenAPIFilePath);

        // Now, we need to update the status to process the next file
        var (openAPIDocumentStream, _) = await downloadService.LoadStreamAsync(Configuration.OpenAPIFilePath, Configuration, null, false, cancellationToken).ConfigureAwait(false);
        var openApiDocument = await downloadService.GetDocumentFromStreamAsync(openAPIDocumentStream, Configuration, false, cancellationToken).ConfigureAwait(false);
        if (openApiDocument is null)
            throw new InvalidOperationException($"Failed to load OpenAPI document from {Configuration.OpenAPIFilePath}");
        KiotaBuilder.CleanupOperationIdForPlugins(openApiDocument);
        var urlTreeNode = OpenApiUrlTreeNode.Create(openApiDocument, Constants.DefaultOpenApiLabel);

        // Update the fields to reflect on the new file
        this.OAIDocument = openApiDocument;
        this.TreeNode = urlTreeNode;
    }

    /// <summary>
    /// Generates the next file path based on the original file path.
    /// This method is used to handle multiple OpenAPI files by following a specific naming convention <see cref="MultipleFilesPrefix" />
    /// </summary>
    /// <param name="originalFilePath">The original file path of the OpenAPI document, which is used to derive the next file's path.</param>
    /// <param name="fileNumber">The number of the file to process next, starting from 2 for subsequent files.</param>
    /// <returns>The updated OpenAPI file path for the next file.</returns>
    /// <exception cref="ArgumentException">Thrown if the original client class name or file path is null or empty, or if the file path does not contain the required prefix.</exception>
    internal string GetNextFilePath(string originalFilePath, uint fileNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalFilePath, nameof(originalFilePath));

        // Check that originalFilePath contains the expected prefix
        if (!originalFilePath.Contains(MultipleFilesPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The originalFilePath '{originalFilePath}' does not contain the prefix '{MultipleFilesPrefix}'.");

        var updatedOpenAPIFilePath = Regex.Replace(originalFilePath, $"{MultipleFilesPrefix}1-", $"{MultipleFilesPrefix}{fileNumber}-", RegexOptions.IgnoreCase);

        return updatedOpenAPIFilePath;
    }

    internal string SanitizeClientClassName()
    {
        return PluginNameCleanupRegex().Replace(Configuration.ClientClassName, string.Empty);
    }

    internal void EnsureOutputDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private async Task WriteOpenApiDescriptionAsync(string descriptionFullPath, CancellationToken cancellationToken)
    {
        var trimmedPluginDocument = GetDocumentWithTrimmedComponentsAndResponses(OAIDocument);
        PrepareDescriptionForCopilot(trimmedPluginDocument);

        // trimming a second time to remove any components that are no longer used after the inlining
        trimmedPluginDocument = GetDocumentWithTrimmedComponentsAndResponses(trimmedPluginDocument);
        trimmedPluginDocument.Info.Title = trimmedPluginDocument.Info.Title?[..^9]; // Remove the second ` - Subset` suffix

        trimmedPluginDocument = GetDocumentWithDefaultResponses(trimmedPluginDocument);
        // Ensure reference_id extension value is written according to the plugin auth
        EnsureSecuritySchemeExtensions(trimmedPluginDocument);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var descriptionStream = File.Create(descriptionFullPath, 4096);
        await using var fileWriter = new StreamWriter(descriptionStream);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        // Write the OpenAPI description to the file
        var descriptionWriter = new OpenApiYamlWriter(fileWriter, new() { InlineLocalReferences = true, InlineExternalReferences = true });
        trimmedPluginDocument.SerializeAsV3(descriptionWriter);
        await descriptionWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private ApiManifestDocument CreateApiManifestDocument(string descriptionRelativePath)
    {
        var apiManifest = new ApiManifestDocument("application"); // TODO: Add application name
        // pass empty config hash so that its not included in this manifest.
        apiManifest.ApiDependencies[Configuration.ClientClassName] = Configuration.ToApiDependency(
            string.Empty,
            TreeNode?.GetRequestInfo().ToDictionary(static x => x.Key, static x => x.Value) ?? [],
            WorkingDirectory
        );

        var publisherName = string.IsNullOrEmpty(OAIDocument.Info?.Contact?.Name)
            ? DefaultContactName
            : OAIDocument.Info.Contact.Name;
        var publisherEmail = string.IsNullOrEmpty(OAIDocument.Info?.Contact?.Email)
            ? DefaultContactEmail
            : OAIDocument.Info.Contact.Email;

        apiManifest.Publisher = new Publisher(publisherName, publisherEmail);
        return apiManifest;
    }

    private async Task GeneratePluginManifestAsync(PluginType pluginType, string descriptionRelativePath, string manifestOutputPath, CancellationToken cancellationToken)
    {
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
                var apiManifest = CreateApiManifestDocument(descriptionRelativePath);
                apiManifest.Write(writer);
                break;
            case PluginType.OpenAI:
                // OpenAI plugins are no longer supported.
                break;
            default:
                throw new NotImplementedException($"The {pluginType} plugin is not implemented.");
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? GetExistingReferenceId(IOpenApiSecurityScheme schema)
    {
        if (schema.Extensions is not null
            && schema.Extensions.TryGetValue(OpenApiAiAuthReferenceIdExtension.Name, out var authReferenceIdExtension)
            && authReferenceIdExtension is OpenApiAiAuthReferenceIdExtension authReferenceId)
            return authReferenceId.AuthenticationReferenceId;
        return null;
    }

    private static void EnsureSecuritySchemeExtensions(OpenApiDocument document)
    {
        var securitySchemes = document?.Components?.SecuritySchemes;
        if (securitySchemes is null)
            return;
        foreach (var securitySchemeItem in securitySchemes)
        {
            if (securitySchemeItem.Value is not { Extensions: not null } securityScheme)
                continue;
            if (GetExistingReferenceId(securityScheme) is null
                && TryGetAuthFromSecurityScheme(securitySchemeItem.Key, securityScheme) is Auth auth)
            {
                var authReferenceExtension = new OpenApiAiAuthReferenceIdExtension
                {
                    AuthenticationReferenceId = auth.GetReferenceId()
                };
                securityScheme.Extensions[OpenApiAiAuthReferenceIdExtension.Name] = authReferenceExtension;
            }
        }
    }

    private sealed class MappingCleanupVisitor(OpenApiDocument openApiDocument) : OpenApiVisitorBase
    {
        private readonly OpenApiDocument _document = openApiDocument;

        public override void Visit(IOpenApiSchema schema)
        {
            if (schema.Discriminator?.Mapping is null)
                return;
            var keysToRemove = schema.Discriminator
                                    .Mapping
                                    .Where(x => _document.Components?.Schemas is null ||
                                                                    x.Value.Reference.Id is not null &&
                                                                    !_document.Components.Schemas.ContainsKey(x.Value.Reference.Id.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1]))
                                    .Select(static x => x.Key)
                                    .ToArray();
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
        public override void Visit(IOpenApiMediaType mediaType)
        {
            if (mediaType is OpenApiMediaType openApiMediaType)
                openApiMediaType.Schema = GetFirstSchema(mediaType.Schema);
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

    private static OpenApiDocument GetDocumentWithDefaultResponses(OpenApiDocument document)
    {
        if (document.Paths is null || document.Paths.Count == 0) return document;

        foreach (var path in document.Paths)
        {
            if (path.Value.Operations is null) continue;

            foreach (var operation in path.Value.Operations)
            {
                operation.Value.Responses ??= new OpenApiResponses();

                if (operation.Value.Responses.Count == 0)
                {
                    operation.Value.Responses["200"] = new OpenApiResponse
                    {
                        Description = "The request has succeeded.",
                        Content = new Dictionary<string, IOpenApiMediaType>
                        {
                            ["text/plain"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                            }
                        }
                    };
                }
            }
        }
        return document;
    }

    private PluginManifestDocument GetManifestDocument(string openApiDocumentPath)
    {
        var (runtimes, functions, _) = GetRuntimesFunctionsAndConversationStartersFromTree(OAIDocument, Configuration, TreeNode, openApiDocumentPath, Logger);
        var descriptionForHuman = OAIDocument.Info?.Description is string d && !string.IsNullOrEmpty(d) ? d : $"Description for {OAIDocument.Info?.Title}";
        var manifestInfo = ExtractInfoFromDocument(OAIDocument.Info);
        var pluginManifestDocument = new PluginManifestDocument
        {
            Schema = "https://developer.microsoft.com/json-schemas/copilot/plugin/v2.4/schema.json",
            SchemaVersion = "v2.4",
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

    private static (OpenApiRuntime[], Function[], ConversationStarter[]) GetRuntimesFunctionsAndConversationStartersFromTree(OpenApiDocument document, GenerationConfiguration configuration, OpenApiUrlTreeNode currentNode,
        string openApiDocumentPath, ILogger<KiotaBuilder> logger)
    {
        var runtimes = new List<OpenApiRuntime>();
        var functions = new List<Function>();
        var conversationStarters = new List<ConversationStarter>();
        var configAuth = configuration.PluginAuthInformation?.ToPluginManifestAuth();
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
                    Capabilities = GetFunctionCapabilitiesFromOperation(operation, configuration, logger),

                });
                conversationStarters.Add(new ConversationStarter
                {
                    Text = !string.IsNullOrEmpty(summary) ? summary : description
                });

            }
        }

        foreach (var node in currentNode.Children)
        {
            var (childRuntimes, childFunctions, childConversationStarters) = GetRuntimesFunctionsAndConversationStartersFromTree(document, configuration, node.Value, openApiDocumentPath, logger);
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
        return (opSecurity is null || opSecurity.UnresolvedReference) ? new AnonymousAuth() : GetAuthFromSecuritySchemeReference(opSecurity);
    }

    private static Auth GetAuthFromSecuritySchemeReference(OpenApiSecuritySchemeReference securityScheme)
    {
        var name = securityScheme.Reference.Id;
        var auth = TryGetAuthFromSecurityScheme(name, securityScheme);
        if (auth != null)
            return auth;

        throw new UnsupportedSecuritySchemeException(["Bearer Token", "Api Key", "OpenId Connect", "OAuth"],
                $"Unsupported security scheme type '{securityScheme.Type}'.");
    }

    private static Auth? TryGetAuthFromSecurityScheme(string? name, IOpenApiSecurityScheme securityScheme)
    {
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
            SecuritySchemeType.OAuth2 when securityScheme.Flows?.Implicit != null => new AnonymousAuth(),
            SecuritySchemeType.OAuth2 => new OAuthPluginVault
            {
                ReferenceId = string.IsNullOrEmpty(authenticationReferenceId) ? $"{{{name}_REGISTRATION_ID}}" : authenticationReferenceId
            },
            _ => null
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

    private static FunctionCapabilities? GetFunctionCapabilitiesFromOperation(OpenApiOperation openApiOperation, GenerationConfiguration configuration, ILogger<KiotaBuilder> logger)
    {
        var capabilities = GetFunctionCapabilitiesFromCapabilitiesExtension(openApiOperation, OpenApiAiCapabilitiesExtension.Name);
        if (capabilities != null)
        {
            return capabilities;
        }

        var responseSemantics = GetResponseSemanticsFromAdaptiveCardExtension(openApiOperation, OpenApiAiAdaptiveCardExtension.Name);
        if (responseSemantics != null)
        {
            return new FunctionCapabilities
            {
                ResponseSemantics = responseSemantics
            };
        }

        var responseSemanticsFromTemplate = GetResponseSemanticsFromTemplate(openApiOperation, configuration, logger);
        if (responseSemanticsFromTemplate != null)
        {
            return new FunctionCapabilities
            {
                ResponseSemantics = responseSemanticsFromTemplate
            };
        }
        return null;
    }

    private static FunctionCapabilities? GetFunctionCapabilitiesFromCapabilitiesExtension(OpenApiOperation openApiOperation, string extensionName)
    {
        if (openApiOperation.Extensions is not null &&
            openApiOperation.Extensions.TryGetValue(extensionName, out var capabilitiesExtension) &&
            capabilitiesExtension is OpenApiAiCapabilitiesExtension capabilities)
        {
            var functionCapabilities = new FunctionCapabilities();

            // Set ResponseSemantics
            if (capabilities.ResponseSemantics is not null)
            {
                var responseSemantics = new ResponseSemantics();

                responseSemantics.DataPath = capabilities.ResponseSemantics.DataPath;
                if (capabilities.ResponseSemantics.StaticTemplate is not null && capabilities.ResponseSemantics.StaticTemplate is JsonObject staticTemplateObj)
                {
                    using JsonDocument doc = JsonDocument.Parse(staticTemplateObj.ToJsonString());
                    JsonElement staticTemplate = doc.RootElement.Clone();
                    responseSemantics.StaticTemplate = staticTemplate;
                }
                if (capabilities.ResponseSemantics.Properties is not null)
                {
                    responseSemantics.Properties = new ResponseSemanticsProperties
                    {
                        Title = capabilities.ResponseSemantics.Properties.Title,
                        Subtitle = capabilities.ResponseSemantics.Properties.Subtitle,
                        Url = capabilities.ResponseSemantics.Properties.Url,
                        ThumbnailUrl = capabilities.ResponseSemantics.Properties.ThumbnailUrl,
                        InformationProtectionLabel = capabilities.ResponseSemantics.Properties.InformationProtectionLabel,
                        TemplateSelector = capabilities.ResponseSemantics.Properties.TemplateSelector
                    };
                }
                responseSemantics.OAuthCardPath = capabilities.ResponseSemantics.OauthCardPath;
                functionCapabilities.ResponseSemantics = responseSemantics;
            }

            // Set Confirmation
            if (capabilities.Confirmation is not null)
            {
                var confirmation = new Confirmation
                {
                    Type = capabilities.Confirmation.Type,
                    Title = capabilities.Confirmation.Title,
                    Body = capabilities.Confirmation.Body,
                };
                functionCapabilities.Confirmation = confirmation;
            }

            // Set SecurityInfo
            if (capabilities.SecurityInfo is not null)
            {
                var securityInfo = new SecurityInfo
                {
                    DataHandling = capabilities.SecurityInfo.DataHandling,
                };
                functionCapabilities.SecurityInfo = securityInfo;
            }
            return functionCapabilities;
        }

        return null;
    }

    private static ResponseSemantics? GetResponseSemanticsFromAdaptiveCardExtension(OpenApiOperation openApiOperation, string extensionName)
    {
        if (openApiOperation.Extensions is not null &&
            openApiOperation.Extensions.TryGetValue(extensionName, out var adaptiveCardExtension) && adaptiveCardExtension is OpenApiAiAdaptiveCardExtension adaptiveCard)
        {
            // This is a workaround for integration with TypeSpec when passing empty object from adaptiveCardExtension
            if (string.IsNullOrEmpty(adaptiveCard.DataPath) || string.IsNullOrEmpty(adaptiveCard.File) || string.IsNullOrEmpty(adaptiveCard.Title))
            {
                return null;
            }

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

    private static ResponseSemantics? GetResponseSemanticsFromTemplate(OpenApiOperation openApiOperation, GenerationConfiguration configuration, ILogger<KiotaBuilder> logger)
    {
        if (openApiOperation.Responses is null
            || openApiOperation.Responses.Count == 0
            || openApiOperation.OperationId is null
            || !openApiOperation.Responses.TryGetValue("200", out var response)
            || response is null
            || response.Content is null
            || response.Content.Count == 0
            || !response.Content.TryGetValue("application/json", out var mediaType)
            || mediaType.Schema is null)
        {
            return null;
        }

        // This is a workaround for integration with TypeSpec when passing empty object from adaptiveCardExtension
        if (openApiOperation.Extensions is not null &&
    openApiOperation.Extensions.TryGetValue(OpenApiAiAdaptiveCardExtension.Name, out var adaptiveCardExtension) && adaptiveCardExtension is OpenApiAiAdaptiveCardExtension adaptiveCard)
        {
            if (string.IsNullOrEmpty(adaptiveCard.DataPath) || string.IsNullOrEmpty(adaptiveCard.File) || string.IsNullOrEmpty(adaptiveCard.Title))
            {
                return null;
            }
        }

        string functionName = openApiOperation.OperationId;
        string fileName = $"{functionName}.json";
        string staticTemplateJson = $"{{\"file\": \"./adaptiveCards/{fileName}\"}}";
        try
        {
            WriteAdaptiveCardTemplate(configuration, fileName, logger);
            using JsonDocument doc = JsonDocument.Parse(staticTemplateJson);
            JsonElement staticTemplate = doc.RootElement.Clone();
            return new ResponseSemantics()
            {
                DataPath = "$",
                StaticTemplate = staticTemplate
            };
        }
        catch (IOException)
        {

            return null;
        }
    }

    private static void WriteAdaptiveCardTemplate(GenerationConfiguration configuration, string fileName, ILogger<KiotaBuilder> logger)
    {
        var adaptiveCardOutputPath = Path.Combine(configuration.OutputPath, "adaptiveCards", fileName);
        new AdaptiveCardTemplate(logger).Write(adaptiveCardOutputPath);
    }
}
