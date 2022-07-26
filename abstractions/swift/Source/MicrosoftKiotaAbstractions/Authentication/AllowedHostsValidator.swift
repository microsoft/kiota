import Foundation
public class AllowedHostsValidator {
    private var validHostsIntl = Set<String>()
    public init(validHosts: [String]) {
        self.validHosts = validHosts;
    }
    public var validHosts: [String] { get {
        return Array(validHostsIntl)

    } set (validHosts) {
        self.validHostsIntl = Set(validHosts.map { $0.lowercased() })
    }}
    public func isUrlHostValid(url: URL) -> Bool {
        if let host = url.host {
            return validHostsIntl.contains(host.lowercased())
        }
        return false
    }
}
public let isSchemeHttps = { (url: URL) -> Bool in
    if let scheme = url.scheme {
        return scheme.lowercased() == "https"
    }
    return false
}