/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module DefaultFetchHandler
 */

import { FetchResponse } from "../../utils/fetchDefinitions";
import { Middleware } from "../middleware";
import { MiddlewareContext } from "../middlewareContext";

/**
 * @class
 * @implements Middleware
 * Class for DefaultFetchHandler
 */
export class DefaultFetchHandler implements Middleware {
	/**
	 * @private
	 * The next middleware in the middleware chain
	 */
	next: Middleware;

	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The request context object
	 * @returns A promise that resolves to nothing
	 */
	public async execute(context: MiddlewareContext): Promise<FetchResponse> {
		return (await fetch(context.request, context.options as RequestInit)) as FetchResponse;
	}
}
