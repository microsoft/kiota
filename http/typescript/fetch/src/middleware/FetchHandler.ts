/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module FetchHandler
 */

import { MiddlewareContext } from "../middlewareContext";
import { Middleware } from "./IMiddleware";

/**
 * @class
 * @implements Middleware
 * Class for HTTPMessageHandler
 */
export class FetchHandler implements Middleware {
	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The request context object
	 * @returns A promise that resolves to nothing
	 */
	public async execute(context: MiddlewareContext): Promise<void> {
	}
}
