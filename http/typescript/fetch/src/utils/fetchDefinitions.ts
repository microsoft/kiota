export type FetchRequestInfo = string; // We only ever call fetch() on string urls.

export type FetchHeadersInit = Record<string,string>;

interface FetchBody {
    readonly body: ReadableStream<Uint8Array> | null;
    readonly bodyUsed: boolean;
    arrayBuffer(): Promise<ArrayBuffer>;
    blob(): Promise<Blob>;
    formData(): Promise<FormData>;
    json(): Promise<any>;
    text(): Promise<string>;
}

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
}


export type FetchResponse = Response & FetchBody & {
        readonly headers: FetchHeaders;
        readonly ok: boolean;
        readonly redirected: boolean;
        readonly status: number;
        readonly statusText: string;
        readonly type: unknown;
        readonly url: string;
        clone(): Response;
}



export type FetchRequestInit = Omit<RequestInit, "body|headers"|"redirect"> &{
    /**
     * A BodyInit object or null to set request's body.
     */
    body?: unknown;
    /**
     * A string indicating how the request will interact with the browser's cache to set request's cache.
     */
    cache?: unknown;
    /**
     * A string indicating whether credentials will be sent with the request always, never, or only when sent to a same-origin URL. Sets request's credentials.
     */
    credentials?: unknown;
    /**
     * A Headers object, an object literal, or an array of two-item arrays to set request's headers.
     */
    headers?: FetchHeadersInit;
    /**
     * A cryptographic hash of the resource to be fetched by request. Sets request's integrity.
     */
    integrity?: string;
    /**
     * A boolean to set request's keepalive.
     */
    keepalive?: boolean;
    /**
     * A string to set request's method.
     */
    method?: string;
    /**
     * A string to indicate whether the request will use CORS, or will be restricted to same-origin URLs. Sets request's mode.
     */
    mode?: unknown;
    /**
     * A string indicating whether request follows redirects, results in an error upon encountering a redirect, or returns the redirect (in an opaque fashion). Sets request's redirect.
     */
    redirect?: unknown;
    /**
     * A string whose value is a same-origin URL, "about:client", or the empty string, to set request's referrer.
     */
    referrer?: string;
    /**
     * A referrer policy to set request's referrerPolicy.
     */
    referrerPolicy?: unknown;
    /**
     * An AbortSignal to set request's signal.
     */
    signal?: unknown | null;
    /**
     * Can only be null. Used to disassociate request from any Window.
     */
    window?: any;

    //Node-Fetch
    agent?: unknown;
	compress?: boolean;
	counter?: number;
	follow?: number;
	hostname?: string;
	port?: number;
	protocol?: string;
	size?: number;
	highWaterMark?: number;
	insecureHTTPParser?: boolean;
}