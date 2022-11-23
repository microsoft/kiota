using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Contents.Item {
    /// <summary>Builds and executes requests for operations under \repos\{owner}\{repo}\contents\{path}</summary>
    public class WithPathItemRequestBuilder {
        /// <summary>Path parameters for the request</summary>
        private Dictionary<string, object> PathParameters { get; set; }
        /// <summary>The request adapter to use to execute the requests.</summary>
        private IRequestAdapter RequestAdapter { get; set; }
        /// <summary>Url template to use to build the URL for the current request builder</summary>
        private string UrlTemplate { get; set; }
        /// <summary>
        /// Instantiates a new WithPathItemRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithPathItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) {
            _ = pathParameters ?? throw new ArgumentNullException(nameof(pathParameters));
            _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
            UrlTemplate = "{+baseurl}/repos/{owner}/{repo}/contents/{path}{?ref*}";
            var urlTplParams = new Dictionary<string, object>(pathParameters);
            PathParameters = urlTplParams;
            RequestAdapter = requestAdapter;
        }
        /// <summary>
        /// Instantiates a new WithPathItemRequestBuilder and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithPathItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) {
            if(string.IsNullOrEmpty(rawUrl)) throw new ArgumentNullException(nameof(rawUrl));
            _ = requestAdapter ?? throw new ArgumentNullException(nameof(requestAdapter));
            UrlTemplate = "{+baseurl}/repos/{owner}/{repo}/contents/{path}{?ref*}";
            var urlTplParams = new Dictionary<string, object>();
            urlTplParams.Add("request-raw-url", rawUrl);
            PathParameters = urlTplParams;
            RequestAdapter = requestAdapter;
        }
        /// <summary>
        /// Deletes a file in a repository.You can provide an additional `committer` parameter, which is an object containing information about the committer. Or, you can provide an `author` parameter, which is an object containing information about the author.The `author` section is optional and is filled in with the `committer` information if omitted. If the `committer` information is omitted, the authenticated user&apos;s information is used.You must provide values for both `name` and `email`, whether you choose to use `author` or `committer`. Otherwise, you&apos;ll receive a `422` status code.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public RequestInformation CreateDeleteRequestInformation(WithPathDeleteRequestBody body, Action<WithPathItemRequestBuilderDeleteRequestConfiguration> requestConfiguration = default) {
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = new RequestInformation {
                HttpMethod = Method.DELETE,
                UrlTemplate = UrlTemplate,
                PathParameters = PathParameters,
            };
            requestInfo.Headers.Add("Accept", "application/json");
            requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);
            if (requestConfiguration != null) {
                var requestConfig = new WithPathItemRequestBuilderDeleteRequestConfiguration();
                requestConfiguration.Invoke(requestConfig);
                requestInfo.AddRequestOptions(requestConfig.Options);
                requestInfo.AddHeaders(requestConfig.Headers);
            }
            return requestInfo;
        }
        /// <summary>
        /// Gets the contents of a file or directory in a repository. Specify the file path or directory in `:path`. If you omit`:path`, you will receive the contents of the repository&apos;s root directory. See the description below regarding what the API response includes for directories. Files and symlinks support [a custom media type](https://docs.github.com/rest/reference/repos#custom-media-types) forretrieving the raw content or rendered HTML (when supported). All content types support [a custom mediatype](https://docs.github.com/rest/reference/repos#custom-media-types) to ensure the content is returned in a consistentobject format.**Note**:*   To get a repository&apos;s contents recursively, you can [recursively get the tree](https://docs.github.com/rest/reference/git#trees).*   This API has an upper limit of 1,000 files for a directory. If you need to retrieve more files, use the [Git TreesAPI](https://docs.github.com/rest/reference/git#get-a-tree).*   This API supports files up to 1 megabyte in size.#### If the content is a directoryThe response will be an array of objects, one object for each item in the directory.When listing the contents of a directory, submodules have their &quot;type&quot; specified as &quot;file&quot;. Logically, the value_should_ be &quot;submodule&quot;. This behavior exists in API v3 [for backwards compatibility purposes](https://git.io/v1YCW).In the next major version of the API, the type will be returned as &quot;submodule&quot;.#### If the content is a symlink If the requested `:path` points to a symlink, and the symlink&apos;s target is a normal file in the repository, then theAPI responds with the content of the file (in the format shown in the example. Otherwise, the API responds with an object describing the symlink itself.#### If the content is a submoduleThe `submodule_git_url` identifies the location of the submodule repository, and the `sha` identifies a specificcommit within the submodule repository. Git uses the given URL when cloning the submodule repository, and checks outthe submodule at that specific commit.If the submodule repository is not hosted on github.com, the Git URLs (`git_url` and `_links[&quot;git&quot;]`) and thegithub.com URLs (`html_url` and `_links[&quot;html&quot;]`) will have null values.
        /// </summary>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public RequestInformation CreateGetRequestInformation(Action<WithPathItemRequestBuilderGetRequestConfiguration> requestConfiguration = default) {
            var requestInfo = new RequestInformation {
                HttpMethod = Method.GET,
                UrlTemplate = UrlTemplate,
                PathParameters = PathParameters,
            };
            requestInfo.Headers.Add("Accept", "application/json");
            if (requestConfiguration != null) {
                var requestConfig = new WithPathItemRequestBuilderGetRequestConfiguration();
                requestConfiguration.Invoke(requestConfig);
                requestInfo.AddQueryParameters(requestConfig.QueryParameters);
                requestInfo.AddRequestOptions(requestConfig.Options);
                requestInfo.AddHeaders(requestConfig.Headers);
            }
            return requestInfo;
        }
        /// <summary>
        /// Creates a new file or replaces an existing file in a repository.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public RequestInformation CreatePutRequestInformation(WithPathPutRequestBody body, Action<WithPathItemRequestBuilderPutRequestConfiguration> requestConfiguration = default) {
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = new RequestInformation {
                HttpMethod = Method.PUT,
                UrlTemplate = UrlTemplate,
                PathParameters = PathParameters,
            };
            requestInfo.Headers.Add("Accept", "application/json");
            requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);
            if (requestConfiguration != null) {
                var requestConfig = new WithPathItemRequestBuilderPutRequestConfiguration();
                requestConfiguration.Invoke(requestConfig);
                requestInfo.AddRequestOptions(requestConfig.Options);
                requestInfo.AddHeaders(requestConfig.Headers);
            }
            return requestInfo;
        }
        /// <summary>
        /// Deletes a file in a repository.You can provide an additional `committer` parameter, which is an object containing information about the committer. Or, you can provide an `author` parameter, which is an object containing information about the author.The `author` section is optional and is filled in with the `committer` information if omitted. If the `committer` information is omitted, the authenticated user&apos;s information is used.You must provide values for both `name` and `email`, whether you choose to use `author` or `committer`. Otherwise, you&apos;ll receive a `422` status code.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public async Task<FileCommit> DeleteAsync(WithPathDeleteRequestBody body, Action<WithPathItemRequestBuilderDeleteRequestConfiguration> requestConfiguration = default, CancellationToken cancellationToken = default) {
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = CreateDeleteRequestInformation(body, requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
                {"404", BasicError.CreateFromDiscriminatorValue},
                {"409", BasicError.CreateFromDiscriminatorValue},
                {"422", ValidationError.CreateFromDiscriminatorValue},
                {"503", FileCommit503Error.CreateFromDiscriminatorValue},
            };
            return await RequestAdapter.SendAsync<FileCommit>(requestInfo, FileCommit.CreateFromDiscriminatorValue, errorMapping, cancellationToken);
        }
        /// <summary>
        /// Gets the contents of a file or directory in a repository. Specify the file path or directory in `:path`. If you omit`:path`, you will receive the contents of the repository&apos;s root directory. See the description below regarding what the API response includes for directories. Files and symlinks support [a custom media type](https://docs.github.com/rest/reference/repos#custom-media-types) forretrieving the raw content or rendered HTML (when supported). All content types support [a custom mediatype](https://docs.github.com/rest/reference/repos#custom-media-types) to ensure the content is returned in a consistentobject format.**Note**:*   To get a repository&apos;s contents recursively, you can [recursively get the tree](https://docs.github.com/rest/reference/git#trees).*   This API has an upper limit of 1,000 files for a directory. If you need to retrieve more files, use the [Git TreesAPI](https://docs.github.com/rest/reference/git#get-a-tree).*   This API supports files up to 1 megabyte in size.#### If the content is a directoryThe response will be an array of objects, one object for each item in the directory.When listing the contents of a directory, submodules have their &quot;type&quot; specified as &quot;file&quot;. Logically, the value_should_ be &quot;submodule&quot;. This behavior exists in API v3 [for backwards compatibility purposes](https://git.io/v1YCW).In the next major version of the API, the type will be returned as &quot;submodule&quot;.#### If the content is a symlink If the requested `:path` points to a symlink, and the symlink&apos;s target is a normal file in the repository, then theAPI responds with the content of the file (in the format shown in the example. Otherwise, the API responds with an object describing the symlink itself.#### If the content is a submoduleThe `submodule_git_url` identifies the location of the submodule repository, and the `sha` identifies a specificcommit within the submodule repository. Git uses the given URL when cloning the submodule repository, and checks outthe submodule at that specific commit.If the submodule repository is not hosted on github.com, the Git URLs (`git_url` and `_links[&quot;git&quot;]`) and thegithub.com URLs (`html_url` and `_links[&quot;html&quot;]`) will have null values.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public async Task<WithPathResponse> GetAsync(Action<WithPathItemRequestBuilderGetRequestConfiguration> requestConfiguration = default, CancellationToken cancellationToken = default) {
            var requestInfo = CreateGetRequestInformation(requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
                {"403", BasicError.CreateFromDiscriminatorValue},
                {"404", BasicError.CreateFromDiscriminatorValue},
            };
            return await RequestAdapter.SendAsync<WithPathResponse>(requestInfo, WithPathResponse.CreateFromDiscriminatorValue, errorMapping, cancellationToken);
        }
        /// <summary>
        /// Creates a new file or replaces an existing file in a repository.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        public async Task<FileCommit> PutAsync(WithPathPutRequestBody body, Action<WithPathItemRequestBuilderPutRequestConfiguration> requestConfiguration = default, CancellationToken cancellationToken = default) {
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = CreatePutRequestInformation(body, requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
                {"404", BasicError.CreateFromDiscriminatorValue},
                {"409", BasicError.CreateFromDiscriminatorValue},
                {"422", ValidationError.CreateFromDiscriminatorValue},
            };
            return await RequestAdapter.SendAsync<FileCommit>(requestInfo, FileCommit.CreateFromDiscriminatorValue, errorMapping, cancellationToken);
        }
        /// <summary>Configuration for the request such as headers, query parameters, and middleware options.</summary>
        public class WithPathItemRequestBuilderDeleteRequestConfiguration {
            /// <summary>Request headers</summary>
            public IDictionary<string, string> Headers { get; set; }
            /// <summary>Request options</summary>
            public IList<IRequestOption> Options { get; set; }
            /// <summary>
            /// Instantiates a new WithPathItemRequestBuilderDeleteRequestConfiguration and sets the default values.
            /// </summary>
            public WithPathItemRequestBuilderDeleteRequestConfiguration() {
                Options = new List<IRequestOption>();
                Headers = new Dictionary<string, string>();
            }
        }
        /// <summary>Gets the contents of a file or directory in a repository. Specify the file path or directory in `:path`. If you omit`:path`, you will receive the contents of the repository&apos;s root directory. See the description below regarding what the API response includes for directories. Files and symlinks support [a custom media type](https://docs.github.com/rest/reference/repos#custom-media-types) forretrieving the raw content or rendered HTML (when supported). All content types support [a custom mediatype](https://docs.github.com/rest/reference/repos#custom-media-types) to ensure the content is returned in a consistentobject format.**Note**:*   To get a repository&apos;s contents recursively, you can [recursively get the tree](https://docs.github.com/rest/reference/git#trees).*   This API has an upper limit of 1,000 files for a directory. If you need to retrieve more files, use the [Git TreesAPI](https://docs.github.com/rest/reference/git#get-a-tree).*   This API supports files up to 1 megabyte in size.#### If the content is a directoryThe response will be an array of objects, one object for each item in the directory.When listing the contents of a directory, submodules have their &quot;type&quot; specified as &quot;file&quot;. Logically, the value_should_ be &quot;submodule&quot;. This behavior exists in API v3 [for backwards compatibility purposes](https://git.io/v1YCW).In the next major version of the API, the type will be returned as &quot;submodule&quot;.#### If the content is a symlink If the requested `:path` points to a symlink, and the symlink&apos;s target is a normal file in the repository, then theAPI responds with the content of the file (in the format shown in the example. Otherwise, the API responds with an object describing the symlink itself.#### If the content is a submoduleThe `submodule_git_url` identifies the location of the submodule repository, and the `sha` identifies a specificcommit within the submodule repository. Git uses the given URL when cloning the submodule repository, and checks outthe submodule at that specific commit.If the submodule repository is not hosted on github.com, the Git URLs (`git_url` and `_links[&quot;git&quot;]`) and thegithub.com URLs (`html_url` and `_links[&quot;html&quot;]`) will have null values.</summary>
        public class WithPathItemRequestBuilderGetQueryParameters {
            /// <summary>The name of the commit/branch/tag. Default: the repository’s default branch (usually `master`)</summary>
            public string Ref { get; set; }
        }
        /// <summary>Configuration for the request such as headers, query parameters, and middleware options.</summary>
        public class WithPathItemRequestBuilderGetRequestConfiguration {
            /// <summary>Request headers</summary>
            public IDictionary<string, string> Headers { get; set; }
            /// <summary>Request options</summary>
            public IList<IRequestOption> Options { get; set; }
            /// <summary>Request query parameters</summary>
            public WithPathItemRequestBuilderGetQueryParameters QueryParameters { get; set; } = new WithPathItemRequestBuilderGetQueryParameters();
            /// <summary>
            /// Instantiates a new WithPathItemRequestBuilderGetRequestConfiguration and sets the default values.
            /// </summary>
            public WithPathItemRequestBuilderGetRequestConfiguration() {
                Options = new List<IRequestOption>();
                Headers = new Dictionary<string, string>();
            }
        }
        /// <summary>Configuration for the request such as headers, query parameters, and middleware options.</summary>
        public class WithPathItemRequestBuilderPutRequestConfiguration {
            /// <summary>Request headers</summary>
            public IDictionary<string, string> Headers { get; set; }
            /// <summary>Request options</summary>
            public IList<IRequestOption> Options { get; set; }
            /// <summary>
            /// Instantiates a new WithPathItemRequestBuilderPutRequestConfiguration and sets the default values.
            /// </summary>
            public WithPathItemRequestBuilderPutRequestConfiguration() {
                Options = new List<IRequestOption>();
                Headers = new Dictionary<string, string>();
            }
        }
        /// <summary>Composed type wrapper for classes WithPathResponseMember1, contentFile, contentSymlink, contentSubmodule</summary>
        public class WithPathResponse : IAdditionalDataHolder, IParsable {
            /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
            public IDictionary<string, object> AdditionalData { get; set; }
            /// <summary>Composed type representation for type contentFile</summary>
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentFile ContentFile { get; set; }
            /// <summary>Composed type representation for type contentSubmodule</summary>
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentSubmodule ContentSubmodule { get; set; }
            /// <summary>Composed type representation for type contentSymlink</summary>
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentSymlink ContentSymlink { get; set; }
            /// <summary>Serialization hint for the current wrapper.</summary>
            public string SerializationHint { get; set; }
            /// <summary>Composed type representation for type WithPathResponseMember1</summary>
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.WithPathResponseMember1 WithPathResponseMember1 { get; set; }
            /// <summary>
            /// Instantiates a new WithPathResponse and sets the default values.
            /// </summary>
            public WithPathResponse() {
                AdditionalData = new Dictionary<string, object>();
            }
            /// <summary>
            /// Creates a new instance of the appropriate class based on discriminator value
            /// </summary>
            /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
            public static WithPathResponse CreateFromDiscriminatorValue(IParseNode parseNode) {
                _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
                var mappingValue = parseNode.GetChildNode("")?.GetStringValue();
                var result = new WithPathResponse();
                if("content-file".Equals(mappingValue, StringComparison.OrdinalIgnoreCase)) {
                    result.ContentFile = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentFile();
                }
                else if("content-submodule".Equals(mappingValue, StringComparison.OrdinalIgnoreCase)) {
                    result.ContentSubmodule = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentSubmodule();
                }
                else if("content-symlink".Equals(mappingValue, StringComparison.OrdinalIgnoreCase)) {
                    result.ContentSymlink = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentSymlink();
                }
                else if("".Equals(mappingValue, StringComparison.OrdinalIgnoreCase)) {
                    result.WithPathResponseMember1 = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.WithPathResponseMember1();
                }
                return result;
            }
            /// <summary>
            /// The deserialization information for the current model
            /// </summary>
            public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
                if(ContentFile != null) {
                    return ContentFile.GetFieldDeserializers();
                }
                else if(ContentSubmodule != null) {
                    return ContentSubmodule.GetFieldDeserializers();
                }
                else if(ContentSymlink != null) {
                    return ContentSymlink.GetFieldDeserializers();
                }
                else if(WithPathResponseMember1 != null) {
                    return WithPathResponseMember1.GetFieldDeserializers();
                }
                return new Dictionary<string, Action<IParseNode>>();
            }
            /// <summary>
            /// Serializes information the current object
            /// </summary>
            /// <param name="writer">Serialization writer to use to serialize this model</param>
            public void Serialize(ISerializationWriter writer) {
                _ = writer ?? throw new ArgumentNullException(nameof(writer));
                if(ContentFile != null) {
                    writer.WriteObjectValue<Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentFile>(null, ContentFile);
                }
                else if(ContentSubmodule != null) {
                    writer.WriteObjectValue<Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentSubmodule>(null, ContentSubmodule);
                }
                else if(ContentSymlink != null) {
                    writer.WriteObjectValue<Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.ContentSymlink>(null, ContentSymlink);
                }
                else if(WithPathResponseMember1 != null) {
                    writer.WriteObjectValue<Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.WithPathResponseMember1>(null, WithPathResponseMember1);
                }
                writer.WriteAdditionalData(AdditionalData);
            }
        }
    }
}
