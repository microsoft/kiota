package com.microsoft.kiota.serialization;

import com.google.common.collect.Lists;

import java.lang.UnsupportedOperationException;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalTime;
import java.time.OffsetDateTime;
import java.time.Period;
import java.util.Base64;
import java.util.EnumSet;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.AdditionalDataHolder;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public class TextParseNode implements ParseNode {
    private final String text;
    private final static String NoStructuredDataMessage = "text does not support structured data";
    public TextParseNode(@Nonnull final String rawJson) {
        Objects.requireNonNull(rawJson, "parameter node cannot be null");
        text = rawJson.startsWith("\"") && rawJson.endsWith("\"") ? rawJson.substring(1, text.length() - 2) : rawJson;
    }
    public ParseNode getChildNode(final String identifier) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public String getStringValue() {
        return text;
    }
    public Boolean getBooleanValue() {
        return Boolean.parseBoolean(text);
    }
    public Byte getByteValue() {
        return Byte.parseByte(text);
    }
    public Short getShortValue() {
        return Short.parseShort(text);
    }
    public BigDecimal getBigDecimalValue() {
        return new BigDecimal(text);
    }
    public Integer getIntegerValue() {
        return Integer.parseInt(text);
    }
    public Float getFloatValue() {
        return Float.parseFloat(text);
    }
    public Double getDoubleValue() {
        return Double.parseDouble(text);
    }
    public Long getLongValue() {
        return Long.parseLong(text);
    }
    public UUID getUUIDValue() {
        return UUID.fromString(this.getStringValue());
    }
    public OffsetDateTime getOffsetDateTimeValue() {
        return OffsetDateTime.parse(this.getStringValue());
    }
    public LocalDate getLocalDateValue() {
        return LocalDate.parse(this.getStringValue());
    }
    public LocalTime getLocalTimeValue() {
        return LocalTime.parse(this.getStringValue());
    }
    public Period getPeriodValue() {
        return Period.parse(this.getStringValue());
    }
    public <T> List<T> getCollectionOfPrimitiveValues(final Class<T> targetClass) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Parsable> List<T> getCollectionOfObjectValues(@Nonnull final ParsableFactory<T> factory) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Enum<T>> List<T> getCollectionOfEnumValues(@Nonnull final Class<T> targetEnum) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Parsable> T getObjectValue(@Nonnull final ParsableFactory<T> factory) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    @Nullable
    public <T extends Enum<T>> T getEnumValue(@Nonnull final Class<T> targetEnum) {
        final String rawValue = this.getStringValue();
        if(rawValue == null || rawValue.isEmpty()) {
            return null;
        }
        return getEnumValueInt(rawValue, targetEnum);
    }
    @SuppressWarnings("unchecked")
    private <T extends Enum<T>> T getEnumValueInt(@Nonnull final String rawValue, @Nonnull final Class<T> targetEnum) {
        try {
            return (T)targetEnum.getMethod("forValue", String.class).invoke(null, rawValue);
        } catch (Exception ex) {
            return null;
        }
    }
    @Nullable
    public <T extends Enum<T>> EnumSet<T> getEnumSetValue(@Nonnull final Class<T> targetEnum) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public Consumer<Parsable> getOnBeforeAssignFieldValues() {
        return this.onBeforeAssignFieldValues;
    }
    public Consumer<Parsable> getOnAfterAssignFieldValues() {
        return this.onAfterAssignFieldValues;
    }
    private Consumer<Parsable> onBeforeAssignFieldValues;
    public void setOnBeforeAssignFieldValues(final Consumer<Parsable> value) {
        this.onBeforeAssignFieldValues = value;
    }
    private Consumer<Parsable> onAfterAssignFieldValues;
    public void setOnAfterAssignFieldValues(final Consumer<Parsable> value) {
        this.onAfterAssignFieldValues = value;
    }
    public byte[] getByteArrayValue() {
        final var base64 = this.getStringValue();
        if(base64 == null || base64.isEmpty()) {
            return null;
        }
        return Base64.getDecoder().decode(base64);
    }
}
