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

public interface SerializationWriter extends Closeable {
    void writeStringValue(@Nullable final String key, @Nonnull final String value);
    void writeBooleanValue(@Nullable final String key, @Nonnull final Boolean value);
    void writeIntegerValue(@Nullable final String key, @Nonnull final Integer value);
    void writeFloatValue(@Nullable final String key, @Nonnull final Float value);
    void writeLongValue(@Nullable final String key, @Nonnull final Long value);
    void writeUUIDValue(@Nullable final String key, @Nonnull final UUID value);
    void writeOffsetDateTimeValue(@Nullable final String key, @Nonnull final OffsetDateTime value);
    <T> void writeCollectionOfPrimitiveValues(@Nullable final String key, @Nonnull final Iterable<T> values);
    <T extends Parsable> void writeCollectionOfObjectValues(@Nullable final String key, @Nonnull final Iterable<T> values);
    <T extends Parsable> void writeObjectValue(@Nullable final String key, @Nonnull final T value);
    @Nonnull
    InputStream getSerializedContent();
    <T extends Enum<T>> void writeEnumSetValue(@Nullable final String key, @Nullable final EnumSet<T> values);
    <T extends Enum<T>> void writeEnumValue(@Nullable final String key, @Nullable final T value);
    void writeAdditionalData(@Nonnull final Map<String, Object> value);
    @Nullable
    Consumer<Parsable> getOnBeforeObjectSerialization();
    @Nullable
    Consumer<Parsable> getOnAfterObjectSerialization();
    void setOnBeforeObjectSerialization(@Nullable final Consumer<Parsable> value);
    void setOnAfterObjectSerialization(@Nullable final Consumer<Parsable> value);
}