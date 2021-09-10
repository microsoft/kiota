using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Http.HttpClient.Middleware;
using Microsoft.Kiota.Http.HttpClient.Middleware.Options;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClient.Tests.Middleware
{
    public class ChaosHandlerTests
    {
        [Fact]
        public async Task RandomChaosShouldReturnRandomFailures()
        {
            // Arrange
            var handler = new ChaosHandler
            {
                InnerHandler = new FakeSuccessHandler()
            };

            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage();

            // Act
            Dictionary<HttpStatusCode, object> responses = new Dictionary<HttpStatusCode, object>();

            // Make calls until all known failures have been triggered
            while(responses.Count < 3)
            {
                var response = await invoker.SendAsync(request, new CancellationToken());
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    responses[response.StatusCode] = null;
                }
            }

            // Assert
            Assert.True(responses.ContainsKey(HttpStatusCode.TooManyRequests));
            Assert.True(responses.ContainsKey(HttpStatusCode.ServiceUnavailable));
            Assert.True(responses.ContainsKey(HttpStatusCode.GatewayTimeout));
        }

        [Fact]
        public async Task RandomChaosWithCustomKnownFailuresShouldReturnAllFailuresRandomly()
        {

            // Arrange
            var handler = new ChaosHandler(new ChaosHandlerOption
            {
                KnownChaos = new List<HttpResponseMessage>
                {
                    ChaosHandler.Create429TooManyRequestsResponse(new TimeSpan(0,0,5)),
                    ChaosHandler.Create500InternalServerErrorResponse(),
                    ChaosHandler.Create503Response(new TimeSpan(0,0,5)),
                    ChaosHandler.Create502BadGatewayResponse(),
                    ChaosHandler.Create504GatewayTimeoutResponse(new TimeSpan(0,0,5))
                }
            })
            {
                InnerHandler = new FakeSuccessHandler()
            };

            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage();

            // Act
            Dictionary<HttpStatusCode, object> responses = new Dictionary<HttpStatusCode, object>();

            // Make calls until all known failures have been triggered
            while(responses.Count < 5)
            {
                var response = await invoker.SendAsync(request, new CancellationToken());
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    responses[response.StatusCode] = null;
                }
            }

            // Assert
            Assert.True(responses.ContainsKey(HttpStatusCode.TooManyRequests));
            Assert.True(responses.ContainsKey(HttpStatusCode.InternalServerError));
            Assert.True(responses.ContainsKey(HttpStatusCode.BadGateway));
            Assert.True(responses.ContainsKey(HttpStatusCode.ServiceUnavailable));
            Assert.True(responses.ContainsKey(HttpStatusCode.GatewayTimeout));
        }

        [Fact]
        public async Task PlannedChaosShouldReturnChaosWhenPlanned()
        {
            // Arrange

            Func<HttpRequestMessage, HttpResponseMessage> plannedChaos = (req) =>
            {
                if(req.RequestUri?.OriginalString.Contains("/fail") ?? false)
                {
                    return ChaosHandler.Create429TooManyRequestsResponse(new TimeSpan(0, 0, 5));
                }
                return null;
            };

            var handler = new ChaosHandler(new ChaosHandlerOption
            {
                PlannedChaosFactory = plannedChaos
            })
            {
                InnerHandler = new FakeSuccessHandler()
            };

            var invoker = new HttpMessageInvoker(handler);


            // Act
            var request1 = new HttpRequestMessage
            {
                RequestUri = new Uri("http://example.org/success")
            };
            var response1 = await invoker.SendAsync(request1, new CancellationToken());

            var request2 = new HttpRequestMessage
            {
                RequestUri = new Uri("http://example.org/fail")
            };
            var response2 = await invoker.SendAsync(request2, new CancellationToken());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, response2.StatusCode);
        }

    }

    internal class FakeSuccessHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                RequestMessage = request
            };
            return Task.FromResult(response);
        }
    }
}
