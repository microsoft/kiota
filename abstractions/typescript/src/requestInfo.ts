import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';
import { Parsable } from "./serialization";
import { HttpCore } from "./httpCore";
import { MiddlewareOption } from "./middlewareOption";

/** This class represents an abstract HTTP request. */
export class RequestInfo {
    /** The URI of the request. */
    public URI?: string;
    public setUri(currentPath: string, pathSegment: string, isRawUrl: boolean) : void {
        if(isRawUrl) {
            const questionMarkSplat = currentPath.split('?');
            const schemeHostAndPath = questionMarkSplat[0];
            this.URI = schemeHostAndPath;
            if(questionMarkSplat.length > 1) {
                const queryString = questionMarkSplat[1];
                queryString?.split('&').forEach(queryPair => {
                    const keyValue = queryPair.split('=');
                    if(keyValue.length > 1) {
                        const key = keyValue[0];
                        if(key) {
                            this.queryParameters.set(key, keyValue[1]);
                        }
                    }
                });
            }
        } else {
            this.URI = currentPath + pathSegment;
        }
    }
    /** The HTTP method for the request */
    public httpMethod?: HttpMethod;
    /** The Request Body. */
    public content?: ReadableStream;
    /** The Query Parameters of the request. */
    public queryParameters: Map<string, string | number | boolean | undefined> = new Map<string, string | number | boolean | undefined>(); //TODO: case insensitive
    /** The Request Headers. */
    public headers: Map<string, string> = new Map<string, string>(); //TODO: case insensitive
    private _middlewareOptions = new Map<string, MiddlewareOption>(); //TODO: case insensitive
    /** Gets the middleware options for the request. */
    public getMiddlewareOptions() { return this._middlewareOptions.values(); }
    public addMiddlewareOptions(...options: MiddlewareOption[]) {
        if(!options || options.length === 0) return;
        options.forEach(option => {
            this._middlewareOptions.set(option.getKey(), option);
        });
    }
    /** Removes the middleware options for the request. */
    public removeMiddlewareOptions(...options: MiddlewareOption[]) {
        if(!options || options.length === 0) return;
        options.forEach(option => {
            this._middlewareOptions.delete(option.getKey());
        });
    }
    private static binaryContentType = "application/octet-stream";
    private static contentTypeHeader = "Content-Type";
    /**
     * Sets the request body from a model with the specified content type.
     * @param values the models.
     * @param contentType the content type.
     * @param httpCore The core service to get the serialization writer from.
     * @typeParam T the model type.
     */
    public setContentFromParsable = <T extends Parsable>(httpCore?: HttpCore | undefined, contentType?: string | undefined, ...values: T[]): void => {
        if(!httpCore) throw new Error("httpCore cannot be undefined");
        if(!contentType) throw new Error("contentType cannot be undefined");
        if(!values || values.length === 0) throw new Error("values cannot be undefined or empty");

        const writer = httpCore.getSerializationWriterFactory().getSerializationWriter(contentType);
        this.headers.set(RequestInfo.contentTypeHeader, contentType);
        if(values.length === 1) 
            writer.writeObjectValue(undefined, values[0]);
        else
            writer.writeCollectionOfObjectValues(undefined, values);
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