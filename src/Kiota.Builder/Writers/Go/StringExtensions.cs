namespace Kiota.Builder.Writers.Go {
    public static class StringExtensions {
        public static string TrimCollectionAndPointerSymbols(this string s) =>
        string.IsNullOrEmpty(s) ? s : s.TrimStart('[').TrimStart(']').TrimStart('*');
    }
}
