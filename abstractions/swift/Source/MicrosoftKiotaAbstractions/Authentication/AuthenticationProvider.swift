public protocol AuthenticationProvider {
    func authenticateRequest(request: RequestInformation) async throws
}