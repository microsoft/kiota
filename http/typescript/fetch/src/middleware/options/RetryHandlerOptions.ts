/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module RetryHandlerOptions
 */

import { FetchOptions } from "../../IFetchOptions";
import { MiddlewareOptions } from "./IMiddlewareOptions";

/**
 * @type
 * A type declaration for shouldRetry callback
 */
export type ShouldRetry = (delay: number, attempt: number, request: RequestInfo, options: FetchOptions | undefined, response: Response) => boolean;

/**
 * @class
 * @implements MiddlewareOptions
 * Class for RetryHandlerOptions
 */

export class RetryHandlerOptions implements MiddlewareOptions {
	/**
	 * @private
	 * @static
	 * A member holding default delay value in seconds
	 */
	private static DEFAULT_DELAY = 3;

	/**
	 * @private
	 * @static
	 * A member holding default maxRetries value
	 */
	private static DEFAULT_MAX_RETRIES = 3;

	/**
	 * @private
	 * @static
	 * A member holding maximum delay value in seconds
	 */
	private static MAX_DELAY = 180;

	/**
	 * @private
	 * @static
	 * A member holding maximum maxRetries value
	 */
	private static MAX_MAX_RETRIES = 10;

	/**
	 * @public
	 * A member holding delay value in seconds
	 */
	public delay: number;

	/**
	 * @public
	 * A member holding maxRetries value
	 */
	public maxRetries: number;

	/**
	 * @public
	 * A member holding shouldRetry callback
	 */
	public shouldRetry: ShouldRetry;

	/**
	 * @private
	 * A member holding default shouldRetry callback
	 */
	private static defaultShouldRetry: ShouldRetry = () => true;

	/**
	 * @public
	 * @constructor
	 * To create an instance of RetryHandlerOptions
	 * @param {number} [delay = RetryHandlerOptions.DEFAULT_DELAY] - The delay value in seconds
	 * @param {number} [maxRetries = RetryHandlerOptions.DEFAULT_MAX_RETRIES] - The maxRetries value
	 * @param {ShouldRetry} [shouldRetry = RetryHandlerOptions.DEFAULT_SHOULD_RETRY] - The shouldRetry callback function
	 * @returns An instance of RetryHandlerOptions
	 */
	public constructor(delay: number = RetryHandlerOptions.DEFAULT_DELAY, maxRetries: number = RetryHandlerOptions.DEFAULT_MAX_RETRIES, shouldRetry: ShouldRetry = RetryHandlerOptions.defaultShouldRetry) {
		if (delay > RetryHandlerOptions.MAX_DELAY && maxRetries > RetryHandlerOptions.MAX_MAX_RETRIES) {
			const error = new Error(`Delay and MaxRetries should not be more than ${RetryHandlerOptions.MAX_DELAY} and ${RetryHandlerOptions.MAX_MAX_RETRIES}`);
			error.name = "MaxLimitExceeded";
			throw error;
		} else if (delay > RetryHandlerOptions.MAX_DELAY) {
			const error = new Error(`Delay should not be more than ${RetryHandlerOptions.MAX_DELAY}`);
			error.name = "MaxLimitExceeded";
			throw error;
		} else if (maxRetries > RetryHandlerOptions.MAX_MAX_RETRIES) {
			const error = new Error(`MaxRetries should not be more than ${RetryHandlerOptions.MAX_MAX_RETRIES}`);
			error.name = "MaxLimitExceeded";
			throw error;
		} else if (delay < 0 && maxRetries < 0) {
			const error = new Error(`Delay and MaxRetries should not be negative`);
			error.name = "MinExpectationNotMet";
			throw error;
		} else if (delay < 0) {
			const error = new Error(`Delay should not be negative`);
			error.name = "MinExpectationNotMet";
			throw error;
		} else if (maxRetries < 0) {
			const error = new Error(`MaxRetries should not be negative`);
			error.name = "MinExpectationNotMet";
			throw error;
		}
		this.delay = Math.min(delay, RetryHandlerOptions.MAX_DELAY);
		this.maxRetries = Math.min(maxRetries, RetryHandlerOptions.MAX_MAX_RETRIES);
		this.shouldRetry = shouldRetry;
	}

	/**
	 * @public
	 * To get the maximum delay
	 * @returns A maximum delay
	 */
	public getMaxDelay(): number {
		return RetryHandlerOptions.MAX_DELAY;
	}
}
