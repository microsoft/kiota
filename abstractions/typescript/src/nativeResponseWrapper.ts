import { RequestOption } from "./requestOption";
import { NativeResponseHandler } from "./nativeResponseHandler";
import { ResponseHandler } from "./responseHandler";

type originalCallType<modelType, queryParametersType, headersType> = (q?: queryParametersType, h?: headersType, o?: RequestOption[] | undefined, responseHandler?: ResponseHandler) => Promise<modelType>;
type originalCallWithBodyType<modelType, queryParametersType, headersType, requestBodyType> = (requestBody: requestBodyType, q?: queryParametersType, h?: headersType, o?: RequestOption[] | undefined, responseHandler?: ResponseHandler) => Promise<modelType>;

/** This class can be used to wrap a request using the fluent API and get the native response object in return. */
export class NativeResponseWrapper {
    public static CallAndGetNative = async <modelType, nativeResponseType, queryParametersType, headersType>(
        originalCall: originalCallType<modelType, queryParametersType, headersType>,
        q?: queryParametersType,
        h?: headersType,
        o?: RequestOption[] | undefined
    ) : Promise<nativeResponseType> => {
        const responseHandler = new NativeResponseHandler();
        await originalCall(q, h, o, responseHandler);
        return responseHandler.value as nativeResponseType;
    }
    public static CallAndGetNativeWithBody = async <modelType, nativeResponseType, queryParametersType, headersType, requestBodyType>(
        originalCall: originalCallWithBodyType<modelType, queryParametersType, headersType, requestBodyType>,
        requestBody: requestBodyType,
        q?: queryParametersType,
        h?: headersType,
        o?: RequestOption[] | undefined
    ) : Promise<nativeResponseType> => {
        const responseHandler = new NativeResponseHandler();
        await originalCall(requestBody, q, h, o, responseHandler);
        return responseHandler.value as nativeResponseType;
    }
}