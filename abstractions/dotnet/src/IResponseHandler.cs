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
    ///     Defines the contract for a response handler.
    /// </summary>
    public interface IResponseHandler
    {
        /// <summary>
        ///     Callback method that is invoked when a response is received.
        /// </summary>
        /// <param name="response">The native response object.</param>
        /// <param name="errorMappings">The error mappings for the response to use when deserializing failed responses bodies. Where an error code like 401 applies specifically to that status code, a class code like 4XX applies to all status codes within the range if an the specific error code is not present.</param>
        /// <typeparam name="NativeResponseType">The type of the native response object.</typeparam>
        /// <typeparam name="ModelType">The type of the response model object.</typeparam>
        /// <returns>A task that represents the asynchronous operation and contains the deserialized response.</returns>
        Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response, Dictionary<string, ParsableFactory<IParsable>> errorMappings);
    }
}
