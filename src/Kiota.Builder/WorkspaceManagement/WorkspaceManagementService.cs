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
using AsyncKeyedLock;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Lock;
using Kiota.Builder.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.WorkspaceManagement;

public class WorkspaceManagementService
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
    public async Task<bool> IsClientPresent(string clientName, CancellationToken cancellationToken = default)
    {
        if (!UseKiotaConfig) return false;
        var (wsConfig, _) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        return wsConfig?.Clients.ContainsKey(clientName) ?? false;
    }
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, Dictionary<string, HashSet<string>> templatesWithOperations, Stream descriptionStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        if (UseKiotaConfig)
        {
            var (wsConfig, manifest) = await LoadConfigurationAndManifestAsync(cancellationToken).ConfigureAwait(false);
            var generationClientConfig = new ApiClientConfiguration(generationConfiguration);
            generationClientConfig.NormalizePaths(WorkingDirectory);
            wsConfig.Clients.AddOrReplace(generationConfiguration.ClientClassName, generationClientConfig);
            var inputConfigurationHash = await GetConfigurationHashAsync(generationClientConfig, descriptionHash).ConfigureAwait(false);
            manifest.ApiDependencies.AddOrReplace(generationConfiguration.ClientClassName, generationConfiguration.ToApiDependency(inputConfigurationHash, templatesWithOperations));
            await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
            if (descriptionStream != Stream.Null)
                await descriptionStorageService.UpdateDescriptionAsync(generationConfiguration.ClientClassName, descriptionStream, new Uri(generationConfiguration.OpenAPIFilePath).GetFileExtension(), cancellationToken).ConfigureAwait(false);
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
    private static readonly ApiDependencyComparer apiDependencyComparer = new();
    public async Task<bool> ShouldGenerateAsync(GenerationConfiguration inputConfig, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputConfig);
        if (inputConfig.CleanOutput) return true;
        if (UseKiotaConfig)
        {
            var (wsConfig, apiManifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            if ((wsConfig?.Clients.TryGetValue(inputConfig.ClientClassName, out var existingClientConfig) ?? false) &&
                (apiManifest?.ApiDependencies.TryGetValue(inputConfig.ClientClassName, out var existingApiManifest) ?? false))
            {
                var inputClientConfig = new ApiClientConfiguration(inputConfig);
                inputClientConfig.NormalizePaths(WorkingDirectory);
                var inputConfigurationHash = await GetConfigurationHashAsync(inputClientConfig, descriptionHash).ConfigureAwait(false);
                return !clientConfigurationComparer.Equals(existingClientConfig, inputClientConfig) ||
                       !apiDependencyComparer.Equals(inputConfig.ToApiDependency(inputConfigurationHash, []), existingApiManifest);
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
                Logger.LogWarning("API client was generated with version {ExistingVersion} and the current version is {CurrentVersion}, it will be upgraded and you should upgrade dependencies", existingLock.KiotaVersion, configurationLock.KiotaVersion);
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
    public async Task RemoveClientAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        await RemoveConsumerInternalAsync(clientName,
        wsConfig =>
        {
            if (cleanOutput && wsConfig.Clients.TryGetValue(clientName, out var clientConfig) && Directory.Exists(clientConfig.OutputPath))
                Directory.Delete(clientConfig.OutputPath, true);

            if (!wsConfig.Clients.Remove(clientName))
                throw new InvalidOperationException($"The client {clientName} was not found in the configuration");
        },
        cleanOutput, cancellationToken).ConfigureAwait(false);
    }
    public async Task RemovePluginAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        await RemoveConsumerInternalAsync(clientName,
        wsConfig =>
        {
            if (cleanOutput && wsConfig.Plugins.TryGetValue(clientName, out var pluginConfig) && Directory.Exists(pluginConfig.OutputPath))
                Directory.Delete(pluginConfig.OutputPath, true);

            if (!wsConfig.Plugins.Remove(clientName))
                throw new InvalidOperationException($"The client {clientName} was not found in the configuration");
        },
        cleanOutput, cancellationToken).ConfigureAwait(false);
    }
    private async Task RemoveConsumerInternalAsync(string clientName, Action<WorkspaceConfiguration> consumerRemoval, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        if (!UseKiotaConfig)
            throw new InvalidOperationException("Cannot remove a client in lock mode");
        var (wsConfig, manifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (wsConfig is null)
            throw new InvalidOperationException("Cannot remove a client without a configuration");

        consumerRemoval(wsConfig);

        manifest?.ApiDependencies.Remove(clientName);
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
        descriptionStorageService.RemoveDescription(clientName);
        if (wsConfig.AnyConsumerPresent)
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

    private async Task<string> GetConfigurationHashAsync(ApiClientConfiguration apiClientConfiguration, string descriptionHash)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, apiClientConfiguration, context.ApiClientConfiguration).ConfigureAwait(false);
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
                Logger.LogError("The client {ClientName} is already present in the configuration", generationConfiguration.ClientClassName);
                clientsGenerationConfigurations.Remove(generationConfiguration);
                continue;
            }
            var (stream, _) = await openApiDocumentDownloadService.LoadStreamAsync(generationConfiguration.OpenAPIFilePath, generationConfiguration, null, false, cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var ms = new MemoryStream();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            var document = await openApiDocumentDownloadService.GetDocumentFromStreamAsync(ms, generationConfiguration, false, cancellationToken).ConfigureAwait(false);
            if (document is null)
            {
                Logger.LogError("The client {ClientName} could not be migrated because the OpenAPI document could not be loaded", generationConfiguration.ClientClassName);
                clientsGenerationConfigurations.Remove(generationConfiguration);
                continue;
            }
            generationConfiguration.ApiRootUrl = document.GetAPIRootUrl(generationConfiguration.OpenAPIFilePath);
            ms.Seek(0, SeekOrigin.Begin);
            await descriptionStorageService.UpdateDescriptionAsync(generationConfiguration.ClientClassName, ms, new Uri(generationConfiguration.OpenAPIFilePath).GetFileExtension(), cancellationToken).ConfigureAwait(false);

            var clientConfiguration = new ApiClientConfiguration(generationConfiguration);
            clientConfiguration.NormalizePaths(WorkingDirectory);
            wsConfig.Clients.Add(generationConfiguration.ClientClassName, clientConfiguration);
            var inputConfigurationHash = await GetConfigurationHashAsync(clientConfiguration, "migrated-pending-generate").ConfigureAwait(false);
            // because it's a migration, we don't want to calculate the exact hash since the description might have changed since the initial generation that created the lock file
            apiManifest.ApiDependencies.Add(generationConfiguration.ClientClassName, generationConfiguration.ToApiDependency(inputConfigurationHash, []));
            lockManagementService.DeleteLockFile(Path.Combine(WorkingDirectory, clientConfiguration.OutputPath));
        }
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, apiManifest, cancellationToken).ConfigureAwait(false);
        return clientsGenerationConfigurations.OfType<GenerationConfiguration>().Select(static x => x.ClientClassName);
    }
    private async Task<GenerationConfiguration?> LoadConfigurationFromLockAsync(string clientName, string lockFilePath, CancellationToken cancellationToken)
    {
        if (Path.GetDirectoryName(lockFilePath) is not string lockFileDirectory)
        {
            Logger.LogWarning("The lock file {LockFilePath} is not in a directory, it will be skipped", lockFilePath);
            return null;
        }
        var lockInfo = await lockManagementService.GetLockFromDirectoryAsync(lockFileDirectory, cancellationToken).ConfigureAwait(false);
        if (lockInfo is null)
        {
            Logger.LogWarning("The lock file {LockFilePath} is not valid, it will be skipped", lockFilePath);
            return null;
        }
        var generationConfiguration = new GenerationConfiguration();
        lockInfo.UpdateGenerationConfigurationFromLock(generationConfiguration);
        generationConfiguration.OutputPath = "./" + Path.GetRelativePath(WorkingDirectory, lockFileDirectory);
        if (!string.IsNullOrEmpty(clientName))
        {
            generationConfiguration.ClientClassName = clientName;
        }
        return generationConfiguration;
    }
}
