import Foundation

public protocol SerializationWriter {
    func writeStringValue(key : String, value : String?) throws
    func writeBoolValue(key : String, value : Bool?) throws
    func writeUint8Value(key : String, value : UInt8?) throws
    func writeInt8Value(key : String, value : Int8?) throws
    func writeInt32Value(key : String, value : Int32?) throws
    func writeInt64Value(key : String, value : Int64?) throws
    func writeFloat32Value(key : String, value : Float32?) throws
    func writeFloat64Value(key : String, value : Float64?) throws
    func writeByteArrayValue(key : String, value : [UInt8]) throws
    func writeDateValue(key : String, value : Date?) throws //TODO
    func writeTimeOnlyValue(key : String, value : Date?) throws //TODO
    func writeDateOnlyValue(key : String, value : Date?) throws
    func writeDurationValue(key : String, value : Date?) throws //TODO
    func writeUUIDValue(key : String, value : UUID?) throws
    func writeObjectValue<T: Parsable>(key: String, value: T) throws
    func writeCollectionOfObjectValues<T: Parsable>(key: String, value: [T]) throws
    func writeCollectionOfPrimitiveValues<T>(key: String, value: [T]) throws
    func getSerializedContent() throws -> Data?
    func writeAdditionalData(value: [String:Any]?) throws
    //TODO write enum value & write collection of enum values
}