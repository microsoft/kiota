using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Refiners;
public class CodeUsingComparer : IEqualityComparer<CodeUsing>
{
    private readonly bool _compareOnDeclaration;
    public CodeUsingComparer(bool compareOnDeclaration)
    {
        _compareOnDeclaration = compareOnDeclaration;
    }
    public bool Equals(CodeUsing? x, CodeUsing? y)
    {
        return (!_compareOnDeclaration || x?.Declaration == y?.Declaration) && string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        var hash = new HashCode();
        if (_compareOnDeclaration) hash.Add(obj?.Declaration);
        hash.Add(obj?.Name, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
