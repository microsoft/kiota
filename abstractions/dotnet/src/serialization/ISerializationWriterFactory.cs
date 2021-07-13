namespace Microsoft.Kiota.Abstractions.Serialization {
    /// <summary>
    ///     Defines the contract for a factory that creates <see cref="ISerializationWriter" /> instances.
    /// </summary>
    public interface ISerializationWriterFactory {
        /// <summary>
        /// Gets the content type this factory creates serialization writers for.
        /// </summary>
        string ValidContentType { get; }
        /// <summary>
        ///     Creates a new <see cref="ISerializationWriter" /> instance for the given content type.
        /// </summary>
        /// <param name="contentType">The content type for which a serialization writer should be created.</param>
        /// <returns>A new <see cref="ISerializationWriter" /> instance for the given content type.</returns>
        ISerializationWriter GetSerializationWriter(string contentType);
    }
}
