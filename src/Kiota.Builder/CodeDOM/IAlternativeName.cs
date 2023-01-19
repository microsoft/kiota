namespace Kiota.Builder.CodeDOM;

/// <summary>
/// Defines a contract for elements that can have an alternative name for serialization.
/// </summary>
public interface IAlternativeName {
    /// <summary>
    /// Gets a value indicating whether the name is escaped/has an alternative for serialization.
    /// </summary>
    bool IsNameEscaped {get;}
    /// <summary>
    /// Gets the name to be used for serialization whether it is escaped or not.
    /// </summary>
    string WireName {get;}
    /// <summary>
    /// Gets or sets the name to be used for serialization.
    /// Language implementers: use WireName instead so you don't have to implement a comparison.
    /// </summary>
    string SerializationName { get; set;}
    /// <summary>
    /// Gets the symbol name of the element.
    /// </summary>
    string SymbolName { get; }
}
