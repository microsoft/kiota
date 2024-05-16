using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeIntersectionTypeWriter : CodeComposedTypeBaseWriter<CodeIntersectionType, TypeScriptConventionService>
{
    public CodeIntersectionTypeWriter(TypeScriptConventionService conventionService) : base(conventionService)
    {
    }

    public override string TypesDelimiter
    {
        get => " & ";
    }
}