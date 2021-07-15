import { RequestInfo } from "./requestInfo";
import { ResponseHandler } from "./responseHandler";
import { Parsable, SerializationWriterFactory } from "./serialization";

/** Service responsible for translating abstract Request Info into concrete native HTTP requests. */
export interface HttpCore {
    /**
     * Gets the serialization writer factory currently in use for the HTTP core service.
     * @return the serialization writer factory currently in use for the HTTP core service.
     */
    getSerializationWriterFactory(): SerializationWriterFactory;
    /**
     * Excutes the HTTP request specified by the given RequestInfo and returns the deserialized response model.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @param type the class of the response model to deserialize the response into.
     * @typeParam ModelType the type of the response model to deserialize the response into.
     * @return a {@link Promise} with the deserialized response model.
     */
    sendAsync<ModelType extends Parsable>(requestInfo: RequestInfo, type: new() => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType>;
    /**
     * Excutes the HTTP request specified by the given RequestInfo and returns the deserialized primitive response model.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @param responseType the class of the response model to deserialize the response into.
     * @typeParam responseType the type of the response model to deserialize the response into.
     * @return a {@link Promise} with the deserialized primitive response model.
     */
    sendPrimitiveAsync<ResponseType>(requestInfo: RequestInfo, responseType: "string" | "number" | "boolean" | "Date" | "ReadableStream", responseHandler: ResponseHandler | undefined): Promise<ResponseType>;
    /**
     * Excutes the HTTP request specified by the given RequestInfo and returns the deserialized primitive response model.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @return a {@link Promise} of void.
     */
    sendNoResponseContentAsync(requestInfo: RequestInfo, responseHandler: ResponseHandler | undefined): Promise<void>;
    /** Enables the backing store proxies for the SerializationWriters and ParseNodes in use. */
    enableBackingStore(): void;
}