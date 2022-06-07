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
    public abstract IResponseHandler CreateResponseHandler();

    /// <inheritdoc />
    public abstract IDictionary<string, IEnumerable<string>> ExtractResponseContentHeaders(IResponseHandler responseHandler);

    /// <inheritdoc />
    public abstract IDictionary<string, IEnumerable<string>> ExtractResponseHeaders(IResponseHandler responseHandler);

    /// <inheritdoc />
    public abstract Task<Stream> ExtractResponseStreamAsync(IResponseHandler responseHandler, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<Uri?> GetNextPageLinkAsync(PageLinkData pageLinkData, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<Stream> GetPagedDataAsync(Func<RequestInformation, IResponseHandler, CancellationToken, Task> requestExecutorAsync, PageLinkData pageLinkData, bool fetchAllPages = false, CancellationToken cancellationToken = default)
    {
        if (!OnBeforeGetPagedData(pageLinkData, fetchAllPages))
        {
            return Stream.Null;
        }

        var requestInfo = pageLinkData.RequestInformation;
        Uri? nextLink;
        Stream? response = null;
        do
        {
            var responseHandler = CreateResponseHandler();
            await requestExecutorAsync(requestInfo, responseHandler, cancellationToken);
            var pageData = await ExtractResponseStreamAsync(responseHandler, cancellationToken);
            var headers = ExtractResponseHeaders(responseHandler);
            var contentHeaders = ExtractResponseContentHeaders(responseHandler);
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

        return response ?? Stream.Null;
    }

    /// <inheritdoc />
    public abstract Task<Stream?> MergePageAsync(Stream? currentResult, PageLinkData newPageData, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual bool OnBeforeGetPagedData(PageLinkData pageLinkData, bool fetchAllPages = false)
    {
        return true;
    }
}
