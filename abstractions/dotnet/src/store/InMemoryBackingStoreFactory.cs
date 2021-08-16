namespace Microsoft.Kiota.Abstractions.Store {
    /// <summary>
    ///     This class is used to create instances of <see cref="InMemoryBackingStore" />.
    /// </summary>
    public class InMemoryBackingStoreFactory : IBackingStoreFactory {
        public IBackingStore CreateBackingStore() {
            return new InMemoryBackingStore();
        }
    }
}
