/**
 * @interface
 * An AccessTokenProvider implementation retrieves an access token
 * to be used by an AuthenticationProvider implementation.
 * @property {Function} authenticateRequest - The function to authenticate the request.
 */
 export interface AccessTokenProvider {
    getAuthorizationToken: (url:string) => Promise<string>;
}