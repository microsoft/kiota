using System.Diagnostics;

namespace kiota.Extension;

internal static class TagListExtensions
{
    public static TagList AddAll(this TagList tagList, IEnumerable<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags) tagList.Add(tag);
        return tagList;
    }
}
