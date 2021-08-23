// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Kiota.Http.HttpClient
{
    /// <summary>
    /// The <see cref="IHttpCore"/> implementation for sending requests.
    /// </summary>
    public class HttpCore : IHttpCore, IDisposable
    {
        private readonly System.Net.Http.HttpClient client;
        private readonly IAuthenticationProvider authProvider;
        private IParseNodeFactory pNodeFactory;
        private ISerializationWriterFactory sWriterFactory;
        private readonly bool createdClient;
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCore"/> class.
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="parseNodeFactory">The parse node factory.</param>
        /// <param name="serializationWriterFactory">The serialization writer factory.</param>
        /// <param name="httpClient">The native HTTP client.</param>
        /// </summary>
        public HttpCore(IAuthenticationProvider authenticationProvider, IParseNodeFactory parseNodeFactory = null, ISerializationWriterFactory serializationWriterFactory = null, System.Net.Http.HttpClient httpClient = null)
        {
            authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            createdClient = httpClient == null;
            client = httpClient ?? HttpClientBuilder.Create(authProvider);
            pNodeFactory = parseNodeFactory ?? ParseNodeFactoryRegistry.DefaultInstance;
            sWriterFactory = serializationWriterFactory ?? SerializationWriterFactoryRegistry.DefaultInstance;
        }
        /// <summary>Factory to use to get a serializer for payload serialization</summary>
        public ISerializationWriterFactory SerializationWriterFactory
        {
            get
            {
                return sWriterFactory;
            }
        }
        /// <summary>
        /// Send a <see cref="RequestInfo"/> instance with a collection instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInfo"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        public async Task<IEnumerable<ModelType>> SendCollectionAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default) where ModelType : IParsable
        {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
            {
                var rootNode = await GetRootParseNode(response);
                var result = rootNode.GetCollectionOfObjectValues<ModelType>();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, IEnumerable<ModelType>>(response);
        }
        /// <summary>
        /// Send a <see cref="RequestInfo"/> instance with an instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInfo"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        public async Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = null) where ModelType : IParsable
        {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
            {
                var rootNode = await GetRootParseNode(response);
                var result = rootNode.GetObjectValue<ModelType>();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
        }
        /// <summary>
        /// Send a <see cref="RequestInfo"/> instance with a primitive instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInfo"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        /// <returns></returns>
        public async Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default)
        {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
            {
                var modelType = typeof(ModelType);
                if(modelType == typeof(Stream))
                {
                    return (ModelType)(await response.Content.ReadAsStreamAsync() as object);
                }
                else
                {
                    var rootNode = await GetRootParseNode(response);
                    object result;
                    if(modelType == typeof(bool))
                    {
                        result = rootNode.GetBoolValue();
                    }
                    else if(modelType == typeof(string))
                    {
                        result = rootNode.GetStringValue();
                    }
                    else if(modelType == typeof(int))
                    {
                        result = rootNode.GetIntValue();
                    }
                    else if(modelType == typeof(float))
                    {
                        result = rootNode.GetFloatValue();
                    }
                    else if(modelType == typeof(double))
                    {
                        result = rootNode.GetDoubleValue();
                    }
                    else if(modelType == typeof(Guid))
                    {
                        result = rootNode.GetGuidValue();
                    }
                    else if(modelType == typeof(DateTimeOffset))
                    {
                        result = rootNode.GetDateTimeOffsetValue();
                    }
                    else throw new InvalidOperationException("error handling the response, unexpected type");
                    return (ModelType)result;
                }
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response);
        }
        /// <summary>
        /// Send a <see cref="RequestInfo"/> instance with an empty request body
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInfo"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        /// <returns></returns>
        public async Task SendNoContentAsync(RequestInfo requestInfo, IResponseHandler responseHandler = null)
        {
            var response = await GetHttpResponseMessage(requestInfo);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
                response.Dispose();
            else
                await responseHandler.HandleResponseAsync<HttpResponseMessage, object>(response);
        }
        private async Task<IParseNode> GetRootParseNode(HttpResponseMessage response)
        {
            var responseContentType = response.Content.Headers?.ContentType?.MediaType?.ToLowerInvariant();
            if(string.IsNullOrEmpty(responseContentType))
                throw new InvalidOperationException("no response content type header for deserialization");
            using var contentStream = await response.Content.ReadAsStreamAsync();
            var rootNode = pNodeFactory.GetRootParseNode(responseContentType, contentStream);
            response.Dispose();
            return rootNode;
        }
        private async Task<HttpResponseMessage> GetHttpResponseMessage(RequestInfo requestInfo)
        {
            if(requestInfo == null)
                throw new ArgumentNullException(nameof(requestInfo));

            await authProvider.AuthenticateRequestAsync(requestInfo);

            using var message = GetRequestMessageFromRequestInfo(requestInfo);
            var response = await this.client.SendAsync(message);
            if(response == null)
                throw new InvalidOperationException("Could not get a response after calling the service");
            return response;
        }
        private const string ContentTypeHeaderName = "content-type";
        private HttpRequestMessage GetRequestMessageFromRequestInfo(RequestInfo requestInfo)
        {
            var message = new HttpRequestMessage
            {
                Method = new System.Net.Http.HttpMethod(requestInfo.HttpMethod.ToString().ToUpperInvariant()),
                RequestUri = new Uri(requestInfo.URI +
                                        ((requestInfo.QueryParameters?.Any() ?? false) ?
                                            "?" + requestInfo.QueryParameters
                                                        .Select(x => $"{x.Key}{(x.Value == null ? string.Empty : "=")}{x.Value?.ToString() ?? string.Empty}")
                                                        .Aggregate((x, y) => $"{x}&{y}") :
                                            string.Empty)),
            };

            if(requestInfo.MiddlewareOptions.Any())
                requestInfo.MiddlewareOptions.ToList().ForEach(x => message.Options.Set(new HttpRequestOptionsKey<IMiddlewareOption>(x.GetType().FullName), x));
            if(requestInfo.Headers?.Any() ?? false)
                requestInfo.Headers.Where(x => !ContentTypeHeaderName.Equals(x.Key, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => message.Headers.Add(x.Key, x.Value));
            if(requestInfo.Content != null)
            {
                message.Content = new StreamContent(requestInfo.Content);
                if(requestInfo?.Headers?.ContainsKey(ContentTypeHeaderName) ?? false)
                    message.Content.Headers.ContentType = new MediaTypeHeaderValue(requestInfo.Headers[ContentTypeHeaderName]);
            }
            return message;
        }
        /// <summary>
        /// Enable the backing store with the provided <see cref="IBackingStoreFactory"/>
        /// </summary>
        /// <param name="backingStoreFactory">The <see cref="IBackingStoreFactory"/> to use</param>
        public void EnableBackingStore(IBackingStoreFactory backingStoreFactory)
        {
            pNodeFactory = ApiClientBuilder.EnableBackingStoreForParseNodeFactory(pNodeFactory) ?? throw new InvalidOperationException("Could not enable backing store for the parse node factory");
            sWriterFactory = ApiClientBuilder.EnableBackingStoreForSerializationWriterFactory(sWriterFactory) ?? throw new InvalidOperationException("Could not enable backing store for the serializer writer factory");
            if(backingStoreFactory != null)
                BackingStoreFactorySingleton.Instance = backingStoreFactory;
        }
        /// <summary>
        /// Dispose/cleanup the client
        /// </summary>
        public void Dispose()
        {
            if(createdClient)
                client?.Dispose();
        }
    }
}
