using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;
#pragma warning disable CA1711
public class CodeEnum : CodeBlock<BlockDeclaration, BlockEnd>, IDocumentedElement, ITypeDefinition, IDeprecableElement
{
#pragma warning restore CA2227
    public bool Flags
    {
        get; set;
    }
    [JsonIgnore]
    public CodeDocumentation Documentation { get; set; } = new();
    private readonly ConcurrentQueue<CodeEnumOption> OptionsInternal = new(); // this structure is used to maintain the order of the options

    public void AddOption(params CodeEnumOption[] codeEnumOptions)
    {
        if (codeEnumOptions is null) return;
        var result = AddRange(codeEnumOptions);
        foreach (var option in result.Distinct())
        {
            OptionsInternal.Enqueue(option);
        }
    }
    public IEnumerable<CodeEnumOption> Options
    {
        get
        {
            return OptionsInternal.Join(InnerChildElements.Values.OfType<CodeEnumOption>().ToHashSet(), static x => x, static y => y, static (x, y) => x);
            // maintaining order of the options is important for enums as they are often used with comparisons
        }
    }
    [JsonIgnore]
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
    public CodeConstant? CodeEnumObject
    {
        get; set;
    }
}
