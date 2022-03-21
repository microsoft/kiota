/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module FetchHandler
 */

import { Middleware } from "./middleware";

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
	next: Middleware | undefined;

	constructor(private customFetch: (input: string, init: RequestInit) => Promise<Response>) {}

	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The request context object
	 * @returns A promise that resolves to nothing
	 */
	public async execute(url: string, requestInit: RequestInit): Promise<Response> {
		return await this.customFetch(url, requestInit);
	}
}
