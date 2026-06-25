using System;

namespace Kiota.Builder.Writers.Php;

public static class PhpStringExtensions
{
    /// <summary>
    /// Sanitizes a string for safe inclusion inside a PHP double-quoted string literal.
    /// In addition to the standard escaping performed by <see cref="StringExtensions.SanitizeDoubleQuote"/>,
    /// this escapes the '$' character so that PHP does not interpolate variables or expressions
    /// (for example "$var", "${...}" or "{$...}") embedded in attacker-controlled input such as
    /// OpenAPI descriptions, enum values or content types. Failing to escape '$' allows arbitrary
    /// PHP code injection when the generated client is compiled or executed.
    /// </summary>
    /// <param name="original">The string to sanitize.</param>
    /// <returns>The sanitized string, safe to embed within a PHP double-quoted literal.</returns>
    public static string EscapePhpDoubleQuote(this string original)
        => string.IsNullOrEmpty(original)
            ? original
            : original.SanitizeDoubleQuote().Replace("$", "\\$", StringComparison.Ordinal);
}
