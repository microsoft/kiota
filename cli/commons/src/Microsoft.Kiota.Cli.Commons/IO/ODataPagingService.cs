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
            var doc = await JsonDocument.ParseAsync(pageLinkData.Response, cancellationToken: cancellationToken);
            var hasNextLink = doc.RootElement.TryGetProperty(pageLinkData.NextLinkName, out var nextLink);
            if (hasNextLink && nextLink.ValueKind == JsonValueKind.String)
            {
                string? link = nextLink.GetString();
                if (!string.IsNullOrWhiteSpace(link)) return new Uri(link);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<Stream> GetPagedDataAsync(Func<RequestInformation, CancellationToken, Task<Stream>> requestExecutorAsync, PageLinkData pageLinkData, bool fetchAllPages = false, CancellationToken cancellationToken = default)
    {
        // Set the page size to 999 if the user asked to fetch all pages and top either isn't specified or is invalid
        if (fetchAllPages && (!pageLinkData.RequestInformation.QueryParameters.TryGetValue("%24top", out var topVal) || (topVal as int?) < 1))
        {
            pageLinkData.RequestInformation.QueryParameters["%24top"] = 999;
        }
        var requestInfo = pageLinkData.RequestInformation;
        Uri? nextLink;
        Stream? response = null;
        do
        {
            var pageData = await requestExecutorAsync(requestInfo, cancellationToken);
            if (fetchAllPages)
            {
                pageLinkData = new PageLinkData(requestInfo, pageData, pageLinkData.ResponseFormat, pageLinkData.ItemName, pageLinkData.NextLinkName);
                nextLink = await GetNextPageLinkAsync(pageLinkData, cancellationToken);
                if (nextLink != null) pageLinkData.RequestInformation.URI = nextLink;
            }
            else
            {
                nextLink = null;
            }
            response = await MergeJsonStreamsAsync(response, pageData, pageLinkData.ItemName, pageLinkData.NextLinkName, cancellationToken);
        } while (nextLink != null);

        return response ?? Stream.Null;
    }

    /// <summary>
    /// Merges 2 streams of JSON on the property defined by <code>itemName</code>. The property should be a JSON array
    /// </summary>
    /// <param name="left">The first stream.</param>
    /// <param name="right">The second stream.</param>
    /// <param name="itemName">The name of the array property to merge on.</param>
    /// <param name="nextLinkName">The name of the property containing the next link name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async Task<Stream?> MergeJsonStreamsAsync(Stream? left, Stream? right, string itemName = "value", string nextLinkName = "nextLink", CancellationToken cancellationToken = default)
    {
        if (left?.CanSeek == true) left?.Seek(0, SeekOrigin.Begin);
        if (right?.CanSeek == true) right?.Seek(0, SeekOrigin.Begin);
        if (left == null || right == null)
        {
            return left ?? right;
        }

        JsonNode? nodeLeft = null;
        if (left != null)
        {
            nodeLeft = JsonNode.Parse(left);
            if (left?.CanSeek == true) left?.Seek(0, SeekOrigin.Begin);
        }
        JsonNode? nodeRight = null;
        if (right != null)
        {
            nodeRight = JsonNode.Parse(right);
            if (right?.CanSeek == true) right?.Seek(0, SeekOrigin.Begin);
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

        // Replace next link with new page's next link
        if (!string.IsNullOrWhiteSpace(nextLinkName))
        {
            var obj1 = nodeLeft as JsonObject;
            if (obj1?[nextLinkName] != null)
                obj1.Remove(nextLinkName);
            if (nodeRight is JsonObject obj2 && obj2?[nextLinkName] != null)
            {
                var nextLink = obj2[nextLinkName];
                obj2.Remove(nextLinkName);
                obj1?.Add(nextLinkName, nextLink);
            }
        }
        var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        nodeLeft?.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;

        return stream;
    }
}
