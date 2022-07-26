import Foundation
public enum ParseNodeFactoryRegistryErrors : Error {
    case registrySupportsMultipleTypesGetOneFactory
    case factoryNotFoundForContentType
    case contentCannotBeNil
}
public class ParseNodeFactoryRegistry : ParseNodeFactory {
    public var contentTypeAssociatedFactories = [String:ParseNodeFactory]()
    public func getValidContentType() throws -> String {
        throw ParseNodeFactoryRegistryErrors.registrySupportsMultipleTypesGetOneFactory
    }
    public func getRootParseNode(contentType: String, content: Data?) throws -> ParseNode {
        guard let factory = contentTypeAssociatedFactories[contentType] else {
            throw ParseNodeFactoryRegistryErrors.factoryNotFoundForContentType
        }
        guard content != nil else {
            throw ParseNodeFactoryRegistryErrors.contentCannotBeNil
        }
        return try factory.getRootParseNode(contentType: contentType, content: content)
    }
}