using System;

namespace Kiota.Builder.CodeDOM;
public class CodeIndexer : CodeTerminal, IDocumentedElement
{
#nullable disable // exposing property is required
    private CodeTypeBase indexType;
#nullable enable
    public required CodeTypeBase IndexType
    {
        get => indexType; set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            indexType = value;
        }
    }
#nullable disable // exposing property is required
    private CodeTypeBase returnType;
#nullable enable
    public required CodeTypeBase ReturnType
    {
        get => returnType; set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            returnType = value;
        }
    }
    public string SerializationName { get; set; } = string.Empty;
    public CodeDocumentation Documentation { get; set; } = new();
    /// <summary>
    /// The Path segment to use for the method name when using back-compatible methods.
    /// </summary>
    public string PathSegment { get; set; } = string.Empty;
}
