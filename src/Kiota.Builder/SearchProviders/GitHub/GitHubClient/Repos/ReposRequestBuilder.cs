using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item;
using Microsoft.Kiota.Abstractions;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos
{
    /// <summary>
    /// Builds and executes requests for operations under \repos
    /// </summary>
    public class ReposRequestBuilder : BaseRequestBuilder
    {
        /// <summary>Gets an item from the Kiota.Builder.SearchProviders.GitHub.GitHubClient.repos.item collection</summary>
        public WithOwnerItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                if (!string.IsNullOrWhiteSpace(position)) urlTplParams.Add("owner", position);
                return new WithOwnerItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new ReposRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public ReposRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/repos", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new ReposRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public ReposRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/repos", rawUrl)
        {
        }
    }
}
