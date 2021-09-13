using System.Net;
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

        [Fact]
        public void ChainHandlersCollectionAndGetFirstLinkWithMultipleHandlersSetsFinalHandler()
        {
            // Arrange
            var handler1 = new TestHttpMessageHandler();
            var handler2 = new TestHttpMessageHandler();
            var finalHandler = new HttpClientHandler();
            // Act
            var delegatingHandler = HttpClientBuilder.ChainHandlersCollectionAndGetFirstLink(finalHandler, handler1, handler2);
            // Assert
            Assert.NotNull(delegatingHandler);
            Assert.NotNull(delegatingHandler.InnerHandler); // first handler has an inner handler
            
            var innerHandler = delegatingHandler.InnerHandler as DelegatingHandler;
            Assert.NotNull(innerHandler);
            Assert.NotNull(innerHandler.InnerHandler);
            Assert.IsType<HttpClientHandler>(innerHandler.InnerHandler);
        }

        [Fact]
        public void GetDefaultHttpMessageHandlerSetsUpProxy()
        {
            // Arrange
            var proxy = new WebProxy("http://localhost:8888", false);
            // Act
            var defaultHandler = HttpClientBuilder.GetDefaultHttpMessageHandler(proxy);
            // Assert
            Assert.NotNull(defaultHandler);
            Assert.IsType<HttpClientHandler>(defaultHandler);
            Assert.Equal(proxy, ((HttpClientHandler)defaultHandler).Proxy);

        }
    }
}
