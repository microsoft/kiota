using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Paging service
/// </summary>
public interface IPagingService
{
    /// <summary>
    /// Create a paging response handler.
    /// </summary>
    IPagingResponseHandler CreateResponseHandler();

    /// <summary>
    /// Gets the next page's link
    /// </summary>
    /// <param name="pageLinkData">Holds data that could be used to extract paging information</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A Uri of the next page's link or null if there's no next page.</returns>
    Task<Uri?> GetNextPageLinkAsync(PageLinkData pageLinkData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the next page or all pages if fetch all pages is true
    /// </summary>
    /// <param name="requestExecutorAsync">Callback to run that returns a stream with the next page of data</param>
    /// <param name="pageLinkData">Metadata that is used when fetching paging data</param>
    /// <param name="fetchAllPages">If this is true, the result will be a stream with all available pages</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task<PageResponse?> GetPagedDataAsync(Func<RequestInformation, IResponseHandler, CancellationToken, Task> requestExecutorAsync, PageLinkData pageLinkData, bool fetchAllPages = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges any new page received on each page request.
    /// </summary>
    /// <param name="currentResult">Cumulative results up until the previous page.</param>
    /// <param name="newPageData">The new page data that should be merged.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream with the merged new page data.</returns>
    Task<Stream?> MergePageAsync(Stream currentResult, PageLinkData newPageData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs before getting paging data. Can be used to set request headers or query parameters before making a request
    /// </summary>
    /// <returns>A boolean result that if false, cancels the paging</returns>
    bool OnBeforeGetPagedData(PageLinkData pageLinkData, bool fetchAllPages = false);
}

/// <summary>
/// Holds data that could be used to extract paging information
/// </summary>
public readonly struct PageLinkData
{
    /// <summary>
    /// Holds data that could be used to extract paging information
    /// </summary>
    /// <param name="requestInformation">The request information. Paging information (top, skip etc) can be extracted from a request.</param>
    /// <param name="response">The response body stream.</param>
    /// <param name="responseHeaders">The response headers.</param>
    /// <param name="responseContentHeaders">The response content-related headers.</param>
    /// <param name="itemName">The name of the property that has the data.</param>
    /// <param name="nextLinkName">The name of the property that holds the next link.</param>
    public PageLinkData(RequestInformation requestInformation, Stream? response, IDictionary<string, IEnumerable<string>>? responseHeaders = null, IDictionary<string, IEnumerable<string>>? responseContentHeaders = null, string itemName = "value", string nextLinkName = "nextLink")
    {
        ItemName = itemName;
        NextLinkName = nextLinkName;
        Response = response;
        ResponseHeaders = responseHeaders ?? new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        ResponseContentHeaders = responseContentHeaders ?? new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        RequestInformation = requestInformation;
    }

    /// <summary>
    /// The name of the property that has the data.
    /// </summary>
    public string ItemName
    {
        get; private init;
    }

    /// <summary>
    /// The name of the property that holds the next link.
    /// </summary>
    public string NextLinkName
    {
        get; private init;
    }

    /// <summary>
    /// The request information. Paging information (top, skip etc) can be extracted from a request
    /// </summary>
    public RequestInformation RequestInformation
    {
        get; private init;
    }

    /// <summary>
    /// The response body stream. Some responses provide paging data e.g. total item count or next page link.
    /// </summary>
    public Stream? Response
    {
        get; private init;
    }

    /// <summary>
    /// The response headers. Some responses provide paging data in headers e.g. GitHub's next page link.
    /// </summary>
    public IDictionary<string, IEnumerable<string>> ResponseHeaders
    {
        get; private init;
    }

    /// <summary>
    /// The response content related headers e.g. Content-Type
    /// </summary>
    public IDictionary<string, IEnumerable<string>> ResponseContentHeaders
    {
        get; private init;
    }
}

/// <summary>
/// Response for the paging service.
/// </summary>
public readonly struct PageResponse
{
    ///<summary>
    /// Creates new instance
    ///</summary>
    public PageResponse(int statusCode = 0, Stream? response = null)
    {
        Response = response;
        StatusCode = statusCode;
    }

    /// <summary>
    /// The response body stream.
    /// </summary>
    public Stream? Response
    {
        get; init;
    }

    /// <summary>
    /// The http response status code. Use to check for success or error.
    /// </summary>
    public int StatusCode
    {
        get; init;
    }
}
