import { ResponseHandler } from "./responseHandler";

/** Default response handler to access the native response object. */
export class NativeResponseHandler implements ResponseHandler {
  /** Native response object as returned by the core service */
  public value?: any;
  public handleResponseAsync<NativeResponseType, ModelType>(
    response: NativeResponseType
  ): Promise<ModelType> {
    this.value = response;
    return Promise.resolve<ModelType>(undefined as any);
  }
}
