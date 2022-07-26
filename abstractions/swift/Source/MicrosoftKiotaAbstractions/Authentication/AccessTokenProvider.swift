import Foundation

public protocol AccessTokenProvider {
    func getAuthenticationToken(url: URL) async throws -> String?
    var allowedHostsValidator: AllowedHostsValidator { get }
}