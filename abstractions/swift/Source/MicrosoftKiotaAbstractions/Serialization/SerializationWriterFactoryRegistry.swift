public enum SerializationWriterFactoryRegistryErrors : Error {
    case registrySupportsMultipleTypesGetOneFactory
    case factoryNotFoundForContentType
}
public class SerializationWriterFactoryRegistry : SerializationWriterFactory {
    public var contentTypeAssociatedFactories = [String:SerializationWriterFactory]()
    public func getValidContentType() throws -> String {
        throw SerializationWriterFactoryRegistryErrors.registrySupportsMultipleTypesGetOneFactory
    }
    public func getSerializationWriter(contentType: String) throws -> SerializationWriter {
        guard let factory = contentTypeAssociatedFactories[contentType] else {
            throw SerializationWriterFactoryRegistryErrors.factoryNotFoundForContentType
        }
        return try factory.getSerializationWriter(contentType: contentType)
    }
}