// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Net.Http;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Http.HttpClient.Middleware.Options
{
    /// <summary>
    /// The redirect middleware option class
    /// </summary>
    public class RedirectHandlerOption: IMiddlewareOption
    {
        private const int DefaultMaxRedirect = 5;
        private const int MaxMaxRedirect = 20;
        private int _maxRedirect = DefaultMaxRedirect;

        /// <summary>
        /// The maximum number of redirects with a maximum value of 20. This defaults to 5 redirects.
        /// </summary>
        public int MaxRedirect
        {
            get
            {
                return _maxRedirect;
            }
            set
            {
                if(value > MaxMaxRedirect)
                    throw new InvalidOperationException($"Maximum value for {nameof(MaxRedirect)} property exceeded ");

                _maxRedirect = value;
            }
        }

        /// <summary>
        /// A delegate that's called to determine whether a response should be redirected or not. The delegate method should accept <see cref="HttpResponseMessage"/> as it's parameter and return a <see cref="bool"/>. This defaults to true.
        /// </summary>
        public Func<HttpResponseMessage, bool> ShouldRedirect { get; set; } = (response) => true;
    }
}
