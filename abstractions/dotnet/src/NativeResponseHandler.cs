// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Threading.Tasks;

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
        /// Handles the response of type <typeparam name="NativeResponseType"/>and return an instance of <typeparam name="ModelType"/>
        /// </summary>
        /// <param name="response">The response to be handled</param>
        /// <returns></returns>
        public Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response)
        {
            Value = response;
            return Task.FromResult(default(ModelType));
        }
    }
}
