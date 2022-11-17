using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;
internal class CodeTypeComparer : IComparer<CodeTypeBase>
{
    private readonly bool OrderByDesc;
    public CodeTypeComparer(bool orderByDesc = false)
    {
        OrderByDesc = orderByDesc;
    }
    public int GetHashCode([DisallowNull] CodeTypeBase obj)
    {
        if(obj is CodeType type && !OrderByDesc)
            return (type.TypeDefinition, type.IsCollection) switch {
                (CodeClass or CodeInterface or CodeEnum, false) => 7,
                (null, false) => 11,
                (CodeClass or CodeInterface or CodeEnum, true) => 13,
                (_, _) => 17,
            };
        if(obj is CodeType type2 && OrderByDesc)
            return (type2.TypeDefinition, type2.IsCollection) switch {
                (null, false) => 7,
                (CodeClass or CodeInterface or CodeEnum, true) => 13,
                (CodeClass or CodeInterface or CodeEnum, false) => 11,
                (_, _) => 17,
            };
        return 23;
    }
    public int Compare(CodeTypeBase x, CodeTypeBase y)
    {
        if(x == null && y == null) return 0;
        return GetHashCode(x).CompareTo(GetHashCode(y));
    }
}
