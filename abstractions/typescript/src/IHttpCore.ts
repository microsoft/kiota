export default interface IHttpCore {
    sendAsync(requestInfo: RequestInfo): Promise<ReadableStream>;
    sendNativeAsync<NativeResponseType>(requestInfo: RequestInfo): Promise<NativeResponseType>;
}