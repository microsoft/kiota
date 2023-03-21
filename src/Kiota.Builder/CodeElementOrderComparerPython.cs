using Kiota.Builder.CodeDOM;

namespace Kiota.Builder;
public class CodeElementOrderComparerPython : CodeElementOrderComparer
{
    protected override int methodKindWeight { get; } = 200;
    protected override int GetMethodKindFactor(CodeElement element)
    {
        if (element is CodeMethod method)
            return method.Kind switch
            {
                CodeMethodKind.ClientConstructor => 1,
                CodeMethodKind.Constructor => 0,
                CodeMethodKind.RawUrlConstructor => 3,
                _ => 2,
            };
        return 0;
    }
}
