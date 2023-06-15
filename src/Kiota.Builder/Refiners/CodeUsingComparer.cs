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
        return (!_compareOnDeclaration || x?.Declaration == y?.Declaration) && (x?.Name?.Equals(y?.Name, StringComparison.Ordinal) ?? false);
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        return (_compareOnDeclaration ? (obj?.Declaration == null ? 0 : obj.Declaration.GetHashCode()) * 7 : 0) +
                    (string.IsNullOrEmpty(obj?.Name) ? 0 : obj.Name.GetHashCode(StringComparison.Ordinal));
    }
}
