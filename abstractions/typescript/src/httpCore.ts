import { RequestInfo } from "./requestInfo";
import { ResponseHandler } from "./responseHandler";
import { Parsable } from "./serialization";
export interface HttpCore {
    sendAsync<ModelType extends Parsable>(requestInfo: RequestInfo, type: new() => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType>;
    sendPrimitiveAsync<ResponseType>(requestInfo: RequestInfo, responseType: "string" | "number" | "boolean" | "Date" | "ReadableStream", responseHandler: ResponseHandler | undefined): Promise<ResponseType>;
    sendNoResponseContentAsync(requestInfo: RequestInfo, responseHandler: ResponseHandler | undefined): Promise<void>;
}