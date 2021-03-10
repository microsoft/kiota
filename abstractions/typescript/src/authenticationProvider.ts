export interface AuthenticationProvider {
    getAuthorizationToken: (requestUrl: URL) => Promise<string>;
}