/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module RetryHandler
 */

import { MiddlewareContext } from "../middlewareContext";
import { Middleware } from "./IMiddleware";

/**
 * @class
 * @implements Middleware
 * Class for RetryHandler
 */
export class RetryHandler implements Middleware {
	private nextMiddleware: Middleware;

	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The context object of the request
	 * @returns A Promise that resolves to nothing
	 */
	public async execute(context: MiddlewareContext): Promise<void> {
	}

	/**
	 * @public
	 * To set the next middleware in the chain
	 * @param {Middleware} next - The middleware instance
	 * @returns Nothing
	 */
	public setNext(next: Middleware): void {
		this.nextMiddleware = next;
	}
}
