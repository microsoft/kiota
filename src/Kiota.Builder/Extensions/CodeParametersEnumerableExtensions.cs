using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Extensions;
public static class CodeParametersEnumerableExtensions {
    public static CodeParameter OfKind(this IEnumerable<CodeParameter> parameters, CodeParameterKind kind) {
        return parameters.FirstOrDefault(x => x.IsOfKind(kind));
    }
    public static bool OfKind(this IEnumerable<CodeParameter> parameters, CodeParameterKind kind, out CodeParameter parameter) {
        parameter = parameters.OfKind(kind);
        return parameter != null;
    }
}
