package com.microsoft.kiota.serialization;

import java.io.Closeable;
import java.io.InputStream;
import java.time.OffsetDateTime;
import java.util.Map;
import java.util.UUID;
import java.util.EnumSet;
import java.lang.Enum;
import java.util.function.Consumer;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** Defines an interface for serialization of objects to a stream. */
public interface SerializationWriter extends Closeable {
    /**
     * Writes the specified string value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeStringValue(@Nullable final String key, @Nonnull final String value);
    /**
     * Writes the specified Boolean value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeBooleanValue(@Nullable final String key, @Nonnull final Boolean value);
    /**
     * Writes the specified Integer value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeIntegerValue(@Nullable final String key, @Nonnull final Integer value);
    /**
     * Writes the specified Float value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeFloatValue(@Nullable final String key, @Nonnull final Float value);
    /**
     * Writes the specified Long value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeLongValue(@Nullable final String key, @Nonnull final Long value);
    /**
     * Writes the specified UUID value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeUUIDValue(@Nullable final String key, @Nonnull final UUID value);
    /**
     * Writes the specified OffsetDateTime value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    void writeOffsetDateTimeValue(@Nullable final String key, @Nonnull final OffsetDateTime value);
    /**
     * Writes the specified collection of primitive values to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    <T> void writeCollectionOfPrimitiveValues(@Nullable final String key, @Nonnull final Iterable<T> values);
    /**
     * Writes the specified collection of object values to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    <T extends Parsable> void writeCollectionOfObjectValues(@Nullable final String key, @Nonnull final Iterable<T> values);
    /**
     * Writes the specified model object value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    <T extends Parsable> void writeObjectValue(@Nullable final String key, @Nonnull final T value);
    /**
     * Gets the value of the serialized content.
     * @return the value of the serialized content.
     */
    @Nonnull
    InputStream getSerializedContent();
    /**
     * Writes the specified enum set value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    <T extends Enum<T>> void writeEnumSetValue(@Nullable final String key, @Nullable final EnumSet<T> values);
    /**
     * Writes the specified enum value to the stream with an optional given key.
     * @param key the key to write the value with.
     * @param value the value to write to the stream.
     */
    <T extends Enum<T>> void writeEnumValue(@Nullable final String key, @Nullable final T value);
    /**
     * Writes the specified additional data values to the stream with an optional given key.
     * @param value the values to write to the stream.
     */
    void writeAdditionalData(@Nonnull final Map<String, Object> value);
    /**
     * Gets the callback called before the object gets serialized.
     * @return the callback called before the object gets serialized.
     */
    @Nullable
    Consumer<Parsable> getOnBeforeObjectSerialization();
    /**
     * Gets the callback called after the object gets serialized.
     * @return the callback called after the object gets serialized.
     */
    @Nullable
    Consumer<Parsable> getOnAfterObjectSerialization();
    /**
     * Sets the callback called before the objects gets serialized.
     * @param value the callback called before the objects gets serialized.
     */
    void setOnBeforeObjectSerialization(@Nullable final Consumer<Parsable> value);
    /**
     * Sets the callback called after the objects gets serialized.
     * @param value the callback called after the objects gets serialized.
     */
    void setOnAfterObjectSerialization(@Nullable final Consumer<Parsable> value);
}