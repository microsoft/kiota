namespace Microsoft.Kiota.Abstractions.Store {
    public interface IBackedModel {
        IBackingStore BackingStore { get; }
    }
}
