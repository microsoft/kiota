import { ParseNodeFactory, ParseNodeProxyFactory } from "../serialization";
import { BackedModel } from "./backedModel";

export class BackingStoreParseNodeFactory extends ParseNodeProxyFactory {
    public constructor(concrete: ParseNodeFactory) {
        super(concrete, 
            value => {
                const backedModel = value as unknown as BackedModel;
                if(backedModel && backedModel.backingStore)
                    backedModel.backingStore.initializationCompleted = false;
            }, 
            value => {
                const backedModel = value as unknown as BackedModel;
                if(backedModel && backedModel.backingStore)
                    backedModel.backingStore.initializationCompleted = true;
            });
    }
}