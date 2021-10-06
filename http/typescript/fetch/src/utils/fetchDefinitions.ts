
export type FetchRequestInfo = string | FetchRequest; // We only ever call fetch() on string urls.

interface FetchBody {
    readonly body?: unknown
    readonly bodyUsed?: boolean;
    arrayBuffer?(): unknown;
    blob?(): unknown;
    formData?(): unknown;
    json?(): unknown;
    text?(): unknown;
}

export interface FetchRequestInit extends RequestInit{
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
    headers?: unknown;
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

export interface FetchResponse extends Response,FetchBody{
        readonly headers: FetchHeaders;
        readonly ok: boolean;
        readonly redirected: boolean;
        readonly status: number;
        readonly statusText: string;
        readonly type: unknown;
        readonly url: string;
        clone(): Response;
}

export interface FetchHeaders extends Headers{
    append(name: string, value: string): void;
    delete(name: string): void;
    get(name: string): string | null;
    has(name: string): boolean;
    set(name: string, value: string): void;
    forEach(callbackfn: (value: string, key: string, parent: Headers) => void, thisArg?: any): void;
}

/** This Fetch API interface represents a resource request. */
export interface FetchRequest extends Request, FetchBody {
    /**
     * Returns the cache mode associated with request, which is a string indicating how the request will interact with the browser's cache when fetching.
     */
    readonly cache?: unknown;
    /**
     * Returns the credentials mode associated with request, which is a string indicating whether credentials will be sent with the request always, never, or only when sent to a same-origin URL.
     */
    readonly credentials?: unknown;
    /**
     * Returns the kind of resource requested by request, e.g., "document" or "script".
     */
    readonly destination?: unknown;
    /**
     * Returns a Headers object consisting of the headers associated with request. Note that headers added in the network layer by the user agent will not be accounted for in this object, e.g., the "Host" header.
     */
    readonly headers?: FetchHeaders;
    /**
     * Returns request's subresource integrity metadata, which is a cryptographic hash of the resource being fetched. Its value consists of multiple hashes separated by whitespace. [SRI]
     */
    readonly integrity?: string;
    /**
     * Returns a boolean indicating whether or not request can outlive the global in which it was created.
     */
    readonly keepalive?: boolean;
    /**
     * Returns request's HTTP method, which is "GET" by default.
     */
    readonly method?: string;
    /**
     * Returns the mode associated with request, which is a string indicating whether the request will use CORS, or will be restricted to same-origin URLs.
     */
    readonly mode?: unknown;
    /**
     * Returns the redirect mode associated with request, which is a string indicating how redirects for the request will be handled during fetching. A request will follow redirects by default.
     */
    readonly redirect?: unknown;
    /**
     * Returns the referrer of request. Its value can be a same-origin URL if explicitly set in init, the empty string to indicate no referrer, and "about:client" when defaulting to the global's default. This is used during fetching to determine the value of the `Referer` header of the request being made.
     */
    readonly referrer?: string;
    /**
     * Returns the referrer policy associated with request. This is used during fetching to compute the value of the request's referrer.
     */
    readonly referrerPolicy?: unknown;
    /**
     * Returns the signal associated with request, which is an AbortSignal object indicating whether or not request has been aborted, and its abort event handler.
     */
    readonly signal?: unknown;
    /**
     * Returns the URL of request as a string.
     */
    readonly url?: string;
    clone?(): FetchRequest;
}

