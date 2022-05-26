using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Kiota.Abstractions;

[assembly: InternalsVisibleTo("Microsoft.Kiota.Cli.Commons.Tests")]
namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Paging service that supports the x-ms-pageable extension
/// </summary>
public class ODataPagingService : IPagingService
{
    /// <inheritdoc />
    public async Task<Uri?> GetNextPageLinkAsync(PageLinkData pageLinkData, CancellationToken cancellationToken = default)
    {
        if (pageLinkData.ResponseFormat == ResponseFormat.JSON)
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

    /// <inheritdoc />
    public async Task<Stream> GetPagedDataAsync(Func<RequestInformation, CancellationToken, Task<Stream>> requestExecutorAsync, PageLinkData pageLinkData, bool fetchAllPages = false, CancellationToken cancellationToken = default)
    {
        var requestInfo = pageLinkData.RequestInformation;
        var nextLink = requestInfo.URI;
        Stream? response = null;
        while (nextLink != null)
        {
            var pageData = await requestExecutorAsync(requestInfo, cancellationToken);
            if (fetchAllPages)
            {
                nextLink = await GetNextPageLinkAsync(pageLinkData, cancellationToken);
                pageLinkData.RequestInformation.URI = nextLink;
            }
            else
            {
                nextLink = null;
            }
            response = await MergeJsonStreamsAsync(response, pageData, pageLinkData.ItemName, cancellationToken);
        }

        return response ?? Stream.Null;
    }

    /// <summary>
    /// Merges 2 streams of JSON on the property defined by <code>itemName</code>. The property should be a JSON array
    /// </summary>
    /// <param name="left">The first stream.</param>
    /// <param name="right">The second stream.</param>
    /// <param name="itemName">The name of the array property to merge on.</param>
    internal async Task<Stream?> MergeJsonStreamsAsync(Stream? left, Stream? right, string itemName = "value", CancellationToken cancellationToken = default)
    {
        if (left == null || right == null)
        {
            return left ?? right;
        }

        JsonNode? nodeLeft = null;
        if (left != null)
        {
            nodeLeft = JsonNode.Parse(left);
        }
        JsonNode? nodeRight = null;
        if (right != null)
        {
            nodeRight = JsonNode.Parse(right);
        }

        JsonArray? leftArray = null;
        JsonArray? rightArray = null;
        if (!string.IsNullOrWhiteSpace(itemName))
        {
            if (nodeLeft?[itemName] == null || nodeRight?[itemName] == null)
            {
                return left ?? right;
            }

            leftArray = nodeLeft[itemName]?.AsArray();
            rightArray = nodeRight[itemName]?.AsArray();
        }
        else
        {
            leftArray = nodeLeft?.AsArray();
            rightArray = nodeRight?.AsArray();
        }


        if (leftArray != null && rightArray != null)
        {
            var elements = rightArray.Where(i => i != null);
            var item = elements.FirstOrDefault();
            while (item != null)
            {
                rightArray.Remove(item);
                leftArray.Add(item);
                item = elements.FirstOrDefault();
            }
        }
        if (!string.IsNullOrWhiteSpace(itemName) && nodeLeft != null)
        {
            nodeLeft[itemName] = leftArray ?? rightArray;
        }
        else
        {
            nodeLeft = leftArray ?? rightArray;
        }
        var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        nodeLeft?.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;

        return stream;
    }
}
