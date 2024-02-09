using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.WorkspaceManagement;

public class WorkspaceConfigurationStorageService
{
    private const string ConfigurationFileName = "kiota-config.json";
    public string TargetDirectory
    {
        get; private set;
    }
    private readonly string targetConfigurationFilePath;
    public WorkspaceConfigurationStorageService() : this(Directory.GetCurrentDirectory())
    {

    }
    public WorkspaceConfigurationStorageService(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetDirectory);
        TargetDirectory = targetDirectory;
        targetConfigurationFilePath = Path.Combine(TargetDirectory, ConfigurationFileName);
    }
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await IsInitializedAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("The workspace configuration already exists");
        await UpdateWorkspaceConfigurationAsync(new WorkspaceConfiguration(), cancellationToken).ConfigureAwait(false);
    }
    public async Task UpdateWorkspaceConfigurationAsync(WorkspaceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!Directory.Exists(TargetDirectory))
            Directory.CreateDirectory(TargetDirectory);
#pragma warning disable CA2007
        await using var fileStream = File.Open(targetConfigurationFilePath, FileMode.Create);
#pragma warning restore CA2007
        await JsonSerializer.SerializeAsync(fileStream, configuration, context.WorkspaceConfiguration, cancellationToken).ConfigureAwait(false);
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
    public async Task<WorkspaceConfiguration?> GetWorkspaceConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(targetConfigurationFilePath))
        {
#pragma warning disable CA2007
            await using var fileStream = File.OpenRead(targetConfigurationFilePath);
#pragma warning restore CA2007
            return await JsonSerializer.DeserializeAsync(fileStream, context.WorkspaceConfiguration, cancellationToken).ConfigureAwait(false);
        }
        return null;
    }
}
