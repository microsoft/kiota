using System;

namespace Kiota.Builder.Writers.Go;
public static class StringExtensions
{
    public static string TrimCollectionAndPointerSymbols(this string s) =>
    string.IsNullOrEmpty(s) ? s : s.TrimStart('[').TrimStart(']').TrimStart('*');

    public static string TrimSuffix(this string s, string suffix) =>
        !string.IsNullOrEmpty(s) && suffix != null && s.EndsWith(suffix, StringComparison.Ordinal) ? s[..^suffix.Length] : s;
}
