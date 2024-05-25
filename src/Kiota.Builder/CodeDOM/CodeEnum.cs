using System.Collections.Concurrent;
using System.Collections.Generic;

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

    private readonly ConcurrentQueue<CodeEnumOption> OptionsInternal = new(); // this structure is used to maintain the order of the options

    public void AddOption(params CodeEnumOption[] codeEnumOptions)
    {
        if (codeEnumOptions is null) return;

        var optionsToAdd = new HashSet<CodeEnumOption>(codeEnumOptions);
        foreach (var option in optionsToAdd)
        {
            if (option != null)
            {
                AddRange(option);
                OptionsInternal.Enqueue(option);
            }
        }
    }

    public IEnumerable<CodeEnumOption> Options
    {
        get
        {
            var optionsSet = new HashSet<CodeEnumOption>();
            foreach (var element in InnerChildElements.Values)
            {
                if (element is CodeEnumOption enumOption)
                {
                    optionsSet.Add(enumOption);
                }
            }

            foreach (var option in OptionsInternal)
            {
                if (optionsSet.Contains(option))
                {
                    yield return option;
                }
            }
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
