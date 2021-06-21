using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public class SerializationWriterFactoryRegistry : ISerializationWriterFactory {
        public static readonly SerializationWriterFactoryRegistry DefaultInstance = new();
        public Dictionary<string, ISerializationWriterFactory> ContentTypeAssociatedFactories { get; set; } = new Dictionary<string, ISerializationWriterFactory>();
        public ISerializationWriter GetSerializationWriter(string contentType) {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            
            if(ContentTypeAssociatedFactories.ContainsKey(contentType))
                return ContentTypeAssociatedFactories[contentType].GetSerializationWriter(contentType);
            else
                throw new InvalidOperationException($"Content type {contentType} does not have a factory registered to be parsed");
        }

    }
}
