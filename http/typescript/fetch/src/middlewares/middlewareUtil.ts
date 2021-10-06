/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { FetchRequest, FetchRequestInfo, FetchRequestInit, FetchHeaders } from "../utils/fetchDefinitions";

/**
 * @module MiddlewareUtil
 */

/**
 * @constant
 * To get the request header from the request
 * @param {RequestInfo} request - The request object or the url string
 * @param {RequestInit|undefined} options - The request options object
 * @param {string} key - The header key string
 * @returns A header value for the given key from the request
 */
export const getRequestHeader = (request: FetchRequestInfo, options: FetchRequestInit | undefined, key: string): string | null => {
	let value: string = null;

	// check for request object
	if (typeof options !== "undefined" && options.headers !== undefined) {
		// if (typeof Headers !== "undefined" && options.headers instanceof Headers) {
		// 	value = (options.headers[key];
		//} else 
		if (options.headers instanceof Array) {
			const headers = options.headers as string[][];
			for (let i = 0, l = headers.length; i < l; i++) {
				if (headers[i][0] === key) {
					value = headers[i][1];
					break;
				}
			}
		} else if (options.headers[key] !== undefined) {
			value = options.headers[key];
		}
	}
	return value;
};

/**
 * @constant
 * To set the header value to the given request
 * @param {RequestInfo} request - The request object or the url string
 * @param {RequestInit|undefined} options - The request options object
 * @param {string} key - The header key string
 * @param {string } value - The header value string
 * @returns Nothing
 */
export const setRequestHeader = (request: FetchRequestInfo, options: FetchRequestInit | undefined, key: string, value: string): void => {
	if (typeof options !== "undefined") {
		// if (options.headers === undefined) {
		// 	options.headers = new Headers({
		// 		[key]: value,
		// 	});
		// } else {
		// 	if (typeof FetchHeaders !== "undefined" && options.headers instanceof Headers) {
		// 		(options.headers as Headers).set(key, value);
		// 	} else 
			if (options.headers instanceof Array) {
				let i = 0;
				const l = options.headers.length;
				for (; i < l; i++) {
					const header = options.headers[i];
					if (header[0] === key) {
						header[1] = value;
						break;
					}
				}
				if (i === l) {
					(options.headers as string[][]).push([key, value]);
				}
			} else {
				Object.assign(options.headers, { [key]: value });
			}
		}
	//}
};

/**
 * @constant
 * To append the header value to the given request
 * @param {RequestInfo} request - The request object or the url string
 * @param {RequestInit|undefined} options - The request options object
 * @param {string} key - The header key string
 * @param {string } value - The header value string
 * @returns Nothing
 */
export const appendRequestHeader = (request: FetchRequestInfo, options: FetchRequestInit | undefined, key: string, value: string): void => {
	if (typeof options !== "undefined") {
		if (options.headers instanceof Array) {
			(options.headers as string[][]).push([key, value]);
		} else if (options.headers === undefined) {
			options.headers = { [key]: value };
		} else if (options.headers[key] === undefined) {
			options.headers[key] = value;
		} else {
			options.headers[key] += `, ${value}`;
		}
	}
//}
};

/**
 * @constant
 * To clone the request with the new url
 * @param {string} url - The new url string
 * @param {Request} request - The request object
 * @returns A promise that resolves to request object
 */
export const cloneRequestWithNewUrl = async (newUrl: string, request: FetchRequest): Promise<FetchRequest> => {
	const body = request.headers.get("Content-Type") ? await request.blob() : await Promise.resolve(undefined);
	const { method, headers, referrer, referrerPolicy, mode, credentials, cache, redirect, integrity, keepalive, signal } = request;
	// foreach(){

	// }
	const s: FetchRequest = {
		url: newUrl, method, headers, body, referrer, referrerPolicy, mode, credentials, cache, redirect, integrity, keepalive, signal
	};
	return s;
}
