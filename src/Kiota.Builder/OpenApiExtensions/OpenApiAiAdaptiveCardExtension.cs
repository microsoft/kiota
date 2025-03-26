using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
namespace Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Writers;

public class OpenApiAiAdaptiveCardExtension : IOpenApiExtension
{
    public static string Name => "x-ai-adaptive-card";
    public string? DataPath
    {
        get; set;
    }
    public string? File
    {
        get; set;
    }
    public static OpenApiAiAdaptiveCardExtension Parse(JsonNode source)
    {
        if (source is not JsonObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiAiAdaptiveCardExtension();
        if (rawObject.TryGetPropertyValue(nameof(DataPath).ToFirstCharacterLowerCase().ToSnakeCase(), out var dataPath) && dataPath is JsonValue dataPathValue && dataPathValue.GetValueKind() is JsonValueKind.String && dataPathValue.TryGetValue<string>(out var dataPathStrValue))
        {
            extension.DataPath = dataPathStrValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(File).ToFirstCharacterLowerCase(), out var file) && file is JsonValue fileValue && fileValue.GetValueKind() is JsonValueKind.String && fileValue.TryGetValue<string>(out var fileStrValue))
        {
            extension.File = fileStrValue;
        }
        if (string.IsNullOrEmpty(extension.DataPath) || string.IsNullOrEmpty(extension.File))
            throw new ArgumentOutOfRangeException(nameof(source), "Both of the properties 'x-ai-adaptive-card.dataPath' and 'x-ai-adaptive-card.file' must be set.");
        return extension;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrEmpty(DataPath) && !string.IsNullOrEmpty(File))
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(DataPath).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteValue(DataPath);
            writer.WritePropertyName(nameof(File).ToFirstCharacterLowerCase());
            writer.WriteValue(File);
            writer.WriteEndObject();
        }
    }
}
