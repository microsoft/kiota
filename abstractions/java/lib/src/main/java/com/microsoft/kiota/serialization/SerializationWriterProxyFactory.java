package com.microsoft.kiota.serialization;

import java.util.function.Consumer;
import java.util.function.BiConsumer;
import java.util.Objects;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** Proxy factory that allows the composition of before and after callbacks on existing factories. */
public abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {
    public String getValidContentType() {
        return _concrete.getValidContentType();
    }
    private final SerializationWriterFactory _concrete;
    private final Consumer<Parsable> _onBefore;
    private final Consumer<Parsable> _onAfter;
    private final BiConsumer<Parsable, SerializationWriter> _onStart;
    /**
     * Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
     * @param concreteFactory the concrete factory to wrap
     * @param onBeforeSerialization the callback to invoke before the serialization of any model object.
     * @param onAfterSerialization the callback to invoke after the serialization of any model object.
     * @param onStartSerialization the callback to invoke when the serialization of a model object starts.
     */
    public SerializationWriterProxyFactory(@Nonnull final SerializationWriterFactory concrete,
        @Nullable final Consumer<Parsable> onBeforeSerialization,
        @Nullable final Consumer<Parsable> onAfterSerialization,
        @Nullable final BiConsumer<Parsable, SerializationWriter> onStartObjectSerialization) {
        _concrete = Objects.requireNonNull(concrete);
        _onBefore = onBeforeSerialization;
        _onAfter = onAfterSerialization;
        _onStart = onStartObjectSerialization;
    }
    public SerializationWriter getSerializationWriter(final String contentType) {
        final SerializationWriter writer = _concrete.getSerializationWriter(contentType);
        final Consumer<Parsable> originalBefore = writer.getOnBeforeObjectSerialization();
        final Consumer<Parsable> originalAfter = writer.getOnAfterObjectSerialization();
        final BiConsumer<Parsable, SerializationWriter> originalStart = writer.getOnStartObjectSerialization();
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
        writer.setOnStartObjectSerialization((x, y) -> {
            if(_onStart != null) {
                _onStart.accept(x, y);
            }
            if(originalStart != null) {
                originalStart.accept(x, y);
            }
        });
        return writer;
    }

}