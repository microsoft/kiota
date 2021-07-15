namespace Microsoft.Kiota.Abstractions.Store {
    /// <summary>
    ///     Defines the contracts for a model that is backed by a store.
    /// </summary>
    public interface IBackedModel {
        /// <summary>
        ///     Gets the store that is backing the model.
        /// </summary>
        IBackingStore BackingStore { get; }
    }
}
