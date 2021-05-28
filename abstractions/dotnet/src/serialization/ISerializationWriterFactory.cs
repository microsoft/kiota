namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface ISerializationWriterFactory {
        ISerializationWriter GetSerializationWriter(string contentType);
    }
}
