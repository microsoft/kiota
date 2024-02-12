using System;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Microsoft.Extensions.Logging;

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
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, CancellationToken cancellationToken = default)
    {
        if (UseKiotaConfig) throw new NotImplementedException("Not implemented yet");
        var configurationLock = new KiotaLock(generationConfiguration)
        {
            DescriptionHash = descriptionHash ?? string.Empty,
        };
        await lockManagementService.WriteLockFileAsync(generationConfiguration.OutputPath, configurationLock, cancellationToken).ConfigureAwait(false);
    }
    public async Task RestoreStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseKiotaConfig) throw new NotImplementedException("Not implemented yet");
        await lockManagementService.RestoreLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
    }
    public async Task BackupStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseKiotaConfig) throw new NotImplementedException("Not implemented yet");
        await lockManagementService.BackupLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> ShouldGenerateAsync(GenerationConfiguration inputConfig, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputConfig);
        if (UseKiotaConfig) throw new NotImplementedException("Not implemented yet");
        if (inputConfig.CleanOutput) return true;
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
