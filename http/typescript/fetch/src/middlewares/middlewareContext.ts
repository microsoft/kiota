/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { FetchRequestInfo, FetchRequestInit, FetchResponse } from "../utils/fetchDefinitions";
import { MiddlewareControl } from "./middlewareControl";

/**
 * @interface
 * @property {RequestInfo} request - The request url string or the Request instance
 * @property {RequestInit} [options] - The options for the request
 * @property {Response} [response] - The response content
 *
 */

export interface MiddlewareContext {
	request: FetchRequestInfo;
	response?: FetchResponse;
	options?: FetchRequestInit;
	middlewareControl?: MiddlewareControl; // this can get updated depending on the use of request options
}
