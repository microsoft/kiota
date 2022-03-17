package com.microsoft.kiota.serialization;

import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.ValuedEnum;
import com.microsoft.kiota.serialization.Parsable;

import java.lang.Enum;
import java.lang.reflect.Field;
import java.lang.UnsupportedOperationException;
import java.math.BigDecimal;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStreamWriter;
import java.time.LocalDate;
import java.time.LocalTime;
import java.time.OffsetDateTime;
import java.time.Period;
import java.time.format.DateTimeFormatter;
import java.util.Base64;
import java.util.EnumSet;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import java.util.function.Consumer;
import java.util.function.BiConsumer;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public class TextSerializationWriter implements SerializationWriter {
    private final static String NoStructuredDataMessage = "text does not support structured data";
    private final ByteArrayOutputStream stream = new ByteArrayOutputStream();
    private final OutputStreamWriter writer;
    private boolean written;
    public TextSerializationWriter() {
        this.writer = new OutputStreamWriter(this.stream);
    }
    public void writeStringValue(final String key, final String value) {
        if(key != null && !key.isEmpty())
            throw new UnsupportedOperationException(NoStructuredDataMessage);
        if(value != null && !value.isEmpty())
            if(written)
                throw new UnsupportedOperationException("a value was already written for this serialization writer, text content only supports a single value");
            else {
                written = true;
                try {
                    writer.write(value);
                } catch (IOException e) {
                    throw new RuntimeException(e);
                }
            }
    }
    public void writeBooleanValue(final String key, final Boolean value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeShortValue(final String key, final Short value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeByteValue(final String key, final Byte value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeBigDecimalValue(final String key, final BigDecimal value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeIntegerValue(final String key, final Integer value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeFloatValue(final String key, final Float value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeDoubleValue(final String key, final Double value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeLongValue(final String key, final Long value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeUUIDValue(final String key, final UUID value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public void writeOffsetDateTimeValue(final String key, final OffsetDateTime value) {
        if(value != null)
            writeStringValue(key, value.format(DateTimeFormatter.ISO_OFFSET_DATE_TIME));
    }
    public void writeLocalDateValue(final String key, final LocalDate value) {
        if(value != null)
            writeStringValue(key, value.format(DateTimeFormatter.ISO_LOCAL_DATE));
    }
    public void writeLocalTimeValue(final String key, final LocalTime value) {
        if(value != null)
            writeStringValue(key, value.format(DateTimeFormatter.ISO_LOCAL_TIME));
    }
    public void writePeriodValue(final String key, final Period value) {
        if(value != null)
            writeStringValue(key, value.toString());
    }
    public <T> void writeCollectionOfPrimitiveValues(final String key, final Iterable<T> values) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Parsable> void writeCollectionOfObjectValues(final String key, final Iterable<T> values) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Enum<T>> void writeCollectionOfEnumValues(@Nullable final String key, @Nullable final Iterable<T> values) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Parsable> void writeObjectValue(final String key, final T value) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Enum<T>> void writeEnumSetValue(@Nullable final String key, @Nullable final EnumSet<T> values) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public <T extends Enum<T>> void writeEnumValue(@Nullable final String key, @Nullable final T value) {
        if(value != null) {
            writeStringValue(key, getStringValueFromValuedEnum(value));
        }
    }
    public void writeNullValue(@Nullable final String key) {
        writeStringValue(null, "null");
    }
    private <T extends Enum<T>> String getStringValueFromValuedEnum(final T value) {
        if(value instanceof ValuedEnum) {
            final ValuedEnum valued = (ValuedEnum)value;
            return valued.getValue();
        } else return null;
    }
    public InputStream getSerializedContent() {
        try {
            this.writer.flush();
            return new ByteArrayInputStream(this.stream.toByteArray());
            //This copies the whole array in memory could result in memory pressure for large objects, we might want to replace by some kind of piping in the future
        } catch (IOException ex) {
            throw new RuntimeException(ex);
        }
    }
    public void close() throws IOException {
        this.writer.close();
        this.stream.close();
    }
    public void writeAdditionalData(@Nonnull final Map<String, Object> value) {
        throw new UnsupportedOperationException(NoStructuredDataMessage);
    }
    public Consumer<Parsable> getOnBeforeObjectSerialization() {
        return this.onBeforeObjectSerialization;
    }
    public Consumer<Parsable> getOnAfterObjectSerialization() {
        return this.onAfterObjectSerialization;
    }
    public BiConsumer<Parsable, SerializationWriter> getOnStartObjectSerialization() {
        return this.onStartObjectSerialization;
    }
    private Consumer<Parsable> onBeforeObjectSerialization;
    public void setOnBeforeObjectSerialization(final Consumer<Parsable> value) {
        this.onBeforeObjectSerialization = value;
    }
    private Consumer<Parsable> onAfterObjectSerialization;
    public void setOnAfterObjectSerialization(final Consumer<Parsable> value) {
        this.onAfterObjectSerialization = value;
    }
    private BiConsumer<Parsable, SerializationWriter> onStartObjectSerialization;
    public void setOnStartObjectSerialization(final BiConsumer<Parsable, SerializationWriter> value) {
        this.onStartObjectSerialization = value;
    }
    public void writeByteArrayValue(@Nullable final String key, @Nonnull final byte[] value) {
        if(value != null)
            this.writeStringValue(key, Base64.getEncoder().encodeToString(value));
    }
}
