export default interface IHttpCore<NativeResponseType> {
    sendAsync(requestInfo: RequestInfo): Promise<ReadableStream>;
    sendNativeAsync(requestInfo: RequestInfo): Promise<NativeResponseType>;
}