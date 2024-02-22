using System;

namespace Kiota.Builder.Writers;

public static class StringExtensions
{
    public static string StripArraySuffix(this string original) =>
        string.IsNullOrEmpty(original) ? original : original.TrimEnd(']').TrimEnd('[');

    /// <summary>
    /// Sanitize a string for direct writing.
    /// </summary>
    /// <param name="original">The string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string SanitizeDoubleQuote(this string original)
        => string.IsNullOrEmpty(original)
            ? original
            : original.Replace("\"", "\\\"", StringComparison.Ordinal);

    /// <summary>
    /// Sanitize a string for direct writing.
    /// </summary>
    /// <param name="original">The string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string SanitizeSingleQuote(this string original)
        => string.IsNullOrEmpty(original)
            ? original
            : original.Replace("'", "\\'", StringComparison.Ordinal);
}
