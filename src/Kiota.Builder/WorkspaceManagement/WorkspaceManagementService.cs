using System;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Extensions;

namespace Kiota.Builder.WorkspaceManagement;

public class WorkspaceManagementService
{
    private readonly bool UseKiotaConfig;
    private readonly ILogger Logger;
    public WorkspaceManagementService(ILogger logger, bool useKiotaConfig = false)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
        UseKiotaConfig = useKiotaConfig;
    }
    private readonly LockManagementService lockManagementService = new();
    private readonly WorkspaceConfigurationStorageService workspaceConfigurationStorageService = new();
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        if (UseKiotaConfig)
        {
            var wsConfig = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false) ??
                            new WorkspaceConfiguration();
            wsConfig.Clients.AddOrReplace(generationConfiguration.ClientClassName, new ApiClientConfiguration(generationConfiguration));
            await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, cancellationToken).ConfigureAwait(false);
            //TODO generate API manifest
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
            await workspaceConfigurationStorageService.RestoreConfigAsync(outputPath, cancellationToken).ConfigureAwait(false);
        else
            await lockManagementService.RestoreLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
    }
    public async Task BackupStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseKiotaConfig)
            await workspaceConfigurationStorageService.BackupConfigAsync(outputPath, cancellationToken).ConfigureAwait(false);
        else
            await lockManagementService.BackupLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> ShouldGenerateAsync(GenerationConfiguration inputConfig, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputConfig);
        if (inputConfig.CleanOutput) return true;
        if (UseKiotaConfig)
        {
            var wsConfig = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            if (wsConfig?.Clients.TryGetValue(inputConfig.ClientClassName, out var existingClientConfig) ?? false)
            {
                var comparer = new ApiClientConfigurationComparer();
                //TODO also compare the api manifest file
                return !comparer.Equals(existingClientConfig, new ApiClientConfiguration(inputConfig));
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
            var comparer = new KiotaLockComparer();
            if (!string.IsNullOrEmpty(existingLock?.KiotaVersion) && !configurationLock.KiotaVersion.Equals(existingLock.KiotaVersion, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("API client was generated with version {ExistingVersion} and the current version is {CurrentVersion}, it will be upgraded and you should upgrade dependencies", existingLock?.KiotaVersion, configurationLock.KiotaVersion);
            }
            return !comparer.Equals(existingLock, configurationLock);
        }

    }
}
