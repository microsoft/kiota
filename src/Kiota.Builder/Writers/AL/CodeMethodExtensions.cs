using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.AL;

internal static class CodeMethodExtensions
{
    private static ALConventionService ConventionService { get; } = new();
    private static ALReservedNamesProvider ReservedNamesProvider { get; } = new();
    public static string GetSingularName(this CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return GetSingularName(parameter.Name);
    }
    public static string GetSingularName(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var newInput = input.EndsWith('s'.ToString(), StringComparison.OrdinalIgnoreCase) ? input[..^1] : input;
        if (newInput.Equals(input, StringComparison.OrdinalIgnoreCase))
            newInput = newInput.Remove(newInput.Length - 1);
        if (ReservedNamesProvider.ReservedNames.Contains(newInput, StringComparer.OrdinalIgnoreCase))
            newInput += "_";
        return newInput;
    }
    public static bool HasVariables(this CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.Parameters.Any(p1 => p1.IsLocalVariable());
    }
    public static IEnumerable<CodeParameter> Variables(this CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.Parameters.Where(p1 => p1.IsLocalVariable());
    }
    public static IEnumerable<CodeParameter> OrderedParameters(this CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.Parameters.Where(p1 => !p1.IsLocalVariable()).OrderBy(x => x.DefaultValue);
    }
    public static bool IsPropertyMethod(this CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.Kind == CodeMethodKind.Custom && method.GetCustomProperty("source").Contains("from property", StringComparison.OrdinalIgnoreCase);
    }
    public static ALVariable ToVariable(this CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return new ALVariable(method.Name, method.ReturnType, "", "", method.GetPragmas());
    }
    public static IEnumerable<ALVariable> ToVariables(this IEnumerable<CodeMethod> methods)
    {
        ArgumentNullException.ThrowIfNull(methods);
        return methods.Select(p1 => p1.ToVariable());
    }
}
