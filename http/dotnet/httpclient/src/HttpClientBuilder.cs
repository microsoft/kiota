// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClient.Middleware;

namespace Microsoft.Kiota.Http.HttpClient
{
    /// <summary>
    /// This class is used to build the HttpClient instance used by the core service.
    /// </summary>
    public static class HttpClientBuilder
    {
        /// <summary>
        /// Initializes the <see cref="HttpClient"/> with the default configuration and middlewares including a authentication middleware using the <see cref="IAuthenticationProvider"/> if provided.
        /// </summary>
        /// <param name="authenticationProvider">The <see cref="IAuthenticationProvider"/> to use for authentication.</param>
        /// <returns>The <see cref="HttpClient"/> with the default middlewares.</returns>
        public static System.Net.Http.HttpClient Create(IAuthenticationProvider authenticationProvider = default)
        {
            var defaultHandlers = CreateDefaultHandlers(authenticationProvider);
            var handler = ChainHandlersCollectionAndGetFirstLink(defaultHandlers.ToArray());
            return handler != null ? new System.Net.Http.HttpClient(handler) : new System.Net.Http.HttpClient(); //TODO configure the default client options
        }
        /// <summary>
        /// Creates a default set of middleware to be used by the <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="authenticationProvider">The <see cref="IAuthenticationProvider"/> to authenticate requests.</param>
        /// <returns>A list of the default handlers used by the client.</returns>
        public static IList<DelegatingHandler> CreateDefaultHandlers(IAuthenticationProvider authenticationProvider = default)
        {
            return new List<DelegatingHandler>
            {
                //add the default middlewares as they are ready
                new RetryHandler(),
                new RedirectHandler()
            };
        }
        /// <summary>
        /// Creates a <see cref="DelegatingHandler"/> to use for the <see cref="HttpClient" /> from the provided <see cref="DelegatingHandler"/> instances. Order matters.
        /// </summary>
        /// <param name="handlers">The <see cref="DelegatingHandler"/> instances to create the <see cref="DelegatingHandler"/> from.</param>
        /// <returns>The created <see cref="DelegatingHandler"/>.</returns>
        public static DelegatingHandler ChainHandlersCollectionAndGetFirstLink(params DelegatingHandler[] handlers)
        {
            if(handlers == null || !handlers.Any()) return default;
            var handlersCount = handlers.Length;
            for(var i = 0; i < handlersCount; i++)
            {
                var handler = handlers[i];
                var previousItemIndex = i - 1;
                if(previousItemIndex >= 0)
                {
                    var previousHandler = handlers[previousItemIndex];
                    previousHandler.InnerHandler = handler;
                }
            }
            return handlers.First();
        }
    }
}
