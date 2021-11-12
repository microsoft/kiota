// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    /// This class can be used to wrap a request using the fluent API and get the native response object in return.
    /// </summary>
    public class NativeResponseWrapper
    {
        /// <summary>
        /// Makes a request with the <typeparam name="QueryParametersType"/> instance to get a response with
        /// a <typeparam name="NativeResponseType"/> instance and expect an instance of <typeparam name="ModelType"/>
        /// </summary>
        /// <param name="originalCall">The original request to make</param>
        /// <param name="q">The query parameters of the request</param>
        /// <param name="h">The request headers of the request</param>
        /// <param name="o">Request options</param>
        /// <returns></returns>
        public static async Task<NativeResponseType> CallAndGetNativeType<ModelType, NativeResponseType, QueryParametersType>(
                Func<Action<QueryParametersType>, Action<IDictionary<string, string>>, IEnumerable<IRequestOption>, IResponseHandler, Task<ModelType>> originalCall,
                Action<QueryParametersType> q = default,
                Action<IDictionary<string, string>> h = default,
                IEnumerable<IRequestOption> o = default) where NativeResponseType : class
        {
            var responseHandler = new NativeResponseHandler();
            await originalCall.Invoke(q, h, o, responseHandler);
            return responseHandler.Value as NativeResponseType;
        }

        /// <summary>
        /// Makes a request with the <typeparam name="RequestBodyType"/> and <typeparam name="QueryParametersType"/> instances to get a response with
        /// a <typeparam name="NativeResponseType"/> instance and expect an instance of <typeparam name="ModelType"/>
        /// </summary>
        /// <param name="originalCall">The original request to make</param>
        /// <param name="requestBody">The request body of the request</param>
        /// <param name="q">The query parameters of the request</param>
        /// <param name="h">The request headers of the request</param>
        /// <param name="o">Request options</param>
        public static async Task<NativeResponseType> CallAndGetNativeType<ModelType, NativeResponseType, QueryParametersType, RequestBodyType>(
                Func<RequestBodyType, Action<QueryParametersType>, Action<IDictionary<string, string>>, IEnumerable<IRequestOption>, IResponseHandler, Task<ModelType>> originalCall,
                RequestBodyType requestBody,
                Action<QueryParametersType> q = default,
                Action<IDictionary<string, string>> h = default,
                IEnumerable<IRequestOption> o = default) where NativeResponseType : class
        {
            var responseHandler = new NativeResponseHandler();
            await originalCall.Invoke(requestBody, q, h, o, responseHandler);
            return responseHandler.Value as NativeResponseType;
        }
    }
}
