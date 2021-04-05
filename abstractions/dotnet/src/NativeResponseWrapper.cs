using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kiota.Abstractions {
    public class NativeResponseWrapper {
        public static async Task<NativeResponseType> CallAndGetNativeType<ModelType, NativeResponseType, QueryParametersType>(
                Func<Action<QueryParametersType>, Action<IDictionary<string, string>>, IResponseHandler, Task<ModelType>> originalCall,
                Action<QueryParametersType> q = default, 
                Action<IDictionary<string, string>> h = default) where NativeResponseType : class {
            var responseHandler = new NativeResponseHandler();
            await originalCall.Invoke(q, h, responseHandler);
            return responseHandler.Value as NativeResponseType;
        }

        public static async Task<NativeResponseType> CallAndGetNativeType<ModelType, NativeResponseType, QueryParametersType, RequestBodyType>(
                Func<RequestBodyType, Action<QueryParametersType>, Action<IDictionary<string, string>>, IResponseHandler, Task<ModelType>> originalCall,
                RequestBodyType requestBody,
                Action<QueryParametersType> q = default, 
                Action<IDictionary<string, string>> h = default) where NativeResponseType : class {
            var responseHandler = new NativeResponseHandler();
            await originalCall.Invoke(requestBody, q, h, responseHandler);
            return responseHandler.Value as NativeResponseType;
        }
    }
}
