package com.microsoft.kiota.serialization;

import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.ValuedEnum;
import com.microsoft.kiota.serialization.Parsable;

import java.lang.Enum;
import java.lang.reflect.Field;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStreamWriter;
import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.EnumSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;
import java.util.function.BiConsumer;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.google.gson.stream.JsonWriter;

public class JsonSerializationWriter implements SerializationWriter {
    private final ByteArrayOutputStream stream = new ByteArrayOutputStream();
    private final JsonWriter writer;
    public JsonSerializationWriter() {
        this.writer = new JsonWriter(new OutputStreamWriter(this.stream));
    }
    public void writeStringValue(final String key, final String value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value);
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeBooleanValue(final String key, final Boolean value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value);
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeIntegerValue(final String key, final Integer value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value);
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeFloatValue(final String key, final Float value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value);
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeDoubleValue(final String key, final Double value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value);
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeLongValue(final String key, final Long value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value);
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeUUIDValue(final String key, final UUID value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value.toString());
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public void writeOffsetDateTimeValue(final String key, final OffsetDateTime value) {
        if(value != null)
            try {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.value(value.format(DateTimeFormatter.ISO_ZONED_DATE_TIME));
            } catch (IOException ex) {
                throw new RuntimeException("could not serialize value", ex);
            }
    }
    public <T> void writeCollectionOfPrimitiveValues(final String key, final Iterable<T> values) {
        try {
            if(values != null) { //empty array is meaningful
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.beginArray();
                for (final T t : values) {
                    this.writeAnyValue(null, t);
                }
                writer.endArray();
            }
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
    }
    public <T extends Parsable> void writeCollectionOfObjectValues(final String key, final Iterable<T> values) {
        try {
            if(values != null) { //empty array is meaningful
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.beginArray();
                for (final T t : values) {
                    this.writeObjectValue(null, t);
                }
                writer.endArray();
            }
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
    }
    public <T extends Enum<T>> void writeCollectionOfEnumValues(@Nullable final String key, @Nullable final Iterable<T> values) {
        try {
            if(values != null) { //empty array is meaningful
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                writer.beginArray();
                for (final T t : values) {
                    this.writeEnumValue(null, t);
                }
                writer.endArray();
            }
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
    }
    public <T extends Parsable> void writeObjectValue(final String key, final T value) {
        try {
            if(value != null) {
                if(key != null && !key.isEmpty()) {
                    writer.name(key);
                }
                if(onBeforeObjectSerialization != null) {
                    onBeforeObjectSerialization.accept(value);
                }
                writer.beginObject();
                if(onStartObjectSerialization != null) {
                    onStartObjectSerialization.accept(value, this);
                }
                value.serialize(this);
                writer.endObject();
                if(onAfterObjectSerialization != null) {
                    onAfterObjectSerialization.accept(value);
                }
            }
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
    }
    public <T extends Enum<T>> void writeEnumSetValue(@Nullable final String key, @Nullable final EnumSet<T> values) {
        if(values != null && !values.isEmpty()) {
            final Optional<String> concatenatedValue = values.stream().map(v -> this.getStringValueFromValuedEnum(v)).reduce((x, y) -> { return x + "," + y; });
            if(concatenatedValue.isPresent()) {
                this.writeStringValue(key, concatenatedValue.get());
            }
        }
    }
    public <T extends Enum<T>> void writeEnumValue(@Nullable final String key, @Nullable final T value) {
        if(value != null) {
            this.writeStringValue(key, getStringValueFromValuedEnum(value));
        }
    }
    public void writeNullValue(@Nullable final String key) {
        try {
            if(key != null && !key.isEmpty()) {
                writer.name(key);
            }
            writer.nullValue();
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
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
        if(value == null) return;
        for(final Map.Entry<String, Object> dataValue : value.entrySet()) {
            this.writeAnyValue(dataValue.getKey(), dataValue.getValue());
        }
    }
    private void writeNonParsableObject(final String key, final Object value) {
        try {
            if(key != null && !key.isEmpty())
                this.writer.name(key);
            if(value == null)
                this.writer.nullValue();
            else {
                final Class<?> valueClass = value.getClass();
                for(final Field oProp : valueClass.getFields())
                    this.writeAnyValue(oProp.getName(), oProp.get(value));
            }
        } catch (IOException | IllegalAccessException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
    }
    private void writeAnyValue(final String key, final Object value) {
        if(value == null) {
            this.writeNullValue(key);
        } else {
            final Class<?> valueClass = value.getClass();
            if(valueClass.equals(String.class))
                this.writeStringValue(key, (String)value);
            else if(valueClass.equals(Boolean.class))
                this.writeBooleanValue(key, (Boolean)value);
            else if(valueClass.equals(Float.class))
                this.writeFloatValue(key, (Float)value);
            else if(valueClass.equals(Long.class))
                this.writeLongValue(key, (Long)value);
            else if(valueClass.equals(Integer.class))
                this.writeIntegerValue(key, (Integer)value);
            else if(valueClass.equals(UUID.class))
                this.writeUUIDValue(key, (UUID)value);
            else if(valueClass.equals(OffsetDateTime.class))
                this.writeOffsetDateTimeValue(key, (OffsetDateTime)value);
            else if(value instanceof Iterable<?>)
                this.writeCollectionOfPrimitiveValues(key, (Iterable<?>)value);
            else if(!valueClass.isPrimitive())
                this.writeNonParsableObject(key, value);
            else
                throw new RuntimeException("unknown type to serialize " + valueClass.getName());
        }
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
