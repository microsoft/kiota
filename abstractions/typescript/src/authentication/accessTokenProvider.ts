import { RequestInformation } from "../requestInformation";

/**
 * @interface
 * An AccessTokenProvider implementation retrieves an access token
 * to be used by an AuthenticationProvider implementation.
 * @property {Function} authenticateRequest - The function to authenticate the request.
 */
 export interface AccessTokenProvider {
    getAuthorizationToken: (request?: RequestInformation) => Promise<string>;
}