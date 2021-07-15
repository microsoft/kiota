using System;
using System.Linq;
using System.Reflection;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;

namespace Microsoft.Kiota.Abstractions {
    public static class ApiClientBuilder {
        public static void RegisterDefaultSerializer<T>() where T: ISerializationWriterFactory, new() {
            var serializationWriterFactory = new T();
            SerializationWriterFactoryRegistry.DefaultInstance
                                            .ContentTypeAssociatedFactories
                                            .TryAdd(serializationWriterFactory.ValidContentType, serializationWriterFactory);
        }
        public static void RegisterDefaultDeserializer<T>() where T: IParseNodeFactory, new() {
            var deserializerFactory = new T();
            ParseNodeFactoryRegistry.DefaultInstance
                                    .ContentTypeAssociatedFactories
                                    .TryAdd(deserializerFactory.ValidContentType, deserializerFactory);
        }
        public static ISerializationWriterFactory EnableBackingStoreForSerializationWriterFactory(ISerializationWriterFactory original) {
            ISerializationWriterFactory result = original ?? throw new ArgumentNullException(nameof(original));
            if(original is SerializationWriterFactoryRegistry registry)
                EnableBackingStoreForSerializationRegistry(registry);
            else
                result = new BackingStoreSerializationWriterProxyFactory(original);
            EnableBackingStoreForSerializationRegistry(SerializationWriterFactoryRegistry.DefaultInstance);
            return result;
        }
        public static IParseNodeFactory EnableBackingStoreForParseNodeFactory(IParseNodeFactory original) {
            IParseNodeFactory result = original ?? throw new ArgumentNullException(nameof(original));
            if(original is ParseNodeFactoryRegistry registry)
                EnableBackingStoreForParseNodeRegistry(registry);
            else
                result = new BackingStoreParseNodeFactory(original);
            EnableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.DefaultInstance);
            return result;
        }
        private static void EnableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry registry) {
            foreach(var entry in registry
                                    .ContentTypeAssociatedFactories
                                    .Where(x => !(x.Value is BackingStoreParseNodeFactory || 
                                                    x.Value is ParseNodeFactoryRegistry))) {
                registry.ContentTypeAssociatedFactories[entry.Key] = new BackingStoreParseNodeFactory(entry.Value);
            }
        }
        private static void EnableBackingStoreForSerializationRegistry(SerializationWriterFactoryRegistry registry) {
            foreach(var entry in registry
                                    .ContentTypeAssociatedFactories
                                    .Where(x => !(x.Value is BackingStoreSerializationWriterProxyFactory || 
                                                    x.Value is SerializationWriterFactoryRegistry))) {
                registry.ContentTypeAssociatedFactories[entry.Key] = new BackingStoreSerializationWriterProxyFactory(entry.Value);
            }
        }
    }
}
