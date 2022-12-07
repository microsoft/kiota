namespace Kiota.Builder.CodeDOM;

public class CodeEnumOption : IDocumentedElement, ITypeDefinition
{
    public string SerializationName { get; set; }
    public string Description { get => Documentation.Description; set => Documentation.Description = value; }
    public CodeDocumentation Documentation { get; set; } = new();
    public string Name { get; set; }
    public CodeElement Parent { get; set; }
}
