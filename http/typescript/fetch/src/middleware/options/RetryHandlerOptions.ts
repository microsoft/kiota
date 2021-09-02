/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module RetryHandlerOptions
 */

import { FetchOptions } from "../../IFetchOptions";
import { MiddlewareOptions } from "./IMiddlewareOptions";

/**
 * @type
 * A type declaration for shouldRetry callback
 */
export type ShouldRetry = (delay: number, attempt: number, request: RequestInfo, options: FetchOptions | undefined, response: Response) => boolean;

/**
 * @class
 * @implements MiddlewareOptions
 * Class for RetryHandlerOptions
 */

export class RetryHandlerOptions implements MiddlewareOptions {
}
