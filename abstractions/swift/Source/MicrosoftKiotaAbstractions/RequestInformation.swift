import Foundation
import URITemplate

public enum RequestInformationErrors : Error {
    case emptyUrlTemplate
}

public class RequestInformation {
    public var method: HttpMethod = HttpMethod.get
    public var urlTemplate = ""
    public var queryParameters = [String:String]()
    public var pathParameters = [String:String]()
    public var headers = [String:String]()
    var contentInternal: Data?
    public var content: Data? {
        get {
            return contentInternal
        }
        set(newContent) {
            contentInternal = newContent
            if newContent != nil {
                headers[contentTypeHeaderKey] = binaryContentType
            }
        }
    }
    var uriInternal: URL?
    public func getUri() throws -> URL {
        if let uriInternalValue = uriInternal {
            return uriInternalValue
        } 
        guard !urlTemplate.isEmpty else {
            throw RequestInformationErrors.emptyUrlTemplate
        }
        if let rawUrl = pathParameters[rawUrlKey] {
            if rawUrl != "" {
                let newValue = URL(string: rawUrl)
                if let url = newValue {
                    setUri(newUri: url)
                    return url
                }
            }
        }
        //TODO template resolution
        return uriInternal!
    }
    public func setUri(newUri: URL) {
        uriInternal = newUri
        pathParameters.removeAll()
        queryParameters.removeAll()
    }
    var options = [String:RequestOption]()
    let rawUrlKey = "request-raw-url"
    let contentTypeHeaderKey = "Content-Type"
    let binaryContentType = "application/octet-stream"
    func addRequestOption(options:RequestOption...) {
        for option in options {
            self.options[option.key] = option
        }
    }
    func getRequestOptions() -> [RequestOption] {
        return [RequestOption](options.values)
    }
    //TODO set content from parsable
    //TODO add query parameters from object by reflection
}