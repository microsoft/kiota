/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module MiddlewareUtil
 */

/**
 * @constant
 * To get the request header from the request
 * @param {RequestInfo} request - The request object or the url string
 * @param {FetchOptions|undefined} options - The request options object
 * @param {string} key - The header key string
 * @returns A header value for the given key from the request
 */
export const getRequestHeader = (options: RequestInit | undefined, key: string): string | null => {
	if (typeof options !== "undefined" && options.headers !== undefined) {
		return options.headers[key];
	}
	return undefined;
};

/**
 * @constant
 * To set the header value to the given request
 * @param {RequestInfo} request - The request object or the url string
 * @param {FetchOptions|undefined} options - The request options object
 * @param {string} key - The header key string
 * @param {string } value - The header value string
 * @returns Nothing
 */
export const setRequestHeader = (options: RequestInit | undefined, key: string, value: string): void => {
	if (typeof options !== "undefined") {
		if (!options.headers) {
			options.headers = {};
		}
		options.headers[key] = value;
	}
};

/**
 * @constant
 * To append the header value to the given request
 * @param {RequestInfo} request - The request object or the url string
 * @param {FetchOptions|undefined} options - The request options object
 * @param {string} key - The header key string
 * @param {string } value - The header value string
 * @returns Nothing
 */
export const appendRequestHeader = (options: RequestInit | undefined, key: string, value: string): void => {
	if (typeof options !== "undefined") {
		if (!options.headers) {
			options.headers = {};
		}
		if (options.headers[key] === undefined) {
			options.headers[key] = value;
		} else {
			options.headers[key] += `, ${value}`;
		}
	}
};
