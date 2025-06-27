using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi;

namespace Kiota.Builder.Settings;

public class SettingsFileManagementService : ISettingsManagementService
{
    internal const string SettingsFileName = "settings.json";
    internal const string EnvironmentVariablesKey = "rest-client.environmentVariables";
    internal const string VsCodeDirectoryName = ".vscode";
    public string GetDirectoryContainingSettingsFile(string searchDirectory)
    {
        var currentDirectory = new DirectoryInfo(searchDirectory);
        var vscodeDirectoryPath = Path.Combine(currentDirectory.FullName, VsCodeDirectoryName);
        if (Directory.Exists(vscodeDirectoryPath))
        {
            return vscodeDirectoryPath;
        }
        var pathToWrite = Path.Combine(searchDirectory, VsCodeDirectoryName);
        return Directory.CreateDirectory(pathToWrite).FullName;
    }

    public Task WriteSettingsFileAsync(string directoryPath, OpenApiDocument openApiDocument, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(openApiDocument);
        var settings = GenerateSettingsFile(openApiDocument);
        return WriteSettingsFileInternalAsync(directoryPath, settings, cancellationToken);
    }

    private static SettingsFile GenerateSettingsFile(OpenApiDocument openApiDocument)
    {
        var settings = new SettingsFile();
        if (openApiDocument.Servers is { Count: > 0 } && openApiDocument.Servers[0] is { Url: { } url })
        {
            settings.EnvironmentVariables.Development.HostAddress = url;
            settings.EnvironmentVariables.Remote.HostAddress = url;
        }
        return settings;
    }

    private async Task WriteSettingsFileInternalAsync(string directoryPath, SettingsFile settings, CancellationToken cancellationToken)
    {
        var parentDirectoryPath = Path.GetDirectoryName(directoryPath);
        var vscodeDirectoryPath = GetDirectoryContainingSettingsFile(parentDirectoryPath!);
        var settingsObjectString = JsonSerializer.Serialize(settings, SettingsFileGenerationContext.Default.SettingsFile);
        var fileUpdatePath = Path.Combine(vscodeDirectoryPath, SettingsFileName);
        await VsCodeSettingsManager.UpdateFileAsync(settingsObjectString, fileUpdatePath, EnvironmentVariablesKey, cancellationToken).ConfigureAwait(false);
    }
}
