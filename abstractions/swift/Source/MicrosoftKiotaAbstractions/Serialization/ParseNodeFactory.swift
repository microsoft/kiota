import Foundation
public protocol ParseNodeFactory {
    func getValidContentType() throws -> String
    func getSerializationWriter(contentType: String, content: Data?) throws -> ParseNode
}