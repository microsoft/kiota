using System.Linq;
using Kiota.Builder.CodeDOM;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.Extensions;

public static class CodeComposedTypeBaseExtensions
{
    public static bool IsComposedOfPrimitives(this CodeComposedTypeBase composedType)
    {
        return composedType?.Types.All(x => IsPrimitiveType(GetTypescriptTypeString(x, composedType))) ?? false;
    }
}
