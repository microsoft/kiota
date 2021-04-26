using System;
using Kiota.Abstractions.Serialization;

namespace KiotaCore.Serialization {
    public class JsonSerializationWriterFactory : ISerializationWriterFactory {
        private const string validContentType = "application/json";
        public ISerializationWriter GetSerializationWriter(string contentType) {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            else if(!validContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentOutOfRangeException($"expected a {validContentType} content type");

            return new JsonSerializationWriter();
        }
    }
}
