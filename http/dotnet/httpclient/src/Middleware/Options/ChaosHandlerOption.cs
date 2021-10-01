// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Http.HttpClient.Middleware.Options
{
    /// <summary>
    /// The Chaos Handler Option middleware class
    /// </summary>
    public class ChaosHandlerOption : IRequestOption
    {
        /// <summary>
        /// Percentage of responses that will have KnownChaos responses injected, assuming no PlannedChaosFactory is provided
        /// </summary>
        public int ChaosPercentLevel { get; set; } = 10;
        /// <summary>
        /// List of failure responses that potentially could be returned when
        /// </summary>
        public List<HttpResponseMessage> KnownChaos { get; set; }
        /// <summary>
        /// Function to return chaos response based on current request.  This is used to reproduce detected failure modes.
        /// </summary>
        public Func<HttpRequestMessage, HttpResponseMessage> PlannedChaosFactory { get; set; }
    }
}
