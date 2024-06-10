using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeUnionTypeWriter(TypeScriptConventionService conventionService) : CodeComposedTypeBaseWriter<CodeUnionType, TypeScriptConventionService>(conventionService)
{
    public override string TypesDelimiter
    {
        get => "|";
    }
}
