using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Settings;

public class SettingsFileManagementService : ISettingsManagementService
{
    internal const string SettingsFileName = "settings.json";
    public string GetDirectoryContainingSettingsFile(string searchDirectory)
    {
        throw new NotImplementedException();
    }

    public Task<SettingsFile> GetSettingsFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SettingsFile> GetSettingsFromStreamAsync(Stream stream)
    {
        throw new NotImplementedException();
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

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly SettingsFileGenerationContext context = new(options);

    private static async Task WriteSettingsFileInternalAsync(string directoryPath, SettingsFile settings, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(directoryPath, SettingsFileName);
#pragma warning disable CA2007 // Dispose objects before losing scope
        await using var fileStream = File.Open(filePath, FileMode.Create);
#pragma warning disable CA2007
        await JsonSerializer.SerializeAsync(fileStream, settings, context.SettingsFile, cancellationToken).ConfigureAwait(false);
    }
}
