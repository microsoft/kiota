using System;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Paging service
/// </summary>
public interface IPagingService
{
    /// <summary>
    /// Gets the next page's link
    /// </summary>
    /// <param name="pageLinkData">Holds data that could be used to extract paging information</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A Uri of the next page's link or null if there's no next page.</returns>
    Task<Uri?> GetNextPageLinkAsync(PageLinkData pageLinkData, CancellationToken cancellationToken = default);
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
    /// <param name="nextLinkName">The name of the property that holds the next link.</param>
    public PageLinkData(RequestInformation requestInformation, Stream response, string nextLinkName = "nextLink")
    {
        NextLinkName = nextLinkName;
        Response = response;
        RequestInformation = requestInformation;
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
    /// The response body stream. Some responses provide paging data e.g. total item count or next page link
    /// </summary>
    public Stream Response
    {
        get; private init;
    }
}
