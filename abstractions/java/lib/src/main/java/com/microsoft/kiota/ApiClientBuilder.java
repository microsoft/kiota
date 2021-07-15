package com.microsoft.kiota;

import com.microsoft.kiota.serialization.ParseNodeFactory;
import com.microsoft.kiota.serialization.ParseNodeFactoryRegistry;
import com.microsoft.kiota.serialization.SerializationWriterFactory;
import com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry;

import com.microsoft.kiota.store.BackingStoreSerializationWriterProxyFactory;
import com.microsoft.kiota.store.BackingStoreParseNodeFactory;

import java.lang.reflect.InvocationTargetException;
import java.lang.NoSuchMethodException;

import java.util.Objects;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** Provides a builder for creating an ApiClient and register the default serializers/deserializers. */
public class ApiClientBuilder {
    // private constructor to prevent instantiation
    private ApiClientBuilder() { }
    /**
     * Registers the default serializer to the registry.
     * @param factoryClass the class of the factory to be registered.
     */
    public static void registerDefaultSerializer(@Nonnull final Class<? extends SerializationWriterFactory> factoryClass) {
        Objects.requireNonNull(factoryClass);
        try {
            final SerializationWriterFactory factory = factoryClass.getConstructor().newInstance();
            SerializationWriterFactoryRegistry.defaultInstance
                .contentTypeAssociatedFactories.put(factory.getValidContentType(), factory);
        } catch (InstantiationException | IllegalAccessException | NoSuchMethodException | InvocationTargetException e) {
            throw new RuntimeException(e);
        }
    }
    /**
     * Registers the default deserializer to the registry.
     * @param factoryClass the class of the factory to be registered.
     */
    public static void registerDefaultDeserializer(@Nonnull final Class<? extends ParseNodeFactory> factoryClass) {
        Objects.requireNonNull(factoryClass);
        try {
            final ParseNodeFactory factory = factoryClass.getConstructor().newInstance();
            ParseNodeFactoryRegistry.defaultInstance
                .contentTypeAssociatedFactories.put(factory.getValidContentType(), factory);
        } catch (InstantiationException | IllegalAccessException | NoSuchMethodException | InvocationTargetException e) {
            throw new RuntimeException(e);
        }
    }
    /**
     * Enables the backing store on default serialization writers and the given serialization writer.
     * @param original The serialization writer to enable the backing store on.
     * @return A new serialization writer with the backing store enabled.
     */
    @Nonnull
    public static SerializationWriterFactory enableBackingStoreForSerializationWriterFactory(@Nonnull final SerializationWriterFactory original) {
        SerializationWriterFactory result = Objects.requireNonNull(original);
        if(original instanceof SerializationWriterFactoryRegistry)
            enableBackingStoreForSerializationWriterRegistry((SerializationWriterFactoryRegistry) original);
        else
            result = new BackingStoreSerializationWriterProxyFactory(original);
        enableBackingStoreForSerializationWriterRegistry(SerializationWriterFactoryRegistry.defaultInstance);
        return result;
    }
    /**
     * Enables the backing store on default parse node factories and the given parse node factory.
     * @param original The parse node factory to enable the backing store on.
     * @return A new parse node factory with the backing store enabled.
     */
    @Nonnull
    public static ParseNodeFactory enableBackingStoreForParseNodeFactory(@Nonnull final ParseNodeFactory original) {
        ParseNodeFactory result = Objects.requireNonNull(original);
        if(original instanceof ParseNodeFactoryRegistry)
            enableBackingStoreForParseNodeRegistry((ParseNodeFactoryRegistry) original);
        else
            result = new BackingStoreParseNodeFactory(original);
        enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry.defaultInstance);
        return result;
    }
    private static void enableBackingStoreForParseNodeRegistry(@Nonnull final ParseNodeFactoryRegistry registry) {
        Objects.requireNonNull(registry);
        for(final ParseNodeFactory factory : registry.contentTypeAssociatedFactories.values()) {
            if(!(factory instanceof BackingStoreParseNodeFactory) &&
               !(factory instanceof ParseNodeFactoryRegistry)) {
                registry.contentTypeAssociatedFactories.put(factory.getValidContentType(), new BackingStoreParseNodeFactory(factory));
            }
        }
    }
    private static void enableBackingStoreForSerializationWriterRegistry(@Nonnull final SerializationWriterFactoryRegistry registry) {
        Objects.requireNonNull(registry);
        for(final SerializationWriterFactory factory : registry.contentTypeAssociatedFactories.values()) {
            if(!(factory instanceof BackingStoreSerializationWriterProxyFactory) &&
               !(factory instanceof SerializationWriterFactoryRegistry)) {
                registry.contentTypeAssociatedFactories.put(factory.getValidContentType(), new BackingStoreSerializationWriterProxyFactory(factory));
            }
        }
    }
}