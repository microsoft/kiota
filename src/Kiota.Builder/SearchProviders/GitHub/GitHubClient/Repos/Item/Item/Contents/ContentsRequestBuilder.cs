using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Contents.Item;
using Microsoft.Kiota.Abstractions;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Contents
{
    /// <summary>
    /// Builds and executes requests for operations under \repos\{owner}\{repo}\contents
    /// </summary>
    public class ContentsRequestBuilder : BaseRequestBuilder
    {
        /// <summary>Gets an item from the Kiota.Builder.SearchProviders.GitHub.GitHubClient.repos.item.item.contents.item collection</summary>
        public WithPathItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                if (!string.IsNullOrWhiteSpace(position)) urlTplParams.Add("path", position);
                return new WithPathItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new ContentsRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public ContentsRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/repos/{owner}/{repo}/contents", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new ContentsRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public ContentsRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/repos/{owner}/{repo}/contents", rawUrl)
        {
        }
    }
}
