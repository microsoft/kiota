using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Manifest;
/// <summary>
/// Service used to open and decode an API manifest document.
/// </summary>
public class ManifestManagementService
{
    /// <summary>
    /// Deserializes the API manifest document from a JSON representation.
    /// </summary>
    /// <param name="jsonValue">The API manifest JSON representation</param>
    /// <returns>The deserialized manifest</returns>
    public ApiManifestDocument? DeserializeManifestDocument(string jsonValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(jsonValue);
        var jsonDocument = JsonDocument.Parse(jsonValue);
        return ApiManifestDocument.Load(jsonDocument.RootElement);
    }
    /// <summary>
    /// Deserializes the API manifest document from a JSON representation.
    /// </summary>
    /// <param name="jsonValue">The API manifest JSON representation</param>
    /// <returns>The deserialized manifest</returns>
    public ApiManifestDocument? DeserializeManifestDocument(Stream jsonValue)
    {
        ArgumentNullException.ThrowIfNull(jsonValue);
        var jsonDocument = JsonDocument.Parse(jsonValue);
        return ApiManifestDocument.Load(jsonDocument.RootElement);
    }
}
