package com.microsoft.kiota.serialization;

import java.util.Map;
import java.util.function.BiConsumer;

import javax.annotation.Nonnull;

public interface Parsable {
    // the generic type is on the method to avoid multiple generic interface implementation which is impossible in Java
    @Nonnull
    <T> Map<String, BiConsumer<T, ParseNode>> getDeserializeFields();
    void serialize(@Nonnull final SerializationWriter writer);
}
