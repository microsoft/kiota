package com.microsoft.kiota.serialization;

import javax.annotation.Nonnull;
/** Defines the contract for a factory that creates SerializationWriter instances. */
public interface SerializationWriterFactory {
    /**
     * Gets the content type this factory creates serialization writers for.
     * @return the content type this factory creates serialization writers for.
     */
    @Nonnull
    String getValidContentType();
    /**
     * Creates a new SerializationWriter instance for the given content type.
     * @param contentType the content type to create a serialization writer for.
     * @return a new SerializationWriter instance for the given content type.
     */
    @Nonnull
    SerializationWriter getSerializationWriter(@Nonnull final String contentType);
}