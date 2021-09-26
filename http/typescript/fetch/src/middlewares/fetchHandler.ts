/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module FetchHandler
 */

import fetch from "cross-fetch";
import { MiddlewareContext } from "./middlewareContext";
import { Middleware } from "./middleware";

/**
 * @class
 * @implements Middleware
 * Class for FetchHandler
 */
export class FetchHandler implements Middleware {

    /**
	 * @private
	 * The next middleware in the middleware chain
	 */
	next: Middleware;

    constructor(private customFetch?: (input: RequestInfo, init:RequestInit) => Promise<Response>) {};

    /**
     * @public
     * @async
     * To execute the current middleware
     * @param {Context} context - The request context object
     * @returns A promise that resolves to nothing
     */
    public async execute(context: MiddlewareContext): Promise<void> {
        if (this.customFetch) {
            context.response = await this.customFetch(context.request, context.options);
        }
        else {
            context.response = await fetch(context.request, context.options);
        } 
        return;
    }
}
