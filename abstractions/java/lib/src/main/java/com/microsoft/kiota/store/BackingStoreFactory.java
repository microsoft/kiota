package com.microsoft.kiota.store;

import javax.annotation.Nonnull;

/** Defines the contract for a factory that creates backing stores. */
public interface BackingStoreFactory {
    /** 
     * Creates a new instance of the backing store.
     * @return a new instance of the backing store.
     */
    @Nonnull
    BackingStore createBackingStore();
}