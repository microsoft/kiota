package com.microsoft.kiota.serialization;

import java.util.Map;
import java.util.function.BiConsumer;

import javax.annotation.Nonnull;
/**
 * Defines a serializable model object.
 */
public interface Parsable {
    /**
     * Gets the deserialization information for this object.
     * @return The deserialization information for this object where each entry is a property key with its deserialization callback.
     */
    @Nonnull
    <T> Map<String, BiConsumer<T, ParseNode>> getFieldDeserializers();
    /**
     * Writes the objects properties to the current writer.
     * @param writer The writer to write to.
     */
    void serialize(@Nonnull final SerializationWriter writer);
}
