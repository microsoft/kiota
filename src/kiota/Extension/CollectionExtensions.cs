using System.Diagnostics;

namespace kiota.Extension;

internal static class CollectionExtensions
{
    public static TagList AddAll(this TagList tagList, IEnumerable<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags) tagList.Add(tag);
        return tagList;
    }

    public static T[] OrEmpty<T>(this T[]? source)
    {
        return source ?? [];
    }
    public static List<T> OrEmpty<T>(this List<T>? source)
    {
        return source ?? [];
    }
}
