// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Http.HttpClient.Extensions;
using Microsoft.Kiota.Http.HttpClient.Middleware.Options;

namespace Microsoft.Kiota.Http.HttpClient.Middleware
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation using standard .NET libraries.
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private const string RetryAfter = "Retry-After";
        private const string RetryAttempt = "Retry-Attempt";

        /// <summary>
        /// RetryOption property
        /// </summary>
        internal RetryHandlerOption RetryOption
        {
            get; set;
        }

        /// <summary>
        /// Construct a new <see cref="RetryHandler"/>
        /// </summary>
        /// <param name="retryOption">An OPTIONAL <see cref="RetryHandlerOption"/> to configure <see cref="RetryHandler"/></param>
        public RetryHandler(RetryHandlerOption retryOption = null)
        {
            RetryOption = retryOption ?? new RetryHandlerOption();
        }

        /// <summary>
        /// Send a HTTP request
        /// </summary>
        /// <param name="httpRequest">The HTTP request<see cref="HttpRequestMessage"/>needs to be sent.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            if(httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            RetryOption = httpRequest.GetRequestOption<RetryHandlerOption>() ?? RetryOption;

            var response = await base.SendAsync(httpRequest, cancellationToken);

            // Check whether retries are permitted and that the MaxRetry value is a non - negative, non - zero value
            if(httpRequest.IsBuffered() && RetryOption.MaxRetry > 0 && (ShouldRetry(response.StatusCode) || RetryOption.ShouldRetry(RetryOption.Delay, 0, response)))
            {
                response = await SendRetryAsync(response, cancellationToken);
            }

            return response;
        }

        /// <summary>
        /// Retry sending the HTTP request
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> which is returned and includes the HTTP request needs to be retried.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the retry.</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> SendRetryAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            TimeSpan cumulativeDelay = TimeSpan.Zero;

            while(retryCount < RetryOption.MaxRetry)
            {
                // Drain response content to free connections. Need to perform this
                // before retry attempt and before the TooManyRetries ServiceException.
                await response.Content.ReadAsByteArrayAsync(cancellationToken);

                // Call Delay method to get delay time from response's Retry-After header or by exponential backoff
                Task delay = Delay(response, retryCount, RetryOption.Delay, out double delayInSeconds, cancellationToken);

                // If client specified a retries time limit, let's honor it
                if(RetryOption.RetriesTimeLimit > TimeSpan.Zero)
                {
                    // Get the cumulative delay time
                    cumulativeDelay += TimeSpan.FromSeconds(delayInSeconds);

                    // Check whether delay will exceed the client-specified retries time limit value
                    if(cumulativeDelay > RetryOption.RetriesTimeLimit)
                    {
                        return response;
                    }
                }

                // general clone request with internal CloneAsync (see CloneAsync for details) extension method
                var request = await response.RequestMessage.CloneAsync();

                // Increase retryCount and then update Retry-Attempt in request header
                retryCount++;
                AddOrUpdateRetryAttempt(request, retryCount);

                // Delay time
                await delay;

                // Call base.SendAsync to send the request
                response = await base.SendAsync(request, cancellationToken);

                if(!(request.IsBuffered() && (ShouldRetry(response.StatusCode) || RetryOption.ShouldRetry(RetryOption.Delay, retryCount, response))))
                {
                    return response;
                }
            }

            // Drain response content to free connections. Need to perform this
            // before retry attempt and before the TooManyRetries ServiceException.
            await response.Content.ReadAsByteArrayAsync(cancellationToken);

            throw new InvalidOperationException(
                "Too many retries performed",
                new Exception($"More than {retryCount} retries encountered while sending the request."));
        }

        /// <summary>
        /// Update Retry-Attempt header in the HTTP request
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>needs to be sent.</param>
        /// <param name="retryCount">Retry times</param>
        private static void AddOrUpdateRetryAttempt(HttpRequestMessage request, int retryCount)
        {
            if(request.Headers.Contains(RetryAttempt))
            {
                request.Headers.Remove(RetryAttempt);
            }
            request.Headers.Add(RetryAttempt, retryCount.ToString());
        }

        /// <summary>
        /// Delay task operation for timed-retries based on Retry-After header in the response or exponential back-off
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>returned.</param>
        /// <param name="retryCount">The retry counts</param>
        /// <param name="delay">Delay value in seconds.</param>
        /// <param name="delayInSeconds"></param>
        /// <param name="cancellationToken">The cancellationToken for the Http request</param>
        /// <returns>The <see cref="Task"/> for delay operation.</returns>
        internal Task Delay(HttpResponseMessage response, int retryCount, int delay, out double delayInSeconds, CancellationToken cancellationToken)
        {
            delayInSeconds = delay;
            if(response.Headers.TryGetValues(RetryAfter, out IEnumerable<string> values))
            {
                string retryAfter = values.First();
                // the delay could be in the form of a seconds or a http date. See https://httpwg.org/specs/rfc7231.html#header.retry-after
                if(Int32.TryParse(retryAfter, out int delaySeconds))
                {
                    delayInSeconds = delaySeconds;
                }
                else if(DateTime.TryParseExact(retryAfter, CultureInfo.InvariantCulture.DateTimeFormat.RFC1123Pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                {
                    var timeSpan = dateTime - DateTime.Now;
                    // ensure the delay is a positive span otherwise use the exponential back-off
                    delayInSeconds = timeSpan.Seconds > 0 ? timeSpan.Seconds: CalculateExponentialDelay(retryCount, delay);
                }
            }
            else
            {
                delayInSeconds = CalculateExponentialDelay(retryCount, delay);
            }

            TimeSpan delayTimeSpan = TimeSpan.FromSeconds(Math.Min(delayInSeconds, RetryHandlerOption.MaxDelay));
            return Task.Delay(delayTimeSpan, cancellationToken);
        }

        /// <summary>
        /// Calculates the delay based on the exponential back off
        /// </summary>
        /// <param name="retryCount">The retry count</param>
        /// <param name="delay">The base to use as a delay</param>
        /// <returns></returns>
        private static double CalculateExponentialDelay(int retryCount, int delay)
        {
            return Math.Pow(2, retryCount) * delay;
        }

        /// <summary>
        /// Check the HTTP status to determine whether it should be retried or not.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>returned.</param>
        /// <returns></returns>
        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                _ => false
            };
        }
    }
}
