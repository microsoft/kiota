import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';

export interface RequestInfo {
    URI?: string;
    httpMethod?: HttpMethod;
    content?: ReadableStream;
    queryParameters?: Map<string, object>;
    headers?: Map<string, string>;
}