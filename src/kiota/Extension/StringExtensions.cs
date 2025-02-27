namespace kiota.Extension;

internal static class StringExtensions
{
    public static string OrEmpty(this string? source)
    {
        // Investigate if using spans instead of strings helps perf. i.e. source?.AsSpan() ?? ReadOnlySpan<char>.Empty
        return source ?? string.Empty;
    }
}
