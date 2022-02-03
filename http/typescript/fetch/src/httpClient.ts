import { RequestOption } from "@microsoft/kiota-abstractions";
import { fetch } from "cross-fetch";

import { getDefaultMiddlewares, getDefaultRequestSettings } from "./kiotaClientFactory";
import { Middleware } from "./middleware";

/** Default fetch client with options and a middleware pipleline for requests execution. */
export class HttpClient {
	/**
	 * Instantiates a new HttpClient.
	 * @param middlewares middlewares to be used for requests execution.
	 * @param defaultRequestSettings default request settings to be used for requests execution.
	 */
	public constructor(private readonly middlewares: Middleware[] = getDefaultMiddlewares(), private readonly defaultRequestSettings: RequestInit = getDefaultRequestSettings()) {
		this.middlewares = [...this.middlewares, new FetchMiddleware()];
		this.middlewares.forEach((middleware, idx) => {
			if (idx < this.middlewares.length) middleware.next = this.middlewares[idx + 1];
		});
	}
	/**
	 * Executes a request and returns a promise resolving the response.
	 * @param url the request url.
	 * @param options request options.
	 * @returns the promise resolving the response.
	 */
	public fetch(url: string, options?: RequestInit, requestOptions?: RequestOption[]): Promise<Response> {
		const finalOptions = { ...this.defaultRequestSettings, ...options } as RequestInit;
		if (this.middlewares.length > 0 && this.middlewares[0]) return this.middlewares[0].execute(url, finalOptions, requestOptions);
		else throw new Error("No middlewares found");
	}
}
/** Default middleware executing a request. Internal use only. */
class FetchMiddleware implements Middleware {
	next: Middleware | undefined;
	public execute(url: string, req: RequestInit, _?: RequestOption[]): Promise<Response> {
		return fetch(url, req);
	}
}
