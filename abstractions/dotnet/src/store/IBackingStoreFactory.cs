namespace Microsoft.Kiota.Abstractions.Store {
    /// <summary>
    ///     Defines the contract for a factory that creates backing stores.
    /// </summary>
    public interface IBackingStoreFactory {
        /// <summary>
        ///     Creates a new instance of the backing store.
        /// </summary>
        /// <returns>A new instance of the backing store.</returns>
        IBackingStore CreateBackingStore();
    }
}
