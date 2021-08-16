package com.microsoft.kiota.store;

import com.microsoft.kiota.serialization.SerializationWriterFactory;
import com.microsoft.kiota.serialization.SerializationWriterProxyFactory;

import javax.annotation.Nonnull;
/**Proxy implementation of SerializationWriterFactory for the backing store that automatically sets the state of the backing store when serializing. */
public class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
    /**
     * Initializes a new instance of the BackingStoreSerializationWriterProxyFactory class given a concrete implementation of SerializationWriterFactory.
     * @param concrete a concrete implementation of SerializationWriterFactory to wrap.
     */
    public BackingStoreSerializationWriterProxyFactory(@Nonnull final SerializationWriterFactory concrete) {
        super(concrete,
        (x) -> {
            if(x instanceof BackedModel) {
                final BackedModel backedModel = (BackedModel)x;
                final var backingStore = backedModel.getBackingStore();
                if(backingStore != null) {
                    backingStore.setReturnOnlyChangedValues(true);
                }
            }
        },(x) -> {
            if(x instanceof BackedModel) {
                final BackedModel backedModel = (BackedModel)x;
                final var backingStore = backedModel.getBackingStore();
                if(backingStore != null) {
                    backingStore.setReturnOnlyChangedValues(false);
                    backingStore.setIsInitializationCompleted(true);
                }
            }
        }, (x, y) -> {
            if(x instanceof BackedModel) {
                final BackedModel backedModel = (BackedModel)x;
                final var backingStore = backedModel.getBackingStore();
                if(backingStore != null) {
                    final var keys = backingStore.enumerateKeysForValuesChangedToNull();
                    for(final var key : keys) {
                        y.writeNullValue(key);
                    }
                }
            }
        }); 
    }
}