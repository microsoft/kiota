using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiLogoExtension : IOpenApiExtension
{
    public static string Name => "x-logo";
#pragma warning disable CA1056
    public string? Url
#pragma warning restore CA1056
    {
        get; set;
    }
    public static OpenApiLogoExtension Parse(JsonNode source)
    {
        if (source is not JsonObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiLogoExtension();
        if (rawObject.TryGetPropertyValue(nameof(Url).ToFirstCharacterLowerCase(), out var url) && url is JsonValue urlValue && urlValue.GetValueKind() is JsonValueKind.String && urlValue.TryGetValue<string>(out var urlStrValue))
        {
            extension.Url = urlStrValue;
        }
        return extension;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrEmpty(Url))
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(Url).ToFirstCharacterLowerCase());
            writer.WriteValue(Url);
            writer.WriteEndObject();
        }
    }
}
