namespace kiota.Extension;

internal static class EnumerableExtensions
{
    public static IEnumerable<T>? ConcatNullable<T>(this IEnumerable<T>? left, IEnumerable<T>? right)
    {
        if (left is not null && right is not null) return left.Concat(right);
        // At this point, either left is null, right is null or both are null
        return left ?? right;
    }

    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? [];
    }
}
