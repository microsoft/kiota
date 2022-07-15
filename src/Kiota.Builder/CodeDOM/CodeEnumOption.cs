namespace Kiota.Builder;

public class CodeEnumOption : IDocumentedElement, ITypeDefinition
{
    public string SerializationName { get; set; }
    public string Description { get; set; }
    public string Name { get; set; }
    public CodeElement Parent { get; set; }
}
