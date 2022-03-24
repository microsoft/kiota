import Foundation
public protocol ParseNodeFactory {
    func getValidContentType() throws -> String
    func getRootParseNode(contentType: String, content: Data?) throws -> ParseNode
}