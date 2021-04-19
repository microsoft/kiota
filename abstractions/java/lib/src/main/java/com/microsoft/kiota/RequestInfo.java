package com.microsoft.kiota;

import java.net.URI;
import java.io.IOException;
import java.io.InputStream;
import java.util.HashMap;
import java.util.Objects;
import java.util.function.Function;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.SerializationWriter;

public class RequestInfo {
    @Nullable
    public URI uri;
    @Nullable
    public HttpMethod httpMethod;
    @Nonnull
    public HashMap<String, Object> queryParameters = new HashMap<>(); //TODO case insensitive
    @Nonnull
    public HashMap<String, String> headers = new HashMap<>(); // TODO case insensitive
    @Nullable
    public InputStream content;
    private static String jsonContentType = "application/json";
    public <T extends Parsable> void setJsonContentFromParsable(@Nonnull final T value, @Nonnull final Function<String, SerializationWriter> serializerFactory) {
        Objects.requireNonNull(serializerFactory);
        Objects.requireNonNull(value);
        try(final SerializationWriter writer = serializerFactory.apply(jsonContentType)) {
            headers.put("Content-Type", jsonContentType);
            writer.writeObjectValue(null, value);
            this.content = writer.getSerializedContent();
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize payload", ex);
        }
    }
}
