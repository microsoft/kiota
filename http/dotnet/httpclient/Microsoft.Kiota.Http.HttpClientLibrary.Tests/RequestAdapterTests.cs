using System;
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
                HttpMethod = HttpMethod.GET,
                UrlTemplate = "http://localhost/me{?top,skip,search,filter,count,orderby,select}"
            };
            requestInfo.QueryParameters.Add(queryParam, queryParamObject);

            // Act
            var requestMessage = requestAdapter.GetRequestMessageFromRequestInformation(requestInfo);

            // Assert
            Assert.NotNull(requestMessage.RequestUri);
            Assert.Contains(expectedString, requestMessage.RequestUri.Query);
        }

    }
}
