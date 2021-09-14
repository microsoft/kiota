import { Middleware } from "./middlewares/middleware";
import { Context } from "./Context";

/** Default fetch client with options and a middleware pipleline for requests execution. */
export class HttpClient {
    private middleware: Middleware
    /**
     * Instantiates a new HttpClient.
     * @param middlewares middlewares to be used for requests execution.
     * @param defaultRequestSettings default request settings to be used for requests execution.
     */
    public constructor(private customFetch?: () => Promise<Response>, ...middlewares: Middleware[]) {
        if (middlewares) {
            middlewares.forEach((middleware, idx) => {
                if (idx < middlewares.length)
                    this.middleware.next = middlewares[idx + 1];
            });
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
    public async fetch(context: Context): Promise<Response> {

        if (this.customFetch && !this.middleware) {
            return this.customFetch();
        }
        if (this.middleware) {
            await this.middleware.execute(context);

            return context.response;
        }
        else
            throw new Error("No middlewares found");
    }
}