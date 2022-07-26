import Foundation
public protocol ParseNode {
    func getChileNode(key: String) throws -> ParseNode?
    func getObjectValue<T:Parsable>(ctor: ParsableFactory) throws -> T?
    func getCollectionOfObjectValues<T:Parsable>(ctor: ParsableFactory) throws -> [T]?
    func getCollectionOfPrimitiveValues<T>() throws -> [T]?
    func getStringValue() throws -> String?
    func getBoolValue() throws -> Bool?
    func getUint8Value() throws -> UInt8?
    func getInt8Value() throws -> Int8?
    func getInt32Value() throws -> Int32?
    func getInt64Value() throws -> Int64?
    func getFloat32Value() throws -> Float32?
    func getFloat64Value() throws -> Float64?
    func getByteArrayValue() throws -> [UInt8]?
    func getDateValue() throws -> Date? //TODO
    func getTimeOnlyValue() throws -> Date? //TODO
    func getDateOnlyValue() throws -> Date?
    func getDurationValue() throws -> Date? //TODO
    func getUUIDValue() throws -> UUID?
    func getAdditionalData() throws -> [String:Any]?
    //TODO get enum value & get collection of enum values
}