// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions.Store
{
    /// <summary>
    /// Proxy implementation of <see cref="IParseNodeFactory"/> for the <see cref="IBackingStore">backing store</see> that automatically sets the state of the backing store when deserializing.
    /// </summary>
    public class BackingStoreParseNodeFactory : ParseNodeProxyFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackingStoreParseNodeFactory"/> class given a concrete implementation of <see cref="IParseNodeFactory"/>.
        /// </summary>
        public BackingStoreParseNodeFactory(IParseNodeFactory concrete) : base(
            concrete,
            (x) =>
            {
                if(x is IBackedModel backedModel && backedModel.BackingStore != null)
                    backedModel.BackingStore.InitializationCompleted = false;
            },
            (x) =>
            {
                if(x is IBackedModel backedModel && backedModel.BackingStore != null)
                    backedModel.BackingStore.InitializationCompleted = true;
            }
        )
        { }
    }
}
