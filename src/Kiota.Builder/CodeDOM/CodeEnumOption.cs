using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeEnumOption : IDocumentedElement, ITypeDefinition, IAlternativeName, IDeprecableElement
{
    /// <inheritdoc/>
    public string SerializationName { get; set; } = string.Empty;
    public CodeDocumentation Documentation { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public CodeElement? Parent
    {
        get; set;
    }
    /// <inheritdoc/>
    public bool IsNameEscaped
    {
        get => !string.IsNullOrEmpty(SerializationName);
    }
    /// <inheritdoc/>
    public string WireName => IsNameEscaped ? SerializationName : Name;
    /// <inheritdoc/>
    public string SymbolName
    {
        get => IsNameEscaped ? SerializationName.CleanupSymbolName() : Name;
    }
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
}
