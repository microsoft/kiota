using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Kiota.Builder.CodeDOM;

using static System.StringComparison;

namespace Kiota.Builder.Refiners;
public class CodeUsingDeclarationNameComparer : IEqualityComparer<CodeUsing>
{
    public bool Equals(CodeUsing? x, CodeUsing? y)
    {
        if (x is null || y is null) return x?.Equals(y) == true;
        return x.Name.Equals(y.Name, OrdinalIgnoreCase)
               && x.Declaration?.Name.Equals(y.Declaration?.Name, OrdinalIgnoreCase) == true;
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        var hash = new HashCode();
        hash.Add(obj?.Name);
        hash.Add(obj?.Declaration?.Name);
        return hash.ToHashCode();
    }
}
