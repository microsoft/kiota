using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Kiota.Builder.CodeDOM;

using static System.StringComparison;

namespace Kiota.Builder.Refiners;
public class CodeUsingDeclarationNameComparer : IEqualityComparer<CodeUsing>
{
    public bool Equals(CodeUsing? x, CodeUsing? y) =>
        x == null && y == null ||
        x != null &&
        y != null &&
        GetHashCode(x) == GetHashCode(y);
    public int GetHashCode([DisallowNull] CodeUsing obj) => 
        (string.IsNullOrEmpty(obj?.Name) ? 0 : obj.Name.GetHashCode(OrdinalIgnoreCase)) * 7 +
        (string.IsNullOrEmpty(obj?.Declaration?.Name) ? 0 : obj.Declaration.Name.GetHashCode(OrdinalIgnoreCase));
}
