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
    public int Compare(CodeProperty x, CodeProperty y)
    {
        if (x == null && y == null) return 0;
        return TypeComparer.Compare(x?.Type, y?.Type);
    }
}
