import { SerializationWriterFactory, SerializationWriterProxyFactory } from "../serialization";
import { BackedModel } from "./backedModel";

export class BackingStoreSerializationWriterProxyFactory extends SerializationWriterProxyFactory {
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
            });
    }
}