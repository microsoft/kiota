namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface ISerializationWriterFactory {
        string ValidContentType { get; }
        ISerializationWriter GetSerializationWriter(string contentType);
    }
}
