﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;
#pragma warning disable CA1711
public class CodeEnum : CodeBlock<BlockDeclaration, BlockEnd>, IDocumentedElement, ITypeDefinition, IDeprecableElement, IAccessibleElement
{
#pragma warning restore CA2227
    public AccessModifier Access { get; set; } = AccessModifier.Public;
    public bool Flags
    {
        get; set;
    }

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
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
    public CodeConstant? CodeEnumObject
    {
        get; set;
    }
}
