// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Http.HttpClient.Extensions;
using Microsoft.Kiota.Http.HttpClient.Middleware.Options;

namespace Microsoft.Kiota.Http.HttpClient.Middleware
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation that is used for simulating server failures.
    /// </summary>
    public class ChaosHandler : DelegatingHandler
    {
        private readonly DiagnosticSource _logger = new DiagnosticListener(typeof(ChaosHandler).FullName!);
        private readonly Random _random;
        private readonly ChaosHandlerOption _globalChaosHandlerOptions;
        private List<HttpResponseMessage> _knownGraphFailures;
        private const string Json = "application/json";

        /// <summary>
        /// Create a ChaosHandler.  
        /// </summary>
        /// <param name="chaosHandlerOptions">Optional parameter to change default behavior of handler.</param>
        public ChaosHandler(ChaosHandlerOption chaosHandlerOptions = null)
        {
            _globalChaosHandlerOptions = chaosHandlerOptions ?? new ChaosHandlerOption();
            _random = new Random(DateTime.Now.Millisecond);
            LoadKnownGraphFailures(_globalChaosHandlerOptions.KnownChaos);
        }

        /// <summary>
        /// Sends the request
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Select global or per request options
            var chaosHandlerOptions = request.GetMiddlewareOption<ChaosHandlerOption>() ?? _globalChaosHandlerOptions;

            HttpResponseMessage response = null;
            // Planned Chaos or Random?
            if(chaosHandlerOptions.PlannedChaosFactory != null)
            {
                response = chaosHandlerOptions.PlannedChaosFactory(request);
                if(response != null)
                {
                    response.RequestMessage = request;
                    if(_logger.IsEnabled("PlannedChaosResponse"))
                        _logger.Write("PlannedChaosResponse", response);
                }
            }
            else
            {
                if(_random.Next(100) < chaosHandlerOptions.ChaosPercentLevel)
                {
                    response = CreateChaosResponse(chaosHandlerOptions.KnownChaos ?? _knownGraphFailures);
                    response.RequestMessage = request;
                    if(_logger.IsEnabled("ChaosResponse"))
                        _logger.Write("ChaosResponse", response);
                }
            }

            if(response == null)
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            return response;
        }

        private HttpResponseMessage CreateChaosResponse(List<HttpResponseMessage> knownFailures)
        {
            var responseIndex = _random.Next(knownFailures.Count);
            return knownFailures[responseIndex];
        }

        private void LoadKnownGraphFailures(List<HttpResponseMessage> knownFailures)
        {
            if(knownFailures != null && knownFailures.Count > 0)
            {
                _knownGraphFailures = knownFailures;
            }
            else
            {
                _knownGraphFailures = new List<HttpResponseMessage>
                {
                    Create429TooManyRequestsResponse(new TimeSpan(0, 0, 3)),
                    Create503Response(new TimeSpan(0, 0, 3)),
                    Create504GatewayTimeoutResponse(new TimeSpan(0, 0, 3))
                };
            }
        }

        /// <summary>
        /// Create a HTTP status 429 response message
        /// </summary>
        /// <param name="retry"><see cref="TimeSpan"/> for retry condition header value</param>
        /// <returns>A <see cref="HttpResponseMessage"/> object simulating a 429 response</returns>
        public static HttpResponseMessage Create429TooManyRequestsResponse(TimeSpan retry)
        {
            var contentString = JsonSerializer.Serialize(new
            {
                error = new Error
                {
                    Code = "activityLimitReached",
                    Message ="Client application has been throttled and should not attempt to repeat the request until an amount of time has elapsed."
                }
            });
            var throttleResponse = new HttpResponseMessage()
            {
                StatusCode = (HttpStatusCode)429,
                Content = new StringContent(contentString, Encoding.UTF8, Json)
            };
            throttleResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retry);
            return throttleResponse;
        }

        /// <summary>
        /// Create a HTTP status 503 response message
        /// </summary>
        /// <param name="retry"><see cref="TimeSpan"/> for retry condition header value</param>
        /// <returns>A <see cref="HttpResponseMessage"/> object simulating a 503 response</returns>
        public static HttpResponseMessage Create503Response(TimeSpan retry)
        {
            var contentString = JsonSerializer.Serialize(new
            {
                error = new Error()
                {
                    Code = "serviceNotAvailable",
                    Message = "The service is temporarily unavailable for maintenance or is overloaded. You may repeat the request after a delay, the length of which may be specified in a Retry-After header."
                }
            });
            var serverUnavailableResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(contentString, Encoding.UTF8, Json)
            };
            serverUnavailableResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retry);
            return serverUnavailableResponse;
        }

        /// <summary>
        /// Create a HTTP status 502 response message
        /// </summary>
        /// <returns>A <see cref="HttpResponseMessage"/> object simulating a 502 Response</returns>
        public static HttpResponseMessage Create502BadGatewayResponse()
        {
            var contentString = JsonSerializer.Serialize(new
            {
                error = new Error()
                {
                    Code = "502"
                }
            });
            var badGatewayResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent(contentString, Encoding.UTF8, Json)
            };
            return badGatewayResponse;
        }

        /// <summary>
        /// Create a HTTP status 500 response message
        /// </summary>
        /// <returns>A <see cref="HttpResponseMessage"/> object simulating a 500 Response</returns>
        public static HttpResponseMessage Create500InternalServerErrorResponse()
        {
            var contentString = JsonSerializer.Serialize(new
            {
                error = new Error()
                {
                    Code = "generalException",
                    Message = "There was an internal server error while processing the request."
                }
            });
            var internalServerError = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(contentString, Encoding.UTF8, Json)
            };
            return internalServerError;
        }

        /// <summary>
        /// Create a HTTP status 504 response message
        /// </summary>
        /// <param name="retry"><see cref="TimeSpan"/> for retry condition header value</param>
        /// <returns>A <see cref="HttpResponseMessage"/> object simulating a 504 response</returns>
        public static HttpResponseMessage Create504GatewayTimeoutResponse(TimeSpan retry)
        {
            var contentString = JsonSerializer.Serialize(new
            {
                error = new Error()
                {
                    Code = "504",
                    Message = "The server, while acting as a proxy, did not receive a timely response from the upstream server it needed to access in attempting to complete the request. May occur together with 503."
                }
            });
            var gatewayTimeoutResponse = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.GatewayTimeout,
                Content = new StringContent(contentString, Encoding.UTF8, Json)
            };
            gatewayTimeoutResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retry);
            return gatewayTimeoutResponse;
        }

        // TODO this is not your home!
        private class Error
        {
            /// <summary>
            /// This code represents the HTTP status code when this Error object accessed from the ServiceException.Error object.
            /// This code represent a sub-code when the Error object is in the InnerError or ErrorDetails object.
            /// </summary>
            [JsonPropertyName("code")]
            public string Code
            {
                get; set;
            }

            /// <summary>
            /// The error message.
            /// </summary>
            [JsonPropertyName("message")]
            public string Message
            {
                get; set;
            }

            /// <summary>
            /// Indicates the target of the error, for example, the name of the property in error.
            /// </summary>
            [JsonPropertyName("target")]
            public string Target
            {
                get; set;
            }

            /// <summary>
            /// The inner error of the response. These are additional error objects that may be more specific than the top level error.
            /// </summary>
            [JsonPropertyName("innererror")]
            public Error InnerError
            {
                get; set;
            }

            /// <summary>
            /// The Throw site of the error.
            /// </summary>
            public string ThrowSite
            {
                get; internal set;
            }

            /// <summary>
            /// Gets or set the client-request-id header returned in the response headers collection. 
            /// </summary>
            public string ClientRequestId
            {
                get; internal set;
            }

            /// <summary>
            /// The AdditionalData property bag.
            /// </summary>
            [JsonExtensionData]
            public IDictionary<string, object> AdditionalData
            {
                get; set;
            }

            /// <summary>
            /// Concatenates the error into a string.
            /// </summary>
            /// <returns>A human-readable string error response.</returns>
            public override string ToString()
            {
                var errorStringBuilder = new StringBuilder();

                if(!string.IsNullOrEmpty(this.Code))
                {
                    errorStringBuilder.AppendFormat("Code: {0}", this.Code);
                    errorStringBuilder.Append(Environment.NewLine);
                }

                if(!string.IsNullOrEmpty(this.Message))
                {
                    errorStringBuilder.AppendFormat("Message: {0}", this.Message);
                    errorStringBuilder.Append(Environment.NewLine);
                }

                if(!string.IsNullOrEmpty(this.Target))
                {
                    errorStringBuilder.AppendFormat("Target: {0}", this.Target);
                    errorStringBuilder.Append(Environment.NewLine);
                }

                // if(this.Details != null && this.Details.GetEnumerator().MoveNext())
                // {
                //     errorStringBuilder.Append("Details:");
                //     errorStringBuilder.Append(Environment.NewLine);
                //
                //     int i = 0;
                //     foreach(var detail in this.Details)
                //     {
                //         errorStringBuilder.AppendFormat("\tDetail{0}:{1}", i, detail.ToString());
                //         errorStringBuilder.Append(Environment.NewLine);
                //         i++;
                //     }
                // }

                if(this.InnerError != null)
                {
                    errorStringBuilder.Append("Inner error:");
                    errorStringBuilder.Append(Environment.NewLine);
                    errorStringBuilder.Append("\t" + this.InnerError.ToString());
                }

                if(!string.IsNullOrEmpty(this.ThrowSite))
                {
                    errorStringBuilder.AppendFormat("Throw site: {0}", this.ThrowSite);
                    errorStringBuilder.Append(Environment.NewLine);
                }

                if(!string.IsNullOrEmpty(this.ClientRequestId))
                {
                    errorStringBuilder.AppendFormat("ClientRequestId: {0}", this.ClientRequestId);
                    errorStringBuilder.Append(Environment.NewLine);
                }

                if(this.AdditionalData != null && this.AdditionalData.GetEnumerator().MoveNext())
                {
                    errorStringBuilder.Append("AdditionalData:");
                    errorStringBuilder.Append(Environment.NewLine);
                    foreach(var prop in this.AdditionalData)
                    {
                        errorStringBuilder.AppendFormat("\t{0}: {1}", prop.Key, prop.Value?.ToString() ?? "null");
                        errorStringBuilder.Append(Environment.NewLine);
                    }
                }

                return errorStringBuilder.ToString();
            }
        }
    }
}
