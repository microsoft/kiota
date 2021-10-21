// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Http.HttpClientLibrary.Middleware
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation that handles compression.
    /// </summary>
    public class CompressionHandler : DelegatingHandler
    {
        internal const string GZip = "gzip";

        /// <summary>
        /// Sends a HTTP request.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/> to be sent.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            if(httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            StringWithQualityHeaderValue gzipQHeaderValue = new StringWithQualityHeaderValue(GZip);

            // Add Accept-encoding: gzip header to incoming request if it doesn't have one.
            if(!httpRequest.Headers.AcceptEncoding.Contains(gzipQHeaderValue))
            {
                httpRequest.Headers.AcceptEncoding.Add(gzipQHeaderValue);
            }

            HttpResponseMessage response = await base.SendAsync(httpRequest, cancellationToken);

            // Decompress response content when Content-Encoding: gzip header is present.
            if(ShouldDecompressContent(response))
            {
                StreamContent streamContent = new StreamContent(new GZipStream(await response.Content.ReadAsStreamAsync(cancellationToken), CompressionMode.Decompress));
                // Copy Content Headers to the destination stream content
                foreach(var httpContentHeader in response.Content.Headers)
                {
                    streamContent.Headers.TryAddWithoutValidation(httpContentHeader.Key, httpContentHeader.Value);
                }
                response.Content = streamContent;
            }

            return response;
        }

        /// <summary>
        /// Checks if a <see cref="HttpResponseMessage"/> contains a Content-Encoding: gzip header.
        /// </summary>
        /// <param name="httpResponse">The <see cref="HttpResponseMessage"/> to check for header.</param>
        /// <returns></returns>
        private static bool ShouldDecompressContent(HttpResponseMessage httpResponse)
        {
            return httpResponse.Content.Headers.ContentEncoding.Contains(GZip);
        }
    }
}
