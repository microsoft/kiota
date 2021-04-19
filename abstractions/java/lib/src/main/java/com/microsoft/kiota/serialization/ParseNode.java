package com.microsoft.kiota.serialization;

import java.lang.Enum;
import java.time.OffsetDateTime;
import java.util.EnumSet;
import java.util.List;
import java.util.UUID;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

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
    @Nullable
    <T extends Enum<T>> T getEnumValue(@Nonnull final Class<T> targetEnum);
    @Nullable
    <T extends Enum<T>> EnumSet<T> getEnumSetValue(@Nonnull final Class<T> targetEnum);
    @Nonnull
    <T> List<T> getCollectionOfPrimitiveValues(@Nonnull final Class<T> targetClass);
    @Nonnull
    <T extends Parsable> List<T> getCollectionOfObjectValues(@Nonnull final Class<T> targetClass);
    @Nonnull
    <T extends Parsable> T getObjectValue(@Nonnull final Class<T> targetClass);
}