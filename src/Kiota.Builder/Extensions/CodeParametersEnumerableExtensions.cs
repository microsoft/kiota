using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Extensions;

public static class CodeParametersEnumerableExtensions
{
    public static CodeParameter? OfKind(this IEnumerable<CodeParameter> parameters, CodeParameterKind kind)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return parameters.FirstOrDefault(x => x != null && x.IsOfKind(kind));
    }
}
