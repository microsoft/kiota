using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeIntersectionTypeWriter(TypeScriptConventionService conventionService) : CodeComposedTypeBaseWriter<CodeIntersectionType>(conventionService)
{
    // The `CodeIntersectionType` will utilize the same union symbol `|`, but the methods for serialization and deserialization
    // will differ slightly. This is because the `CodeIntersectionType` for `Foo` and `Bar` can encompass both `Foo` and `Bar`
    // simultaneously, whereas the `CodeUnion` can only include either `Foo` or `Bar`, but not both at the same time.

    public override string TypesDelimiter
    {
        get => "|";
    }
}
