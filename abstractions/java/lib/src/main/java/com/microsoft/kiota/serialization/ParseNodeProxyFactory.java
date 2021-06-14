package com.microsoft.kiota.serialization;

import java.util.function.Consumer;
import java.util.Objects;

import java.io.InputStream;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public abstract class ParseNodeProxyFactory implements ParseNodeFactory {
    private final ParseNodeFactory _concrete;
    private final Consumer<Parsable> _onBefore;
    private final Consumer<Parsable> _onAfter;
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