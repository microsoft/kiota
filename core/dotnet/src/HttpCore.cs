using System;
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

            if(!requestInfo.Headers.ContainsKey(authorizationHeaderKey)) {
                var token = await authProvider.getAuthorizationToken(requestInfo.URI);
                if(string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("Could not get an authorization token");
                requestInfo.Headers.Add(authorizationHeaderKey, $"Bearer {token}");
            }
            
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
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
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
