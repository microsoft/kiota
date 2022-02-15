using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder {
    public class CodeElementOrderComparer : IComparer<CodeElement>
    {
        public int Compare(CodeElement x, CodeElement y)
        {
            return (x, y) switch
            {
                (null, null) => 0,
                (null, _) => -1,
                (_, null) => 1,
                _ => GetTypeFactor(x).CompareTo(GetTypeFactor(y)) * typeWeight +
                    (x.Name?.CompareTo(y.Name) ?? 0) * nameWeight +
                    GetMethodKindFactor(x).CompareTo(GetMethodKindFactor(y)) * methodKindWeight +
                    GetParametersFactor(x).CompareTo(GetParametersFactor(y)) * parametersWeight,
            };
        }
        private static readonly int nameWeight = 100;
        private static readonly int typeWeight = 1000;
        protected virtual int GetTypeFactor(CodeElement element) {
            return element switch {
                CodeUsing => 1,
                CodeClass.Declaration => 2,
                CodeProperty => 3,
                CodeIndexer => 4,
                CodeMethod => 5,
                CodeClass => 6,
                CodeClass.End => 7,
                _ => 0,
            };
        }
        private static readonly int methodKindWeight = 10;
        protected static int GetMethodKindFactor(CodeElement element) {
            if (element is CodeMethod method)
                return method.Kind switch {
                    CodeMethodKind.ClientConstructor => 1,
                    CodeMethodKind.Constructor => 2,
                    CodeMethodKind.RawUrlConstructor => 3,
                    _ => 0,
                };
            else return 0;
        }
        private static readonly int parametersWeight = 1;
        private static int GetParametersFactor(CodeElement element) {
            if(element is CodeMethod method && (method.Parameters?.Any() ?? false))
                return method.Parameters.Count();
            return 0;
        }
    }
}
