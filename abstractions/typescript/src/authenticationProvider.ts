export interface AuthenticationProvider {
    getAuthorizationToken: (requestUrl: string) => Promise<string>;
}