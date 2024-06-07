using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeIntersectionTypeWriter(TypeScriptConventionService conventionService) : CodeComposedTypeBaseWriter<CodeIntersectionType, TypeScriptConventionService>(conventionService)
{
    public override string TypesDelimiter
    {
        get => "&";
    }
}
