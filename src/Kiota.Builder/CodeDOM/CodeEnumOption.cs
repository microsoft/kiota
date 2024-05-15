using System.Text.Json.Serialization;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeEnumOption : CodeElement, IDocumentedElement, ITypeDefinition, IAlternativeName
{
    /// <inheritdoc/>
    public string SerializationName { get; set; } = string.Empty;
    [JsonIgnore]
    public CodeDocumentation Documentation { get; set; } = new();
    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsNameEscaped
    {
        get => !string.IsNullOrEmpty(SerializationName);
    }
    /// <inheritdoc/>
    [JsonIgnore]
    public string WireName => IsNameEscaped ? SerializationName : Name;
    /// <inheritdoc/>
    [JsonIgnore]
    public string SymbolName
    {
        get => IsNameEscaped ? SerializationName.CleanupSymbolName() : Name;
    }
}
