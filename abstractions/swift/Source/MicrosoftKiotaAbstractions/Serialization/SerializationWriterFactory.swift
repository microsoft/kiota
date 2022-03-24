public protocol SerializationWriterFactory {
    func getValidContentType() throws -> String
    func getSerializationWriter(contentType: String) throws -> SerializationWriter
}