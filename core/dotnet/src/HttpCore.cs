using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kiota.Abstractions;
using Kiota.Abstractions.Serialization;
using KiotaCore.Serialization;

namespace KiotaCore
{
    public class HttpCore : IHttpCore
    {
        private const string authorizationHeaderKey = "Authorization";
        private readonly HttpClient client;
        private readonly IAuthenticationProvider authProvider;
        private readonly IParseNodeFactory pNodeFactory;
        public HttpCore(IAuthenticationProvider authenticationProvider, IParseNodeFactory parseNodeFactory = null, HttpClient httpClient = null)
        {
            authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            client = httpClient ?? new HttpClient();
            pNodeFactory = parseNodeFactory ?? new ParseNodeFactoryRegistry() {
                ContentTypeAssociatedFactories = new () {
                    {"application/json", new JsonParseNodeFactory() }
                }
            };
        }
        public async Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = null) where ModelType : class, IParsable<ModelType>, new()
        {
            if(requestInfo == null)
                throw new ArgumentNullException(nameof(requestInfo));

            await AddBearerIfNotPresent(requestInfo);
            
            using var message = GetRequestMessageFromRequestInfo(requestInfo);
            var response = await this.client.SendAsync(message);
            if(response == null)
                throw new InvalidOperationException("Could not get a response after calling the service");
            if(responseHandler == null) {
                var responseContentType = response.Content.Headers?.ContentType?.MediaType?.ToLowerInvariant();
                if(string.IsNullOrEmpty(responseContentType))
                    throw new InvalidOperationException("no response content type header for deserialization");
                using var contentStream = await response.Content.ReadAsStreamAsync();
                var rootNode = pNodeFactory.GetRootParseNode(responseContentType, contentStream);
                var result = rootNode.GetObjectValue<ModelType>();
                response.Dispose();
                requestInfo.Content?.Dispose();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
        }
        public async Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default) {
            if(requestInfo == null)
                throw new ArgumentNullException(nameof(requestInfo));

            await AddBearerIfNotPresent(requestInfo);
            
            using var message = GetRequestMessageFromRequestInfo(requestInfo);
            var response = await this.client.SendAsync(message);
            if(response == null)
                throw new InvalidOperationException("Could not get a response after calling the service");
            if(responseHandler == null) {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                var modelType = typeof(ModelType);
                if(modelType == typeof(Stream)) {
                    return (ModelType)(contentStream as object);
                } else {
                    var responseContentType = response.Content.Headers?.ContentType?.MediaType?.ToLowerInvariant();
                    if(string.IsNullOrEmpty(responseContentType))
                        throw new InvalidOperationException("no response content type header for deserialization");
                    var rootNode = pNodeFactory.GetRootParseNode(responseContentType, contentStream);
                    response.Dispose();
                    requestInfo.Content?.Dispose();
                    if(modelType == typeof(bool)) {
                        return (ModelType)(rootNode.GetBoolValue() as object);
                    } else if(modelType == typeof(string)) {
                        return (ModelType)(rootNode.GetStringValue() as object);
                    } else if(modelType == typeof(int)) {
                        return (ModelType)(rootNode.GetIntValue() as object);
                    } else if(modelType == typeof(float)) {
                        return (ModelType)(rootNode.GetFloatValue() as object);
                    } else if(modelType == typeof(double)) {
                        return (ModelType)(rootNode.GetDoubleValue() as object);
                    } else if(modelType == typeof(Guid)) {
                        return (ModelType)(rootNode.GetGuidValue() as object);
                    } else if(modelType == typeof(DateTimeOffset)) {
                        return (ModelType)(rootNode.GetDateTimeOffsetValue() as object);
                    } else throw new InvalidOperationException("error handling the response, unexpected type");
                }
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
        }

        private async Task AddBearerIfNotPresent(RequestInfo requestInfo) {
            if(!requestInfo.Headers.ContainsKey(authorizationHeaderKey)) {
                var token = await authProvider.getAuthorizationToken(requestInfo.URI);
                if(string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("Could not get an authorization token");
                requestInfo.Headers.Add(authorizationHeaderKey, $"Bearer {token}");
            }
        }
        public async Task SendNoContentAsync(RequestInfo requestInfo, IResponseHandler responseHandler = null)
        {
            if(requestInfo == null)
                throw new ArgumentNullException(nameof(requestInfo));
            
            await AddBearerIfNotPresent(requestInfo);
            
            using var message = GetRequestMessageFromRequestInfo(requestInfo);
            var response = await this.client.SendAsync(message);
            if(response == null)
                throw new InvalidOperationException("Could not get a response after calling the service");
            if(responseHandler != null) 
                await responseHandler.HandleResponseAsync<HttpResponseMessage, object>(response);
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
                requestInfo.Headers.Where(x => !contentTypeHeaderName.Equals(x.Key, StringComparison.InvariantCultureIgnoreCase)).ToList().ForEach(x => message.Headers.Add(x.Key, x.Value));
            if(requestInfo.Content != null) {
                message.Content = new StreamContent(requestInfo.Content);
                if(requestInfo?.Headers?.ContainsKey(contentTypeHeaderName) ?? false)
                    message.Content.Headers.ContentType = new MediaTypeHeaderValue(requestInfo.Headers[contentTypeHeaderName]);
            }
            return message;
        }
    }
}
