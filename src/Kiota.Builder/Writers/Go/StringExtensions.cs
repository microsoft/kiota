using System;
using System.Linq;

namespace Kiota.Builder.Writers.Go;
public static class StringExtensions
{
    public static string TrimCollectionAndPointerSymbols(this string s) =>
        string.IsNullOrEmpty(s) ? s : s.TrimStart('[').TrimStart(']').TrimStart('*');

    public static string TrimPackageReference(this string s) =>
        !string.IsNullOrEmpty(s) && s.Contains('.', StringComparison.InvariantCultureIgnoreCase) ? s.Split('.').Last() : s;

    public static string TrimSuffix(this string s, string suffix) =>
        !string.IsNullOrEmpty(s) && suffix != null && s.EndsWith(suffix, StringComparison.Ordinal) ? s[..^suffix.Length] : s;
}
