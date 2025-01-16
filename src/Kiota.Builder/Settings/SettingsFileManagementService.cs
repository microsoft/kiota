using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Settings;

public class SettingsFileManagementService : ISettingsManagementService
{
    internal const string SettingsFileName = "settings.json";
    internal const string EnvironmentVariablesKey = "rest-client.environmentVariables";
    public string? GetDirectoryContainingSettingsFile(string searchDirectory)
    {
        var currentDirectory = new DirectoryInfo(searchDirectory);

        while (currentDirectory != null)
        {
            var vscodeDirectoryPath = Path.Combine(currentDirectory.FullName, ".vscode");
            if (Directory.Exists(vscodeDirectoryPath))
            {
                return vscodeDirectoryPath;
            }
            currentDirectory = currentDirectory.Parent;
        }

        return null;
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
        var vsCodeDirectoryName = ".vscode";
        var parentDirectoryPath = Path.GetDirectoryName(directoryPath);
        var vscodeDirectoryPath = GetDirectoryContainingSettingsFile(parentDirectoryPath!);
        if (!Directory.Exists(vscodeDirectoryPath))
        {
            Directory.CreateDirectory(vsCodeDirectoryName);
        }
        vscodeDirectoryPath = Path.Combine(parentDirectoryPath!, vsCodeDirectoryName);
        var settingsObjectString = JsonSerializer.Serialize(settings, SettingsFileGenerationContext.Default.SettingsFile);

        VsCodeSettingsManager settingsManager = new(vscodeDirectoryPath, SettingsFileName);
        await settingsManager.UpdateSettingAsync(settingsObjectString, EnvironmentVariablesKey, cancellationToken).ConfigureAwait(false);
    }
}

public class VsCodeSettingsManager
{
    private readonly string _vscodePath;
    private readonly string _settingsPath;

    public VsCodeSettingsManager(string basePath, string settingKey)
    {
        _vscodePath = basePath;
        _settingsPath = Path.Combine(_vscodePath, settingKey);
    }

    public async Task UpdateSettingAsync(string setting, string settingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(setting);
        Dictionary<string, object> settings;

        // Read existing settings or create new if file doesn't exist
        if (File.Exists(_settingsPath))
        {
            string jsonContent = await File.ReadAllTextAsync(_settingsPath, cancellationToken).ConfigureAwait(false);
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

        var settingJsonString = JsonSerializer.Deserialize<Dictionary<string, object>>(setting, SettingsFileGenerationContext.Default.DictionaryStringObject);
        if(settingJsonString is not null)
            settings[settingKey] = settingJsonString[settingKey];

        string updatedJson = JsonSerializer.Serialize(settings, SettingsFileGenerationContext.Default.DictionaryStringObject);
        await File.WriteAllTextAsync(_settingsPath, updatedJson, cancellationToken).ConfigureAwait(false);
    }
}
