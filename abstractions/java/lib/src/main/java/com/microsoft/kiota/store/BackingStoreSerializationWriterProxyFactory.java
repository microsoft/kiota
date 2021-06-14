package com.microsoft.kiota.store;

import com.microsoft.kiota.serialization.SerializationWriterFactory;
import com.microsoft.kiota.serialization.SerializationWriterProxyFactory;

import javax.annotation.Nonnull;

public class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
    public BackingStoreSerializationWriterProxyFactory(@Nonnull final SerializationWriterFactory concrete) {
        super(concrete,
        (x) -> {
            if(x instanceof BackedModel) {
                final BackedModel backedModel = (BackedModel)x;
                if(backedModel.getBackingStore() != null) {
                    backedModel.getBackingStore().setReturnOnlyChangedValues(true);
                }
            }
        },(x) -> {
            if(x instanceof BackedModel) {
                final BackedModel backedModel = (BackedModel)x;
                if(backedModel.getBackingStore() != null) {
                    backedModel.getBackingStore().setReturnOnlyChangedValues(false);
                    backedModel.getBackingStore().setIsInitializationCompleted(true);
                }
            }
        }); 
    }
}