/** Defines the contract for a middleware in the request execution pipeline. */
export interface Middleware {
    /** Next middleware to be executed. The current middleware must execute it in its implementation. */
    next: Middleware | undefined;
    /**
     * Main method of the middleware.
     * @param req The request object.
     * @param url The URL of the request.
     * @return A promise that resolves to the response object.
     */
    execute(url: string, req: RequestInit): Promise<Response>;
}