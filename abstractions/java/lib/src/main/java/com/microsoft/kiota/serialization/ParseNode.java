package com.microsoft.kiota.serialization;

import java.time.OffsetDateTime;
import java.util.List;
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
    Integer getIntegerValue();
    @Nonnull
    Float getFloatValue();
    @Nonnull
    Long getLongValue();
    @Nonnull
    UUID getUUIDValue();
    @Nonnull
    OffsetDateTime getOffsetDateTimeValue();
    @Nonnull
    <T> List<T> getCollectionOfPrimitiveValues(@Nonnull final Class<T> targetClass);
    @Nonnull
    <T extends Parsable> List<T> getCollectionOfObjectValues(@Nonnull final Class<T> targetClass);
    @Nonnull
    <T extends Parsable> T getObjectValue(@Nonnull final Class<T> targetClass);
}