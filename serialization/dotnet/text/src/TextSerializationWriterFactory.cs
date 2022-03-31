using System;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Text;

/// <summary>
/// The <see cref="ISerializationWriterFactory"/> implementation for the text content type
/// </summary>
public class TextSerializationWriterFactory : ISerializationWriterFactory {
    /// <inheritdoc />
    public string ValidContentType { get; } = "text/plain";

    /// <inheritdoc />
    public ISerializationWriter GetSerializationWriter(string contentType)
    {
        if(string.IsNullOrEmpty(contentType))
            throw new ArgumentNullException(nameof(contentType));
        else if(!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");

        return new TextSerializationWriter();
    }
}
