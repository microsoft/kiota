import { RequestInformation } from "../requestInformation";
import { AuthenticationProvider, AccessTokenProvider } from "./authenticationProvider";

/** Provides a base class for implementing AuthenticationProvider for Bearer token scheme. */
export class BaseBearerTokenAuthenticationProvider implements AuthenticationProvider {
    private static readonly authorizationHeaderKey = "Authorization";

    /**
     * 
     * @param accessTokenProvider 
     */
    public constructor(private accessTokenProvider: AccessTokenProvider) { };
    
    public authenticateRequest = async (request: RequestInformation): Promise<void> => {
        if (!request) {
            throw new Error('request info cannot be null');
        }
        if (!request.headers || !request.headers[BaseBearerTokenAuthenticationProvider.authorizationHeaderKey]) {
            const token = await this.accessTokenProvider.getAuthorizationToken(request);
            if (!request.headers) {
                request.headers = {};
            }
            if(token) {
                request.headers[BaseBearerTokenAuthenticationProvider.authorizationHeaderKey] = `Bearer ${token}`;
            }
        }
    }
}