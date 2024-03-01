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
    private readonly HttpClient HttpClient;
    public WorkspaceManagementService(ILogger logger, HttpClient httpClient, bool useKiotaConfig = false, string workingDirectory = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);
        Logger = logger;
        HttpClient = httpClient;
        UseKiotaConfig = useKiotaConfig;
        if (string.IsNullOrEmpty(workingDirectory))
            workingDirectory = Directory.GetCurrentDirectory();
        WorkingDirectory = workingDirectory;
        workspaceConfigurationStorageService = new(workingDirectory);
        descriptionStorageService = new(workingDirectory);
    }
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
    public async Task<Stream?> GetDescriptionCopyAsync(string clientName, string inputPath, CancellationToken cancellationToken = default)
    {
        if (!UseKiotaConfig)
            return null;
        return await descriptionStorageService.GetDescriptionAsync(clientName, new Uri(inputPath).GetFileExtension(), cancellationToken).ConfigureAwait(false);
    }
    public async Task RemoveClientAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        if (!UseKiotaConfig)
            throw new InvalidOperationException("Cannot remove a client in lock mode");
        var (wsConfig, manifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (wsConfig is null)
            throw new InvalidOperationException("Cannot remove a client without a configuration");

        if (cleanOutput && wsConfig.Clients.TryGetValue(clientName, out var clientConfig) && Directory.Exists(clientConfig.OutputPath))
            Directory.Delete(clientConfig.OutputPath, true);

        if (!wsConfig.Clients.Remove(clientName))
            throw new InvalidOperationException($"The client {clientName} was not found in the configuration");
        manifest?.ApiDependencies.Remove(clientName);
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
        descriptionStorageService.RemoveDescription(clientName);
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
        foreach (var configuration in clientsGenerationConfigurations.ToArray()) //to avoid modifying the collection as we iterate and remove some entries
        {
            var generationClientConfig = new ApiClientConfiguration(configuration);
            generationClientConfig.NormalizePaths(WorkingDirectory);
            if (wsConfig.Clients.ContainsKey(configuration.ClientClassName))
            {
                Logger.LogError("The client {ClientName} is already present in the configuration", configuration.ClientClassName);
                clientsGenerationConfigurations.Remove(configuration);
                continue;
            }
            wsConfig.Clients.Add(configuration.ClientClassName, generationClientConfig);
            var inputConfigurationHash = await GetConfigurationHashAsync(generationClientConfig, "migrated-pending-generate").ConfigureAwait(false);
            // because it's a migration, we don't want to calculate the exact hash since the description might have changed since the initial generation that created the lock file
            apiManifest.ApiDependencies.Add(configuration.ClientClassName, configuration.ToApiDependency(inputConfigurationHash, new()));//TODO get the resolved operations?
            var (stream, _) = await DownloadHelper.LoadStream(configuration.OpenAPIFilePath, HttpClient, Logger, configuration, localFilesLock, null, false, cancellationToken).ConfigureAwait(false);
            await descriptionStorageService.UpdateDescriptionAsync(configuration.ClientClassName, stream, string.Empty, cancellationToken).ConfigureAwait(false);
            lockManagementService.DeleteLockFile(Path.GetDirectoryName(configuration.OpenAPIFilePath)!);
        }
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, apiManifest, cancellationToken).ConfigureAwait(false);
        return clientsGenerationConfigurations.OfType<GenerationConfiguration>().Select(static x => x.ClientClassName);
    }
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
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
        if (!string.IsNullOrEmpty(clientName))
        {
            generationConfiguration.ClientClassName = clientName;
        }
        return generationConfiguration;
    }
}
