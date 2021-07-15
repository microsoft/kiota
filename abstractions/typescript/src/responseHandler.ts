/** Defines the contract for a response handler. */
export interface ResponseHandler {
    /**
     * Callback method that is invoked when a response is received.
     * @param response The native response object.
     * @typeParam NativeResponseType The type of the native response object.
     * @typeParam ModelType The type of the response model object.
     * @return A {@link Promise} that represents the asynchronous operation and contains the deserialized response.
     */
    handleResponseAsync<NativeResponseType, ModelType>(response: NativeResponseType): Promise<ModelType>;
}