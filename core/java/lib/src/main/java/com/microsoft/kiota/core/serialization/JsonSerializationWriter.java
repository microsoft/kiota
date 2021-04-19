package com.microsoft.kiota.core.serialization;

import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.Parsable;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStreamWriter;
import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

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
                    final Class<?> clazz = t.getClass();
                    if(clazz == Boolean.class) {
                        writer.value((Boolean)t);
                    } else if(clazz == String.class) {
                        writer.value((String)t);
                    } else if(clazz == Float.class) {
                        writer.value((Float)t);
                    } else if(clazz == Long.class) {
                        writer.value((Long)t);
                    } else if(clazz == Integer.class) {
                        writer.value((Integer)t);
                    } else if(clazz == UUID.class) {
                        writeUUIDValue(null, (UUID)t);
                    } else if(clazz == OffsetDateTime.class) {
                        writeOffsetDateTimeValue(null, (OffsetDateTime)t);
                    } else {
                        throw new RuntimeException("unknown type to serialize " + clazz.getName());
                    }
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
                    writer.beginObject();
                    t.serialize(this);
                    writer.endObject();
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
                writer.beginObject();
                value.serialize(this);
                writer.endObject();
            }
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize value", ex);
        }
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
}
