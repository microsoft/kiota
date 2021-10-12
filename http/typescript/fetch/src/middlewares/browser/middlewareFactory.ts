/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module MiddlewareFactory
 */

import { defaultFetchHandler} from "./defaultFetchHandler";
import { Middleware } from "../middleware";
import { RetryHandlerOptions } from "../options/retryHandlerOptions";
import { RetryHandler } from "../retryHandler";
import { FetchRequestInfo, FetchRequestInit } from "../../utils/fetchDefinitions";


/**
 * @class
 * Class containing function(s) related to the middleware pipelines.
 */
export class MiddlewareFactory {
	/**
	 * @public
	 * @static
	 * Returns the default middleware chain an array with the  middleware handlers
	 * @param {AuthenticationProvider} authProvider - The authentication provider instance
	 * @returns an array of the middleware handlers of the default middleware chain
	 */
	public static getDefaultMiddlewareChain(): Middleware[] {
		const middleware: Middleware[] = [];
		const retryHandler = new RetryHandler(new RetryHandlerOptions());
		middleware.push(retryHandler);
		middleware.push(new defaultFetchHandler());

		return middleware;
	}
}
