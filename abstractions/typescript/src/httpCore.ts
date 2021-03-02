import { RequestInfo } from "./requestInfo";
import { ResponseHandler } from "./responseHandler";
export interface HttpCore {
    sendAsync<ModelType>(requestInfo: RequestInfo, responseHandler: ResponseHandler | undefined): Promise<ModelType>;
}