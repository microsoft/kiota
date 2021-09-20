using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static System.StringComparison;

namespace Kiota.Builder.Refiners {
    public class CodeUsingDeclarationNameComparer : IEqualityComparer<CodeUsing>
    {
        public bool Equals(CodeUsing x, CodeUsing y) =>
            x == null && y == null ||
            x != null &&
            y != null &&
            GetHashCode(x) == GetHashCode(y);
        public int GetHashCode([DisallowNull] CodeUsing obj) => 
            (obj?.Name.GetHashCode(OrdinalIgnoreCase) ?? 0) * 7 +
            (obj?.Declaration?.Name.GetHashCode(OrdinalIgnoreCase) ?? 0);
    }
}
