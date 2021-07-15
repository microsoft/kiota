/** Authenticates the application and returns a token. */
export interface AuthenticationProvider {
    /**
     * Authenticates the application and returns a token base on the provided Uri.
     * @param requestUrl the Uri to authenticate the request for.
     * @return a Promise that will be completed with the token or undefined if the target request Uri doesn't correspond to a valid resource.
     */
    getAuthorizationToken: (requestUrl: string) => Promise<string>;
}