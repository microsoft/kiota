using System.CommandLine;

namespace kiota.Extension;

internal static class PropertyFilterExtensions
{
    internal static List<string> ExpandPropertyGroup(string group)
    {
        group = RemoveEnclosingQuotes(group);
        var separator = group.IndexOf('#', StringComparison.Ordinal);
        if (separator <= 0 || separator == group.Length - 1)
            throw new FormatException($"Invalid property selector '{group}'. Expected component#property1,property2.");
        var component = group[..separator];
        var selectors = new List<string>();
        foreach (var property in group[(separator + 1)..].Split(','))
        {
            var name = property.Trim();
            if (name.Length == 0)
                throw new FormatException($"Invalid property selector '{group}'. Property names cannot be empty.");
            selectors.Add($"{component}#{name}");
        }
        return selectors;
    }

    internal static HashSet<string> GetIncludedProperties(this ParseResult result, Option<List<string>> option) =>
        (result.GetValue(option) ?? [])
            .SelectMany(ExpandPropertyGroup)
            .ToHashSet(StringComparer.Ordinal);

    internal static HashSet<string> GetExcludedProperties(this ParseResult result, Option<List<string>> option) =>
        (result.GetValue(option) ?? [])
            .Select(RemoveEnclosingQuotes)
            .ToHashSet(StringComparer.Ordinal);

    private static string RemoveEnclosingQuotes(string selector) =>
        selector.Length >= 2 && selector[0] == selector[^1] && selector[0] is '"' or '\'' ? selector[1..^1] : selector;
}
