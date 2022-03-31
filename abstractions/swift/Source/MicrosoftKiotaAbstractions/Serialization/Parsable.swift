public typealias FieldDeserializer<T> = (T, ParseNode) throws -> Void
public typealias ParsableFactory = (ParseNode) throws -> Parsable
public protocol Parsable {
    func serialize(writer: SerializationWriter) throws
    func getFieldDeserializers<T>() -> [String:FieldDeserializer<T>]
}