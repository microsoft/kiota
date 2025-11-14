using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.OrderComparers;

public class CodeElementOrderComparerWithExternalMethods : CodeElementOrderComparer
{
    protected override int GetTypeFactor(CodeElement element)
    {
        return element switch
        {
            CodeUsing => 1,
            ClassDeclaration => 2,
            CodeProperty => 3,
            InterfaceDeclaration => 4,
            CodeMethod method when method.Parent is CodeInterface => 5, //methods are declared inside of interfaces
            BlockEnd => 6,
            CodeClass => 7,
            CodeInterface => 8,
            CodeIndexer => 9,
            CodeMethod => 10,
            _ => 0,
        };
    }
}
