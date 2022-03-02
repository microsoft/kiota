namespace Kiota.Builder {
    public class CodeElementOrderComparerWithExternalMethods : CodeElementOrderComparer {
        protected override int GetTypeFactor(CodeElement element) {
            return element switch {
                CodeUsing => 1,
                ClassDeclaration => 2,
                CodeProperty => 3,
                InterfaceDeclaration => 4,
                CodeMethod when element.Parent is CodeInterface => 5, //methods are declared inside of interfaces
                BlockEnd => 6,
                CodeClass => 7,
                CodeIndexer => 8,
                CodeMethod => 9,
                _ => 0,
            };
        }
    }
}
