using System.Runtime.CompilerServices;
using Microsoft.Kiota.Abstractions;

[assembly: InternalsVisibleTo("Microsoft.Kiota.Cli.Commons.Tests")]
namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Paging service that supports the x-ms-pageable extension
/// </summary>
public abstract class BasePagingService : IPagingService
{
    /// <inheritdoc />
    public abstract IPagingResponseHandler CreateResponseHandler();

    /// <inheritdoc />
    public abstract Task<Uri?> GetNextPageLinkAsync(PageLinkData pageLinkData, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<PageResponse?> GetPagedDataAsync(Func<RequestInformation, IResponseHandler, CancellationToken, Task> requestExecutorAsync, PageLinkData pageLinkData, bool fetchAllPages = false, CancellationToken cancellationToken = default)
    {
        if (!OnBeforeGetPagedData(pageLinkData, fetchAllPages))
        {
            return null;
        }

        var requestInfo = pageLinkData.RequestInformation;
        Uri? nextLink;
        Stream? response = null;
        int? statusCode;
        do
        {
            var responseHandler = CreateResponseHandler();
            await requestExecutorAsync(requestInfo, responseHandler, cancellationToken);
            var pageData = await responseHandler.GetResponseStreamAsync(cancellationToken);
            statusCode = responseHandler.GetStatusCode();
            var headers = responseHandler.GetResponseHeaders();
            var contentHeaders = responseHandler.GetResponseContentHeaders();
            if (fetchAllPages)
            {
                pageLinkData = new PageLinkData(requestInfo, pageData, headers, contentHeaders, pageLinkData.ItemName, pageLinkData.NextLinkName);
                nextLink = await GetNextPageLinkAsync(pageLinkData, cancellationToken);
                if (nextLink != null) pageLinkData.RequestInformation.URI = nextLink;
            }
            else
            {
                nextLink = null;
            }

            response = await MergePageAsync(response, pageLinkData, cancellationToken);
        } while (nextLink != null);

        return new PageResponse(statusCode ?? 0, response);
    }

    /// <inheritdoc />
    public abstract Task<Stream?> MergePageAsync(Stream? currentResult, PageLinkData newPageData, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual bool OnBeforeGetPagedData(PageLinkData pageLinkData, bool fetchAllPages = false)
    {
        return true;
    }
}
