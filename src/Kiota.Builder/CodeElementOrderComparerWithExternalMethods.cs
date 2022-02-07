namespace Kiota.Builder {
    public class CodeElementOrderComparerWithExternalMethods : CodeElementOrderComparer {
        protected override int GetTypeFactor(CodeElement element) {
            return element switch {
                CodeUsing => 1,
                CodeClass.Declaration => 2,
                CodeProperty => 3,
                CodeClass.End => 4,
                CodeClass => 5,
                CodeIndexer => 6,
                CodeMethod => 7,
                _ => 0,
            };
        }
    }
}
