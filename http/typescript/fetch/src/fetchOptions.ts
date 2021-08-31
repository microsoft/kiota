/**
 * @interface {@link https://github.com/bitinn/node-fetch/#options}
 * Signature to define the fetch request options for node environment
 * @property {number} [follow] - node-fetch option: maximum redirect count. 0 to not follow redirect
 * @property {number} [compress] - node-fetch option: support gzip/deflate content encoding. false to disable
 * @property {number} [size] - node-fetch option: maximum response body size in bytes. 0 to disable
 * @property {any} [agent] - node-fetch option: HTTP(S).Agent instance, allows custom proxy, certificate, lookup, family etc.
 * @property {number} [highWaterMark] - node-fetch option: maximum number of bytes to store in the internal buffer before ceasing to read from the underlying resource.
 * @property {boolean} [insecureHTTPParser] - node-fetch option: use an insecure HTTP parser that accepts invalid HTTP headers when `true`.
 */
 export interface NodeFetchInit {
	follow?: number;
	compress?: boolean;
	size?: number;
	agent?: any;
	highWaterMark?: number;
	insecureHTTPParser?: boolean;
}

/**
 * @interface
 * Signature to define the fetch api options which includes both fetch standard options and also the extended node fetch options
 * @extends RequestInit @see {@link https://fetch.spec.whatwg.org/#requestinit}
 * @extends NodeFetchInit
 */
export interface FetchOptions extends RequestInit, NodeFetchInit {}
