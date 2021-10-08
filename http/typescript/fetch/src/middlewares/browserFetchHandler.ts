/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module FetchHandler
 */

import { MiddlewareContext } from "./middlewareContext";
import { Middleware } from "./middleware";
import { FetchResponse, FetchRequestInfo, FetchRequestInit } from "../utils/fetchDefinitions";

/**
 * @class
 * @implements Middleware
 * Class for FetchHandler
 */

export class browserFetchHandler implements Middleware {

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
    public async execute(context: MiddlewareContext): Promise<void> {
        context.response = await fetch(context.request, context.options as RequestInit) as FetchResponse;
        return;
    }
}
