using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClient.Extensions;
using Microsoft.Kiota.Http.HttpClient.Middleware.Options;
using Xunit;
using HttpMethod = Microsoft.Kiota.Abstractions.HttpMethod;

namespace Microsoft.Kiota.Http.HttpClient.Tests.Extensions
{
    public class HttpRequestMessageExtensionsTests
    {
        [Fact]
        public void GetRequestOptionCanExtractMiddleWareOptionFromHttpRequestMessage()
        {
            // Arrange
            var requestInfo = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var redirectHandlerOption = new RedirectHandlerOption()
            {
                MaxRedirect = 7
            };
            requestInfo.AddRequestOptions(redirectHandlerOption);
            // Act and get a request message
            var requestMessage = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            var extractedOption = requestMessage.GetRequestOption<RedirectHandlerOption>();
            // Assert
            Assert.NotNull(extractedOption);
            Assert.Equal(redirectHandlerOption, extractedOption);
            Assert.Equal(7, redirectHandlerOption.MaxRedirect);
        }

        [Fact]
        public async Task CloneAsyncWithEmptyHttpContent()
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var originalRequest = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            HttpRequestMessage clonedRequest = await originalRequest.CloneAsync();

            Assert.NotNull(clonedRequest);
            Assert.Equal(originalRequest.Method, clonedRequest.Method);
            Assert.Equal(originalRequest.RequestUri, clonedRequest.RequestUri);
            Assert.Null(clonedRequest.Content);
        }

        [Fact]
        public async Task CloneAsyncWithHttpContent()
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            requestInfo.SetStreamContent(new MemoryStream(Encoding.UTF8.GetBytes("contents")));
            var originalRequest = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            originalRequest.Content = new StringContent("contents");

            var clonedRequest = await originalRequest.CloneAsync();
            var clonedRequestContents = await clonedRequest.Content?.ReadAsStringAsync();

            Assert.NotNull(clonedRequest);
            Assert.Equal(originalRequest.Method, clonedRequest.Method);
            Assert.Equal(originalRequest.RequestUri, clonedRequest.RequestUri);
            Assert.Equal("contents", clonedRequestContents);
            Assert.Equal(originalRequest.Content?.Headers.ContentType, clonedRequest.Content?.Headers.ContentType);
        }

        [Fact]
        public async Task CloneAsyncWithRequestOption()
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var redirectHandlerOption = new RedirectHandlerOption()
            {
                MaxRedirect = 7
            };
            requestInfo.AddRequestOptions(redirectHandlerOption);
            var originalRequest = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            originalRequest.Content = new StringContent("contents");

            var clonedRequest = await originalRequest.CloneAsync();

            Assert.NotNull(clonedRequest);
            Assert.Equal(originalRequest.Method, clonedRequest.Method);
            Assert.Equal(originalRequest.RequestUri, clonedRequest.RequestUri);
            Assert.NotEmpty(clonedRequest.Options);
            Assert.Equal(redirectHandlerOption, clonedRequest.Options.First().Value);
            Assert.Equal(originalRequest.Content?.Headers.ContentType, clonedRequest.Content?.Headers.ContentType);
        }

        [Fact]
        public void IsBufferedReturnsTrueForGetRequest()
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var originalRequest = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            // Act
            var response = originalRequest.IsBuffered();
            // Assert
            Assert.True(response, "Unexpected content type");
        }
        [Fact]
        public void IsBufferedReturnsTrueForPostWithNoContent()
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.POST,
                URI = new Uri("http://localhost")
            };
            var originalRequest = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            // Act
            var response = originalRequest.IsBuffered();
            // Assert
            Assert.True(response, "Unexpected content type");
        }
        [Fact]
        public void IsBufferedReturnsTrueForPostWithBufferStringContent()
        {
            // Arrange
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.POST,
                URI = new Uri("http://localhost"),
                Content = new MemoryStream(data)
            };
            var originalRequest = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);
            // Act
            var response = originalRequest.IsBuffered();
            // Assert
            Assert.True(response, "Unexpected content type");
        }
    }
}
