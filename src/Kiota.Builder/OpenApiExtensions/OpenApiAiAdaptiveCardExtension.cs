using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;

namespace Kiota.Builder.OpenApiExtensions;

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

    public string? Title
    {
        get; set;
    }

#pragma warning disable CA1056 // URI-like properties should not be strings

    public string? Url
#pragma warning restore CA1056 // URI-like properties should not be strings

    {
        get; set;
    }
    public static OpenApiAiAdaptiveCardExtension Parse(JsonNode source)
    {
        // We are supporting empty extension to avoid creating the template when emitting from typespec scenario
        var emptyExtension = new OpenApiAiAdaptiveCardExtension();
        if (source is not JsonObject rawObject)
            return emptyExtension;
        var extension = new OpenApiAiAdaptiveCardExtension();
        if (rawObject.TryGetPropertyValue(nameof(DataPath).ToFirstCharacterLowerCase().ToSnakeCase(), out var dataPath) && dataPath is JsonValue dataPathValue && dataPathValue.GetValueKind() is JsonValueKind.String && dataPathValue.TryGetValue<string>(out var dataPathStrValue))
        {
            extension.DataPath = dataPathStrValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(File).ToFirstCharacterLowerCase(), out var file) && file is JsonValue fileValue && fileValue.GetValueKind() is JsonValueKind.String && fileValue.TryGetValue<string>(out var fileStrValue))
        {
            extension.File = fileStrValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(Title).ToFirstCharacterLowerCase(), out var title) && title is JsonValue titleValue && titleValue.GetValueKind() is JsonValueKind.String && titleValue.TryGetValue<string>(out var titleStrValue))
        {
            extension.Title = titleStrValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(Url).ToFirstCharacterLowerCase(), out var url) && url is JsonValue urlValue && urlValue.GetValueKind() is JsonValueKind.String && urlValue.TryGetValue<string>(out var urlStrValue))
        {
            extension.Url = urlStrValue;
        }
        // We are supporting empty extension to avoid creating the template when emitting from typespec scenario
        if (string.IsNullOrEmpty(extension.DataPath) || string.IsNullOrEmpty(extension.File) || string.IsNullOrEmpty(extension.Title))
            return emptyExtension;
        return extension;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(DataPath) && !string.IsNullOrEmpty(File) && !string.IsNullOrEmpty(Title))
        {
            writer.WritePropertyName(nameof(DataPath).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteValue(DataPath);
            writer.WritePropertyName(nameof(File).ToFirstCharacterLowerCase());
            writer.WriteValue(File);
            writer.WritePropertyName(nameof(Title).ToFirstCharacterLowerCase());
            writer.WriteValue(Title);
            if (!string.IsNullOrEmpty(Url))
            {
                writer.WritePropertyName(nameof(Url).ToFirstCharacterLowerCase());
                writer.WriteValue(Url);
            }
        }
        writer.WriteEndObject();
    }
}
