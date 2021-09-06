using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Http.HttpClient.Middleware;
using Microsoft.Kiota.Http.HttpClient.Middleware.Options;
using Microsoft.Kiota.Http.HttpClient.Tests.Mocks;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClient.Tests.Middleware
{
    public class RetryHandlerTests : IDisposable
    {
        private readonly MockRedirectHandler _testHttpMessageHandler;
        private readonly RetryHandler _retryHandler;
        private readonly HttpMessageInvoker _invoker;
        private const string RetryAfter = "Retry-After";
        private const string RetryAttempt = "Retry-Attempt";

        public RetryHandlerTests()
        {
            this._testHttpMessageHandler = new MockRedirectHandler();
            this._retryHandler = new RetryHandler
            {
                InnerHandler = this._testHttpMessageHandler
            };
            this._invoker = new HttpMessageInvoker(this._retryHandler);
        }

        public void Dispose()
        {
            this._invoker.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void RetryHandlerConstructor()
        {
            // Act
            using RetryHandler retry = new RetryHandler();
            // Assert
            Assert.Null(retry.InnerHandler);
            Assert.NotNull(retry.RetryOption);
            Assert.Equal(RetryHandlerOption.DefaultMaxRetry, retry.RetryOption.MaxRetry);
            Assert.IsType<RetryHandler>(retry);
        }


        [Fact]
        public void RetryHandlerHttpMessageHandlerConstructor()
        {
            // Assert
            Assert.NotNull(_retryHandler.InnerHandler);
            Assert.NotNull(_retryHandler.RetryOption);
            Assert.Equal(RetryHandlerOption.DefaultMaxRetry, _retryHandler.RetryOption.MaxRetry);
            Assert.Equal(_retryHandler.InnerHandler, _testHttpMessageHandler);
            Assert.IsType<RetryHandler>(_retryHandler);
        }

        [Fact]
        public void RetryHandlerRetryOptionConstructor()
        {
            // Act
            using RetryHandler retry = new RetryHandler(new RetryHandlerOption { MaxRetry = 5, ShouldRetry = (_, _, _) => true });
            // Assert
            Assert.Null(retry.InnerHandler);
            Assert.NotNull(retry.RetryOption);
            Assert.Equal(5, retry.RetryOption.MaxRetry);
            Assert.IsType<RetryHandler>(retry);
        }

        [Fact]
        public async Task OkStatusShouldPassThrough()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/foo");
            var retryResponse = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse);
            // Act
            var response = await this._invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.Same(response, retryResponse);
            Assert.NotNull(response.RequestMessage);
            Assert.Same(response.RequestMessage, httpRequestMessage);
            Assert.False(response.RequestMessage.Headers.Contains(RetryAttempt), "The request add header wrong.");

        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData((HttpStatusCode)429)] // 429
        public async Task ShouldRetryWithAddRetryAttemptHeader(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.org/foo");
            var retryResponse = new HttpResponseMessage(statusCode);
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse, response2);
            // Act
            var response = await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.Same(response, response2);
            Assert.NotSame(response.RequestMessage, httpRequestMessage);
            Assert.NotNull(response.RequestMessage);
            Assert.NotNull(response.RequestMessage.Headers);
            Assert.True(response.RequestMessage.Headers.Contains(RetryAttempt));
            Assert.True(response.RequestMessage.Headers.TryGetValues(RetryAttempt, out var values));
            Assert.Single(values);
            Assert.Equal(values.First(), 1.ToString());
        }


        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData((HttpStatusCode)429)] // 429
        public async Task ShouldRetryWithBuffedContent(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://example.org/foo")
            {
                Content = new StringContent("Hello World")
            };
            var retryResponse = new HttpResponseMessage(statusCode);
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse, response2);
            // Act
            var response = await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.Same(response, response2);
            Assert.NotSame(response.RequestMessage, httpRequestMessage);
            Assert.NotNull(response.RequestMessage);
            Assert.NotNull(response.RequestMessage.Content);
            Assert.NotNull(response.RequestMessage.Content.Headers.ContentLength);
            Assert.Equal("Hello World", response.RequestMessage.Content.ReadAsStringAsync().Result);

        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData((HttpStatusCode)429)] // 429
        public async Task ShouldNotRetryWithPostStreaming(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://example.org/foo")
            {
                Content = new StringContent("Test Content")
            };
            httpRequestMessage.Content.Headers.ContentLength = -1;
            var retryResponse = new HttpResponseMessage(statusCode);
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse, response2);
            // Act
            var response = await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.NotEqual(response, response2);
            Assert.Same(response, retryResponse);
            Assert.Same(response.RequestMessage, httpRequestMessage);
            Assert.NotNull(response.RequestMessage);
            Assert.NotNull(response.RequestMessage.Content);
            Assert.NotNull(response.RequestMessage.Content.Headers.ContentLength);
            Assert.Equal(response.RequestMessage.Content.Headers.ContentLength, -1);
        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData((HttpStatusCode)429)] // 429
        public async Task ShouldNotRetryWithPutStreaming(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, "http://example.org/foo")
            {
                Content = new StringContent("Test Content")
            };
            httpRequestMessage.Content.Headers.ContentLength = -1;
            var retryResponse = new HttpResponseMessage(statusCode);
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse, response2);
            // Act
            var response = await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.NotEqual(response, response2);
            Assert.Same(response.RequestMessage, httpRequestMessage);
            Assert.Same(response, retryResponse);
            Assert.NotNull(response.RequestMessage);
            Assert.NotNull(response.RequestMessage.Content);
            Assert.Equal(response.RequestMessage.Content.Headers.ContentLength, -1);
        }


        [Theory(Skip = "Test takes a while to run")]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData(HttpStatusCode.TooManyRequests)] // 429
        public async Task ExceedMaxRetryShouldReturn(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://example.org/foo");
            var retryResponse = new HttpResponseMessage(statusCode);
            var response2 = new HttpResponseMessage(statusCode);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse, response2);
            // Act
            try
            {
                await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            }
            catch(Exception exception)
            {
                // Assert
                Assert.IsType<InvalidOperationException>(exception);
                Assert.Equal("Too many retries performed", exception.Message);
                Assert.False(httpRequestMessage.Headers.TryGetValues(RetryAttempt, out _), "Don't set Retry-Attempt Header");
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData(HttpStatusCode.TooManyRequests)] // 429
        public async Task ShouldDelayBasedOnRetryAfterHeader(HttpStatusCode statusCode)
        {
            // Arrange
            var retryResponse = new HttpResponseMessage(statusCode);
            retryResponse.Headers.TryAddWithoutValidation(RetryAfter, 1.ToString());
            // Act
            await DelayTestWithMessage(retryResponse, 1, "Init");
            // Assert
            Assert.Equal("Init Work 1", Message);
        }


        [Theory(Skip = "Skipped as this takes 9 minutes to run for each scenario")] // Takes 9 minutes to run for each scenario
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData((HttpStatusCode)429)] // 429
        public async Task ShouldDelayBasedOnExponentialBackOff(HttpStatusCode statusCode)
        {
            // Arrange
            var retryResponse = new HttpResponseMessage(statusCode);
            var compareMessage = "Init Work ";

            for(int count = 0; count < 3; count++)
            {
                // Act
                await DelayTestWithMessage(retryResponse, count, "Init");
                // Assert
                Assert.Equal(Message, compareMessage + count);
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData(HttpStatusCode.TooManyRequests)] // 429
        public async Task ShouldReturnSameStatusCodeWhenDelayIsGreaterThanRetryTimeLimit(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://example.org/foo")
            {
                Content = new StringContent("Hello World")
            };
            var retryResponse = new HttpResponseMessage(statusCode);
            retryResponse.Headers.TryAddWithoutValidation(RetryAfter, 20.ToString());
            _retryHandler.RetryOption.RetriesTimeLimit = TimeSpan.FromSeconds(10);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse);
            // Act
            var response = await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.Same(response, retryResponse);
        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]  // 504
        [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
        [InlineData(HttpStatusCode.TooManyRequests)] // 429
        public async Task ShouldRetryBasedOnRetryAfter(HttpStatusCode statusCode)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://example.org/foo")
            {
                Content = new StringContent("Hello World")
            };
            var retryResponse = new HttpResponseMessage(statusCode);
            retryResponse.Headers.TryAddWithoutValidation(RetryAfter, 5.ToString());
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            this._testHttpMessageHandler.SetHttpResponse(retryResponse, response2);
            // Act
            var response = await _invoker.SendAsync(httpRequestMessage, new CancellationToken());
            // Assert
            Assert.Same(response, response2);
            Assert.NotNull(response.RequestMessage);
            Assert.True(response.RequestMessage.Headers.TryGetValues(RetryAttempt, out var values), "Don't set Retry-Attempt Header");
            Assert.Single(values);
            Assert.Equal(values.First(), 1.ToString());
            Assert.NotSame(response.RequestMessage, httpRequestMessage);
        }


        [Theory]
        [InlineData(1, HttpStatusCode.BadGateway, true)]
        [InlineData(2, HttpStatusCode.BadGateway, true)]
        [InlineData(3, HttpStatusCode.BadGateway, true)]
        [InlineData(4, HttpStatusCode.OK, false)]
        public async Task ShouldRetryBasedOnCustomShouldRetryDelegate(int expectedMaxRetry, HttpStatusCode expectedStatusCode, bool isExceptionExpected)
        {
            // Arrange
            var request = new HttpRequestMessage();
            Queue<HttpResponseMessage> httpResponseQueue = new(new HttpResponseMessage[]
            {
                new(HttpStatusCode.BadGateway) { RequestMessage = request },
                new(HttpStatusCode.BadGateway) { RequestMessage = request },
                new(HttpStatusCode.BadGateway) { RequestMessage = request },
                new(HttpStatusCode.BadGateway) { RequestMessage = request },
                new(HttpStatusCode.OK) { RequestMessage = request },
            });

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())
                .Returns(() => Task.FromResult(httpResponseQueue.TryDequeue(out HttpResponseMessage r) ? r : new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request }))
                .Verifiable();

            RetryHandler retryHandler = new(new RetryHandlerOption()
            {
                ShouldRetry = (_, _, httpResponseMessage) => httpResponseMessage.StatusCode == HttpStatusCode.BadGateway,
                MaxRetry = expectedMaxRetry,
                Delay = 0
            })
            {
                InnerHandler = mockHttpMessageHandler.Object
            };

            HttpMessageInvoker httpMessageInvoker = new(retryHandler);

            // Act
            try
            {
                var response = await httpMessageInvoker.SendAsync(request, new CancellationToken());

                Assert.False(isExceptionExpected);
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
            catch(Exception exception)
            {
                // Assert
                Assert.IsType<InvalidOperationException>(exception);
                Assert.True(isExceptionExpected);
                Assert.Equal("Too many retries performed", exception.Message);
            }

            // Assert
            mockHttpMessageHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Exactly(1 + expectedMaxRetry), ItExpr.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>());

        }

        private async Task DelayTestWithMessage(HttpResponseMessage response, int count, string message, int delay = RetryHandlerOption.MaxDelay)
        {
            Message = message;
            await Task.Run(async () =>
            {
                await this._retryHandler.Delay(response, count, delay, out _, new CancellationToken());
                Message += " Work " + count;
            });
        }

        private string Message
        {
            get;
            set;
        }
    }
}
