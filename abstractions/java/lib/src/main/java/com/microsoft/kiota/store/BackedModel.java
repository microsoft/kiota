package com.microsoft.kiota.store;

import javax.annotation.Nonnull;

/** Defines the contracts for a model that is backed by a store. */
public interface BackedModel {
    /**
     * Gets the store that is backing the model.
     * @return the backing store.
     */
    @Nonnull
    BackingStore getBackingStore();
}