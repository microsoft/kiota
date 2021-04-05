import { NativeResponseHandler } from "./nativeResponseHandler";
import { ResponseHandler } from "./responseHandler";

type headersCallBackType = (h: Map<string, string>) => void;
type queryParamsCallbackType<QueryParametersType> = (q: QueryParametersType) => void;
type originalCallType<ModelType, QueryParametersType> = (q?: queryParamsCallbackType<QueryParametersType>, h?: headersCallBackType, responseHandler?: ResponseHandler) => Promise<ModelType>;
type originalCallWithBodyType<ModelType, QueryParametersType, RequestBodyType> = (requestBody: RequestBodyType, q?: queryParamsCallbackType<QueryParametersType>, h?: headersCallBackType, responseHandler?: ResponseHandler) => Promise<ModelType>;

export class NativeResponseWrapper {
    public static CallAndGetNative = async <ModelType, NativeResponseType, QueryParametersType>(
        originalCall: originalCallType<ModelType, QueryParametersType>,
        q?: queryParamsCallbackType<QueryParametersType>,
        h?: headersCallBackType
    ) : Promise<NativeResponseType> => {
        const responseHandler = new NativeResponseHandler();
        await originalCall(q, h, responseHandler);
        return responseHandler.value as NativeResponseType;
    }
    public static CallAndGetNativeWithBody = async <ModelType, NativeResponseType, QueryParametersType, RequestBodyType>(
        originalCall: originalCallWithBodyType<ModelType, QueryParametersType, RequestBodyType>,
        requestBody: RequestBodyType,
        q?: queryParamsCallbackType<QueryParametersType>,
        h?: headersCallBackType
    ) : Promise<NativeResponseType> => {
        const responseHandler = new NativeResponseHandler();
        await originalCall(requestBody, q, h, responseHandler);
        return responseHandler.value as NativeResponseType;
    }
}