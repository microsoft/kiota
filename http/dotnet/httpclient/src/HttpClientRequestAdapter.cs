// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Threading;

namespace Microsoft.Kiota.Http.HttpClientLibrary
{
    /// <summary>
    /// The <see cref="IRequestAdapter"/> implementation for sending requests.
    /// </summary>
    public class HttpClientRequestAdapter : IRequestAdapter, IDisposable
    {
        private readonly HttpClient client;
        private readonly IAuthenticationProvider authProvider;
        private IParseNodeFactory pNodeFactory;
        private ISerializationWriterFactory sWriterFactory;
        private readonly bool createdClient;
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientRequestAdapter"/> class.
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="parseNodeFactory">The parse node factory.</param>
        /// <param name="serializationWriterFactory">The serialization writer factory.</param>
        /// <param name="httpClient">The native HTTP client.</param>
        /// </summary>
        public HttpClientRequestAdapter(IAuthenticationProvider authenticationProvider, IParseNodeFactory parseNodeFactory = null, ISerializationWriterFactory serializationWriterFactory = null, HttpClient httpClient = null)
        {
            authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            createdClient = httpClient == null;
            client = httpClient ?? KiotaClientFactory.Create();
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
        /// The base url for every request.
        /// </summary>
        public string BaseUrl { get; set; }
        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with a collection instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        public async Task<IEnumerable<ModelType>> SendCollectionAsync<ModelType>(RequestInformation requestInfo, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default) where ModelType : IParsable
        {
            var response = await GetHttpResponseMessage(requestInfo, cancellationToken);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
            {
                await ThrowFailedResponse(response, errorMapping);
                var rootNode = await GetRootParseNode(response);
                var result = rootNode.GetCollectionOfObjectValues<ModelType>();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, IEnumerable<ModelType>>(response, errorMapping);
        }
        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns>The deserialized primitive response model collection.</returns>
        public async Task<IEnumerable<ModelType>> SendPrimitiveCollectionAsync<ModelType>(RequestInformation requestInfo, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default) {
            var response = await GetHttpResponseMessage(requestInfo, cancellationToken);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
            {
                await ThrowFailedResponse(response, errorMapping);
                var rootNode = await GetRootParseNode(response);
                var result = rootNode.GetCollectionOfPrimitiveValues<ModelType>();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, IEnumerable<ModelType>>(response, errorMapping);
        }
        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with an instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns>The deserialized response model.</returns>
        public async Task<ModelType> SendAsync<ModelType>(RequestInformation requestInfo, IResponseHandler responseHandler = null, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default) where ModelType : IParsable
        {
            var response = await GetHttpResponseMessage(requestInfo, cancellationToken);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
            {
                await ThrowFailedResponse(response, errorMapping);
                var rootNode = await GetRootParseNode(response);
                var result = rootNode.GetObjectValue<ModelType>();
                return result;
            }
            else
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response, errorMapping);
        }
        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with a primitive instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns>The deserialized primitive response model.</returns>
        public async Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInformation requestInfo, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default)
        {
            var response = await GetHttpResponseMessage(requestInfo, cancellationToken);
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
                    await ThrowFailedResponse(response, errorMapping);
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
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response, errorMapping);
        }
        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with an empty request body
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="responseHandler">The <see cref="IResponseHandler"/> to use with the response</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns></returns>
        public async Task SendNoContentAsync(RequestInformation requestInfo, IResponseHandler responseHandler = null, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default)
        {
            var response = await GetHttpResponseMessage(requestInfo, cancellationToken);
            await ThrowFailedResponse(response, errorMapping);
            requestInfo.Content?.Dispose();
            if(responseHandler == null)
                response.Dispose();
            else
                await responseHandler.HandleResponseAsync<HttpResponseMessage, object>(response, errorMapping);
        }
        private async Task ThrowFailedResponse(HttpResponseMessage response, Dictionary<string, Func<IParsable>> errorMapping)
        {
            if(response.IsSuccessStatusCode) return;

            var statusCodeAsInt = (int)response.StatusCode;
            var statusCodeAsString = statusCodeAsInt.ToString();
            Func<IParsable> errorFactory;
            if(errorMapping == null ||
                !errorMapping.TryGetValue(statusCodeAsString, out errorFactory) &&
                !(statusCodeAsInt >= 400 && statusCodeAsInt < 500 && errorMapping.TryGetValue("4XX", out errorFactory)) &&
                !(statusCodeAsInt >= 500 && statusCodeAsInt < 600 && errorMapping.TryGetValue("5XX", out errorFactory)))
                    throw new HttpRequestException($"The server returned an unexpected status code and no error factory is registered for this code: {statusCodeAsString}");

            var rootNode = await GetRootParseNode(response);
            var result = rootNode.GetErrorValue(errorFactory);
            if(result is not Exception ex)
                throw new HttpRequestException($"The server returned an unexpected status code and the error registered for this code failed to deserialize: {statusCodeAsString}");
            else throw ex;
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
        private async Task<HttpResponseMessage> GetHttpResponseMessage(RequestInformation requestInfo, CancellationToken cancellationToken)
        {
            if(requestInfo == null)
                throw new ArgumentNullException(nameof(requestInfo));

            SetBaseUrlForRequestInformation(requestInfo);
            await authProvider.AuthenticateRequestAsync(requestInfo, cancellationToken);

            using var message = GetRequestMessageFromRequestInformation(requestInfo);
            var response = await this.client.SendAsync(message,cancellationToken);
            if(response == null)
                throw new InvalidOperationException("Could not get a response after calling the service");
            return response;
        }
        private void SetBaseUrlForRequestInformation(RequestInformation requestInfo)
        {
            requestInfo.PathParameters.Add("baseurl", BaseUrl);
        }
        /// <summary>
        /// Creates a <see cref="HttpRequestMessage"/> instance from a <see cref="RequestInformation"/> instance.
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to convert.</param>
        /// <returns>A <see cref="HttpRequestMessage"/> instance</returns>
        public HttpRequestMessage GetRequestMessageFromRequestInformation(RequestInformation requestInfo)
        {
            var message = new HttpRequestMessage
            {
                Method = new HttpMethod(requestInfo.HttpMethod.ToString().ToUpperInvariant()),
                RequestUri = requestInfo.URI,
            };

            if(requestInfo.RequestOptions.Any())
                requestInfo.RequestOptions.ToList().ForEach(x => message.Properties.Add(x.GetType().FullName, x));

            if(requestInfo.Content != null)
                message.Content = new StreamContent(requestInfo.Content);
            if(requestInfo.Headers?.Any() ?? false)
                foreach(var (key,value) in requestInfo.Headers)
                    if(!message.Headers.TryAddWithoutValidation(key, value) && message.Content != null)
                        message.Content.Headers.TryAddWithoutValidation(key, value);// Try to add the headers we couldn't add to the HttpRequestMessage before to the HttpContent

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
            GC.SuppressFinalize(this);
        }
    }
}
