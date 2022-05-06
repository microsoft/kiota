using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Kiota.Builder;
public class CodeEnum : CodeBlock<BlockDeclaration, BlockEnd>, IDocumentedElement, ITypeDefinition {
    private ConcurrentDictionary<string, CodeEnumOption> OptionsInternal { get; set; } = new (StringComparer.OrdinalIgnoreCase);
    public bool Flags { get; set; }
    public string Description {get; set;}

    internal void AddOption(params CodeEnumOption[] codeEnumOptions)
    {
        EnsureElementsAreChildren(codeEnumOptions);
        foreach (var option in codeEnumOptions)
            OptionsInternal.TryAdd(option.Name, option);
    }
    public ICollection<CodeEnumOption> Options => OptionsInternal.Values;
}
