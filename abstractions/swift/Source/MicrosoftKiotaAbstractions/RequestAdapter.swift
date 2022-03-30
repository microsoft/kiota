public typealias ErrorMappings = [String:ParsableFactory]
public typealias ResponseHandler<DeserializationType> = (Any, ErrorMappings) async throws -> DeserializationType
public protocol RequestAdapter {
    func send<T:Parsable>(request: RequestInformation, ctor: ParsableFactory, responseHandler: ResponseHandler<T>?, errorMappings: ErrorMappings?) async throws -> T?
    func sendCollection<T:Parsable>(request: RequestInformation, ctor: ParsableFactory, responseHandler: ResponseHandler<T>?, errorMappings: ErrorMappings?) async throws -> [T]?
    func sendPrimitive<T>(request: RequestInformation, responseHandler: ResponseHandler<T>?, errorMappings: ErrorMappings?) async throws -> T?
    func sendNoContent(request: RequestInformation, responseHandler: ResponseHandler<Void>?, errorMappings: ErrorMappings?) async throws -> Void
    func enableBackingStore() throws
    var baseUrl: String { get set }
    var serializationWriterFactory: SerializationWriterFactory { get }
}