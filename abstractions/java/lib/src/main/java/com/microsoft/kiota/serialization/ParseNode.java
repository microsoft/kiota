package com.microsoft.kiota.serialization;

import java.lang.Enum;
import java.time.OffsetDateTime;
import java.util.EnumSet;
import java.util.List;
import java.util.UUID;
import java.util.function.Consumer;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * Interface for a deserialization node in a parse tree. This interace provides an abstraction layer over serialiation formats, libararies and implementations.
 */
public interface ParseNode {
    /**
     * Gets a new parse node for the given identifier.
     * @param identitier the identifier of the current node property.
     * @return a new parse node for the given identifier.
     */
    @Nonnull
    ParseNode getChildNode(@Nonnull final String identifier);
    /**
     * Gets the string value of the node.
     * @return the string value of the node.
     */
    @Nonnull
    String getStringValue();
    /**
     * Gets the boolean value of the node.
     * @return the boolean value of the node.
     */
    @Nonnull
    Boolean getBooleanValue();
    /**
     * Gets the Integer value of the node.
     * @return the Integer value of the node.
     */
    @Nonnull
    Integer getIntegerValue();
    /**
     * Gets the Float value of the node.
     * @return the Float value of the node.
     */
    @Nonnull
    Float getFloatValue();
    /**
     * Gets the Double value of the node.
     * @return the Double value of the node.
     */
    @Nonnull
    Double getDoubleValue();
    /**
     * Gets the Long value of the node.
     * @return the Long value of the node.
     */
    @Nonnull
    Long getLongValue();
    /**
     * Gets the UUID value of the node.
     * @return the UUID value of the node.
     */
    @Nonnull
    UUID getUUIDValue();
    /**
     * Gets the OffsetDateTime value of the node.
     * @return the OffsetDateTime value of the node.
     */
    @Nonnull
    OffsetDateTime getOffsetDateTimeValue();
    /**
     * Gets the Enum value of the node.
     * @return the Enum value of the node.
     */
    @Nullable
    <T extends Enum<T>> T getEnumValue(@Nonnull final Class<T> targetEnum);
    /**
     * Gets the EnumSet value of the node.
     * @return the EnumSet value of the node.
     */
    @Nullable
    <T extends Enum<T>> EnumSet<T> getEnumSetValue(@Nonnull final Class<T> targetEnum);
    /**
     * Gets the collection of primitive values of the node.
     * @return the collection of primitive values of the node.
     */
    @Nonnull
    <T> List<T> getCollectionOfPrimitiveValues(@Nonnull final Class<T> targetClass);
    /**
     * Gets the collection of object values of the node.
     * @return the collection of object values of the node.
     */
    @Nonnull
    <T extends Parsable> List<T> getCollectionOfObjectValues(@Nonnull final Class<T> targetClass);
    /**
     * Gets the model object value of the node.
     * @return the model object value of the node.
     */
    @Nonnull
    <T extends Parsable> T getObjectValue(@Nonnull final Class<T> targetClass);
    /**
     * Gets the callback called before the node is deserialized.
     * @return the callback called before the node is deserialized.
     */
    @Nullable
    Consumer<Parsable> getOnBeforeAssignFieldValues();
    /**
     * Gets the callback called after the node is deseserialized.
     * @return the callback called after the node is deserialized.
     */
    @Nullable
    Consumer<Parsable> getOnAfterAssignFieldValues();
    /**
     * Sets the callback called before the node is deserialized.
     * @param value the callback called before the node is deserialized.
     */
    void setOnBeforeAssignFieldValues(@Nullable final Consumer<Parsable> value);
    /**
     * Sets the callback called after the node is deserialized.
     * @param value the callback called after the node is deserialized.
     */
    void setOnAfterAssignFieldValues(@Nullable final Consumer<Parsable> value);
}