package com.microsoft.kiota.serialization;

import java.util.HashMap;
import java.util.Objects;

import javax.annotation.Nonnull;

public class SerializationWriterFactoryRegistry implements SerializationWriterFactory {
    public final static SerializationWriterFactoryRegistry defaultInstance = new SerializationWriterFactoryRegistry();
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
