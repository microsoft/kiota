/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { FetchRequest, FetchRequestInfo, FetchRequestInit } from "../utils/fetchDefinitions";

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
export const getRequestHeader = (request: FetchRequestInfo, options: FetchRequestInit | undefined, key: string): string | null => {
	let value: string = null;
	console.log(" inside  get requestheader" + value);
	if (typeof request !== 'string') {
		console.log(" inside ! string" + value);
		value = (request as FetchRequest).headers.get(key);
	} else if (typeof options !== "undefined" && options.headers !== undefined) {
		console.log(options.headers);
		value = options.headers[key];
		console.log(" inside option =svalue" + value);
	}
	console.log("value" + value);
	return value;
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
export const setRequestHeader = (request: FetchRequestInfo, options: FetchRequestInit | undefined, key: string, value: string): void => {
	if (typeof request !== 'string') {
		(request as FetchRequest).headers.set(key, value);
	} else if (typeof options !== "undefined") {
		if(!options.headers){
			options.headers = {};
		}
		console.log(options.headers);
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
export const appendRequestHeader = (request: FetchRequestInfo, options: FetchRequestInit | undefined, key: string, value: string): void => {
	if (typeof request !== "string") {
		(request as FetchRequest).headers.append(key, value);
	} else if (typeof options !== "undefined") {
		if(!options.headers){
			options.headers = {};
		}
		if (options.headers[key] === undefined) {
			options.headers[key] = value;
		} else {
			options.headers[key] += `, ${value}`;
		}
	}

};

