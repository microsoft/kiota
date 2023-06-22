using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.User.Installations
{
    /// <summary>
    /// Builds and executes requests for operations under \user\installations
    /// </summary>
    public class InstallationsRequestBuilder : BaseRequestBuilder
    {
        /// <summary>
        /// Instantiates a new InstallationsRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public InstallationsRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/user/installations{?per_page*,page*}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new InstallationsRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public InstallationsRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/user/installations{?per_page*,page*}", rawUrl)
        {
        }
        /// <summary>
        /// Lists installations of your GitHub App that the authenticated user has explicit permission (`:read`, `:write`, or `:admin`) to access.You must use a [user access token](https://docs.github.com/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app), created for a user who has authorized your GitHub App, to access this endpoint.The authenticated user has explicit permission to access repositories they own, repositories where they are a collaborator, and repositories that they can access through an organization membership.You can find the permissions for the installation under the `permissions` key.
        /// API method documentation <see href="https://docs.github.com/rest/reference/apps#list-app-installations-accessible-to-the-user-access-token" />
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task<InstallationsResponse?> GetAsync(Action<InstallationsRequestBuilderGetRequestConfiguration>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task<InstallationsResponse> GetAsync(Action<InstallationsRequestBuilderGetRequestConfiguration> requestConfiguration = default, CancellationToken cancellationToken = default) {
#endif
            var requestInfo = ToGetRequestInformation(requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
                {"401", BasicError.CreateFromDiscriminatorValue},
                {"403", BasicError.CreateFromDiscriminatorValue},
            };
            return await RequestAdapter.SendAsync<InstallationsResponse>(requestInfo, InstallationsResponse.CreateFromDiscriminatorValue, errorMapping, cancellationToken);
        }
        /// <summary>
        /// Lists installations of your GitHub App that the authenticated user has explicit permission (`:read`, `:write`, or `:admin`) to access.You must use a [user access token](https://docs.github.com/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app), created for a user who has authorized your GitHub App, to access this endpoint.The authenticated user has explicit permission to access repositories they own, repositories where they are a collaborator, and repositories that they can access through an organization membership.You can find the permissions for the installation under the `permissions` key.
        /// </summary>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToGetRequestInformation(Action<InstallationsRequestBuilderGetRequestConfiguration>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToGetRequestInformation(Action<InstallationsRequestBuilderGetRequestConfiguration> requestConfiguration = default) {
#endif
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = UrlTemplate,
                PathParameters = PathParameters,
            };
            requestInfo.Headers.Add("Accept", "application/json");
            if (requestConfiguration != null)
            {
                var requestConfig = new InstallationsRequestBuilderGetRequestConfiguration();
                requestConfiguration.Invoke(requestConfig);
                requestInfo.AddQueryParameters(requestConfig.QueryParameters);
                requestInfo.AddRequestOptions(requestConfig.Options);
                requestInfo.AddHeaders(requestConfig.Headers);
            }
            return requestInfo;
        }
        /// <summary>
        /// Lists installations of your GitHub App that the authenticated user has explicit permission (`:read`, `:write`, or `:admin`) to access.You must use a [user access token](https://docs.github.com/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app), created for a user who has authorized your GitHub App, to access this endpoint.The authenticated user has explicit permission to access repositories they own, repositories where they are a collaborator, and repositories that they can access through an organization membership.You can find the permissions for the installation under the `permissions` key.
        /// </summary>
        public class InstallationsRequestBuilderGetQueryParameters
        {
            /// <summary>Page number of the results to fetch.</summary>
            public int? Page
            {
                get; set;
            }
            /// <summary>The number of results per page (max 100).</summary>
            public int? Per_page
            {
                get; set;
            }
        }
        /// <summary>
        /// Configuration for the request such as headers, query parameters, and middleware options.
        /// </summary>
        public class InstallationsRequestBuilderGetRequestConfiguration
        {
            /// <summary>Request headers</summary>
            public RequestHeaders Headers
            {
                get; set;
            }
            /// <summary>Request options</summary>
            public IList<IRequestOption> Options
            {
                get; set;
            }
            /// <summary>Request query parameters</summary>
            public InstallationsRequestBuilderGetQueryParameters QueryParameters { get; set; } = new InstallationsRequestBuilderGetQueryParameters();
            /// <summary>
            /// Instantiates a new installationsRequestBuilderGetRequestConfiguration and sets the default values.
            /// </summary>
            public InstallationsRequestBuilderGetRequestConfiguration()
            {
                Options = new List<IRequestOption>();
                Headers = new RequestHeaders();
            }
        }
    }
}
