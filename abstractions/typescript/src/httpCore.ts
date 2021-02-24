import { RequestInfo } from "./requestInfo";
export interface HttpCore {
    sendAsync(requestInfo: RequestInfo): Promise<ReadableStream>;
    sendNativeAsync<NativeResponseType>(requestInfo: RequestInfo): Promise<NativeResponseType>;
}