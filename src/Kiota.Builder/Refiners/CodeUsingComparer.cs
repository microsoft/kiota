using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kiota.Builder {
    public class CodeUsingComparer : IEqualityComparer<CodeUsing>
    {
        private readonly bool _compareOnDeclaration;
        public CodeUsingComparer(bool compareOnDeclaration)
        {
            _compareOnDeclaration = compareOnDeclaration;
        }
        public bool Equals(CodeUsing x, CodeUsing y)
        {
            return (!_compareOnDeclaration || x?.Declaration == y?.Declaration) && (x?.Name?.Equals(y?.Name) ?? false);
        }

        public int GetHashCode([DisallowNull] CodeUsing obj)
        {
            return ((_compareOnDeclaration ?  obj?.Declaration?.GetHashCode() * 7 : 0) + 
                        obj?.Name?.GetHashCode()) ?? 0;
        }
    }
}
