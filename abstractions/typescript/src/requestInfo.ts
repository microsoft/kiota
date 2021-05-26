import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';
import { Parsable, SerializationWriterFactory } from "./serialization";

export class RequestInfo {
    public URI?: string;
    public httpMethod?: HttpMethod;
    public content?: ReadableStream;
    public queryParameters: Map<string, object> = new Map<string, object>();
    public headers: Map<string, string> = new Map<string, string>();
    private static binaryContentType = "application/octet-stream";
    private static contentTypeHeader = "Content-Type";
    public setContentFromParsable = <T extends Parsable<T>>(value?: T | undefined, serializerFactory?: SerializationWriterFactory | undefined, contentType?: string | undefined): void => {
        if(!serializerFactory) throw new Error("serializerFactory cannot be undefined");
        if(!contentType) throw new Error("contentType cannot be undefined");

        const writer = serializerFactory.getSerializationWriter(contentType);
        this.headers.set(RequestInfo.contentTypeHeader, contentType);
        writer.writeObjectValue(undefined, value);
        this.content = writer.getSerializedContent();
    }
    public setStreamContent = (value: ReadableStream): void => {
        this.headers.set(RequestInfo.contentTypeHeader, RequestInfo.binaryContentType);
        this.content = value;
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