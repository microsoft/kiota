/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module RedirectHandler
 */

import { Context } from "../IContext";
import { RequestMethod } from "../RequestMethod";
import { Middleware } from "./IMiddleware";
import { MiddlewareControl } from "./MiddlewareControl";
import { cloneRequestWithNewUrl } from "./MiddlewareUtil";
import { RedirectHandlerOptions } from "./options/RedirectHandlerOptions";
import { FeatureUsageFlag, TelemetryHandlerOptions } from "./options/TelemetryHandlerOptions";

/**
 * @class
 * Class
 * @implements Middleware
 * Class representing RedirectHandler
 */
export class RedirectHandler implements Middleware {
	/**
	 * @private
	 * @static
	 * A member holding the array of redirect status codes
	 */
	private static REDIRECT_STATUS_CODES: number[] = [
		301, // Moved Permanently
		302, // Found
		303, // See Other
		307, // Temporary Permanently
		308, // Moved Permanently
	];

	/**
	 * @private
	 * @static
	 * A member holding SeeOther status code
	 */
	private static STATUS_CODE_SEE_OTHER = 303;

	/**
	 * @private
	 * @static
	 * A member holding the name of the location header
	 */
	private static LOCATION_HEADER = "Location";

	/**
	 * @private
	 * @static
	 * A member representing the authorization header name
	 */
	private static AUTHORIZATION_HEADER = "Authorization";

	/**
	 * @private
	 * @static
	 * A member holding the manual redirect value
	 */
	private static MANUAL_REDIRECT: RequestRedirect = "manual";

	/**
	 * @private
	 * A member holding options to customize the handler behavior
	 */
	private options: RedirectHandlerOptions;

	/**
	 * @private
	 * A member to hold next middleware in the middleware chain
	 */
	private nextMiddleware: Middleware;

	/**
	 * @public
	 * @constructor
	 * To create an instance of RedirectHandler
	 * @param {RedirectHandlerOptions} [options = new RedirectHandlerOptions()] - The redirect handler options instance
	 * @returns An instance of RedirectHandler
	 */

	public constructor(options: RedirectHandlerOptions = new RedirectHandlerOptions()) {
		this.options = options;
	}

	/**
	 * @private
	 * To check whether the response has the redirect status code or not
	 * @param {Response} response - The response object
	 * @returns A boolean representing whether the response contains the redirect status code or not
	 */
	private isRedirect(response: Response): boolean {
		return RedirectHandler.REDIRECT_STATUS_CODES.indexOf(response.status) !== -1;
	}

	/**
	 * @private
	 * To check whether the response has location header or not
	 * @param {Response} response - The response object
	 * @returns A boolean representing the whether the response has location header or not
	 */
	private hasLocationHeader(response: Response): boolean {
		return response.headers.has(RedirectHandler.LOCATION_HEADER);
	}

	/**
	 * @private
	 * To get the redirect url from location header in response object
	 * @param {Response} response - The response object
	 * @returns A redirect url from location header
	 */
	private getLocationHeader(response: Response): string {
		return response.headers.get(RedirectHandler.LOCATION_HEADER);
	}

	/**
	 * @private
	 * To check whether the given url is a relative url or not
	 * @param {string} url - The url string value
	 * @returns A boolean representing whether the given url is a relative url or not
	 */
	private isRelativeURL(url: string): boolean {
		return url.indexOf("://") === -1;
	}

	/**
	 * @private
	 * To check whether the authorization header in the request should be dropped for consequent redirected requests
	 * @param {string} requestUrl - The request url value
	 * @param {string} redirectUrl - The redirect url value
	 * @returns A boolean representing whether the authorization header in the request should be dropped for consequent redirected requests
	 */
	private shouldDropAuthorizationHeader(requestUrl: string, redirectUrl: string): boolean {
		const schemeHostRegex = /^[A-Za-z].+?:\/\/.+?(?=\/|$)/;
		const requestMatches: string[] = schemeHostRegex.exec(requestUrl);
		let requestAuthority: string;
		let redirectAuthority: string;
		if (requestMatches !== null) {
			requestAuthority = requestMatches[0];
		}
		const redirectMatches: string[] = schemeHostRegex.exec(redirectUrl);
		if (redirectMatches !== null) {
			redirectAuthority = redirectMatches[0];
		}
		return typeof requestAuthority !== "undefined" && typeof redirectAuthority !== "undefined" && requestAuthority !== redirectAuthority;
	}

	/**
	 * @private
	 * @async
	 * To update a request url with the redirect url
	 * @param {string} redirectUrl - The redirect url value
	 * @param {Context} context - The context object value
	 * @returns Nothing
	 */
	private async updateRequestUrl(redirectUrl: string, context: Context): Promise<void> {
		context.request = typeof context.request === "string" ? redirectUrl : await cloneRequestWithNewUrl(redirectUrl, context.request as Request);
	}

	/**
	 * @private
	 * To get the options for execution of the middleware
	 * @param {Context} context - The context object
	 * @returns A options for middleware execution
	 */
	private getOptions(context: Context): RedirectHandlerOptions {
		let options: RedirectHandlerOptions;
		if (context.middlewareControl instanceof MiddlewareControl) {
			options = context.middlewareControl.getMiddlewareOptions(RedirectHandlerOptions) as RedirectHandlerOptions;
		}
		if (typeof options === "undefined") {
			options = Object.assign(new RedirectHandlerOptions(), this.options);
		}
		return options;
	}

	/**
	 * @private
	 * @async
	 * To execute the next middleware and to handle in case of redirect response returned by the server
	 * @param {Context} context - The context object
	 * @param {number} redirectCount - The redirect count value
	 * @param {RedirectHandlerOptions} options - The redirect handler options instance
	 * @returns A promise that resolves to nothing
	 */
	private async executeWithRedirect(context: Context, redirectCount: number, options: RedirectHandlerOptions): Promise<void> {
		await this.nextMiddleware.execute(context);
		const response = context.response;
		if (redirectCount < options.maxRedirects && this.isRedirect(response) && this.hasLocationHeader(response) && options.shouldRedirect(response)) {
			++redirectCount;
			if (response.status === RedirectHandler.STATUS_CODE_SEE_OTHER) {
				context.options.method = RequestMethod.GET;
				delete context.options.body;
			} else {
				const redirectUrl: string = this.getLocationHeader(response);
				if (!this.isRelativeURL(redirectUrl) && this.shouldDropAuthorizationHeader(response.url, redirectUrl)) {
					delete context.options.headers[RedirectHandler.AUTHORIZATION_HEADER];
				}
				await this.updateRequestUrl(redirectUrl, context);
			}
			await this.executeWithRedirect(context, redirectCount, options);
		} else {
			return;
		}
	}

	/**
	 * @public
	 * @async
	 * To execute the current middleware
	 * @param {Context} context - The context object of the request
	 * @returns A Promise that resolves to nothing
	 */
	public async execute(context: Context): Promise<void> {
		const redirectCount = 0;
		const options = this.getOptions(context);
		context.options.redirect = RedirectHandler.MANUAL_REDIRECT;
		TelemetryHandlerOptions.updateFeatureUsageFlag(context, FeatureUsageFlag.REDIRECT_HANDLER_ENABLED);
		return await this.executeWithRedirect(context, redirectCount, options);
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
