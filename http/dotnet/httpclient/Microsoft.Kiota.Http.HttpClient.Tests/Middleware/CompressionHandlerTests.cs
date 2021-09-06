using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Http.HttpClient.Middleware;
using Microsoft.Kiota.Http.HttpClient.Tests.Mocks;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClient.Tests.Middleware
{
    public class CompressionHandlerTests: IDisposable
    {
        private readonly MockRedirectHandler _testHttpMessageHandler;
        private readonly CompressionHandler _compressionHandler;
        private readonly HttpMessageInvoker _invoker;

        public CompressionHandlerTests()
        {
            this._testHttpMessageHandler = new MockRedirectHandler();
            this._compressionHandler = new CompressionHandler
            {
                InnerHandler = this._testHttpMessageHandler
            };
            this._invoker = new HttpMessageInvoker(this._compressionHandler);
        }

        public void Dispose()
        {
            this._invoker.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void CompressionHandlerShouldConstructHandler()
        {
            Assert.NotNull(this._compressionHandler.InnerHandler);
        }

        [Fact]
        public async Task CompressionHandlerShouldAddAcceptEncodingGzipHeaderWhenNonIsPresent()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/foo");
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(httpResponse);// set the mock response
            // Act
            HttpResponseMessage response = await this._invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.Same(httpRequestMessage, response.RequestMessage);
            Assert.NotNull(response.RequestMessage);
            Assert.Contains(new StringWithQualityHeaderValue(CompressionHandler.GZip), response.RequestMessage.Headers.AcceptEncoding);
        }

        [Fact]
        public async Task CompressionHandlerShouldDecompressResponseWithContentEncodingGzipHeader()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/foo");
            httpRequestMessage.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(CompressionHandler.GZip));
            string stringToCompress = "sample string content";
            // Compress response
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new MockCompressedContent(new StringContent(stringToCompress))
            };
            httpResponse.Content.Headers.ContentEncoding.Add(CompressionHandler.GZip);
            this._testHttpMessageHandler.SetHttpResponse(httpResponse);// set the mock response
            // Act
            HttpResponseMessage decompressedResponse = await this._invoker.SendAsync(httpRequestMessage, new CancellationToken());
            string responseContentString = await decompressedResponse.Content.ReadAsStringAsync();
            // Assert
            Assert.Same(httpResponse, decompressedResponse);
            Assert.Same(httpRequestMessage, decompressedResponse.RequestMessage);
            Assert.Equal(stringToCompress, responseContentString);
        }

        [Fact]
        public async Task CompressionHandlerShouldNotDecompressResponseWithoutContentEncodingGzipHeader()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/foo");
            string stringToCompress = "Microsoft Graph";
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new MockCompressedContent(new StringContent(stringToCompress))
            };
            this._testHttpMessageHandler.SetHttpResponse(httpResponse);// set the mock response
            // Act
            HttpResponseMessage compressedResponse = await this._invoker.SendAsync(httpRequestMessage, new CancellationToken());
            string responseContentString = await compressedResponse.Content.ReadAsStringAsync();
            // Assert
            Assert.Same(httpResponse, compressedResponse);
            Assert.Same(httpRequestMessage, compressedResponse.RequestMessage);
            Assert.NotEqual(stringToCompress, responseContentString);
        }

        [Fact]
        public async Task CompressionHandlerShouldKeepContentHeadersAfterDecompression()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/foo");
            string stringToCompress = "Microsoft Graph";
            StringContent stringContent = new StringContent(stringToCompress);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new MockCompressedContent(stringContent)
            };
            httpResponse.Content.Headers.ContentEncoding.Add(CompressionHandler.GZip);
            httpResponse.Headers.CacheControl = new CacheControlHeaderValue { Private = true };
            // Examples of Custom Headers returned by Microsoft Graph
            httpResponse.Headers.Add("request-id", Guid.NewGuid().ToString());
            httpResponse.Headers.Add("OData-Version", "4.0");

            this._testHttpMessageHandler.SetHttpResponse(httpResponse);// set the mock response
            // Arrange
            HttpResponseMessage compressedResponse = await this._invoker.SendAsync(httpRequestMessage, new CancellationToken());
            string decompressedResponseString = await compressedResponse.Content.ReadAsStringAsync();
            // Assert
            Assert.Equal(decompressedResponseString, stringToCompress);
            // Ensure that headers in the compressedResponse are the same as in the original, expected response.
            Assert.NotEmpty(compressedResponse.Headers);
            Assert.NotEmpty(compressedResponse.Content.Headers);
            Assert.Equal(httpResponse.Headers, compressedResponse.Headers, new HttpHeaderComparer());
            Assert.Equal(httpResponse.Content.Headers, compressedResponse.Content.Headers, new HttpHeaderComparer());
        }

        private class HttpHeaderComparer : IEqualityComparer<KeyValuePair<string, IEnumerable<string>>>
        {
            public bool Equals(KeyValuePair<string, IEnumerable<string>> x, KeyValuePair<string, IEnumerable<string>> y)
            {
                // For each key, the collection of header values should be equal.
                return x.Key == y.Key && x.Value.SequenceEqual(y.Value);
            }

            public int GetHashCode(KeyValuePair<string, IEnumerable<string>> obj)
            {
                return obj.Key.GetHashCode();
            }
        }
    }
}
