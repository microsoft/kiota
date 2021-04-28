using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder {
    public class CodeElementOrderComparer : IComparer<CodeElement>
    {
        public int Compare(CodeElement x, CodeElement y)
        {
            var isXNull = x == null;
            var isYNull = y == null;
            if(isXNull && isYNull) return 0;
            else if (isXNull) return -1;
            else if(isYNull) return 1;


            return GetTypeFactor(x).CompareTo(GetTypeFactor(y)) * typeWeight + 
                (x.Name?.CompareTo(y.Name) ?? 0) * nameWeight + 
                GetParametersFactor(x).CompareTo(GetParametersFactor(y)) * parametersWeight;
        }
        private static readonly int nameWeight = 10;
        private static readonly int typeWeight = 100;
        private int GetTypeFactor(CodeElement element) {
            switch(element) {
                case CodeUsing:
                    return 1;
                case CodeClass.Declaration:
                    return 2;
                case CodeProperty:
                    return 3;
                case CodeIndexer:
                    return 4;
                case CodeMethod:
                    return 5;
                case CodeClass:
                    return 6;
                case CodeClass.End:
                    return 7;
                default:
                    return 0;
            }
        }
        private static readonly int parametersWeight = 1;
        private int GetParametersFactor(CodeElement element) {
            if(element is CodeMethod method && (method.Parameters?.Any() ?? false))
                return method.Parameters.Count;
            return 0;
        }
    }
}
