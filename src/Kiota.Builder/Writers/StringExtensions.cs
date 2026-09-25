using System;
using System.Globalization;
using System.Text;

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
        => SanitizeForQuotedLiteral(original, '"');

    /// <summary>
    /// Sanitizes a string for a C# double-quoted literal.
    /// </summary>
    /// <param name="original">The string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string SanitizeCSharpDoubleQuote(this string original)
        => SanitizeForQuotedLiteral(original, '"', true);

    /// <summary>
    /// Sanitize a string for direct writing.
    /// </summary>
    /// <param name="original">The string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string SanitizeSingleQuote(this string original)
        => SanitizeForQuotedLiteral(original, '\'');

    /// <summary>
    /// Sanitizes the inner content of a quoted string literal while preserving the surrounding quotes.
    /// </summary>
    /// <param name="original">A quoted string literal.</param>
    /// <returns>The sanitized literal if quoted, otherwise the original value.</returns>
    public static string SanitizeQuotedStringLiteral(this string original)
        => SanitizeQuotedStringLiteral(original, false);

    /// <summary>
    /// Sanitizes the inner content of a quoted C# string literal while preserving the surrounding quotes.
    /// </summary>
    /// <param name="original">A quoted string literal.</param>
    /// <returns>The sanitized literal if quoted, otherwise the original value.</returns>
    public static string SanitizeCSharpQuotedStringLiteral(this string original)
        => SanitizeQuotedStringLiteral(original, true);

    private static string SanitizeQuotedStringLiteral(string original, bool escapeUnicodeLineSeparators)
    {
        if (string.IsNullOrEmpty(original) || original.Length < 2) return original;
        return (original[0], original[^1]) switch
        {
            ('"', '"') => $"\"{SanitizeForQuotedLiteral(original[1..^1], '"', escapeUnicodeLineSeparators)}\"",
            ('\'', '\'') => $"'{SanitizeForQuotedLiteral(original[1..^1], '\'', escapeUnicodeLineSeparators)}'",
            _ => original,
        };
    }

    private static string SanitizeForQuotedLiteral(string original, char quote, bool escapeUnicodeLineSeparators = false)
    {
        if (string.IsNullOrEmpty(original)) return original;
        var builder = new StringBuilder(original.Length);
        foreach (var character in original)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                case '\u2028' when escapeUnicodeLineSeparators:
                    builder.Append("\\u2028");
                    break;
                case '\u2029' when escapeUnicodeLineSeparators:
                    builder.Append("\\u2029");
                    break;
                case '"' when quote == '"':
                    builder.Append("\\\"");
                    break;
                case '\'' when quote == '\'':
                    builder.Append("\\'");
                    break;
                default:
                    if (char.IsControl(character))
                        builder.Append(@"\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(character);
                    break;
            }
        }
        return builder.ToString();
    }
}
