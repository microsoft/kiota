using System;
using System.Linq;
using System.Reflection;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;

namespace Microsoft.Kiota.Abstractions {
    public static class ApiClientBuilder {
        public static void RegisterDefaultSerializers(string assemblyName) {
            if(string.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));
            var assembly = Assembly.Load(assemblyName);

            LoadClassesFromAssembly<ISerializationWriterFactory>(assembly, x => {
                SerializationWriterFactoryRegistry.DefaultInstance
                                                .ContentTypeAssociatedFactories
                                                .TryAdd(x.ValidContentType, x);
            });
            LoadClassesFromAssembly<IParseNodeFactory>(assembly, x => {
                ParseNodeFactoryRegistry.DefaultInstance
                                        .ContentTypeAssociatedFactories
                                        .TryAdd(x.ValidContentType, x);
            });
        }
        private static void LoadClassesFromAssembly<T>(Assembly assembly, Action<T> register) {
            var lookupType = typeof(T);
            foreach(var implementation in assembly.GetTypes().Where(x => lookupType.IsAssignableFrom(x) && !x.IsAbstract && x.IsClass)) {
                var constructor = implementation.GetConstructor(Array.Empty<Type>());
                var instance = (T)constructor.Invoke(Array.Empty<Object>());
                register.Invoke(instance);
            }
        }
        public static void EnableBackingStore() {
            foreach(var entry in SerializationWriterFactoryRegistry
                                    .DefaultInstance
                                    .ContentTypeAssociatedFactories
                                    .Where(x => !(x.Value is BackingStoreSerializationWriterProxyFactory || 
                                                    x.Value is SerializationWriterFactoryRegistry))) {
                SerializationWriterFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[entry.Key] = new BackingStoreSerializationWriterProxyFactory(entry.Value);
            }
            foreach(var entry in ParseNodeFactoryRegistry
                                    .DefaultInstance
                                    .ContentTypeAssociatedFactories
                                    .Where(x => !(x.Value is BackingStoreParseNodeFactory || 
                                                    x.Value is ParseNodeFactoryRegistry))) {
                ParseNodeFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[entry.Key] = new BackingStoreParseNodeFactory(entry.Value);
            }
        }
    }
}
