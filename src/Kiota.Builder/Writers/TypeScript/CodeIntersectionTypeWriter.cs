using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeIntersectionTypeWriter(TypeScriptConventionService conventionService) : CodeComposedTypeBaseWriter<CodeIntersectionType>(conventionService)
{
    public override string TypesDelimiter
    {
        get => "&";
    }
}
