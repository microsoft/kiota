import { ResponseHandler } from "./responseHandler";

export class NativeResponseHandler implements ResponseHandler {
    public value?: any;
    public handleResponseAsync<NativeResponseType, ModelType>(response: NativeResponseType): Promise<ModelType> {
        this.value = response;
        return Promise.resolve<ModelType>(undefined as any);
    }
}