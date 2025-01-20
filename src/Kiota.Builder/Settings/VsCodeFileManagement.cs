using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.Settings;

public static class VsCodeSettingsManager
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly SettingsFileGenerationContext context = new(options);
    public static async Task UpdateFileAsync(string fileUpdate, string fileUpdatePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileUpdate);
        Dictionary<string, object> settings;

        // Read existing settings or create new if file doesn't exist
        if (File.Exists(fileUpdatePath))
        {
            using var stream = File.OpenRead(fileUpdatePath);
            settings = await JsonSerializer.DeserializeAsync(
                stream,
                context.DictionaryStringObject,
                cancellationToken
            ).ConfigureAwait(false) ?? [];
        }
        else
        {
            settings = [];
        }

        var fileUpdateDictionary = JsonSerializer.Deserialize(fileUpdate, context.DictionaryStringObject);
        if (fileUpdateDictionary is not null)
        {
            foreach (var kvp in fileUpdateDictionary)
            {
                settings[kvp.Key] = kvp.Value;
            }
        }

#pragma warning disable CA2007
        await using var fileStream = File.Open(fileUpdatePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(fileStream, settings, context.DictionaryStringObject, cancellationToken).ConfigureAwait(false);
    }
}
