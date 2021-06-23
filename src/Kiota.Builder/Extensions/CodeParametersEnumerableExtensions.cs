using System.Linq;
using System.Collections.Generic;

namespace Kiota.Builder {
    public static class CodeParametersEnumerableExtensions {
        public static CodeParameter OfKind(this IEnumerable<CodeParameter> parameters, CodeParameterKind kind) {
            return parameters.FirstOrDefault(x => x.IsOfKind(kind));
        }
    }
}
