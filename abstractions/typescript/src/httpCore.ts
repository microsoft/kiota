import { RequestInfo } from "./requestInfo";
import { ResponseHandler } from "./responseHandler";
import { Parsable } from "./serialization";
export interface HttpCore {
    sendAsync<ModelType extends Parsable<ModelType>>(requestInfo: RequestInfo, type: new() => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType>;
}