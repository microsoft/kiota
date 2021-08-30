// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

namespace Microsoft.Kiota.Abstractions.Store
{
    /// <summary>
    ///     This class is used to create instances of <see cref="InMemoryBackingStore" />.
    /// </summary>
    public class InMemoryBackingStoreFactory : IBackingStoreFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="IBackingStore"/>
        /// </summary>
        /// <returns></returns>
        public IBackingStore CreateBackingStore()
        {
            return new InMemoryBackingStore();
        }
    }
}
