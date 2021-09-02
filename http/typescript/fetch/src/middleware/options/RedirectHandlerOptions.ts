/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module RedirectHandlerOptions
 */

import { MiddlewareOptions } from "./IMiddlewareOptions";

/**
 * @type
 * A type declaration for shouldRetry callback
 */
export type ShouldRedirect = (response: Response) => boolean;

/**
 * @class
 * @implements MiddlewareOptions
 * A class representing RedirectHandlerOptions
 */
export class RedirectHandlerOptions implements MiddlewareOptions {   
}

