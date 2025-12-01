using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.OrderComparers;

public class CodeElementOrderComparerPython : CodeElementOrderComparer
{
    protected override int GetTypeFactor(CodeElement element)
    {
        return element switch
        {
            CodeUsing => 1,
            ClassDeclaration => 2,
            InterfaceDeclaration => 3,
            CodeMethod => 4,
            CodeIndexer => 5,
            CodeProperty => 6,
            CodeClass => 7,
            BlockEnd => 8,
            _ => 0,
        };
    }
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
