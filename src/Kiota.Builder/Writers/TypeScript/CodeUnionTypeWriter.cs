using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeUnionTypeWriter : CodeComposedTypeBaseWriter<CodeUnionType, TypeScriptConventionService>
{
    public CodeUnionTypeWriter(TypeScriptConventionService conventionService) : base(conventionService)
    {
    }

    public override string TypesDelimiter
    {
        get => " | ";
    }
}
