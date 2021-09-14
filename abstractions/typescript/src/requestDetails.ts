import { HttpMethod } from "./httpMethod";
import { ReadableStream } from 'web-streams-polyfill/es2018';
import { MiddlewareOption } from "./middlewareOption";

export interface RequestDetails{
     /** The HTTP method for the request */
    httpMethod?: HttpMethod;
     /** The Request Body. */
    content?: ReadableStream;
     /** The Query Parameters of the request. */
    queryParameters: Map<string, string | number | boolean | undefined> = new Map<string, string | number | boolean | undefined>(); //TODO: case insensitive
     /** The Request Headers. */
    headers: HeadersInit //TODO: case insensitive
}
