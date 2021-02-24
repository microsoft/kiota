import { HttpMethod } from "./httpMethod";

export interface RequestInfo {
    URI?: URL;
    httpMethod?: HttpMethod;
    content?: ReadableStream;
    queryParameters?: Map<string, object>;
    headers?: Map<string, string>;
}