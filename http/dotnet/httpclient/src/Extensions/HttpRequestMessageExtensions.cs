// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using HttpMethod = System.Net.Http.HttpMethod;

namespace Microsoft.Kiota.Http.HttpClient.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="HttpRequestMessage"/>
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Gets a <see cref="IRequestOption"/> from <see cref="HttpRequestMessage"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representation of the request.</param>
        /// <returns>A request option</returns>
        public static T GetRequestOption<T>(this HttpRequestMessage httpRequestMessage) where T : IRequestOption
        {
            if(httpRequestMessage.Options.TryGetValue(
                new HttpRequestOptionsKey<IRequestOption>(typeof(T).FullName),
                out IRequestOption requestOption))
            {
                return (T)requestOption;
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

        /// <summary>
        /// Checks the HTTP request's content to determine if it's buffered or streamed content.
        /// </summary>
        /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/>needs to be sent.</param>
        /// <returns></returns>
        internal static bool IsBuffered(this HttpRequestMessage httpRequestMessage)
        {
            HttpContent requestContent = httpRequestMessage.Content;

            if((httpRequestMessage.Method == HttpMethod.Put || httpRequestMessage.Method == HttpMethod.Post || httpRequestMessage.Method == HttpMethod.Patch)
               && requestContent != null && (requestContent.Headers.ContentLength == null || (int)requestContent.Headers.ContentLength == -1))
            {
                return false;
            }
            return true;
        }
    }
}
