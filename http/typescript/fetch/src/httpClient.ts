import { customFetchHandler } from "./middlewares/customFetchHandler";
import { defaultFetchHandler } from "./middlewares/defaultFetchHandler";
import { Middleware } from "./middlewares/middleware";
import { MiddlewareContext } from "./middlewares/middlewareContext";
import { MiddlewareFactory } from "./middlewares/middlewareFactory";
import { FetchRequestInfo, FetchRequestInit, FetchResponse } from "./utils/fetchDefinitions";

/** Default fetch client with options and a middleware pipleline for requests execution. */
export class HttpClient {
	private middleware: Middleware;
	/**
	 * Instantiates a new HttpClient.
	 * @param middlewares middlewares to be used for requests execution.
	 * @param custom fetch function
	 */
	public constructor(private customFetch?: (request: FetchRequestInfo, init?: FetchRequestInit) => Promise<FetchResponse>, ...middlewares: Middleware[]) {
		// Use default middleware chain if middlewares and custom fetch function are not defined
		if (!middlewares.length) {
			if (this.customFetch) {
				this.setMiddleware(...MiddlewareFactory.getDefaultMiddlewareChain(customFetch));
			} else {
				this.setMiddleware(...MiddlewareFactory.getDefaultMiddlewareChain());
			}
		} else {
			if (middlewares[0] === null) {
				if (!customFetch) {
					this.setMiddleware(new defaultFetchHandler());
				}
				return;
			} else {
				if (this.customFetch) {
					this.setMiddleware(...middlewares, new customFetchHandler(customFetch));
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
			return this.customFetch(context.request, context.options);
		}

		if (this.middleware) {
			await this.middleware.execute(context);
			return context.response;
		} else {
			throw new Error("Please provide middlewares or a custom fetch function to execute the request");
		}
	}
}
