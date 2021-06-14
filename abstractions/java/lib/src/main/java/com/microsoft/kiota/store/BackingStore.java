package com.microsoft.kiota.store;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import java.util.Map;

import com.microsoft.kiota.TriConsumer;

public interface BackingStore {
    @Nullable
    <T> T get(@Nonnull final String key);
    <T> void set(@Nonnull final String key, @Nullable final T value);
    @Nonnull
    Map<String, Object> enumerate();
    @Nonnull
    String subscribe(@Nonnull final TriConsumer<String, Object, Object> callback);
    void unsubscribe(@Nonnull final String subscriptionId);
    void clear();
    void setIsInitializationCompleted(final boolean value);
    boolean getIsInitializationCompleted();
    void setReturnOnlyChangedValues(final boolean value);
    boolean getReturnOnlyChangedValues();
}