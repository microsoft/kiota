// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    /// Default response handler to access the native response object.
    /// </summary>
    public class NativeResponseHandler : IResponseHandler
    {
        /// <summary>
        /// The value of the response
        /// </summary>
        public object Value;

        /// <summary>
        /// The error mappings for the response to use when deserializing failed responses bodies. Where an error code like 401 applies specifically to that status code, a class code like 4XX applies to all status codes within the range if an the specific error code is not present.
        /// </summary>
        public Dictionary<string, ParsableFactory<IParsable>> ErrorMappings { get; set; }

        /// <inheritdoc />
        public Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response, Dictionary<string, ParsableFactory<IParsable>> errorMappings)
        {
            Value = response;
            ErrorMappings = errorMappings;
            return Task.FromResult(default(ModelType));
        }
    }
}
