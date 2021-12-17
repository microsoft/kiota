import { RequestInformation } from "../requestInformation";
import { AuthenticationProvider } from "./authenticationProvider";

/** Provides a base class for implementing AuthenticationProvider for Bearer token scheme. */
export abstract class BaseBearerTokenAuthenticationProvider implements AuthenticationProvider {
    private static readonly authorizationHeaderKey = "Authorization";
    public authenticateRequest = async (request: RequestInformation) : Promise<void> => {
        if(!request) {
            throw new Error('request info cannot be null');
        }
        if(!request.headers[(BaseBearerTokenAuthenticationProvider.authorizationHeaderKey)] {
            const token = await this.getAuthorizationToken(request);
            if(!token) {
                throw new Error('Could not get an authorization token');
            }
            if(!request.headers) {
                request.headers = {};
            }
            request.headers[BaseBearerTokenAuthenticationProvider.authorizationHeaderKey] = `Bearer ${token}`;
        }
    }
    /**
     * This method is called by the BaseBearerTokenAuthenticationProvider class to authenticate the request via the returned access token.
     * @param requestUrl the request to authenticate.
     * @return a Promise that holds the access token to use for the request.
     */
    public abstract getAuthorizationToken: (request: RequestInformation) => Promise<string>;
}