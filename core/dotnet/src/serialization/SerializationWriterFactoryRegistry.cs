using System;
using System.Collections.Generic;
using Kiota.Abstractions.Serialization;

namespace KiotaCore.Serialization {
    public class SerializationWriterFactoryRegistry : ISerializationWriterFactory {
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
