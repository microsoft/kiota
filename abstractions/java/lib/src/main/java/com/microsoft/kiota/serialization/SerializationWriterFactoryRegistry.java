package com.microsoft.kiota.serialization;

import java.util.HashMap;
import java.util.Objects;

import javax.annotation.Nonnull;
/** This factory holds a list of all the registered factories for the various types of nodes. */
public class SerializationWriterFactoryRegistry implements SerializationWriterFactory {
    /** Default singleton instance of the registry to be used when registring new factories that should be available by default. */
    public final static SerializationWriterFactoryRegistry defaultInstance = new SerializationWriterFactoryRegistry();
    /** List of factories that are registered by content type. */
    public HashMap<String, SerializationWriterFactory> contentTypeAssociatedFactories = new HashMap<>();
    public String getValidContentType() {
        throw new UnsupportedOperationException("The registry supports multiple content types. Get the registered factory instead.");
    }
    @Override
    @Nonnull
    public SerializationWriter getSerializationWriter(@Nonnull final String contentType) {
        Objects.requireNonNull(contentType, "parameter contentType cannot be null");
        if(contentType.isEmpty()) {
            throw new NullPointerException("contentType cannot be empty");
        }
        if(contentTypeAssociatedFactories.containsKey(contentType)) {
            return contentTypeAssociatedFactories.get(contentType).getSerializationWriter(contentType);
        } else {
            throw new RuntimeException("Content type " + contentType + " does not have a factory to be serialized");
        }
    }
    
}
