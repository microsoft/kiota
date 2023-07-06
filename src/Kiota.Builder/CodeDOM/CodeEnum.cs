using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;
#pragma warning disable CA1711
public class CodeEnum : CodeBlock<BlockDeclaration, BlockEnd>, IDocumentedElement, ITypeDefinition, IDeprecableElement
{
#pragma warning restore CA2227
    public bool Flags
    {
        get; set;
    }
    public CodeDocumentation Documentation { get; set; } = new();

    public void AddOption(params CodeEnumOption[] codeEnumOptions)
    {
        if (codeEnumOptions is null) return;
        EnsureElementsAreChildren(codeEnumOptions);
        AddRange(codeEnumOptions);
    }
    public IEnumerable<CodeEnumOption> Options => InnerChildElements.Values.OfType<CodeEnumOption>().OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase);
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
}
