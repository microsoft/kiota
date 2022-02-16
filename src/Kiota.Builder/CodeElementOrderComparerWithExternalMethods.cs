namespace Kiota.Builder {
    public class CodeElementOrderComparerWithExternalMethods : CodeElementOrderComparer {
        protected override int GetTypeFactor(CodeElement element) {
            return element switch {
                CodeUsing => 1,
                CodeClass.ClassDeclaration => 2,
                CodeProperty => 3,
                CodeClass.ClassEnd => 4,
                CodeClass => 5,
                CodeIndexer => 6,
                CodeMethod => 7,
                _ => 0,
            };
        }
    }
}
