import { HttpMethod } from "./httpMethod";

export default interface RequestInfo {
    URI?: URL;
    httpMethod?: HttpMethod;
    content?: ReadableStream;
    queryParameters?: Map<string, object>;
    headers?: Map<string, string>;
}