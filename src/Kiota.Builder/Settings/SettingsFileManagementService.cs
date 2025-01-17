using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

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
        settings.EnvironmentVariables.Development.HostAddress = openApiDocument.Servers[0].Url;
        settings.EnvironmentVariables.Remote.HostAddress = openApiDocument.Servers[0].Url;
        return settings;
    }

    private async Task WriteSettingsFileInternalAsync(string directoryPath, SettingsFile settings, CancellationToken cancellationToken)
    {
        var parentDirectoryPath = Path.GetDirectoryName(directoryPath);
        var vscodeDirectoryPath = GetDirectoryContainingSettingsFile(parentDirectoryPath!);
        var settingsObjectString = JsonSerializer.Serialize(settings, SettingsFileGenerationContext.Default.SettingsFile);
        VsCodeSettingsManager settingsManager = new(vscodeDirectoryPath, SettingsFileName);
        await settingsManager.UpdateFileAsync(settingsObjectString, EnvironmentVariablesKey, cancellationToken).ConfigureAwait(false);
    }
}

public class VsCodeSettingsManager
{
    private readonly string _vscodePath;
    private readonly string fileUpdatePath;

    public VsCodeSettingsManager(string basePath, string targetFilePath)
    {
        _vscodePath = basePath;
        fileUpdatePath = Path.Combine(_vscodePath, targetFilePath);
    }

    public async Task UpdateFileAsync(string fileUpdate, string fileUpdateKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileUpdate);
        Dictionary<string, object> settings;

        // Read existing settings or create new if file doesn't exist
        if (File.Exists(fileUpdatePath))
        {
            string jsonContent = await File.ReadAllTextAsync(fileUpdatePath, cancellationToken).ConfigureAwait(false);
            try
            {
                settings = JsonSerializer.Deserialize(
                    jsonContent,
                    SettingsFileGenerationContext.Default.DictionaryStringObject)
                    ?? [];
            }
            catch (JsonException)
            {
                settings = [];
            }
        }
        else
        {
            settings = [];
        }

        var fileUpdateDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(fileUpdate, SettingsFileGenerationContext.Default.DictionaryStringObject);
        if (fileUpdateDictionary is not null)
            settings[fileUpdateKey] = fileUpdateDictionary[fileUpdateKey];

#pragma warning disable CA2007
        await using var fileStream = File.Open(fileUpdatePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(fileStream, settings, SettingsFileGenerationContext.Default.DictionaryStringObject, cancellationToken).ConfigureAwait(false);
    }
}
