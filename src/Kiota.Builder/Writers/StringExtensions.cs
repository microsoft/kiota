using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
    {
        if (string.IsNullOrEmpty(original) || original.Length < 2) return original;
        return (original[0], original[^1]) switch
        {
            ('"', '"') => $"\"{original[1..^1].SanitizeDoubleQuote()}\"",
            ('\'', '\'') => $"'{original[1..^1].SanitizeSingleQuote()}'",
            _ => original,
        };
    }

    private static string SanitizeForQuotedLiteral(string original, char quote)
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

    /// <summary>
    /// The argument is a DateTime value in RFC3339 format with offset: check whether a TimeZoneOffset is specified (
    /// string ends with "Z" or "+00:00" or "-00:00")
    /// or it is a local time.
    /// </summary>
    /// <param name="dateTime">A datetime string, might be null/empty</param>
    /// <returns>true if a timezone offset is found. false otherwise</returns>
    public static bool IsDateTimeWithOffset(this string dateTime)
    {
        if (string.IsNullOrEmpty(dateTime)) return false;
        return dateTime.EndsWith('Z') || dateTime.EndsWith('z') || Regex.IsMatch(dateTime, "[+-]\\d{2}:\\d{2}$");
    }
}
