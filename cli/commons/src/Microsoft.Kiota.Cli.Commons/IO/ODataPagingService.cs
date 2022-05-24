using System.Text.Json;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Paging service that supports the x-ms-pageable extension
/// </summary>
public class ODataPagingService : IPagingService
{
    /// <inheritdoc />
    public async Task<Uri?> GetNextPageLinkAsync(PageLinkData pageLinkData, CancellationToken cancellationToken = default)
    {
        var responseFormat = pageLinkData.RequestInformation.Headers["Accept"];
        if (!string.IsNullOrWhiteSpace(responseFormat) && responseFormat?.Contains("json") == true)
        {
            try
            {
                var doc = await JsonDocument.ParseAsync(pageLinkData.Response, cancellationToken: cancellationToken);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var obj = doc.RootElement.EnumerateObject().FirstOrDefault(o => o.Name == pageLinkData.NextLinkName);
                    var link = obj.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(link)) return new Uri(link);
                }
            }
            catch (JsonException)
            {
                // If the response isn't valid JSON, there will be no next link.
                // TODO: Log warning once logging story is defined
            }
        }

        return null;
    }
}
