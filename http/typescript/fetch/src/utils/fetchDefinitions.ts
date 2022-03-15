/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

// fetch() will only be called using string requests along with options
export type FetchRequestInfo = string;

/**
 *  Use Record type to store request headers.
 *  Node and browser have different implementations of `Headers`.
 *  Record type is used to store headers in most http request and fetch libraries.
 */
export type FetchHeadersInit = Record<string, string>;

export type FetchHeaders = Headers & {
	append?(name: string, value: string): void;
	delete?(name: string): void;
	get?(name: string): string | null;
	has?(name: string): boolean;
	set?(name: string, value: string): void;
	forEach?(callbackfn: (value: string, key: string, parent: FetchHeaders) => void, thisArg?: any): void;
	[Symbol.iterator]?(): IterableIterator<[string, string]>;
	/**
	 * Returns an iterator allowing to go through all key/value pairs contained in this object.
	 */
	entries?(): IterableIterator<[string, string]>;
	/**
	 * Returns an iterator allowing to go through all keys of the key/value pairs contained in this object.
	 */
	keys?(): IterableIterator<string>;
	/**
	 * Returns an iterator allowing to go through all values of the key/value pairs contained in this object.
	 */
	values?(): IterableIterator<string>;

	/** Node-fetch extension */
	raw?(): Record<string, string[]>;
};

export type FetchResponse = Omit<Response, "headers"> & {
	headers: FetchHeaders;
};

export type FetchRequestInit = Omit<RequestInit, "body" | "headers" | "redirect"> & {
	/**
	 * Request's body
	 * Expected type in case of dom - ReadableStream | XMLHttpRequestBodyInit|null
	 * Expected type in case of node-fetch - | Blob | Buffer | URLSearchParams | NodeJS.ReadableStream | string|null
	 */
	body?: unknown;
	/**
	 * A Headers object, an object literal, or an array of two-item arrays to set request's headers.
	 */
	headers?: FetchHeadersInit;
	/**
	 * A string to set request's method.
	 */
	method?: string;
	/**
	 * A string indicating whether request follows redirects, results in an error upon encountering a redirect, or returns the redirect (in an opaque fashion). Sets request's redirect.
	 */
	redirect?: unknown;
};
