import { Middleware } from "./middleware";
import { fetch } from 'cross-fetch';
import { MiddlewareOption } from "@microsoft/kiota-abstractions";

/** Default fetch client with options and a middleware pipleline for requests execution. */
export class HttpClient {
    /**
     * Instantiates a new HttpClient.
     * @param middlewares middlewares to be used for requests execution.
     * @param defaultRequestSettings default request settings to be used for requests execution.
     */
    public constructor(private readonly middlewares: Middleware[] = HttpClient.getDefaultMiddlewares(), private readonly defaultRequestSettings: RequestInit = HttpClient.getDefaultRequestSettings()) {
        this.middlewares = [...this.middlewares, new FetchMiddleware()];
        this.middlewares.forEach((middleware, idx) => {
            if(idx < this.middlewares.length)
                middleware.next = this.middlewares[idx + 1];
        });
    }
    /**
     * Executes a request and returns a promise resolving the response.
     * @param url the request url.
     * @param options request options.
     * @returns the promise resolving the response.
     */
    public fetch(url: string, options?: RequestInit, middlewareOptions?: MiddlewareOption[]): Promise<Response> {
        const finalOptions = {...this.defaultRequestSettings, ...options} as RequestInit;
        if(this.middlewares.length > 0 && this.middlewares[0])
            return this.middlewares[0].execute(url, finalOptions, middlewareOptions);
        else
            throw new Error("No middlewares found");
    }
    /**
     * Gets the default middlewares in use for the client.
     * @returns the default middlewares.
     */
    public static getDefaultMiddlewares(): Middleware[] {
        return []; //TODO add default middlewares
    }
    /**
     * Gets the default request settings to be used for the client.
     * @returns the default request settings.
     */
    public static getDefaultRequestSettings(): RequestInit {
        return {}; //TODO add default request settings
    }
}
/** Default middleware executing a request. Internal use only. */
class FetchMiddleware implements Middleware {
    next: Middleware | undefined;
    public execute(url: string, req: RequestInit, _?: MiddlewareOption[]): Promise<Response> {
        return fetch(url, req);
    }
}