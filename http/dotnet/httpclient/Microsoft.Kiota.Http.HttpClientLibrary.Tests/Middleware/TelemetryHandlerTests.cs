using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using Xunit;

namespace Microsoft.Kiota.Http.HttpClientLibrary.Tests.Middleware
{
    public class TelemetryHandlerTests
    {
        private readonly HttpMessageInvoker _invoker;

        private readonly HttpClientRequestAdapter requestAdapter;
        public TelemetryHandlerTests()
        {
            var telemetryHandler = new TelemetryHandler
            {
                InnerHandler = new FakeSuccessHandler()
            };
            this._invoker = new HttpMessageInvoker(telemetryHandler);
            requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider());
        }

        [Fact]
        public async Task DefaultTelemetryHandlerDoesNotChangeRequest()
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri("http://localhost")
            };
            // Act and get a request message
            var requestMessage = requestAdapter.GetRequestMessageFromRequestInformation(requestInfo);
            Assert.Empty(requestMessage.Headers);

            // Act
            var response = await _invoker.SendAsync(requestMessage, new CancellationToken());

            // Assert the request stays the same
            Assert.Empty(response.RequestMessage?.Headers!);
            Assert.Equal(requestMessage,response.RequestMessage);
        }

        [Fact]
        public async Task TelemetryHandlerSelectivelyEnrichesRequestsBasedOnRequestMiddleWare()
        {
            // Arrange
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri("http://localhost")
            };
            var telemetryHandlerOption = new TelemetryHandlerOption
            {
                TelemetryConfigurator = (httpRequestMessage) =>
                {
                    httpRequestMessage.Headers.Add("SdkVersion","x.x.x");
                    return httpRequestMessage;
                }
            };
            // Configures the telemetry at the request level
            requestInfo.AddRequestOptions(telemetryHandlerOption);
            // Act and get a request message
            var requestMessage = requestAdapter.GetRequestMessageFromRequestInformation(requestInfo);
            Assert.Empty(requestMessage.Headers);

            // Act
            var response = await _invoker.SendAsync(requestMessage, new CancellationToken());

            // Assert the request was enriched as expected
            Assert.NotEmpty(response.RequestMessage?.Headers!);
            Assert.Single(response.RequestMessage?.Headers!);
            Assert.Equal("SdkVersion", response.RequestMessage?.Headers.First().Key);
            Assert.Equal(requestMessage, response.RequestMessage);
        }

        [Fact]
        public async Task TelemetryHandlerGloballyEnrichesRequests()
        {
            // Arrange
            // Configures the telemetry at the handler level
            var telemetryHandlerOption = new TelemetryHandlerOption
            {
                TelemetryConfigurator = (httpRequestMessage) =>
                {
                    httpRequestMessage.Headers.Add("SdkVersion", "x.x.x");
                    return httpRequestMessage;
                }
            };
            var handler = new TelemetryHandler(telemetryHandlerOption)
            {
                InnerHandler = new FakeSuccessHandler()
            };

            var invoker = new HttpMessageInvoker(handler);
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri("http://localhost")
            };

            var requestMessage = requestAdapter.GetRequestMessageFromRequestInformation(requestInfo);// get a request message
            Assert.Empty(requestMessage.Headers);

            // Act
            var response = await invoker.SendAsync(requestMessage, new CancellationToken());

            // Assert the request was enriched as expected
            Assert.NotEmpty(response.RequestMessage?.Headers!);
            Assert.Single(response.RequestMessage?.Headers!);
            Assert.Equal("SdkVersion", response.RequestMessage?.Headers.First().Key);
            Assert.Equal(requestMessage, response.RequestMessage);
        }
    }
}
