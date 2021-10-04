import { MiddlewareControl } from "./MiddlewareControl";

/**
 * @interface
 * @property {RequestInfo} request - The request url string or the Request instance
 * @property {RequestInit} [options] - The options for the request
 * @property {Response} [response] - The response content
 * @property {MiddlewareControl} [middlewareControl] - The options for the middleware chain
 * @property {Set<string>}[customHosts] - A set of custom host names. Should contain hostnames only.
 *
 */

export interface MiddlewareContext {
    request:RequestInfo,
    response?: Response,
    options?:RequestInit,
    middlewareControl?: MiddlewareControl
}