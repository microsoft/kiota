import { BackingStoreParseNodeFactory, BackingStoreSerializationWriterProxyFactory } from "../store";
import { ParseNodeFactory } from "./parseNodeFactory";
import { ParseNodeFactoryRegistry } from "./parseNodeFactoryRegistry";
import { SerializationWriterFactory } from "./serializationWriterFactory";
import { SerializationWriterFactoryRegistry } from "./serializationWriterFactoryRegistry";

export async function registerDefaultSerializers(moduleName: string): Promise<void> {
    const module = await import(moduleName);
    console.log(module); //TODO scan for interfaces implementations
};
export function enableBackingStore(original: SerializationWriterFactory): SerializationWriterFactory {
    let result: SerializationWriterFactory | undefined = undefined;
    if(original instanceof SerializationWriterFactoryRegistry) {
        const registry = original as SerializationWriterFactoryRegistry;
        enableBackingStoreForSerializationRegistry(registry);
        result = registry;
    } else if(original)
        result = new BackingStoreSerializationWriterProxyFactory(original);
    enableBackingStoreForSerializationRegistry(SerializationWriterFactoryRegistry.defaultInstance);
    enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.defaultInstance);
    return result ?? original;
}
export function enableBackingStoreForParseNodeFactory(original: ParseNodeFactory): ParseNodeFactory {
    let result: ParseNodeFactory | undefined = undefined;
    if(original instanceof ParseNodeFactoryRegistry) {
        const registry = original as ParseNodeFactoryRegistry;
        enableBackingStoreForParseNodeRegistry(registry);
        result = registry;
    } else if(original)
        result = new BackingStoreParseNodeFactory(original);
    enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.defaultInstance);
    return result ?? original;
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