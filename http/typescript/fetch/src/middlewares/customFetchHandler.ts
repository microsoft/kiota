/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module FetchHandler
 */

import { FetchRequestInfo, FetchRequestInit, FetchResponse } from "../utils/fetchDefinitions";
import { Middleware } from "./middleware";
import { MiddlewareContext } from "./middlewareContext";

/**
 * @class
 * @implements Middleware
 * Class for FetchHandler
 */

export class CustomFetchHandler implements Middleware {
	/**
	 * @private
	 * The next middleware in the middleware chain
	 */
	next: Middleware;

	constructor(private customFetch?: (input: FetchRequestInfo, init: FetchRequestInit) => Promise<FetchResponse>) {}

	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The request context object
	 * @returns A promise that resolves to nothing
	 */
	public async execute(context: MiddlewareContext): Promise<FetchResponse> {
		return await this.customFetch(context.requestUrl, context.fetchRequestInit);
	}
}
