/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

 import { MiddlewareOption } from "@microsoft/kiota-abstractions";
import { FetchOptions } from "./IFetchOptions";
 import { MiddlewareControl } from "./middleware/MiddlewareControl";
 
 /**
  * @interface
  * @property {RequestInfo} request - The request url string or the Request instance
  * @property {FetchOptions} [options] - The options for the request
  * @property {Response} [response] - The response content
  * @property {MiddlewareControl} [middlewareControl] - The options for the middleware chain
  * @property {Set<string>}[customHosts] - A set of custom host names. Should contain hostnames only.
  *
  */
 
 export interface MiddlewareContext {
 }
 