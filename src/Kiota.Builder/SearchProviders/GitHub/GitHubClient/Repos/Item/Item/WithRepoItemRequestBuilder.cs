using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Contents;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item {
    /// <summary>
    /// Builds and executes requests for operations under \repos\{owner}\{repo}
    /// </summary>
    public class WithRepoItemRequestBuilder : BaseRequestBuilder {
        /// <summary>The contents property</summary>
        public ContentsRequestBuilder Contents { get =>
            new ContentsRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new WithRepoItemRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithRepoItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/repos/{owner}/{repo}", pathParameters) {
        }
        /// <summary>
        /// Instantiates a new WithRepoItemRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithRepoItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/repos/{owner}/{repo}", rawUrl) {
        }
    }
}
