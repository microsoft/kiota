using System;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Configuration;

internal readonly record struct ModelPropertySelector(string Component, string Property)
{
    internal static ModelPropertySelector Parse(string selector)
    {
        var separator = selector?.IndexOf('#', StringComparison.Ordinal) ?? -1;
        if (separator <= 0 || separator == selector!.Length - 1)
            throw new InvalidOperationException($"Invalid property selector '{selector}'. Expected component#property.");
        return new(selector[..separator], selector[(separator + 1)..]);
    }

    public override string ToString() => $"{Component}#{Property}";

    internal bool HasWildcard => Component.Contains('*', StringComparison.Ordinal) || Property.Contains('*', StringComparison.Ordinal);

    internal static Func<string, bool> CreateNameMatcher(string pattern)
    {
        if (!pattern.Contains('*', StringComparison.Ordinal))
            return name => string.Equals(name, pattern, StringComparison.Ordinal);
        return new Regex($"\\A{Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal)}\\z",
            RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking).IsMatch;
    }
}
