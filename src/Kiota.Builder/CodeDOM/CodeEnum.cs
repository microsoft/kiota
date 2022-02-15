using System.Collections.Generic;

namespace Kiota.Builder;
public class CodeEnum : CodeBlock, IDocumentedElement, ITypeDefinition {
    public HashSet<string> Options { get; set; } = new ();
    public bool Flags { get; set; }
    public string Description {get; set;}
}
