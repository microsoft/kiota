using System;
using System.Linq;
using System.Text.Json.Nodes;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiKiotaExtension : IOpenApiExtension
{
    /// <summary>
    /// Name of the extension as used in the description.
    /// </summary>
    public static string Name => "x-ms-kiota-info";
#pragma warning disable CA2227
    public LanguagesInformation LanguagesInformation { get; set; } = new();
#pragma warning restore CA2227

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (LanguagesInformation != null &&
            LanguagesInformation.Any())
        {
            writer.WriteStartObject();
            writer.WriteRequiredObject(nameof(LanguagesInformation).ToFirstCharacterLowerCase(), LanguagesInformation, (w, x) => x.SerializeAsV3(w));
            writer.WriteEndObject();
        }
    }
    public static OpenApiKiotaExtension Parse(JsonNode source)
    {
        if (source is not JsonObject jsonNode) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiKiotaExtension();
        if (jsonNode.TryGetPropertyValue(nameof(LanguagesInformation).ToFirstCharacterLowerCase(), out var languagesInfo) && languagesInfo is JsonObject objectValue)
        {
            extension.LanguagesInformation = LanguagesInformation.Parse(objectValue);
        }
        return extension;
    }
}
