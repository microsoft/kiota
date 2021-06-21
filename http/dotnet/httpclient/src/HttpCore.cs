using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Http.HttpClient
{
    public class HttpCore : IHttpCore
    {
        private const string authorizationHeaderKey = "Authorization";
        private readonly System.Net.Http.HttpClient client;
        private readonly IAuthenticationProvider authProvider;
        private readonly IParseNodeFactory pNodeFactory;
        public HttpCore(IAuthenticationProvider authenticationProvider, IParseNodeFactory parseNodeFactory = null, System.Net.Http.HttpClient httpClient = null)
        {
            authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            client = httpClient ?? new System.Net.Http.HttpClient();
            pNodeFactory = parseNodeFactory ?? ParseNodeFactoryRegistry.DefaultInstance;
        }
        public async Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = null) where ModelType : IParsable
        {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null) {
                var rootNode = await GetRootParseNode(response);
                var result = rootNode.GetObjectValue<ModelType>();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
        }
        public async Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default) {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null) {
                var modelType = typeof(ModelType);
                if(modelType == typeof(Stream)) {
                    return (ModelType)(await response.Content.ReadAsStreamAsync() as object);
                } else {
                    var rootNode = await GetRootParseNode(response);
                    object result;
                    if(modelType == typeof(bool)) {
                        result = rootNode.GetBoolValue();
                    } else if(modelType == typeof(string)) {
                        result = rootNode.GetStringValue();
                    } else if(modelType == typeof(int)) {
                        result = rootNode.GetIntValue();
                    } else if(modelType == typeof(float)) {
                        result = rootNode.GetFloatValue();
                    } else if(modelType == typeof(double)) {
                        result = rootNode.GetDoubleValue();
                    } else if(modelType == typeof(Guid)) {
                        result = rootNode.GetGuidValue();
                    } else if(modelType == typeof(DateTimeOffset)) {
                        result = rootNode.GetDateTimeOffsetValue();
                    } else throw new InvalidOperationException("error handling the response, unexpected type");
                    return (ModelType)result;
                }
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
        }

        private async Task AddBearerIfNotPresent(RequestInfo requestInfo) {
            if(!requestInfo.Headers.ContainsKey(authorizationHeaderKey)) {
                var token = await authProvider.GetAuthorizationToken(requestInfo.URI);
                if(string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("Could not get an authorization token");
                requestInfo.Headers.Add(authorizationHeaderKey, $"Bearer {token}");
            }
        }
        public async Task SendNoContentAsync(RequestInfo requestInfo, IResponseHandler responseHandler = null)
        {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null) 
                response.Dispose();
            else
                await responseHandler.HandleResponseAsync<HttpResponseMessage, object>(response);
        }
        private async Task<IParseNode> GetRootParseNode(HttpResponseMessage response) {
            var responseContentType = response.Content.Headers?.ContentType?.MediaType?.ToLowerInvariant();
            if(string.IsNullOrEmpty(responseContentType))
                throw new InvalidOperationException("no response content type header for deserialization");
            using var contentStream = await response.Content.ReadAsStreamAsync();
            var rootNode = pNodeFactory.GetRootParseNode(responseContentType, contentStream);
            response.Dispose();
            return rootNode;
        }
        private async Task<HttpResponseMessage> GetHttpResponseMessage(RequestInfo requestInfo) {
            if(requestInfo == null)
                throw new ArgumentNullException(nameof(requestInfo));
            
            await AddBearerIfNotPresent(requestInfo);
            
            using var message = GetRequestMessageFromRequestInfo(requestInfo);
            var response = await this.client.SendAsync(message);
            if(response == null)
                throw new InvalidOperationException("Could not get a response after calling the service");
            return response;
        }
        private const string contentTypeHeaderName = "content-type";
        private HttpRequestMessage GetRequestMessageFromRequestInfo(RequestInfo requestInfo) {
            var message = new HttpRequestMessage {
                Method = new System.Net.Http.HttpMethod(requestInfo.HttpMethod.ToString().ToUpperInvariant()),
                RequestUri = new Uri(requestInfo.URI + 
                                        ((requestInfo.QueryParameters?.Any() ?? false) ? 
                                            "?" + requestInfo.QueryParameters
                                                        .Select(x => $"{x.Key}{(x.Value == null ? string.Empty : "=")}{x.Value?.ToString() ?? string.Empty}")
                                                        .Aggregate((x, y) => $"{x}&{y}") :
                                            string.Empty)),
                
            };
            if(requestInfo.Headers?.Any() ?? false)
                requestInfo.Headers.Where(x => !contentTypeHeaderName.Equals(x.Key, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => message.Headers.Add(x.Key, x.Value));
            if(requestInfo.Content != null) {
                message.Content = new StreamContent(requestInfo.Content);
                if(requestInfo?.Headers?.ContainsKey(contentTypeHeaderName) ?? false)
                    message.Content.Headers.ContentType = new MediaTypeHeaderValue(requestInfo.Headers[contentTypeHeaderName]);
            }
            return message;
        }
    }
}
