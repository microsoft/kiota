using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Kiota.Abstractions.Store {
    public class BackingStoreSerializationWriterProxy : SerializationWriterProxy {
        public BackingStoreSerializationWriterProxy(ISerializationWriter concrete) : base(
            concrete,
            (x) => {
                if(x is IBackedModel backedModel && backedModel.BackingStore != null)
                    backedModel.BackingStore.ReturnOnlyChangedValues = true;
            },(x) => {
                if(x is IBackedModel backedModel && backedModel.BackingStore != null) {
                    backedModel.BackingStore.ReturnOnlyChangedValues = false;
                    backedModel.BackingStore.InitilizationCompleted = true;
                }
            }) {}
    }
}
