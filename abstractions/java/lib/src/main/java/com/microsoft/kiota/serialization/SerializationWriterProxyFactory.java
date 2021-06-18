package com.microsoft.kiota.serialization;

import java.util.function.Consumer;
import java.util.Objects;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {
    private final SerializationWriterFactory _concrete;
    private final Consumer<Parsable> _onBefore;
    private final Consumer<Parsable> _onAfter;
    public SerializationWriterProxyFactory(@Nonnull final SerializationWriterFactory concrete,
        @Nullable final Consumer<Parsable> onBeforeSerialization, @Nullable final Consumer<Parsable> onAfterSerialization) {
        _concrete = Objects.requireNonNull(concrete);
        _onBefore = onBeforeSerialization;
        _onAfter = onAfterSerialization;
    }
    public SerializationWriter getSerializationWriter(final String contentType) {
        final SerializationWriter writer = _concrete.getSerializationWriter(contentType);
        final Consumer<Parsable> originalBefore = writer.getOnBeforeObjectSerialization();
        final Consumer<Parsable> originalAfter = writer.getOnAfterObjectSerialization();
        writer.setOnBeforeObjectSerialization((x) -> {
            if(_onBefore != null) {
                _onBefore.accept(x); // the callback set by the implementation (e.g. backing store)
            }
            if(originalBefore != null) {
                originalBefore.accept(x); // some callback that might already be set on the target
            }
        });
        writer.setOnAfterObjectSerialization((x) -> {
            if(_onAfter != null) {
                _onAfter.accept(x);
            }
            if(originalAfter != null) {
                originalAfter.accept(x);
            }
        });
        return writer;
    }

}