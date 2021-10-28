import { CustomFetchHandler } from "./middlewares/customFetchHandler";
import { DefaultFetchHandler } from "./middlewares/defaultFetchHandler";
import { Middleware } from "./middlewares/middleware";
import { MiddlewareContext } from "./middlewares/middlewareContext";
import { MiddlewareFactory } from "./middlewares/middlewareFactory";
import { FetchRequestInfo, FetchRequestInit, FetchResponse } from "./utils/fetchDefinitions";

export class HttpClient {
	private middleware: Middleware;
	/**
	 * @public
	 * @constructor
	 * Creates an instance of a HttpClient which contains the middlewares and fetch implementation for request execution.
	 * @param {...Middleware} middleware - The first middleware of the middleware chain or a sequence of all the Middleware handlers
	 * If middlewares param is undefined, the httpClient instance will use the default array of middlewares.
	 * Set middlewares to `null` if you do not wish to use middlewares.
	 * If custom fetch is undefined, the httpClient instance uses the `DefaultFetchHandler`
	 * @param {(request: FetchRequestInfo, init?: FetchRequestInit) => Promise < FetchResponse >} custom fetch function - a Fetch API implementation
	 *
	 */
	public constructor(private customFetch?: (request: FetchRequestInfo, init?: FetchRequestInit) => Promise<FetchResponse>, ...middlewares: Middleware[]) {
		// Use default middleware chain if middlewares and custom fetch function are  undefined
		if (!middlewares.length) {
			if (this.customFetch) {
				this.setMiddleware(...MiddlewareFactory.getDefaultMiddlewareChain(customFetch));
			} else {
				this.setMiddleware(...MiddlewareFactory.getDefaultMiddlewareChain());
			}
		} else {
			if (middlewares[0] === null) {
				if (!customFetch) {
					this.setMiddleware(new DefaultFetchHandler());
				}
				return;
			} else {
				if (this.customFetch) {
					this.setMiddleware(...middlewares, new CustomFetchHandler(customFetch));
				} else {
					this.setMiddleware(...middlewares);
				}
			}
		}
	}

	/**
	 * @private
	 * Processes the middleware parameter passed to set this.middleware property
	 * The calling function should validate if middleware is not undefined or not empty.
	 * @param {...Middleware} middleware - The middleware passed
	 * @returns Nothing
	 */
	private setMiddleware(...middleware: Middleware[]): void {
		if (middleware.length > 1) {
			this.parseMiddleWareArray(middleware);
		} else {
			this.middleware = middleware[0];
		}
	}

	/**
	 * @private
	 * Processes the middleware array to construct the chain
	 * and sets this.middleware property to the first middlware handler of the array
	 * The calling function should validate if middleware is not undefined or not empty
	 * @param {Middleware[]} middlewareArray - The array of middleware handlers
	 * @returns Nothing
	 */
	private parseMiddleWareArray(middlewareArray: Middleware[]) {
		middlewareArray.forEach((element, index) => {
			if (index < middlewareArray.length - 1) {
				element.next = middlewareArray[index + 1];
			}
		});
		this.middleware = middlewareArray[0];
	}

	/**
	 * Executes a request and returns a promise resolving the response.
	 * @param url the request url.
	 * @param options request options.
	 * @returns the promise resolving the response.
	 */
	public async executeFetch(context: MiddlewareContext): Promise<FetchResponse> {
		if (this.customFetch && !this.middleware) {
			return this.customFetch(context.requestUrl, context.requestInformationOptions);
		}

		if (this.middleware) {
			return await this.middleware.execute(context);
		} else {
			throw new Error("Please provide middlewares or a custom fetch function to execute the request");
		}
	}
}
