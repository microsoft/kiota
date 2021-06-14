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
                    if(backedModel.getBackingStore() != null) {
                        backedModel.getBackingStore().setIsInitializationCompleted(false);
                    }
                }
            },
            (x) -> {
                if(x instanceof BackedModel) {
                    final BackedModel backedModel = (BackedModel)x;
                    if(backedModel.getBackingStore() != null) {
                        backedModel.getBackingStore().setIsInitializationCompleted(true);
                    }
                }
            }
        ); 
    }
}