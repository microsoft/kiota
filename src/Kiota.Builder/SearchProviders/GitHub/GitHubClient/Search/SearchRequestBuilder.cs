using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Search.Repositories;
using Microsoft.Kiota.Abstractions;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Search;
/// <summary>
/// Builds and executes requests for operations under \search
/// </summary>
public class SearchRequestBuilder
{
    /// <summary>Path parameters for the request</summary>
    private Dictionary<string, object> PathParameters
    {
        get; set;
    }
    /// <summary>The repositories property</summary>
    public RepositoriesRequestBuilder Repositories
    {
        get =>
        new RepositoriesRequestBuilder(PathParameters, RequestAdapter);
    }
    /// <summary>The request adapter to use to execute the requests.</summary>
    private IRequestAdapter RequestAdapter
    {
        get; set;
    }
    /// <summary>Url template to use to build the URL for the current request builder</summary>
    private string UrlTemplate
    {
        get; set;
    }
    /// <summary>
    /// Instantiates a new SearchRequestBuilder and sets the default values.
    /// </summary>
    /// <param name="pathParameters">Path parameters for the request</param>
    /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
    public SearchRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter)
    {
        _ = pathParameters ?? throw new ArgumentNullException(nameof(pathParameters));
        _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
        UrlTemplate = "{+baseurl}/search";
        var urlTplParams = new Dictionary<string, object>(pathParameters);
        PathParameters = urlTplParams;
        RequestAdapter = requestAdapter;
    }
    /// <summary>
    /// Instantiates a new SearchRequestBuilder and sets the default values.
    /// </summary>
    /// <param name="rawUrl">The raw URL to use for the request builder.</param>
    /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
    public SearchRequestBuilder(string rawUrl, IRequestAdapter requestAdapter)
    {
        if (string.IsNullOrEmpty(rawUrl)) throw new ArgumentNullException(nameof(rawUrl));
        _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
        UrlTemplate = "{+baseurl}/search";
        var urlTplParams = new Dictionary<string, object>();
        urlTplParams.Add("request-raw-url", rawUrl);
        PathParameters = urlTplParams;
        RequestAdapter = requestAdapter;
    }
}
