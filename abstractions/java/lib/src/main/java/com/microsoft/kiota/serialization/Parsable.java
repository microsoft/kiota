package com.microsoft.kiota.serialization;

import java.util.Map;
import java.util.function.BiConsumer;

import javax.annotation.Nonnull;

public interface Parsable {
    @Nonnull
    <T> Map<String, BiConsumer<T, ParseNode>> getFieldDeserializers();
    void serialize(@Nonnull final SerializationWriter writer);
    @Nonnull
    Map<String, Object> getAdditionalData();
}
