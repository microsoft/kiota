import Foundation
import URITemplate

public enum RequestInformationErrors : Error {
    case emptyUrlTemplate
    case emptyContentType
    case itemsCannotBeNilOrEmpty
    case unableToExpandUriTemplate
    case invalidRawUrl
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
            throw RequestInformationErrors.invalidRawUrl
        } else {
            let urlTemplate = URITemplate(template: self.urlTemplate)
            let merged = pathParameters.merging(queryParameters) 
                            { (first, _) in first }
            let url = urlTemplate.expand(merged)
            if let newValue = URL(string: url) {
                return newValue
            } else {
                throw RequestInformationErrors.unableToExpandUriTemplate
            }
        }
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
    public func addRequestOption(options:RequestOption...) {
        for option in options {
            self.options[option.key] = option
        }
    }
    public func getRequestOptions() -> [RequestOption] {
        return [RequestOption](options.values)
    }
    public func setContentFromParsable<T: Parsable>(requestAdapter: RequestAdapter, contentType: String, items: T...) throws {
        guard contentType != "" else {
            throw RequestInformationErrors.emptyContentType
        }
        guard items.count > 0 else {
            throw RequestInformationErrors.itemsCannotBeNilOrEmpty
        }
        if let writer = try? requestAdapter.serializationWriterFactory.getSerializationWriter(contentType: contentType) {
            if(items.count == 1) {
                try writer.writeObjectValue(key: "", value: items[0])
            } else {
                try writer.writeCollectionOfObjectValues(key: "", value: items)
            }
            self.content = try? writer.getSerializedContent()
            self.headers[contentTypeHeaderKey] = contentType
        }
    }
    //TODO add query parameters from object by reflection
}