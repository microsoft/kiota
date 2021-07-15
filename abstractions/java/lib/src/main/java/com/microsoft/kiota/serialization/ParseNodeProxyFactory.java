package com.microsoft.kiota.serialization;

import java.util.function.Consumer;
import java.util.Objects;

import java.io.InputStream;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** Proxy factory that allows the composition of before and after callbacks on existing factories. */
public abstract class ParseNodeProxyFactory implements ParseNodeFactory {
    public String getValidContentType() {
        return _concrete.getValidContentType();
    }
    private final ParseNodeFactory _concrete;
    private final Consumer<Parsable> _onBefore;
    private final Consumer<Parsable> _onAfter;
    /**
     * Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
     * @param concreteFactory the concrete factory to wrap
     * @param onBefore the callback to invoke before the deserialization of any model object.
     * @param onAfter the callback to invoke after the deserialization of any model object.
     */
    public ParseNodeProxyFactory(@Nonnull final ParseNodeFactory concrete,
        @Nullable final Consumer<Parsable> onBefore,
        @Nullable final Consumer<Parsable> onAfter) {
            _concrete = Objects.requireNonNull(concrete);
            _onBefore = onBefore;
            _onAfter = onAfter;
        }
    public ParseNode getParseNode(final String contentType, final InputStream rawResponse) {
        final ParseNode node = _concrete.getParseNode(contentType, rawResponse);
        final Consumer<Parsable> originalOnBefore = node.getOnBeforeAssignFieldValues();
        final Consumer<Parsable> originalOnAfter = node.getOnAfterAssignFieldValues();
        node.setOnBeforeAssignFieldValues((x) -> {
            if(this._onBefore != null) {
                this._onBefore.accept(x);
            }
            if(originalOnBefore != null) {
                originalOnBefore.accept(x);
            }
        });
        node.setOnAfterAssignFieldValues((x) -> {
            if(this._onAfter != null) {
                this._onAfter.accept(x);
            }
            if(originalOnAfter != null) {
                originalOnAfter.accept(x);
            }
        });
        return node;
    }
}