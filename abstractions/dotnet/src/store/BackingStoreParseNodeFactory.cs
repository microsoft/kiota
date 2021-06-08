using Microsoft.Kiota.Abstractions.Serialization;
namespace Microsoft.Kiota.Abstractions.Store {
    public class BackingStoreParseNodeFactory : ParseNodeProxyFactory {
        public BackingStoreParseNodeFactory(IParseNodeFactory concrete):base(
            concrete,
            (x) => {
                if(x is IBackedModel backedModel && backedModel.BackingStore != null)
                    backedModel.BackingStore.InitilizationCompleted = false;
            },
            (x) => {
                if(x is IBackedModel backedModel && backedModel.BackingStore != null)
                    backedModel.BackingStore.InitilizationCompleted = true;
            }
        ) { }
    }
}
