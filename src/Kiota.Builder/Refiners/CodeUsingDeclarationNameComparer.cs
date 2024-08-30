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
        if (x is null || y is null) return object.Equals(x, y);
        return string.Equals(x.Name, y.Name, OrdinalIgnoreCase)
               && string.Equals(x.Declaration?.Name, y.Declaration?.Name, OrdinalIgnoreCase);
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Name, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.Declaration?.Name, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
