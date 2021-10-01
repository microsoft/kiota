// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Net.Http;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Http.HttpClient.Middleware.Options
{
    /// <summary>
    /// The retry request option class
    /// </summary>
    public class RetryHandlerOption : IRequestOption
    {
        internal const int DefaultDelay = 3;
        internal const int DefaultMaxRetry = 3;
        internal const int MaxMaxRetry = 10;
        internal const int MaxDelay = 180;
        private int _maxRetry = DefaultMaxRetry;
        private int _delay = DefaultDelay;

        /// <summary>
        /// The waiting time in seconds before retrying a request with a maximum value of 180 seconds. This defaults to 3 seconds.
        /// </summary>
        public int Delay
        {
            get
            {
                return _delay;
            }
            set
            {
                if(value > MaxDelay)
                {
                    throw new InvalidOperationException($"Maximum value for {nameof(MaxDelay)} property exceeded ");
                }

                _delay = value;
            }
        }

        /// <summary>
        /// The maximum number of retries for a request with a maximum value of 10. This defaults to 3.
        /// </summary>
        public int MaxRetry
        {
            get
            {
                return _maxRetry;
            }
            set
            {
                if(value > MaxMaxRetry)
                {
                    throw new InvalidOperationException($"Maximum value for {nameof(MaxMaxRetry)} property exceeded ");
                }
                _maxRetry = value;
            }
        }

        /// <summary>
        /// The maximum time allowed for request retries.
        /// </summary>
        public TimeSpan RetriesTimeLimit { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// A delegate that's called to determine whether a request should be retried or not.
        /// The delegate method should accept a delay time in seconds of, number of retry attempts and <see cref="HttpResponseMessage"/> as it's parameters and return a <see cref="bool"/>. This defaults to false
        /// </summary>
        public Func<int, int, HttpResponseMessage, bool> ShouldRetry { get; set; } = (_, _, _) => false;
    }
}
