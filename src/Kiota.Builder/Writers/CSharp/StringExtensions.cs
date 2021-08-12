namespace Kiota.Builder.Writers.CSharp {
    public static class StringExtensions {
        public static string StripArraySuffix(this string original) => string.IsNullOrEmpty(original) ? original : original.TrimEnd(']').TrimEnd('[');
    }
}
