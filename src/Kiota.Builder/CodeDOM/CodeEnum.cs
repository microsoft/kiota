using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Kiota.Builder.CodeDOM;
#pragma warning disable CA1711
public class CodeEnum : CodeBlock<BlockDeclaration, BlockEnd>, IDocumentedElement, ITypeDefinition
{
#pragma warning restore CA2227
    private readonly HashSet<string> optionsNames = new(StringComparer.OrdinalIgnoreCase); // this structure is used to check if an option name is unique   
    private readonly ConcurrentQueue<CodeEnumOption> OptionsInternal = new(); // this structure is used to maintain the order of the options
    public bool Flags
    {
        get; set;
    }
    public CodeDocumentation Documentation { get; set; } = new();

    public void AddOption(params CodeEnumOption[] codeEnumOptions)
    {
        if (codeEnumOptions is null) return;
        EnsureElementsAreChildren(codeEnumOptions);
        foreach (var option in codeEnumOptions)
        {
            optionsNames.Add(option.Name);
            OptionsInternal.Enqueue(option);
        }
    }
    public IEnumerable<CodeEnumOption> Options => OptionsInternal;
}
