// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

namespace Microsoft.Kiota.Abstractions.Store
{
    /// <summary>
    ///     Defines the contract for a factory that creates backing stores.
    /// </summary>
    public interface IBackingStoreFactory
    {
        /// <summary>
        ///     Creates a new instance of the backing store.
        /// </summary>
        /// <returns>A new instance of the backing store.</returns>
        IBackingStore CreateBackingStore();
    }
}
