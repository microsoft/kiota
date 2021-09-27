// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    ///     Provides a builder for creating an ApiClient and register the default serializers/deserializers.
    /// </summary>
    public static class ApiClientBuilder
    {
        /// <summary>
        /// Registers the default serializer to the registry.
        /// </summary>
        /// <typeparam name="T">The type of the serialization factory to register</typeparam>
        public static void RegisterDefaultSerializer<T>() where T : ISerializationWriterFactory, new()
        {
            var serializationWriterFactory = new T();
            SerializationWriterFactoryRegistry.DefaultInstance
                                            .ContentTypeAssociatedFactories
                                            .TryAdd(serializationWriterFactory.ValidContentType, serializationWriterFactory);
        }
        /// <summary>
        /// Registers the default deserializer to the registry.
        /// </summary>
        /// <typeparam name="T">The type of the parse node factory to register</typeparam>
        public static void RegisterDefaultDeserializer<T>() where T : IParseNodeFactory, new()
        {
            var deserializerFactory = new T();
            ParseNodeFactoryRegistry.DefaultInstance
                                    .ContentTypeAssociatedFactories
                                    .TryAdd(deserializerFactory.ValidContentType, deserializerFactory);
        }
        /// <summary>
        /// Enables the backing store on default serialization writers and the given serialization writer.
        /// </summary>
        /// <param name="original">The serialization writer to enable the backing store on.</param>
        /// <returns>A new serialization writer with the backing store enabled.</returns>
        public static ISerializationWriterFactory EnableBackingStoreForSerializationWriterFactory(ISerializationWriterFactory original)
        {
            ISerializationWriterFactory result = original ?? throw new ArgumentNullException(nameof(original));
            if(original is SerializationWriterFactoryRegistry registry)
                EnableBackingStoreForSerializationRegistry(registry);
            else
                result = new BackingStoreSerializationWriterProxyFactory(original);
            EnableBackingStoreForSerializationRegistry(SerializationWriterFactoryRegistry.DefaultInstance);
            return result;
        }
        /// <summary>
        /// Enables the backing store on default parse nodes factories and the given parse node factory.
        /// </summary>
        /// <param name="original">The parse node factory to enable the backing store on.</param>
        /// <returns>A new parse node factory with the backing store enabled.</returns>
        public static IParseNodeFactory EnableBackingStoreForParseNodeFactory(IParseNodeFactory original)
        {
            IParseNodeFactory result = original ?? throw new ArgumentNullException(nameof(original));
            if(original is ParseNodeFactoryRegistry registry)
                EnableBackingStoreForParseNodeRegistry(registry);
            else
                result = new BackingStoreParseNodeFactory(original);
            EnableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.DefaultInstance);
            return result;
        }
        private static void EnableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry registry)
        {
            foreach(var entry in registry
                                    .ContentTypeAssociatedFactories
                                    .Where(x => !(x.Value is BackingStoreParseNodeFactory ||
                                                    x.Value is ParseNodeFactoryRegistry)))
            {
                registry.ContentTypeAssociatedFactories[entry.Key] = new BackingStoreParseNodeFactory(entry.Value);
            }
        }
        private static void EnableBackingStoreForSerializationRegistry(SerializationWriterFactoryRegistry registry)
        {
            foreach(var entry in registry
                                    .ContentTypeAssociatedFactories
                                    .Where(x => !(x.Value is BackingStoreSerializationWriterProxyFactory ||
                                                    x.Value is SerializationWriterFactoryRegistry)))
            {
                registry.ContentTypeAssociatedFactories[entry.Key] = new BackingStoreSerializationWriterProxyFactory(entry.Value);
            }
        }
    }
}
