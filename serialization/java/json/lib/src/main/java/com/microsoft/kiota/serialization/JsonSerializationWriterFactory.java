package com.microsoft.kiota.serialization;

import java.util.Objects;

import javax.annotation.Nonnull;

import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.SerializationWriterFactory;

public class JsonSerializationWriterFactory implements SerializationWriterFactory {
    public String getValidContentType() {
        return validContentType;
    }
    private static final String validContentType = "application/json";
    @Override
    @Nonnull
    public SerializationWriter getSerializationWriter(@Nonnull final String contentType) {
        Objects.requireNonNull(contentType, "parameter contentType cannot be null");
        if(contentType.isEmpty()) {
            throw new NullPointerException("contentType cannot be empty");
        } else if (!contentType.equals(validContentType)) {
            throw new IllegalArgumentException("expected a " + validContentType + " content type");
        }
        return new JsonSerializationWriter();
    }
}
