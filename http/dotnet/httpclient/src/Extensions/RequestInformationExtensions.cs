// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Http.HttpClient.Extensions
{
    /// <summary>
    /// Extenstion methods for the <see cref="RequestInformation"/> class
    /// </summary>
    public static class RequestInformationExtensions
    {
        /// <summary>
        /// Sets the request content from an instance of <see cref="HttpContent"/>
        /// </summary>
        /// <param name="requestInformation">The <see cref="RequestInformation"/> instance to set its content</param>
        /// <param name="httpContent">The <see cref="HttpContent"/> instance to set as a content of the request.</param>
        /// <param name="cancellationToken">The (optional) <see cref="CancellationToken"/> to use.</param>
        public static async Task SetContentFromHttpContentAsync(this RequestInformation requestInformation, HttpContent httpContent, CancellationToken cancellationToken = default)
        {
            if(requestInformation == null)
                throw new ArgumentNullException(nameof(requestInformation));
            if(httpContent == null)
                throw new ArgumentNullException(nameof(httpContent));

            var httpContentStream = await httpContent.ReadAsStreamAsync(cancellationToken);
            requestInformation.Content = httpContentStream;
            foreach(var (key, value) in httpContent.Headers)
                requestInformation.Headers.Add(key, value.First());
        }
    }
}
