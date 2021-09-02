import { Middleware } from "./middleware/middleware";
import { MiddlewareContext } from "./middlewareContext";

/** Default fetch client with options and a middleware pipleline for requests execution. */
export class HttpClient {
    /**
     * Instantiates a new HttpClient.
     * @param middlewares middlewares to be used for requests execution.
     * @param defaultRequestSettings default request settings to be used for requests execution.
     */
    public constructor(private readonly middlewares: Middleware[]) {
        this.middlewares.forEach((middleware, idx) => {
            if (idx < this.middlewares.length)
                middleware.next = this.middlewares[idx + 1];
        });
    }
    /**
     * Executes a request and returns a promise resolving the response.
     * @param url the request url.
     * @param options request options.
     * @returns the promise resolving the response.
     */
    public fetch(context: MiddlewareContext): Promise<Response> {
        if (this.middlewares.length > 0 && this.middlewares[0])
            return this.middlewares[0].execute(context);
        else
            throw new Error("No middlewares found");
    }
}