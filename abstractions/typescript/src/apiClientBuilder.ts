import { BackingStoreParseNodeFactory, BackingStoreSerializationWriterProxyFactory } from "./store";
import { ParseNodeFactory, ParseNodeFactoryRegistry, SerializationWriterFactory, SerializationWriterFactoryRegistry } from "./serialization";

export function registerDefaultSerializer(type: new() => SerializationWriterFactory): void {
    if(!type) throw new Error("Type is required");
    const serializer = new type();
    SerializationWriterFactoryRegistry.defaultInstance.contentTypeAssociatedFactories.set(serializer.getValidContentType(), serializer);
}
export function registerDefaultDeserializer(type: new() => ParseNodeFactory): void {
    if(!type) throw new Error("Type is required");
    const deserializer = new type();
    ParseNodeFactoryRegistry.defaultInstance.contentTypeAssociatedFactories.set(deserializer.getValidContentType(), deserializer);
}
export function enableBackingStoreForSerializationWriterFactory(original: SerializationWriterFactory): SerializationWriterFactory {
    if(!original) throw new Error("Original must be specified");
    let result = original;
    if(original instanceof SerializationWriterFactoryRegistry)
        enableBackingStoreForSerializationRegistry(original as SerializationWriterFactoryRegistry);
    else
        result = new BackingStoreSerializationWriterProxyFactory(original);
    enableBackingStoreForSerializationRegistry(SerializationWriterFactoryRegistry.defaultInstance);
    enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.defaultInstance);
    return result;
}
export function enableBackingStoreForParseNodeFactory(original: ParseNodeFactory): ParseNodeFactory {
    if(!original) throw new Error("Original must be specified");
    let result = original;
    if(original instanceof ParseNodeFactoryRegistry)
        enableBackingStoreForParseNodeRegistry(original as ParseNodeFactoryRegistry);
    else
        result = new BackingStoreParseNodeFactory(original);
    enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.defaultInstance);
    return result;
}
function enableBackingStoreForParseNodeRegistry(registry: ParseNodeFactoryRegistry): void {
    for (const [k, v] of registry.contentTypeAssociatedFactories) {
        if(!(v instanceof BackingStoreParseNodeFactory || v instanceof ParseNodeFactoryRegistry))
            registry.contentTypeAssociatedFactories.set(k, new BackingStoreParseNodeFactory(v));
    }
}
function enableBackingStoreForSerializationRegistry(registry: SerializationWriterFactoryRegistry): void {
    for (const [k, v] of registry.contentTypeAssociatedFactories) {
        if(!(v instanceof BackingStoreSerializationWriterProxyFactory || v instanceof SerializationWriterFactoryRegistry))
            registry.contentTypeAssociatedFactories.set(k, new BackingStoreSerializationWriterProxyFactory(v));
    }
}