/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module MiddlewareFactory
 */

import { AuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchHandler} from "../fetchHandler";
import { Middleware } from "./middleware";
import { RedirectHandlerOptions } from "./options/RedirectHandlerOptions";
import { RetryHandlerOptions } from "./options/RetryHandlerOptions";
import { RedirectHandler } from "./redirectHandler";
import { RetryHandler } from "./retryHandler";

/**
 * @private
 * To check whether the environment is node or not
 * @returns A boolean representing the environment is node or not
 */
const isNodeEnvironment = (): boolean => {
	return typeof process === "object" && typeof require === "function";
};

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
	public static getDefaultMiddlewareChain(authProvider: AuthenticationProvider): Middleware[] {
		const middleware: Middleware[] = [];
		// TODO :const authenticationHandler = new AuthenticationHandler(authProvider);
		const retryHandler = new RetryHandler(new RetryHandlerOptions());

		//middleware.push(authenticationHandler);
		middleware.push(retryHandler);
		if (isNodeEnvironment()) {
			const redirectHandler = new RedirectHandler(new RedirectHandlerOptions());
			middleware.push(redirectHandler);
		}
		middleware.push(new FetchHandler());

		return middleware;
	}
}
