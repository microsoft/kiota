package com.microsoft.kiota.serialization;

import java.util.Map;
import java.util.function.BiConsumer;

import javax.annotation.Nonnull;

public interface Parsable<T> {
    @Nonnull
    Map<String, BiConsumer<T, ParseNode>> getDeserializeFields();
    void Serialize(@Nonnull final SerializationWriter writer);
}
