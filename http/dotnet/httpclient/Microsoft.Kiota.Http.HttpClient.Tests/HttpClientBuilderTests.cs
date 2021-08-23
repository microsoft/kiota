using System.Net.Http;
using Microsoft.Kiota.Http.HttpClient.Tests.Mocks;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClient.Tests
{
    public class HttpClientBuilderTests
    {
        [Fact]
        public void ChainHandlersCollectionAndGetFirstLinkReturnsNullOnDefaultParams()
        {
            // Act
            var delegatingHandler = HttpClientBuilder.ChainHandlersCollectionAndGetFirstLink();
            // Assert
            Assert.Null(delegatingHandler);
        }

        [Fact]
        public void ChainHandlersCollectionAndGetFirstLinkWithSingleHandler()
        {
            // Arrange
            var handler = new TestHttpMessageHandler();
            // Act
            var delegatingHandler = HttpClientBuilder.ChainHandlersCollectionAndGetFirstLink(handler);
            // Assert
            Assert.NotNull(delegatingHandler);
            Assert.Null(delegatingHandler.InnerHandler);
        }

        [Fact]
        public void ChainHandlersCollectionAndGetFirstLinkWithMultipleHandlers()
        {
            // Arrange
            var handler1 = new TestHttpMessageHandler();
            var handler2 = new TestHttpMessageHandler();
            // Act
            var delegatingHandler = HttpClientBuilder.ChainHandlersCollectionAndGetFirstLink(handler1, handler2);
            // Assert
            Assert.NotNull(delegatingHandler);
            Assert.NotNull(delegatingHandler.InnerHandler); // first handler has an inner handler

            var innerHandler = delegatingHandler.InnerHandler as DelegatingHandler;
            Assert.NotNull(innerHandler);
            Assert.Null(innerHandler.InnerHandler);// end of the chain
        }
    }
}
