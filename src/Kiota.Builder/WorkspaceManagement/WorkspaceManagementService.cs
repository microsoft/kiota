using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
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
    public WorkspaceManagementService(ILogger logger, bool useKiotaConfig = false)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
        UseKiotaConfig = useKiotaConfig;
    }
    private readonly LockManagementService lockManagementService = new();
    private readonly WorkspaceConfigurationStorageService workspaceConfigurationStorageService = new();
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, Dictionary<string, HashSet<string>> templatesWithOperations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        if (UseKiotaConfig)
        {
            var (wsConfig, manifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            wsConfig ??= new WorkspaceConfiguration();
            manifest ??= new ApiManifestDocument("application"); //TODO get the application name
            var generationClientConfig = new ApiClientConfiguration(generationConfiguration);
            wsConfig.Clients.AddOrReplace(generationConfiguration.ClientClassName, generationClientConfig);
            var inputConfigurationHash = await GetConfigurationHashAsync(generationClientConfig, descriptionHash).ConfigureAwait(false);
            manifest.ApiDependencies.AddOrReplace(generationConfiguration.ClientClassName, generationConfiguration.ToApiDependency(inputConfigurationHash, templatesWithOperations));
            await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
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
                Logger.LogWarning("API client was generated with version {ExistingVersion} and the current version is {CurrentVersion}, it will be upgraded and you should upgrade dependencies", existingLock?.KiotaVersion, configurationLock.KiotaVersion);
            }
            return !lockComparer.Equals(existingLock, configurationLock);
        }

    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly WorkspaceConfigurationGenerationContext context = new(options);
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private async Task<string> GetConfigurationHashAsync(ApiClientConfiguration apiClientConfiguration, string descriptionHash)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, apiClientConfiguration, context.ApiClientConfiguration);
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
}
