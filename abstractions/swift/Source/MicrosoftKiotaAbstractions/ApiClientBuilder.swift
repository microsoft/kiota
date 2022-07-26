public class ApiClientBuilder {
    private init() {

    }
    private static let defaultSerializationWriterFactoryInstanceIntl = SerializationWriterFactoryRegistry()
    public static var defaultSerializationWriterFactoryInstance: SerializationWriterFactory {
        get {
            return defaultSerializationWriterFactoryInstanceIntl
        }
    }
    public static func registerDefaultSerializer(metaFactory: () -> SerializationWriterFactory) {
        let factory = metaFactory()
        if let contentType = try? factory.getValidContentType() {
            if contentType != "" {
                defaultSerializationWriterFactoryInstanceIntl.contentTypeAssociatedFactories[contentType] = factory
            }
        }
    }
    private static let defaultParseNodeFactoryInstanceIntl = ParseNodeFactoryRegistry()
    public static var defaultParseNodeFactoryInstance: ParseNodeFactory {
        get {
            return defaultParseNodeFactoryInstanceIntl
        }
    }
    public static func registerDefaultParser(metaFactory: () -> ParseNodeFactory) {
        let factory = metaFactory()
        if let contentType = try? factory.getValidContentType() {
            if contentType != "" {
                defaultParseNodeFactoryInstanceIntl.contentTypeAssociatedFactories[contentType] = factory
            }
        }
    }
}