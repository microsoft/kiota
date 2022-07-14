using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kiota.Builder.Writers.CSharp;
internal class CodeTypeComparer : IComparer<CodeTypeBase>
{
    private readonly bool OrderByDesc;
    public int DescFactor { get => OrderByDesc ? -1 : 1; }
    public CodeTypeComparer(bool orderByDesc = false)
    {
        OrderByDesc = orderByDesc;
    }
    public int GetHashCode([DisallowNull] CodeTypeBase obj)
    {
        if(obj is CodeType type)
            return (type.TypeDefinition, type.IsCollection) switch {
                (CodeClass or CodeInterface or CodeEnum, false) => 7 * DescFactor,
                (null, false) => 11 * DescFactor,
                (CodeClass or CodeInterface or CodeEnum, true) => 13 * DescFactor,
                (_, _) => 17 * DescFactor,
            };
        else 
            return 23 * DescFactor;
    }
    public int Compare(CodeTypeBase x, CodeTypeBase y)
    {
        if(x == null && y == null) return 0;
        return GetHashCode(x).CompareTo(GetHashCode(y));
    }
}
