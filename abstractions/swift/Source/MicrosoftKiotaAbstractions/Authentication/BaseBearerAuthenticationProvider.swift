public class BaseBearerAuthenticationProvider : AuthenticationProvider {
    public init (accessTokenProvider: AccessTokenProvider) {
        accessTokenProviderIntl = accessTokenProvider
    }
    private var accessTokenProviderIntl: AccessTokenProvider
    public var accessTokenProvider: AccessTokenProvider {
        get {
            return accessTokenProviderIntl
        }
    }
    public let authorizationHeaderKey = "Authorization"
    public let authorizationHeaderValuePrefix = "Bearer "
    public func authenticateRequest(request: RequestInformation) async throws {
        if request.headers[authorizationHeaderKey] == nil {
            let url = try request.getUri()
            let tokenResult = try? await accessTokenProvider.getAuthenticationToken(url: url)
            if let token = tokenResult {
                request.headers[authorizationHeaderKey] = authorizationHeaderValuePrefix + token
            }
        }
    }
}