using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Manifest;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.WorkspaceManagement;

public class WorkspaceConfigurationStorageService
{
    private const string ConfigurationFileName = "kiota-config.json";
    private const string ManifestFileName = "apimanifest.json";
    public string TargetDirectory
    {
        get; private set;
    }
    private readonly string targetConfigurationFilePath;
    private readonly string targetManifestFilePath;
    private readonly ManifestManagementService manifestManagementService = new();
    public WorkspaceConfigurationStorageService() : this(Directory.GetCurrentDirectory())
    {

    }
    public WorkspaceConfigurationStorageService(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetDirectory);
        TargetDirectory = targetDirectory;
        targetConfigurationFilePath = Path.Combine(TargetDirectory, ConfigurationFileName);
        targetManifestFilePath = Path.Combine(TargetDirectory, ManifestFileName);
    }
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await IsInitializedAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("The workspace configuration already exists");
        await UpdateWorkspaceConfigurationAsync(new WorkspaceConfiguration(), null, cancellationToken).ConfigureAwait(false);
    }
    public async Task UpdateWorkspaceConfigurationAsync(WorkspaceConfiguration configuration, ApiManifestDocument? manifestDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!Directory.Exists(TargetDirectory))
            Directory.CreateDirectory(TargetDirectory);
#pragma warning disable CA2007
        await using var configStream = File.Open(targetConfigurationFilePath, FileMode.Create);
#pragma warning restore CA2007
        await JsonSerializer.SerializeAsync(configStream, configuration, context.WorkspaceConfiguration, cancellationToken).ConfigureAwait(false);
        if (manifestDocument != null)
        {
#pragma warning disable CA2007
            await using var manifestStream = File.Open(targetManifestFilePath, FileMode.Create);
#pragma warning restore CA2007
            await manifestManagementService.SerializeManifestDocumentAsync(manifestDocument, manifestStream).ConfigureAwait(false);
        }
    }
    public Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
    {// keeping this as a task in case we want to do more complex validation in the future
        return Task.FromResult(File.Exists(targetConfigurationFilePath));
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly WorkspaceConfigurationGenerationContext context = new(options);
    public async Task<(WorkspaceConfiguration?, ApiManifestDocument?)> GetWorkspaceConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(targetConfigurationFilePath))
        {
#pragma warning disable CA2007
            await using var configStream = File.OpenRead(targetConfigurationFilePath);
#pragma warning restore CA2007
            var config = await JsonSerializer.DeserializeAsync(configStream, context.WorkspaceConfiguration, cancellationToken).ConfigureAwait(false);
            if (File.Exists(targetManifestFilePath))
            {
#pragma warning disable CA2007
                await using var manifestStream = File.OpenRead(targetManifestFilePath);
#pragma warning restore CA2007
                var manifest = await manifestManagementService.DeserializeManifestDocumentAsync(manifestStream).ConfigureAwait(false);
                return (config, manifest);
            }
            return (config, null);
        }
        return (null, null);
    }
    public Task BackupConfigAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        BackupFile(directoryPath, ConfigurationFileName);
        BackupFile(directoryPath, ManifestFileName);
        return Task.CompletedTask;
    }
    private static void BackupFile(string directoryPath, string fileName)
    {
        var sourceFilePath = Path.Combine(directoryPath, fileName);
        if (File.Exists(sourceFilePath))
        {
            var backupFilePath = GetBackupFilePath(directoryPath, fileName);
            var targetDirectory = Path.GetDirectoryName(backupFilePath);
            if (string.IsNullOrEmpty(targetDirectory)) return;
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            File.Copy(sourceFilePath, backupFilePath, true);
        }
    }
    public Task RestoreConfigAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        RestoreFile(directoryPath, ConfigurationFileName);
        RestoreFile(directoryPath, ManifestFileName);
        return Task.CompletedTask;
    }
    private static void RestoreFile(string directoryPath, string fileName)
    {
        var sourceFilePath = Path.Combine(directoryPath, fileName);
        var targetDirectory = Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrEmpty(targetDirectory)) return;
        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);
        var backupFilePath = GetBackupFilePath(directoryPath, fileName);
        if (File.Exists(backupFilePath))
        {
            File.Copy(backupFilePath, sourceFilePath, true);
        }
    }
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private static string GetBackupFilePath(string outputPath, string fileName)
    {
        var hashedPath = BitConverter.ToString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(outputPath))).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "backup", hashedPath, fileName);
    }
}
