// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Http.HttpClient.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="HttpRequestMessage"/>
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Gets a <see cref="IMiddlewareOption"/> from <see cref="HttpRequestMessage"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representation of the request.</param>
        /// <returns>A middleware option</returns>
        public static T GetMiddlewareOption<T>(this HttpRequestMessage httpRequestMessage) where T : IMiddlewareOption
        {
            if(httpRequestMessage.Options.TryGetValue(
                new HttpRequestOptionsKey<IMiddlewareOption>(typeof(T).FullName),
                out IMiddlewareOption middlewareOption))
            {
                return (T)middlewareOption;
            }
            return default;
        }

        /// <summary>
        /// Create a new HTTP request by copying previous HTTP request's headers and properties from response's request message.
        /// </summary>
        /// <param name="originalRequest">The previous <see cref="HttpRequestMessage"/> needs to be copy.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        /// <remarks>
        /// Re-issue a new HTTP request with the previous request's headers and properties
        /// </remarks>
        internal static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage originalRequest)
        {
            var newRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);

            // Copy request headers.
            foreach(var (key, value) in originalRequest.Headers)
                newRequest.Headers.TryAddWithoutValidation(key, value);

            // Copy request properties.
            foreach(var (key, value) in originalRequest.Options)
                newRequest.Options.TryAdd(key, value);

            // Set Content if previous request had one.
            if(originalRequest.Content != null)
            {
                // HttpClient doesn't rewind streams and we have to explicitly do so.
                var contentStream = await originalRequest.Content.ReadAsStreamAsync();
                
                if(contentStream.CanSeek)
                    contentStream.Seek(0, SeekOrigin.Begin);

                newRequest.Content = new StreamContent(contentStream);

                // Copy content headers.
                foreach(var (key, value) in originalRequest.Content.Headers)
                {
                    newRequest.Content?.Headers.TryAddWithoutValidation(key, value);
                }
            }

            return newRequest;
        }
    }
}
