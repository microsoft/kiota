namespace Kiota.Builder.Writers.Go;
public static class StringExtensions
{
    public static string TrimCollectionAndPointerSymbols(this string s) =>
    string.IsNullOrEmpty(s) ? s : s.TrimStart('[').TrimStart(']').TrimStart('*');

    public static string RemoveSuffix(this string s, string suffix) =>
        s.EndsWith(suffix) ? s.Substring(0, s.Length - suffix.Length) : s;
}
