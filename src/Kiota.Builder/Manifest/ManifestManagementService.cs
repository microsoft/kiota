using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
    public async Task<ApiManifestDocument?> DeserializeManifestDocumentAsync(Stream jsonValue)
    {
        ArgumentNullException.ThrowIfNull(jsonValue);
        var jsonDocument = await JsonDocument.ParseAsync(jsonValue).ConfigureAwait(false);
        return ApiManifestDocument.Load(jsonDocument.RootElement);
    }
}
