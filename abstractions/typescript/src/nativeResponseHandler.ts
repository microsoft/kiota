import { ResponseHandler } from "./responseHandler";
import { Parsable, ParsableFactory } from "./serialization";

/** Default response handler to access the native response object. */
export class NativeResponseHandler implements ResponseHandler {
  /** Native response object as returned by the core service */
  public value?: any;
  /** The error mappings for the response to use when deserializing failed responses bodies. Where an error code like 401 applies specifically to that status code, a class code like 4XX applies to all status codes within the range if an the specific error code is not present. */
  public errorMappings: Record<string, ParsableFactory<Parsable>> | undefined;
  public handleResponseAsync<NativeResponseType, ModelType>(
    response: NativeResponseType,
    errorMappings: Record<string, ParsableFactory<Parsable>> | undefined
  ): Promise<ModelType> {
    this.value = response;
    this.errorMappings = errorMappings;
    return Promise.resolve<ModelType>(undefined as any);
  }
}
