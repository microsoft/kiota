using System;
using System.Collections.Generic;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Extensions;
public static class CodeParametersEnumerableExtensions
{
    public static CodeParameter? OfKind(this IEnumerable<CodeParameter> parameters, CodeParameterKind kind)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var parameter in parameters)
        {
            if (parameter != null && parameter.IsOfKind(kind))
            {
                return parameter;
            }
        }

        return null;
    }
}
