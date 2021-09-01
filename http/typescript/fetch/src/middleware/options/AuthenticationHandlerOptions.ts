/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module AuthenticationHandlerOptions
 */

import { AuthenticationProvider } from "../../IAuthenticationProvider";
import { AuthenticationProviderOptions } from "../../IAuthenticationProviderOptions";
import { MiddlewareOptions } from "./IMiddlewareOptions";

/**
 * @class
 * @implements MiddlewareOptions
 * Class representing AuthenticationHandlerOptions
 */
export class AuthenticationHandlerOptions implements MiddlewareOptions {
	/**
	 * @public
	 * A member holding an instance of an authentication provider
	 */
	public authenticationProvider: AuthenticationProvider;

	/**
	 * @public
	 * A member holding an instance of authentication provider options
	 */
	public authenticationProviderOptions: AuthenticationProviderOptions;

	/**
	 * @public
	 * @constructor
	 * To create an instance of AuthenticationHandlerOptions
	 * @param {AuthenticationProvider} [authenticationProvider] - The authentication provider instance
	 * @param {AuthenticationProviderOptions} [authenticationProviderOptions] - The authentication provider options instance
	 * @returns An instance of AuthenticationHandlerOptions
	 */
	public constructor(authenticationProvider?: AuthenticationProvider, authenticationProviderOptions?: AuthenticationProviderOptions) {
		this.authenticationProvider = authenticationProvider;
		this.authenticationProviderOptions = authenticationProviderOptions;
	}
}
