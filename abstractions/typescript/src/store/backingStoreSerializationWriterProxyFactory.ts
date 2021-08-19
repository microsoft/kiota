import { SerializationWriterFactory, SerializationWriterProxyFactory } from "../serialization";
import { BackedModel } from "./backedModel";

/**Proxy implementation of SerializationWriterFactory for the backing store that automatically sets the state of the backing store when serializing. */
export class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
    /**
     * Initializes a new instance of the BackingStoreSerializationWriterProxyFactory class given a concrete implementation of SerializationWriterFactory.
     * @param concrete a concrete implementation of SerializationWriterFactory to wrap.
     */
    public constructor(concrete: SerializationWriterFactory) {
        super(concrete,
            value => {
                const backedModel = value as unknown as BackedModel;
                if(backedModel && backedModel.backingStore)
                    backedModel.backingStore.returnOnlyChangedValues = true;
            }, 
            value => {
                const backedModel = value as unknown as BackedModel;
                if(backedModel && backedModel.backingStore) {
                    backedModel.backingStore.returnOnlyChangedValues = false;
                    backedModel.backingStore.initializationCompleted = true;
                }
            },
            (value, writer) => {
                const backedModel = value as unknown as BackedModel;
                if(backedModel && backedModel.backingStore) {
                    const keys = backedModel.backingStore.enumerateKeysForValuesChangedToNull();
                    for(const key of keys)
                        writer.writeNullValue(key);
                }
            });
    }
}