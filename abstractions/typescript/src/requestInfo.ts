import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';
import { Parsable } from "./serialization";
import { HttpCore } from "./httpCore";

/** This class represents an abstract HTTP request. */
export class RequestInfo {
    /** The URI of the request. */
    public URI?: string;
    /** The HTTP method for the request */
    public httpMethod?: HttpMethod;
    /** The Request Body. */
    public content?: ReadableStream;
    /** The Query Parameters of the request. */
    public queryParameters: Map<string, object> = new Map<string, object>(); //TODO: case insensitive
    /** The Request Headers. */
    public headers: Map<string, string> = new Map<string, string>(); //TODO: case insensitive
    private static binaryContentType = "application/octet-stream";
    private static contentTypeHeader = "Content-Type";
    /**
     * Sets the request body from a model with the specified content type.
     * @param value the model.
     * @param contentType the content type.
     * @param httpCore The core service to get the serialization writer from.
     * @typeParam T the model type.
     */
    public setContentFromParsable = <T extends Parsable>(value?: T | undefined, httpCore?: HttpCore | undefined, contentType?: string | undefined): void => {
        if(!httpCore) throw new Error("httpCore cannot be undefined");
        if(!contentType) throw new Error("contentType cannot be undefined");

        const writer = httpCore.getSerializationWriterFactory().getSerializationWriter(contentType);
        this.headers.set(RequestInfo.contentTypeHeader, contentType);
        writer.writeObjectValue(undefined, value);
        this.content = writer.getSerializedContent();
    }
    /**
     * Sets the request body to be a binary stream.
     * @param value the binary stream
     */
    public setStreamContent = (value: ReadableStream): void => {
        this.headers.set(RequestInfo.contentTypeHeader, RequestInfo.binaryContentType);
        this.content = value;
    }
    /**
     * Sets the request headers from a raw object.
     * @param headers the headers.
     */
    public setHeadersFromRawObject = (h: object) : void => {
        Object.entries(h).forEach(([k, v]) => {
            this.headers.set(k, v as string);
        });
    }
    /**
     * Sets the query string parameters from a raw object.
     * @param parameters the parameters.
     */
    public setQueryStringParametersFromRawObject = (q: object): void => {
        Object.entries(q).forEach(([k, v]) => {
            this.headers.set(k, v as string);
        });
    }
}