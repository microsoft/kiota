using System.Collections.Generic;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

internal class CodePropertyTypeComparer : IComparer<CodeProperty>
{
    private readonly CodeTypeComparer TypeComparer;
    public CodePropertyTypeComparer(bool orderByDesc = false)
    {
        TypeComparer = new CodeTypeComparer(orderByDesc);
    }
    public int Compare(CodeProperty? x, CodeProperty? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => TypeComparer.Compare(x?.Type, y?.Type),
        };
    }
}
