using System;
using System.IO;
using System.Text;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Store;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClientLibrary.Tests
{
    public class HttpClientRequestAdapterTests
    {
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly HttpClientRequestAdapter requestAdapter;

        public HttpClientRequestAdapterTests()
        {
            _authenticationProvider = new Mock<IAuthenticationProvider>().Object;
            requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider());
        }

        [Fact]
        public void ThrowsArgumentNullExceptionOnNullAuthenticationProvider()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new HttpClientRequestAdapter(null));
            Assert.Equal("authenticationProvider", exception.ParamName);
        }

        [Fact]
        public void EnablesBackingStore()
        {
            // Arrange
            var requestAdapter = new HttpClientRequestAdapter(_authenticationProvider);
            var backingStore = new Mock<IBackingStoreFactory>().Object;

            //Assert the that we originally have an in memory backing store
            Assert.IsAssignableFrom<InMemoryBackingStoreFactory>(BackingStoreFactorySingleton.Instance);

            // Act
            requestAdapter.EnableBackingStore(backingStore);

            //Assert the backing store has been updated
            Assert.IsAssignableFrom(backingStore.GetType(), BackingStoreFactorySingleton.Instance);
        }

        [Theory]
        [InlineData("select", new[] { "id", "displayName" }, "select=id,displayName")]
        [InlineData("count", true, "count=true")]
        [InlineData("skip", 10, "skip=10")]
        [InlineData("skip", null, "")]// query parameter no placed
        public void GetRequestMessageFromRequestInformationSetsQueryParametersCorrectlyWithSelect(string queryParam, object queryParamObject, string expectedString)
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "http://localhost/me{?top,skip,search,filter,count,orderby,select}"
            };
            requestInfo.QueryParameters.Add(queryParam, queryParamObject);

            // Act
            var requestMessage = requestAdapter.GetRequestMessageFromRequestInformation(requestInfo);

            // Assert
            Assert.NotNull(requestMessage.RequestUri);
            Assert.Contains(expectedString, requestMessage.RequestUri.Query);
        }

        [Fact]
        public void GetRequestMessageFromRequestInformationSetsContentHeaders()
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.PUT,
                UrlTemplate = "https://sn3302.up.1drv.com/up/fe6987415ace7X4e1eF866337"
            };
            requestInfo.Headers.Add("Content-Length", "26");
            requestInfo.Headers.Add("Content-Range", "bytes 0-25/128");
            requestInfo.SetStreamContent(new MemoryStream(Encoding.UTF8.GetBytes("contents")));

            // Act
            var requestMessage = requestAdapter.GetRequestMessageFromRequestInformation(requestInfo);

            // Assert
            Assert.NotNull(requestMessage.Content);
            // Content length set correctly
            Assert.Equal(26,requestMessage.Content.Headers.ContentLength);
            // Content range set correctly
            Assert.Equal("bytes", requestMessage.Content.Headers.ContentRange.Unit);
            Assert.Equal(0, requestMessage.Content.Headers.ContentRange.From);
            Assert.Equal(25, requestMessage.Content.Headers.ContentRange.To);
            Assert.Equal(128,requestMessage.Content.Headers.ContentRange.Length);
            Assert.True(requestMessage.Content.Headers.ContentRange.HasRange);
            Assert.True(requestMessage.Content.Headers.ContentRange.HasLength);
            // Content type set correctly
            Assert.Equal("application/octet-stream", requestMessage.Content.Headers.ContentType.MediaType);

        }
    }
}
