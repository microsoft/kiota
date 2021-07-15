package com.microsoft.kiota.store;

import javax.annotation.Nonnull;

import com.microsoft.kiota.serialization.ParseNodeProxyFactory;
import com.microsoft.kiota.serialization.ParseNodeFactory;
/** Proxy implementation of ParseNodeFactory for the backing store that automatically sets the state of the backing store when deserializing. */
public class BackingStoreParseNodeFactory extends ParseNodeProxyFactory {
    /**
     * Initializes a new instance of the BackingStoreParseNodeFactory class given the concrete implementation.
     * @param concrete the concrete implementation of the ParseNodeFactory
     */
    public BackingStoreParseNodeFactory(@Nonnull final ParseNodeFactory concrete) {
        super(
            concrete,
            (x) -> {
                if(x instanceof BackedModel) {
                    final BackedModel backedModel = (BackedModel)x;
                    final BackingStore backingStore = backedModel.getBackingStore();
                    if(backingStore != null) {
                        backingStore.setIsInitializationCompleted(false);
                    }
                }
            },
            (x) -> {
                if(x instanceof BackedModel) {
                    final BackedModel backedModel = (BackedModel)x;
                    final BackingStore backingStore = backedModel.getBackingStore();
                    if(backingStore != null) {
                        backingStore.setIsInitializationCompleted(true);
                    }
                }
            }
        ); 
    }
}