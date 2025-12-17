using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Lock;
using Kiota.Builder.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.WorkspaceManagement;

public partial class WorkspaceManagementService
{
    private readonly bool UseKiotaConfig;
    private readonly ILogger Logger;
    public WorkspaceManagementService(ILogger logger, HttpClient httpClient, bool useKiotaConfig = false, string workingDirectory = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);
        Logger = logger;
        UseKiotaConfig = useKiotaConfig;
        if (string.IsNullOrEmpty(workingDirectory))
            workingDirectory = Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
        workspaceConfigurationStorageService = new(workingDirectory);
        descriptionStorageService = new(workingDirectory);
        openApiDocumentDownloadService = new(httpClient, Logger);
    }
    private readonly OpenApiDocumentDownloadService openApiDocumentDownloadService;
    private readonly LockManagementService lockManagementService = new();
    private readonly WorkspaceConfigurationStorageService workspaceConfigurationStorageService;
    private readonly DescriptionStorageService descriptionStorageService;
    public async Task<bool> IsConsumerPresentAsync(string clientName, CancellationToken cancellationToken = default)
    {
        if (!UseKiotaConfig) return false;
        var (wsConfig, _) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        return wsConfig is not null && (wsConfig.Clients.ContainsKey(clientName) || wsConfig.Plugins.ContainsKey(clientName));
    }
    private BaseApiConsumerConfiguration UpdateConsumerConfiguration(GenerationConfiguration generationConfiguration, WorkspaceConfiguration wsConfig)
    {
        if (generationConfiguration.IsPluginConfiguration)
        {
            var generationPluginConfig = new ApiPluginConfiguration(generationConfiguration);
            generationPluginConfig.NormalizeOutputPath(WorkingDirectory);
            generationPluginConfig.NormalizeDescriptionLocation(WorkingDirectory);
            wsConfig.Plugins.AddOrReplace(generationConfiguration.ClientClassName, generationPluginConfig);
            return generationPluginConfig;
        }
        else
        {
            var generationClientConfig = new ApiClientConfiguration(generationConfiguration);
            generationClientConfig.NormalizeOutputPath(WorkingDirectory);
            generationClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
            wsConfig.Clients.AddOrReplace(generationConfiguration.ClientClassName, generationClientConfig);
            return generationClientConfig;
        }
    }
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, Dictionary<string, HashSet<string>> templatesWithOperations, Stream descriptionStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        if (UseKiotaConfig)
        {
            var (wsConfig, manifest) = await LoadConfigurationAndManifestAsync(cancellationToken).ConfigureAwait(false);
            var generationClientConfig = UpdateConsumerConfiguration(generationConfiguration, wsConfig);
            generationClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
            var inputConfigurationHash = await GetConsumerConfigurationHashAsync(generationClientConfig, descriptionHash).ConfigureAwait(false);
            manifest.ApiDependencies.AddOrReplace(generationConfiguration.ClientClassName, generationConfiguration.ToApiDependency(inputConfigurationHash, templatesWithOperations, WorkingDirectory));
            await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
            if (descriptionStream != Stream.Null)
                await descriptionStorageService.UpdateDescriptionAsync(generationConfiguration.ClientClassName, descriptionStream, generationConfiguration.OpenAPIFilePath.GetFileExtension(), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var configurationLock = new KiotaLock(generationConfiguration)
            {
                DescriptionHash = descriptionHash ?? string.Empty,
            };
            await lockManagementService.WriteLockFileAsync(generationConfiguration.OutputPath, configurationLock, cancellationToken).ConfigureAwait(false);
        }
    }
    public async Task RestoreStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseKiotaConfig)
            await workspaceConfigurationStorageService.RestoreConfigAsync(cancellationToken).ConfigureAwait(false);
        else
            await lockManagementService.RestoreLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
    }
    public async Task BackupStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseKiotaConfig)
            await workspaceConfigurationStorageService.BackupConfigAsync(cancellationToken).ConfigureAwait(false);
        else
            await lockManagementService.BackupLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
    }
    private static readonly KiotaLockComparer lockComparer = new();
    private static readonly ApiClientConfigurationComparer clientConfigurationComparer = new();
    private static readonly ApiPluginConfigurationComparer pluginConfigurationComparer = new();
    private static readonly ApiDependencyComparer apiDependencyComparer = new();
    public async Task<bool> ShouldGenerateAsync(GenerationConfiguration inputConfig, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputConfig);
        if (inputConfig.CleanOutput) return true;
        if (UseKiotaConfig)
        {
            var (wsConfig, apiManifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            if (wsConfig is null || apiManifest is null)
                return true;
            if (wsConfig.Clients.TryGetValue(inputConfig.ClientClassName, out var existingClientConfig) &&
                apiManifest.ApiDependencies.TryGetValue(inputConfig.ClientClassName, out var existingApiManifest))
            {
                var inputClientConfig = new ApiClientConfiguration(inputConfig);
                inputClientConfig.NormalizeOutputPath(WorkingDirectory);
                inputClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
                var inputConfigurationHash = await GetConsumerConfigurationHashAsync(inputClientConfig, descriptionHash).ConfigureAwait(false);
                return !clientConfigurationComparer.Equals(existingClientConfig, inputClientConfig) ||
                       !apiDependencyComparer.Equals(inputConfig.ToApiDependency(inputConfigurationHash, [], WorkingDirectory), existingApiManifest);
            }
            if (wsConfig.Plugins.TryGetValue(inputConfig.ClientClassName, out var existingPluginConfig) &&
                apiManifest.ApiDependencies.TryGetValue(inputConfig.ClientClassName, out var existingPluginApiManifest))
            {
                var inputClientConfig = new ApiPluginConfiguration(inputConfig);
                inputClientConfig.NormalizeOutputPath(WorkingDirectory);
                inputClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
                var inputConfigurationHash = await GetConsumerConfigurationHashAsync(inputClientConfig, descriptionHash).ConfigureAwait(false);
                return !pluginConfigurationComparer.Equals(existingPluginConfig, inputClientConfig) ||
                       !apiDependencyComparer.Equals(inputConfig.ToApiDependency(inputConfigurationHash, [], WorkingDirectory), existingPluginApiManifest);
            }
            return true;
        }
        else
        {
            var existingLock = await lockManagementService.GetLockFromDirectoryAsync(inputConfig.OutputPath, cancellationToken).ConfigureAwait(false);
            var configurationLock = new KiotaLock(inputConfig)
            {
                DescriptionHash = descriptionHash,
            };
            if (!string.IsNullOrEmpty(existingLock?.KiotaVersion) && !configurationLock.KiotaVersion.Equals(existingLock.KiotaVersion, StringComparison.OrdinalIgnoreCase))
            {
                LogClientVersionMismatch(existingLock.KiotaVersion, configurationLock.KiotaVersion);
            }
            return !lockComparer.Equals(existingLock, configurationLock);
        }

    }
    public async Task<Stream?> GetDescriptionCopyAsync(string clientName, string inputPath, bool cleanOutput, CancellationToken cancellationToken = default)
    {
        if (!UseKiotaConfig || cleanOutput)
            return null;
        return await descriptionStorageService.GetDescriptionAsync(clientName, new Uri(inputPath).GetFileExtension(), cancellationToken).ConfigureAwait(false);
    }
    public Task RemoveClientAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        return RemoveConsumerInternalAsync(clientName,
            static wsConfig => wsConfig.Clients,
            cleanOutput,
            "client",
            cancellationToken
        );
    }
    public Task RemovePluginAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        return RemoveConsumerInternalAsync(clientName,
            static wsConfig => wsConfig.Plugins,
            cleanOutput,
            "plugin",
            cancellationToken
        );
    }
    private async Task RemoveConsumerInternalAsync<T>(string consumerName, Func<WorkspaceConfiguration, Dictionary<string, T>> consumerRetrieval, bool cleanOutput, string consumerDisplayName, CancellationToken cancellationToken) where T : BaseApiConsumerConfiguration
    {
        if (!UseKiotaConfig)
            throw new InvalidOperationException($"Cannot remove a {consumerDisplayName} in lock mode");
        var (wsConfig, manifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (wsConfig is null)
            throw new InvalidOperationException($"Cannot remove a {consumerDisplayName} without a configuration");

        var consumers = consumerRetrieval(wsConfig);
        if (cleanOutput && consumers.TryGetValue(consumerName, out var consumerConfig) && Directory.Exists(consumerConfig.OutputPath))
            Directory.Delete(consumerConfig.OutputPath, true);

        if (!consumers.Remove(consumerName))
            throw new InvalidOperationException($"The {consumerDisplayName} {consumerName} was not found in the configuration");

        manifest?.ApiDependencies.Remove(consumerName);
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
        descriptionStorageService.RemoveDescription(consumerName);
        if (!wsConfig.AnyConsumerPresent)
            descriptionStorageService.Clean();
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly WorkspaceConfigurationGenerationContext context = new(options);
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private readonly string WorkingDirectory;
    private async Task<string> GetConsumerConfigurationHashAsync<T>(T apiClientConfiguration, string descriptionHash) where T : BaseApiConsumerConfiguration
    {
        using var stream = new MemoryStream();
        if (apiClientConfiguration is ApiClientConfiguration)
            await JsonSerializer.SerializeAsync(stream, apiClientConfiguration, context.ApiClientConfiguration).ConfigureAwait(false);
        else
            await JsonSerializer.SerializeAsync(stream, apiClientConfiguration, context.ApiPluginConfiguration).ConfigureAwait(false);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(descriptionHash)).ConfigureAwait(false);
        stream.Position = 0;
        if (HashAlgorithm.Value is null)
            throw new InvalidOperationException("Hash algorithm is not available");
        return ConvertByteArrayToString(await HashAlgorithm.Value.ComputeHashAsync(stream).ConfigureAwait(false));
    }
    private static string ConvertByteArrayToString(byte[] hash)
    {
        // Build the final string by converting each byte
        // into hex and appending it to a StringBuilder
        var sbLength = hash.Length * 2;
        var sb = new StringBuilder(sbLength, sbLength);
        for (var i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
    private async Task<(WorkspaceConfiguration, ApiManifestDocument)> LoadConfigurationAndManifestAsync(CancellationToken cancellationToken)
    {
        if (!await workspaceConfigurationStorageService.IsInitializedAsync(cancellationToken).ConfigureAwait(false))
            await workspaceConfigurationStorageService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var (wsConfig, apiManifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (wsConfig is null)
            throw new InvalidOperationException("The workspace configuration is not initialized");
        apiManifest ??= new("application"); //TODO get the application name
        return (wsConfig, apiManifest);
    }
    private async Task<List<GenerationConfiguration>> LoadGenerationConfigurationsFromLockFilesAsync(string lockDirectory, string clientName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(lockDirectory);
        if (!UseKiotaConfig)
            throw new InvalidOperationException("Cannot migrate from lock file in kiota config mode");
        if (!Path.IsPathRooted(lockDirectory))
            lockDirectory = Path.Combine(WorkingDirectory, lockDirectory);
        if (Path.GetRelativePath(WorkingDirectory, lockDirectory).StartsWith("..", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The lock directory must be a subdirectory of the working directory");

        var lockFiles = Directory.GetFiles(lockDirectory, LockManagementService.LockFileName, SearchOption.AllDirectories);
        if (lockFiles.Length == 0)
            throw new InvalidOperationException("No lock file found in the specified directory");
        var clientNamePassed = !string.IsNullOrEmpty(clientName);
        if (lockFiles.Length > 1 && clientNamePassed)
            throw new InvalidOperationException("Multiple lock files found in the specified directory and the client name was specified");
        var clientsGenerationConfigurations = new List<GenerationConfiguration?>();
        if (lockFiles.Length == 1)
            clientsGenerationConfigurations.Add(await LoadConfigurationFromLockAsync(clientNamePassed ? clientName : string.Empty, lockFiles[0], cancellationToken).ConfigureAwait(false));
        else
            clientsGenerationConfigurations.AddRange(await Task.WhenAll(lockFiles.Select(x => LoadConfigurationFromLockAsync(string.Empty, x, cancellationToken))).ConfigureAwait(false));
        return clientsGenerationConfigurations.OfType<GenerationConfiguration>().ToList();
    }
    public async Task<IEnumerable<string>> MigrateFromLockFileAsync(string clientName, string lockDirectory, CancellationToken cancellationToken = default)
    {
        var (wsConfig, apiManifest) = await LoadConfigurationAndManifestAsync(cancellationToken).ConfigureAwait(false);

        var clientsGenerationConfigurations = await LoadGenerationConfigurationsFromLockFilesAsync(lockDirectory, clientName, cancellationToken).ConfigureAwait(false);
        foreach (var generationConfiguration in clientsGenerationConfigurations.ToArray()) //to avoid modifying the collection as we iterate and remove some entries
        {

            if (wsConfig.Clients.ContainsKey(generationConfiguration.ClientClassName))
            {
                LogClientAlreadyPresent(generationConfiguration.ClientClassName);
                clientsGenerationConfigurations.Remove(generationConfiguration);
                continue;
            }
            var (stream, _) = await openApiDocumentDownloadService.LoadStreamAsync(generationConfiguration.OpenAPIFilePath, generationConfiguration, null, false, cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var ms = new MemoryStream();
            await using var msForParsing = new MemoryStream();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(msForParsing, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            // OpenAPI.net or STJ disposes the stream, working on a copy avoids a stream disposed exception
            msForParsing.Seek(0, SeekOrigin.Begin);
            var document = await openApiDocumentDownloadService.GetDocumentFromStreamAsync(msForParsing, generationConfiguration, false, cancellationToken).ConfigureAwait(false);
            if (document is null)
            {
                LogClientMigrationFailed(generationConfiguration.ClientClassName);
                clientsGenerationConfigurations.Remove(generationConfiguration);
                continue;
            }
            generationConfiguration.ApiRootUrl = document.GetAPIRootUrl(generationConfiguration.OpenAPIFilePath);
            await descriptionStorageService.UpdateDescriptionAsync(generationConfiguration.ClientClassName, ms, new Uri(generationConfiguration.OpenAPIFilePath).GetFileExtension(), cancellationToken).ConfigureAwait(false);

            var clientConfiguration = new ApiClientConfiguration(generationConfiguration);
            clientConfiguration.NormalizeOutputPath(WorkingDirectory);
            clientConfiguration.NormalizeDescriptionLocation(WorkingDirectory);
            wsConfig.Clients.Add(generationConfiguration.ClientClassName, clientConfiguration);
            var inputConfigurationHash = await GetConsumerConfigurationHashAsync(clientConfiguration, "migrated-pending-generate").ConfigureAwait(false);
            // because it's a migration, we don't want to calculate the exact hash since the description might have changed since the initial generation that created the lock file
            apiManifest.ApiDependencies.Add(
                generationConfiguration.ClientClassName,
                generationConfiguration.ToApiDependency(
                    inputConfigurationHash,
                    new Dictionary<string, HashSet<string>> {
                        { MigrationPlaceholderPath, new HashSet<string> { "GET" } }
                    },
                    WorkingDirectory));
            lockManagementService.DeleteLockFile(Path.Combine(WorkingDirectory, clientConfiguration.OutputPath));
        }
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, apiManifest, cancellationToken).ConfigureAwait(false);
        return clientsGenerationConfigurations.OfType<GenerationConfiguration>().Select(static x => x.ClientClassName);
    }
    internal const string MigrationPlaceholderPath = "/migration-placeholder";
    private async Task<GenerationConfiguration?> LoadConfigurationFromLockAsync(string clientName, string lockFilePath, CancellationToken cancellationToken)
    {
        if (Path.GetDirectoryName(lockFilePath) is not string lockFileDirectory)
        {
            LogLockFileNotInDirectory(lockFilePath);
            return null;
        }
        var lockInfo = await lockManagementService.GetLockFromDirectoryAsync(lockFileDirectory, cancellationToken).ConfigureAwait(false);
        if (lockInfo is null)
        {
            LogLockFileInvalid(lockFilePath);
            return null;
        }
        var generationConfiguration = new GenerationConfiguration();
        lockInfo.UpdateGenerationConfigurationFromLock(generationConfiguration);
        generationConfiguration.OutputPath = "./" + Path.GetRelativePath(WorkingDirectory, lockFileDirectory).NormalizePathSeparators();
        if (!string.IsNullOrEmpty(clientName))
        {
            generationConfiguration.ClientClassName = clientName;
        }
        return generationConfiguration;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "API client was generated with version {ExistingVersion} and the current version is {CurrentVersion}, it will be upgraded and you should upgrade dependencies")]
    private partial void LogClientVersionMismatch(string? existingVersion, string currentVersion);

    [LoggerMessage(Level = LogLevel.Error, Message = "The client {ClientName} is already present in the configuration")]
    private partial void LogClientAlreadyPresent(string clientName);

    [LoggerMessage(Level = LogLevel.Error, Message = "The client {ClientName} could not be migrated because the OpenAPI document could not be loaded")]
    private partial void LogClientMigrationFailed(string clientName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The lock file {LockFilePath} is not in a directory, it will be skipped")]
    private partial void LogLockFileNotInDirectory(string lockFilePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The lock file {LockFilePath} is not valid, it will be skipped")]
    private partial void LogLockFileInvalid(string lockFilePath);
}
