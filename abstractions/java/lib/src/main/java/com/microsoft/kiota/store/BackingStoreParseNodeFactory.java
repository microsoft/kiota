package com.microsoft.kiota.store;

import javax.annotation.Nonnull;

import com.microsoft.kiota.serialization.ParseNodeProxyFactory;
import com.microsoft.kiota.serialization.ParseNodeFactory;

public class BackingStoreParseNodeFactory extends ParseNodeProxyFactory {
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