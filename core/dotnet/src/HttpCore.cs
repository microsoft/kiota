using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
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
        public HttpCore(IAuthenticationProvider authenticationProvider, HttpClient httpClient = null)
        {
            authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            client = httpClient ?? new HttpClient();
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
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var jsonDocument = JsonDocument.Parse(contentStream);
                var rootNode = new JsonParseNode(jsonDocument.RootElement);
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
                    using var jsonDocument = JsonDocument.Parse(contentStream);
                    var rootNode = new JsonParseNode(jsonDocument.RootElement);
                    response.Dispose();
                    requestInfo.Content?.Dispose();
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
                requestInfo.Headers.ToList().ForEach(x => message.Headers.Add(x.Key, x.Value));
            if(requestInfo.Content != null)
                message.Content = new StreamContent(requestInfo.Content); //TODO we're making a big assumption here and we probably need to default the content type in case it's not provided
            return message;
        }
    }
}
