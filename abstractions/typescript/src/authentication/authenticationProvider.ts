import { RequestInfo } from "../requestInfo";

/** Authenticates the application request. */
export interface AuthenticationProvider {
    /**
     * Authenticates the application and returns a token base on the provided Uri.
     * @param request the request to authenticate.
     * @return a Promise to await for the authentication to be completed.
     */
    authenticateRequest: (request: RequestInfo) => Promise<void>;
}