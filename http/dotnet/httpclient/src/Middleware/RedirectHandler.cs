// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Http.HttpClient.Extensions;
using Microsoft.Kiota.Http.HttpClient.Middleware.Options;

namespace Microsoft.Kiota.Http.HttpClient.Middleware
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation for handling redirection of requests.
    /// </summary>
    public class RedirectHandler: DelegatingHandler
    {
        /// <summary>
        /// Constructs a new <see cref="RedirectHandler"/> 
        /// </summary>
        /// <param name="redirectOption">An OPTIONAL <see cref="RedirectHandlerOption"/> to configure <see cref="RedirectHandler"/></param>
        public RedirectHandler(RedirectHandlerOption redirectOption = null)
        {
            RedirectOption = redirectOption ?? new RedirectHandlerOption();
        }

        /// <summary>
        /// RedirectOption property
        /// </summary>
        internal RedirectHandlerOption RedirectOption
        {
            get; set;
        }

        /// <summary>
        /// Sends the Request and handles redirect responses if needed
        /// </summary>
        /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>for the request.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            if(httpRequestMessage == null) throw new ArgumentNullException(nameof(httpRequestMessage));

            RedirectOption = httpRequestMessage.GetMiddlewareOption<RedirectHandlerOption>() ?? RedirectOption;

            // send request first time to get response
            var response = await base.SendAsync(httpRequestMessage, cancellationToken);

            // check response status code and redirect handler option
            if(ShouldRedirect(response))
            {
                if(response.Headers.Location == null)
                {
                    throw new InvalidOperationException(
                        "Unable to perform redirect as Location Header is not set in response",
                                new Exception($"No header present in response with status code {response.StatusCode}"));
                }

                var redirectCount = 0;

                while(redirectCount < RedirectOption.MaxRedirect)
                {
                    // Drain response content to free responses.
                    await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    // general clone request with internal CloneAsync (see CloneAsync for details) extension method 
                    var newRequest = await response.RequestMessage.CloneAsync();

                    // status code == 303: change request method from post to get and content to be null
                    if(response.StatusCode == HttpStatusCode.SeeOther)
                    {
                        newRequest.Content = null;
                        newRequest.Method = HttpMethod.Get;
                    }

                    // Set newRequestUri from response
                    if(response.Headers.Location?.IsAbsoluteUri ?? false)
                    {
                        newRequest.RequestUri = response.Headers.Location;
                    }
                    else
                    {
                        var baseAddress = newRequest.RequestUri?.GetComponents(UriComponents.SchemeAndServer | UriComponents.KeepDelimiter, UriFormat.Unescaped);
                        newRequest.RequestUri = new Uri(baseAddress + response.Headers.Location);
                    }

                    // Remove Auth if http request's scheme or host changes
                    if(!newRequest.RequestUri.Host.Equals(httpRequestMessage.RequestUri?.Host) ||
                       !newRequest.RequestUri.Scheme.Equals(httpRequestMessage.RequestUri?.Scheme))
                    {
                        newRequest.Headers.Authorization = null;
                    }

                    // If scheme has changed. Ensure that this has been opted in for security reasons
                    if(!newRequest.RequestUri.Scheme.Equals(httpRequestMessage.RequestUri?.Scheme) && !RedirectOption.AllowRedirectOnSchemeChange)
                    {
                        throw new InvalidOperationException(
                            $"Redirects with changing schemes not allowed by default. You can change this by modifying the {nameof(RedirectOption.AllowRedirectOnSchemeChange)} option",
                            new Exception($"Scheme changed from {httpRequestMessage.RequestUri?.Scheme} to {newRequest.RequestUri.Scheme}."));
                    }

                    // Send redirect request to get response
                    response = await base.SendAsync(newRequest, cancellationToken);

                    // Check response status code
                    if(ShouldRedirect(response))
                    {
                        redirectCount++;
                    }
                    else
                    {
                        return response;
                    }
                }

                throw new InvalidOperationException(
                    "Too many redirects performed",
                    new Exception($"Max redirects exceeded. Redirect count : {redirectCount}"));
            }
            return response;
        }

        private bool ShouldRedirect(HttpResponseMessage responseMessage)
        {
            return IsRedirect(responseMessage.StatusCode) && RedirectOption.ShouldRedirect(responseMessage) && RedirectOption.MaxRedirect > 0;
        }

        /// <summary>
        /// Checks whether <see cref="HttpStatusCode"/> is redirected
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        /// <returns>Bool value for redirection or not</returns>
        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.MovedPermanently => true,
                HttpStatusCode.Found => true,
                HttpStatusCode.SeeOther => true,
                HttpStatusCode.TemporaryRedirect => true,
                (HttpStatusCode)308 => true,
                _ => false
            };
        }

    }
}
