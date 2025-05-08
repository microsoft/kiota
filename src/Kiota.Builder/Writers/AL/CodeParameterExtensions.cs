using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.AL;

internal static class CodeParameterExtensions
{
    public static ALVariable ToVariable(this CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return new ALVariable(parameter.Name, parameter.Type, parameter.DefaultValue, parameter.GetCustomProperty("value"), parameter.GetPragmas());
    }
    public static IEnumerable<ALVariable> ToVariables(this IEnumerable<CodeParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return parameters.Select(p1 => p1.ToVariable());
    }
}