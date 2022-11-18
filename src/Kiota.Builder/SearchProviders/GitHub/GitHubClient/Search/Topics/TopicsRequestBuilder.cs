using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Search.Topics {
    /// <summary>Builds and executes requests for operations under \search\topics</summary>
    public class TopicsRequestBuilder {
        /// <summary>Path parameters for the request</summary>
        private Dictionary<string, object> PathParameters { get; set; }
        /// <summary>The request adapter to use to execute the requests.</summary>
        private IRequestAdapter RequestAdapter { get; set; }
        /// <summary>Url template to use to build the URL for the current request builder</summary>
        private string UrlTemplate { get; set; }
        /// <summary>
        /// Instantiates a new TopicsRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public TopicsRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) {
            _ = pathParameters ?? throw new ArgumentNullException(nameof(pathParameters));
            _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
            UrlTemplate = "{+baseurl}/search/topics{?q*}";
            var urlTplParams = new Dictionary<string, object>(pathParameters);
            PathParameters = urlTplParams;
            RequestAdapter = requestAdapter;
        }
        /// <summary>
        /// Instantiates a new TopicsRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public TopicsRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) {
            if(string.IsNullOrEmpty(rawUrl)) throw new ArgumentNullException(nameof(rawUrl));
            _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
            UrlTemplate = "{+baseurl}/search/topics{?q*}";
            var urlTplParams = new Dictionary<string, object>();
            urlTplParams.Add("request-raw-url", rawUrl);
            PathParameters = urlTplParams;
            RequestAdapter = requestAdapter;
        }
        /// <summary>
        /// Find topics via various criteria. Results are sorted by best match. This method returns up to 100 results [per page](https://docs.github.com/rest/overview/resources-in-the-rest-api#pagination). See &quot;[Searching topics](https://help.github.com/articles/searching-topics/)&quot; for a detailed list of qualifiers.When searching for topics, you can get text match metadata for the topic&apos;s **short\_description**, **description**, **name**, or **display\_name** field when you pass the `text-match` media type. For more details about how to receive highlighted search results, see [Text match metadata](https://docs.github.com/rest/reference/search#text-match-metadata).For example, if you want to search for topics related to Ruby that are featured on https://github.com/topics. Your query might look like this:`q=ruby+is:featured`This query searches for topics with the keyword `ruby` and limits the results to find only topics that are featured. The topics that are the best match for the query appear first in the search results.
        /// </summary>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public RequestInformation CreateGetRequestInformation(Action<TopicsRequestBuilderGetRequestConfiguration> requestConfiguration = default) {
            var requestInfo = new RequestInformation {
                HttpMethod = Method.GET,
                UrlTemplate = UrlTemplate,
                PathParameters = PathParameters,
            };
            requestInfo.Headers.Add("Accept", "application/json");
            if (requestConfiguration != null) {
                var requestConfig = new TopicsRequestBuilderGetRequestConfiguration();
                requestConfiguration.Invoke(requestConfig);
                requestInfo.AddQueryParameters(requestConfig.QueryParameters);
                requestInfo.AddRequestOptions(requestConfig.Options);
                requestInfo.AddHeaders(requestConfig.Headers);
            }
            return requestInfo;
        }
        /// <summary>
        /// Find topics via various criteria. Results are sorted by best match. This method returns up to 100 results [per page](https://docs.github.com/rest/overview/resources-in-the-rest-api#pagination). See &quot;[Searching topics](https://help.github.com/articles/searching-topics/)&quot; for a detailed list of qualifiers.When searching for topics, you can get text match metadata for the topic&apos;s **short\_description**, **description**, **name**, or **display\_name** field when you pass the `text-match` media type. For more details about how to receive highlighted search results, see [Text match metadata](https://docs.github.com/rest/reference/search#text-match-metadata).For example, if you want to search for topics related to Ruby that are featured on https://github.com/topics. Your query might look like this:`q=ruby+is:featured`This query searches for topics with the keyword `ruby` and limits the results to find only topics that are featured. The topics that are the best match for the query appear first in the search results.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public async Task<TopicsResponse> GetAsync(Action<TopicsRequestBuilderGetRequestConfiguration> requestConfiguration = default, CancellationToken cancellationToken = default) {
            var requestInfo = CreateGetRequestInformation(requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
                {"415", Topics415Error.CreateFromDiscriminatorValue},
            };
            return await RequestAdapter.SendAsync<TopicsResponse>(requestInfo, TopicsResponse.CreateFromDiscriminatorValue, errorMapping, cancellationToken);
        }
        /// <summary>Find topics via various criteria. Results are sorted by best match. This method returns up to 100 results [per page](https://docs.github.com/rest/overview/resources-in-the-rest-api#pagination). See &quot;[Searching topics](https://help.github.com/articles/searching-topics/)&quot; for a detailed list of qualifiers.When searching for topics, you can get text match metadata for the topic&apos;s **short\_description**, **description**, **name**, or **display\_name** field when you pass the `text-match` media type. For more details about how to receive highlighted search results, see [Text match metadata](https://docs.github.com/rest/reference/search#text-match-metadata).For example, if you want to search for topics related to Ruby that are featured on https://github.com/topics. Your query might look like this:`q=ruby+is:featured`This query searches for topics with the keyword `ruby` and limits the results to find only topics that are featured. The topics that are the best match for the query appear first in the search results.</summary>
        public class TopicsRequestBuilderGetQueryParameters {
            /// <summary>The query contains one or more search keywords and qualifiers. Qualifiers allow you to limit your search to specific areas of GitHub. The REST API supports the same qualifiers as GitHub.com. To learn more about the format of the query, see [Constructing a search query](https://docs.github.com/rest/reference/search#constructing-a-search-query).</summary>
            public string Q { get; set; }
        }
        /// <summary>Configuration for the request such as headers, query parameters, and middleware options.</summary>
        public class TopicsRequestBuilderGetRequestConfiguration {
            /// <summary>Request headers</summary>
            public IDictionary<string, string> Headers { get; set; }
            /// <summary>Request options</summary>
            public IList<IRequestOption> Options { get; set; }
            /// <summary>Request query parameters</summary>
            public TopicsRequestBuilderGetQueryParameters QueryParameters { get; set; } = new TopicsRequestBuilderGetQueryParameters();
            /// <summary>
            /// Instantiates a new topicsRequestBuilderGetRequestConfiguration and sets the default values.
            /// </summary>
            public TopicsRequestBuilderGetRequestConfiguration() {
                Options = new List<IRequestOption>();
                Headers = new Dictionary<string, string>();
            }
        }
    }
}
