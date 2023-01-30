using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item;
using Microsoft.Kiota.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item;
/// <summary>
/// Builds and executes requests for operations under \repos\{owner}
/// </summary>
public class WithOwnerItemRequestBuilder {
    /// <summary>Path parameters for the request</summary>
    private Dictionary<string, object> PathParameters { get; set; }
    /// <summary>The request adapter to use to execute the requests.</summary>
    private IRequestAdapter RequestAdapter { get; set; }
    /// <summary>Url template to use to build the URL for the current request builder</summary>
    private string UrlTemplate { get; set; }
    /// <summary>Gets an item from the Kiota.Builder.SearchProviders.GitHub.GitHubClient.repos.item.item collection</summary>
    public WithRepoItemRequestBuilder this[string position] { get {
        var urlTplParams = new Dictionary<string, object>(PathParameters);
        urlTplParams.Add("repo", position);
        return new WithRepoItemRequestBuilder(urlTplParams, RequestAdapter);
    } }
    /// <summary>
    /// Instantiates a new WithOwnerItemRequestBuilder and sets the default values.
    /// </summary>
    /// <param name="pathParameters">Path parameters for the request</param>
    /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
    public WithOwnerItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) {
        _ = pathParameters ?? throw new ArgumentNullException(nameof(pathParameters));
        _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
        UrlTemplate = "{+baseurl}/repos/{owner}";
        var urlTplParams = new Dictionary<string, object>(pathParameters);
        PathParameters = urlTplParams;
        RequestAdapter = requestAdapter;
    }
    /// <summary>
    /// Instantiates a new WithOwnerItemRequestBuilder and sets the default values.
    /// </summary>
    /// <param name="rawUrl">The raw URL to use for the request builder.</param>
    /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
    public WithOwnerItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) {
        if(string.IsNullOrEmpty(rawUrl)) throw new ArgumentNullException(nameof(rawUrl));
        _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
        UrlTemplate = "{+baseurl}/repos/{owner}";
        var urlTplParams = new Dictionary<string, object>();
        urlTplParams.Add("request-raw-url", rawUrl);
        PathParameters = urlTplParams;
        RequestAdapter = requestAdapter;
    }
}
