using System;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Store;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClient.Tests
{
    public class HttpCoreTests
    {
        private readonly IAuthenticationProvider _authenticationProvider;

        public HttpCoreTests()
        {
            _authenticationProvider = new Mock<IAuthenticationProvider>().Object;
        }

        [Fact]
        public void ThrowsArgumentNullExceptionOnNullAuthenticationProvider()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new HttpCore(null));
            Assert.Equal("authenticationProvider", exception.ParamName);
        }

        [Fact]
        public void EnablesBackingStore()
        {
            // Arrange
            var httpCore = new HttpCore(_authenticationProvider);
            var backingStore = new Mock<IBackingStoreFactory>().Object;

            //Assert the that we originally have an in memory backing store
            Assert.IsAssignableFrom<InMemoryBackingStoreFactory>(BackingStoreFactorySingleton.Instance);

            // Act
            httpCore.EnableBackingStore(backingStore);

            //Assert the backing store has been updated
            Assert.IsAssignableFrom(backingStore.GetType(), BackingStoreFactorySingleton.Instance);
        }

        [Theory]
        [InlineData("select", new[] { "id", "displayName" }, "select=id,displayName")]
        [InlineData("count", true, "count=true")]
        [InlineData("skip", 10, "skip=10")]
        [InlineData("skip", null, "skip")]
        public void GetRequestMessageFromRequestInformationSetsQueryParametersCorrectlyWithSelect(string queryParam, object queryParamObject, string expectedString)
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = HttpMethod.GET,
            };
            requestInfo.SetURI("http://localhost/me", "", true);
            requestInfo.QueryParameters.Add(queryParam, queryParamObject);

            // Act
            var requestMessage = HttpCore.GetRequestMessageFromRequestInformation(requestInfo);

            // Assert
            Assert.NotNull(requestMessage.RequestUri);
            Assert.Contains(expectedString, requestMessage.RequestUri.Query);
        }

    }
}
