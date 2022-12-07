using System;

namespace Kiota.Builder.CodeDOM;
public interface IDocumentedElement {
    [Obsolete("Use the Documentation property instead.")]
    string Description {get; set;}
    CodeDocumentation Documentation {get; set;}
}
