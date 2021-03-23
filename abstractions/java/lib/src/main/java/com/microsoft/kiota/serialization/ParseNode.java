package com.microsoft.kiota.serialization;

import java.time.OffsetDateTime;
import java.util.UUID;

import javax.annotation.Nonnull;

public interface ParseNode {
    @Nonnull
    ParseNode getChildNode(@Nonnull final String identifier);
    @Nonnull
    String getStringValue();
    @Nonnull
    Boolean getBooleanValue();
    @Nonnull
    Integer getIntValue();
    @Nonnull
    Float getFloatValue();
    @Nonnull
    Long getDoubleValue();
    @Nonnull
    UUID getGuidValue();
    @Nonnull
    OffsetDateTime getOffsetDateTimeValue();
    @Nonnull
    <T extends Parsable<T>> Iterable<T> getCollectionOfObjectValues(@Nonnull final Class<T> targetClass);
    @Nonnull
    <T extends Parsable<T>> T getObjectValue(@Nonnull final Class<T> targetClass);
}