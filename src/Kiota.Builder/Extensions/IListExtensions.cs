using System.Collections.Generic;

namespace Kiota.Builder.Extensions;

public static class IListExtensions
{
    /// <summary>
    /// Returns the only element of this list when it has count of exactly <c>1</c>
    /// </summary>
    /// <typeparam name="T">The contained item type.</typeparam>
    /// <param name="items">The items.</param>
    /// <returns>The only element or null.</returns>
    internal static T? OnlyOneOrDefault<T>(this IList<T>? items) =>
        items is { Count: 1 } ? items[0] : default;

    /// <summary>
    /// Adds the provided <paramref name="values"/> to this list.
    /// </summary>
    /// <typeparam name="T">The contained item type.</typeparam>
    /// <param name="items">The items.</param>
    /// <param name="values">The values to add.</param>
    internal static void AddRange<T>(this IList<T> items, IEnumerable<T> values)
    {
        foreach (var item in values)
        {
            items.Add(item);
        }
    }
}
