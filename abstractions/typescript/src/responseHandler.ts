export interface ResponseHandler {
    handleResponseAsync<NativeResponseType, ModelType>(response: NativeResponseType): Promise<ModelType>;
}