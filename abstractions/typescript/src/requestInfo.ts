import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';
import { Parsable, SerializationWriter } from "./serialization";

export class RequestInfo {
    public URI?: string;
    public httpMethod?: HttpMethod;
    public content?: ReadableStream;
    public queryParameters: Map<string, object> = new Map<string, object>();
    public headers: Map<string, string> = new Map<string, string>();
    private static jsonContentType = "application/json";
    public setJsonContentFromParsable<T extends Parsable<T>>(value?: T | undefined, serializerFactory?: ((mediaType: string) => SerializationWriter) | undefined): void {
        if(serializerFactory) {
            const writer = serializerFactory(RequestInfo.jsonContentType);
            this.headers.set("Content-Type", RequestInfo.jsonContentType);
            writer.writeObjectValue(undefined, value);
            this.content = writer.getSerializedContent();
        }
    }
    public setHeadersFromRawObject = (h: object) : void => {
        Object.entries(h).forEach(([k, v]) => {
            this.headers.set(k, v as string);
        });
    }
    public setQueryStringParametersFromRawObject = (q: object): void => {
        Object.entries(q).forEach(([k, v]) => {
            this.headers.set(k, v as string);
        });
    }
}