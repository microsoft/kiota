import { HttpMethod } from "./httpMethod";

export default class RequestInfo {
    public URI?: URL;
    public httpMethod?: HttpMethod;
    public content?: ReadableStream;
    public queryParameters: Map<string, object> = new Map<string, object>();
    public headers: Map<string, string> = new Map<string, string>();
}