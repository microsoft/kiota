using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Refiners;

public class CodeUsingComparer : IEqualityComparer<CodeUsing>
{
    private readonly bool _compareOnDeclaration;
    private readonly StringComparer _stringComparer;
    public CodeUsingComparer(bool compareOnDeclaration, StringComparer? stringComparer = null)
    {
        _compareOnDeclaration = compareOnDeclaration;
        _stringComparer = stringComparer ?? StringComparer.Ordinal;
    }
    public bool Equals(CodeUsing? x, CodeUsing? y)
    {
        return (!_compareOnDeclaration || x?.Declaration == y?.Declaration) && _stringComparer.Equals(x?.Name, y?.Name);
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        var hash = new HashCode();
        if (_compareOnDeclaration) hash.Add(obj?.Declaration);
        hash.Add(obj?.Name, _stringComparer);
        return hash.ToHashCode();
    }
}
