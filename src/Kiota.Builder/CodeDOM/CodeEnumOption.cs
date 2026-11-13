using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeEnumOption : CodeElement, IDocumentedElement, ITypeDefinition, IAlternativeName
{
    /// <inheritdoc/>
    public string SerializationName
    {
        get => serializationName;
        set
        {
            serializationName = value ?? string.Empty;
            hasSerializationName = true;
        }
    }
    private string serializationName = string.Empty;
    private bool hasSerializationName;
    public CodeDocumentation Documentation { get; set; } = new();
    /// <inheritdoc/>
    public bool IsNameEscaped
    {
        get => hasSerializationName;
    }
    /// <inheritdoc/>
    public string WireName => IsNameEscaped ? SerializationName : Name;
    /// <inheritdoc/>
    public string SymbolName
    {
        get => IsNameEscaped && !string.IsNullOrEmpty(SerializationName) ?
            SerializationName.CleanupSymbolName() :
            Name;
    }
}
