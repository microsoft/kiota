/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module AuthenticationHandler
 */

import { AuthenticationProvider } from "@microsoft/kiota-abstractions";
import { MiddlewareContext } from "../middlewareContext";
import { Middleware } from "./IMiddleware";

/**
 * @class
 * @implements Middleware
 * Class representing AuthenticationHandler
 */
export class AuthenticationHandler implements Middleware {

	/**
	 * @private
	 * A member to hold an AuthenticationProvider instance
	 */
	private authenticationProvider: AuthenticationProvider;

	/**
	 * @private
	 * A member to hold next middleware in the middleware chain
	 */
	private nextMiddleware: Middleware;

	/**
	 * @public
	 * @constructor
	 * Creates an instance of AuthenticationHandler
	 * @param {AuthenticationProvider} authenticationProvider - The authentication provider for the authentication handler
	 */
	public constructor(authenticationProvider: AuthenticationProvider) {
		this.authenticationProvider = authenticationProvider;
	}

	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The context object of the request
	 * @returns A Promise that resolves to nothing
	 */
	public async execute(context: MiddlewareContext): Promise<void> {
		return await this.nextMiddleware.execute(context);
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
