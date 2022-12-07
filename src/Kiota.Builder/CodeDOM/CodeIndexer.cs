namespace Kiota.Builder.CodeDOM;
public class CodeIndexer : CodeTerminal, IDocumentedElement
{
    private CodeTypeBase indexType;
    public CodeTypeBase IndexType {get => indexType; set {
        EnsureElementsAreChildren(value);
        indexType = value;
    }}
    private CodeTypeBase returnType;
    public CodeTypeBase ReturnType {get => returnType; set {
        EnsureElementsAreChildren(value);
        returnType = value;
    }}
    public string SerializationName { get; set; }
    public string Description { get => Documentation.Description; set => Documentation.Description = value; }
    public CodeDocumentation Documentation { get; set; } = new();
    /// <summary>
    /// The Path segment to use for the method name when using back-compatible methods.
    /// </summary>
    public string PathSegment { get; set; }
}
