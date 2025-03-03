using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Crystal;

public static class CrystalExtensions
{
    public static bool IsAbstract(this CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.IsOfKind(CodeMethodKind.Factory);
    }
}
