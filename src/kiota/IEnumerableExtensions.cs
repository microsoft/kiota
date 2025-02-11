namespace kiota;

public static class IEnumerableExtensions {
    public static IEnumerable<T> ConcatNullable<T>(this IEnumerable<T>? left, IEnumerable<T>? right)
    {
        if (left is not null && right is not null) return left.Concat(right);
        if (right is null) return left!;
        if (left is null) return right;
        return [];
    }

    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? [];
    }
}
