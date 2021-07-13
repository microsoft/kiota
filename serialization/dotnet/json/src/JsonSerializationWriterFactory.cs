using System;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Json {
    public class JsonSerializationWriterFactory : ISerializationWriterFactory {
        public string ValidContentType { get; } = "application/json";
        public ISerializationWriter GetSerializationWriter(string contentType) {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            else if(!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");

            return new JsonSerializationWriter();
        }
    }
}
