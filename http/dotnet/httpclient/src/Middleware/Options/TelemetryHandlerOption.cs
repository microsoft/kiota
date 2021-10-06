using System;
using System.Net.Http;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options
{
    /// <summary>
    /// The Telemetry request option class
    /// </summary>
    public class TelemetryHandlerOption : IRequestOption
    {
        /// <summary>
        /// A delegate that's called to configure the <see cref="HttpRequestMessage"/> with the appropriate telemetry values.
        /// </summary>
        public Func<HttpRequestMessage, HttpRequestMessage> TelemetryConfigurator { get; set; } = (request) => request;
    }
}
