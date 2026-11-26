using System;
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
    public bool IsNumeric
    {
        get; set;
    }
    public string? Format
    {
        get; set;
    }
    private bool? _isFractional;
    public bool IsFractional
    {
        get => _isFractional ?? (IsNumeric && (
            string.Equals(Format, "float", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Format, "double", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Format, "decimal", StringComparison.OrdinalIgnoreCase) ||
            Options.Any(static x => x.WireName.Contains('.', StringComparison.Ordinal) ||
                                    x.WireName.Contains('e', StringComparison.OrdinalIgnoreCase) ||
                                    (!long.TryParse(x.WireName, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _) &&
                                     double.TryParse(x.WireName, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)))
        ));
        set => _isFractional = value;
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
